using UnityEngine;
using Harmony12;
using UnityModManagerNet;
using Kingmaker.Blueprints;
using System.Reflection;
using System;
using System.Linq;
using Kingmaker.Visual.CharacterSystem;
using System.Collections.Generic;
using Kingmaker.Blueprints.Classes;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.View;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.ResourceLinks;
using static VisualAdjustments.Settings;
using Kingmaker.Items.Slots;
using Kingmaker.View.Equipment;
using Kingmaker.UI.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.Buffs;

namespace VisualAdjustments
{

    public class Main
    {
        public static UnityModManager.ModEntry.ModLogger logger;
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugLog(string msg)
        {
            if(logger != null) logger.Log(msg);
        }
        public static bool enabled;
        public static Settings settings;
        public static bool disableEquipmentClassPatch = false;
        public static String[] classes = new String[] {
            "Default",
            "Alchemist",
            "Barbarian",
            "Bard",
            "Cleric",
            "Druid",
            "Fighter",
            "Inquisitor",
            "Magus",
            "Monk",
            "Paladin",
            "Ranger",
            "Rogue",
            "Sorcerer",
            "Wizard"
        };
        static Dictionary<string, Settings.CharacterSettings> settingsLookup = new Dictionary<string, Settings.CharacterSettings>();
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
                var harmony = HarmonyInstance.Create(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                foreach(var characterSettings in settings.characterSettings)
                {
                    settingsLookup[characterSettings.characterName] = characterSettings;
                }
                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                logger = modEntry.Logger;
            }
            catch (Exception e){
                DebugLog(e.ToString() + "\n" + e.StackTrace);
                throw e;
            }
            return true;
        }
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
        // Called when the mod is turned to on/off.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            enabled = value;
            return true; // Permit or not.
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            try
            {
                if (!enabled) return;
                foreach (UnitEntityData unitEntityData in Game.Instance.Player.ControllableCharacters)
                {
                    Settings.CharacterSettings characterSettings;
                    if (settingsLookup.ContainsKey(unitEntityData.CharacterName))
                    {
                        characterSettings = settingsLookup[unitEntityData.CharacterName];
                    }
                    else
                    {
                        characterSettings = new Settings.CharacterSettings();
                        characterSettings.characterName = unitEntityData.CharacterName;
                        settings.characterSettings.Add(characterSettings);
                        settingsLookup[characterSettings.characterName] = characterSettings;
                    }
                    if (unitEntityData.Descriptor.IsPet)
                    {
                        GUILayout.Label(string.Format("{0}", unitEntityData.CharacterName), "box", GUILayout.Width(300f));
                        return;
                    }
                    GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                    GUILayout.Label(string.Format("{0}", unitEntityData.CharacterName), "box", GUILayout.Width(300f));
                    characterSettings.showClassSelection = GUILayout.Toggle(characterSettings.showClassSelection, "Select Outfit", GUILayout.ExpandWidth(false));
                    if (unitEntityData.Descriptor.Doll != null)
                    {
                        characterSettings.showDollSelection = GUILayout.Toggle(characterSettings.showDollSelection, "Select Doll", GUILayout.ExpandWidth(false));
                    }
                    else
                    {
                        characterSettings.showDollSelection = GUILayout.Toggle(characterSettings.showDollSelection, "Select Colors", GUILayout.ExpandWidth(false));
                    }
                    characterSettings.showEquipmentSelection = GUILayout.Toggle(characterSettings.showEquipmentSelection, "Show Equipment Selection", GUILayout.ExpandWidth(false));
                    characterSettings.showOverrideSelection = GUILayout.Toggle(characterSettings.showOverrideSelection, "Show Override Selection", GUILayout.ExpandWidth(false));
#if (DEBUG)
                    characterSettings.showInfo = GUILayout.Toggle(characterSettings.showInfo, "Show Info", GUILayout.ExpandWidth(false));
#endif
                    GUILayout.EndHorizontal();
                    if (characterSettings.showClassSelection) ChooseClassOutfit(characterSettings, unitEntityData);
                    if (unitEntityData.Descriptor.Doll != null && characterSettings.showDollSelection)
                    {
                        ChooseDoll(unitEntityData);
                    }
                    if (unitEntityData.Descriptor.Doll == null && characterSettings.showDollSelection)
                    {
                        ChooseCompanionColor(characterSettings, unitEntityData);
                    }
                    if (characterSettings.showEquipmentSelection) ChooseEquipment(unitEntityData, characterSettings);
                    if (characterSettings.showOverrideSelection) ChooseEquipmentOverride(unitEntityData, characterSettings);
#if (DEBUG)
                    if (characterSettings.showInfo) InfoManager.ShowInfo(unitEntityData);
#endif
                }
            } catch(Exception e)
            {
                DebugLog(e.ToString() + " " + e.StackTrace);
            }
        }
        static void ChooseClassOutfit(CharacterSettings characterSettings, UnitEntityData unitEntityData)
        {
            var normalColor = GUI.skin.button.normal.textColor;
            var focusedColor = GUI.skin.button.focused.textColor;
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            foreach (var _class in classes)
            {
                if(characterSettings.classOutfit == _class)
                {
                    GUI.skin.button.normal.textColor = Color.yellow;
                    GUI.skin.button.focused.textColor = Color.yellow;
                } else
                {
                    GUI.skin.button.normal.textColor = normalColor;
                    GUI.skin.button.focused.textColor = focusedColor;
                }
                if (GUILayout.Button(_class, Array.Empty<GUILayoutOption>()))
                {
                    characterSettings.classOutfit = _class;
                    RebuildCharacter(unitEntityData);
                    unitEntityData.View.UpdateClassEquipment();
                }
            }
            GUI.skin.button.normal.textColor = normalColor;
            GUI.skin.button.focused.textColor = focusedColor;
            GUILayout.EndHorizontal();
        }
        static void ChooseEquipment(UnitEntityData unitEntityData, bool currentValue, string display, Action<bool> onSelect)
        {
            bool newValue = GUILayout.Toggle(currentValue, display, GUILayout.ExpandWidth(false));
            if(newValue != currentValue)
            {
                onSelect(newValue);
                RebuildCharacter(unitEntityData);
                UpdateModel(unitEntityData.View);
            }
        }
        static void ChooseWeapon(UnitEntityData unitEntityData, bool currentValue, string display, Action<bool> onSelect)
        {
            bool newValue = GUILayout.Toggle(currentValue, display, GUILayout.ExpandWidth(false));
            if (newValue != currentValue)
            {
                onSelect(newValue);
                unitEntityData.View.HandsEquipment.HandleEquipmentSetChanged();
            }
        }
        static void ChooseEquipment(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            ChooseEquipment(unitEntityData, characterSettings.hideCap, "Hide Cap", (value) => characterSettings.hideCap = value);
            ChooseEquipment(unitEntityData, characterSettings.hideBackpack, "Hide Backpack", (value) => characterSettings.hideBackpack = value);
            ChooseEquipment(unitEntityData, characterSettings.hideCloak, "Hide All Cloaks", (value) => characterSettings.hideCloak = value);
            ChooseEquipment(unitEntityData, characterSettings.hideHelmet, "Hide Helmet", (value) => characterSettings.hideHelmet = value);
            ChooseEquipment(unitEntityData, characterSettings.hideEquipCloak, "Hide Equip Cloak", (value) => characterSettings.hideEquipCloak = value);
            ChooseEquipment(unitEntityData, characterSettings.hideArmor, "Hide Armor", (value) => characterSettings.hideArmor = value);
            ChooseEquipment(unitEntityData, characterSettings.hideBracers, "Hide Bracers", (value) => characterSettings.hideBracers = value);
            ChooseEquipment(unitEntityData, characterSettings.hideGloves, "Hide Gloves", (value) => characterSettings.hideGloves = value);
            ChooseEquipment(unitEntityData, characterSettings.hideBoots, "Hide Boots", (value) => characterSettings.hideBoots = value);
            ChooseWeapon(unitEntityData, characterSettings.hideWeapons, "Hide Inactive Weapons", (value) => characterSettings.hideWeapons = value);
        }
        static void ChooseEquipmentOverride(UnitEntityData unitEntityData, string name, SortedList<string, string> items, string currentItem, Action<string> setter)
        {
            var currentIndex = items.IndexOfKey(currentItem);
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label(name + " ", GUILayout.Width(300));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, -1, items.Count - 1, GUILayout.Width(300)), 0);
            var displayText = newIndex == -1 ? "None" : items.Values[newIndex];
            GUILayout.Label(" " + displayText, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if(currentIndex != newIndex)
            {
                setter(newIndex == -1 ? "" : items.Keys[newIndex]);
                RebuildCharacter(unitEntityData);
                UpdateModel(unitEntityData.View);
            }
        }
        static void ChooseEquipmentOverride(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            ChooseEquipmentOverride(unitEntityData, "Override Helm ", EquipmentManager.Helm, characterSettings.overrideHelm, (string id) => characterSettings.overrideHelm = id);
            ChooseEquipmentOverride(unitEntityData, "Override Cloak ", EquipmentManager.Cloak, characterSettings.overrideCloak, (string id) => characterSettings.overrideCloak = id);
            ChooseEquipmentOverride(unitEntityData, "Override Armor ", EquipmentManager.Armor, characterSettings.overrideArmor, (string id) => characterSettings.overrideArmor = id);
            ChooseEquipmentOverride(unitEntityData, "Override Bracers ", EquipmentManager.Bracers, characterSettings.overrideBracers, (string id) => characterSettings.overrideBracers = id);
            ChooseEquipmentOverride(unitEntityData, "Override Gloves ", EquipmentManager.Gloves, characterSettings.overrideGloves, (string id) => characterSettings.overrideGloves = id);
            ChooseEquipmentOverride(unitEntityData, "Override Boots ", EquipmentManager.Boots, characterSettings.overrideBoots, (string id) => characterSettings.overrideBoots = id);
            GUILayout.BeginHorizontal();
            ChooseEquipmentOverride(unitEntityData, "Override View ", EquipmentManager.Units, characterSettings.overrideView, 
                (string id) => {
                    characterSettings.overrideView = id;
                    ReplaceView(unitEntityData, id);
                    });
            if (GUILayout.Button("PrevView"))
            {
                var currentIndex = EquipmentManager.Units.IndexOfKey(characterSettings.overrideView);
                if (currentIndex == 0)
                {
                    characterSettings.overrideView = "";
                }
                else
                {
                    characterSettings.overrideView = EquipmentManager.Units.Keys[currentIndex - 1];
                }
                ReplaceView(unitEntityData, characterSettings.overrideView);
            }
            if (GUILayout.Button("NextView"))
            {
                var currentIndex = EquipmentManager.Units.IndexOfKey(characterSettings.overrideView);
                if (currentIndex == EquipmentManager.Units.Count) return;
                characterSettings.overrideView = EquipmentManager.Units.Keys[currentIndex + 1];
                ReplaceView(unitEntityData, characterSettings.overrideView);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Override Scale", GUILayout.Width(300));
            var currentScale = Traverse.Create(unitEntityData.View).Field("m_Scale").GetValue<float>();
            var originalScale = Traverse.Create(unitEntityData.View).Field("m_OriginalScale").GetValue<Vector3>();
            var newScale = GUILayout.HorizontalSlider(currentScale, 0.1f, 5, GUILayout.Width(300));
            Traverse.Create(unitEntityData.View).Field("m_Scale").SetValue(newScale);
            unitEntityData.View.transform.localScale = originalScale * newScale;
            var sizeDiff = Math.Log(1 / newScale, 0.66);
            var size = unitEntityData.Descriptor.OriginalSize + (int)Math.Round(sizeDiff, 0);
            
            GUILayout.Label($" Scale {newScale} sizeChange {sizeDiff} sizeCategory {size}", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
        static UnitEntityView GetView(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return ResourcesLibrary.TryGetResource<UnitEntityView>(id);
        }
        static void ReplaceView(UnitEntityData unit, string id)
        {
            var original = unit.View;
            if (original == null) return;
            UnitEntityView template = GetView(id);
            if(template == null) template = unit.Blueprint.Prefab.Load();
            var instance = UnityEngine.Object.Instantiate<UnitEntityView>(template).GetComponent<UnitEntityView>();
            instance.transform.SetParent(original.transform.parent);
            instance.transform.position = original.transform.position;
            instance.transform.rotation = original.transform.rotation;
            if (id != null && id != "") instance.DisableSizeScaling = true;
            instance.Blueprint = unit.Blueprint;
            unit.AttachToViewOnLoad(instance);
            //base.Owner.Unit.Commands.InterruptAll((UnitCommand cmd) => !(cmd is UnitMoveTo));
            SelectionManager selectionManager = Game.Instance.UI.SelectionManager;
            if (selectionManager != null)
            {
                selectionManager.ForceCreateMarks();
            }
            UnityEngine.Object.Destroy(original.gameObject);
        }

        static void ChooseEEL(UnitEntityData unitEntityData, DollState doll, EquipmentEntityLink[] links, EquipmentEntityLink currentLink, string name, Action<EquipmentEntityLink> setter)
        {
            if (links.Length == 0) return;
            var currentIndex = links.ToList().FindIndex((eel) => eel.AssetId == currentLink.AssetId);
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label(name + " ", GUILayout.Width(300));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, links.Length - 1, GUILayout.Width(300)), 0);
            GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (newIndex != currentIndex && newIndex < links.Length)
            {
                setter(links[newIndex]);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                RebuildCharacter(unitEntityData);
            }
        }
        static void ChooseEELRamp<T>(UnitEntityData unitEntityData, DollState doll, List<T> ramps, int currentIndex, string name, Action<int> setter)
        {
            if (ramps.Count == 0) return;
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label(name + " ", GUILayout.Width(300));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, ramps.Count - 1, GUILayout.Width(300)), 0);
            GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (newIndex != currentIndex && newIndex < ramps.Count)
            {
                setter(newIndex);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                RebuildCharacter(unitEntityData);
            }
        }
        static void ChoosePortrait(UnitEntityData unitEntityData)
        {
            var key = unitEntityData.Descriptor.UISettings.PortraitBlueprint?.name;
            var oldIndex = DollManager.Portrait.IndexOfKey(key != null ? key: "");
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label("Portrait:  ", GUILayout.Width(300));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(oldIndex, 0, DollManager.Portrait.Count, GUILayout.Width(300)), 0);
            var value = newIndex >= 0 && newIndex < DollManager.Portrait.Count ? DollManager.Portrait.Values[newIndex] : null;
            GUILayout.Label(" " + value, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (newIndex != oldIndex && value != null)
            {
                unitEntityData.Descriptor.UISettings.SetPortrait(value);
            }
        }
        static void ChooseAsks(UnitEntityData unitEntityData)
        {
            var oldIndex = DollManager.Asks.IndexOfKey(unitEntityData.Descriptor.CustomAsks.name);
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label("Asks:  ", GUILayout.Width(300));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(oldIndex, 0, DollManager.Asks.Count, GUILayout.Width(300)), 0);
            var value = (newIndex >= 0 && newIndex < DollManager.Asks.Count) ? DollManager.Asks.Values[newIndex] : null;
            GUILayout.Label(" " + value, GUILayout.ExpandWidth(false));
            /*if (GUILayout.Button("Preview", GUILayout.ExpandWidth(false)))
            {
                var barks = value?.GetComponent<UnitAsksComponent>();
                if (barks != null) barks.PlayPreview();
                else DebugLog("Missing Backs");
            }*/
            GUILayout.EndHorizontal();
            if (newIndex != oldIndex && value != null)
            {
                unitEntityData.Descriptor.CustomAsks = value;
            }
        }
        static void ChooseDoll(UnitEntityData unitEntityData)
        {
            var doll = DollManager.GetDoll(unitEntityData);
            var race = unitEntityData.Descriptor.Progression.Race;
            var gender = unitEntityData.Gender;
            CustomizationOptions customizationOptions = gender != Gender.Male ? race.FemaleOptions : race.MaleOptions;
            ChooseEEL(unitEntityData, doll, customizationOptions.Heads, doll.Head, "Face", (EquipmentEntityLink ee) => doll.SetHead(ee));
            ChooseEEL(unitEntityData, doll, customizationOptions.Hair, doll.Hair, "Hair", (EquipmentEntityLink ee) => doll.SetHair(ee));
            ChooseEEL(unitEntityData, doll, customizationOptions.Beards, doll.Beard, "Beards", (EquipmentEntityLink ee) => doll.SetBeard(ee));
            ChooseEELRamp(unitEntityData, doll, doll.GetHairRamps(), doll.HairRampIndex, "Hair Color", (int index) => doll.SetHairColor(index));
            ChooseEELRamp(unitEntityData, doll, doll.GetSkinRamps(), doll.SkinRampIndex, "Skin Color", (int index) => doll.SetSkinColor(index));
            ChooseEELRamp(unitEntityData, doll, doll.GetOutfitRampsPrimary(), doll.EquipmentRampIndex, "Primary Outfit Color", (int index) => doll.SetEquipColors(index, doll.EquipmentRampIndexSecondary));
            ChooseEELRamp(unitEntityData, doll, doll.GetOutfitRampsSecondary(), doll.EquipmentRampIndexSecondary, "Secondary Outfit Color", (int index) => doll.SetEquipColors(doll.EquipmentRampIndex, index));
            //ChooseEELRamp(unitEntityData, doll, (new int[] { 0, 1 }).ToList(), doll.LeftHanded ? 1 : 0, "Left Handed", (int value) => doll.SetLeftHanded(value > 0)); //TODO
            ChoosePortrait(unitEntityData);
            ChooseAsks(unitEntityData);
           
        }
        static void ChooseCompanionColor(CharacterSettings characterSettings, UnitEntityData unitEntityData)
        {
            GUILayout.Label("Note: Only applies to non-default outfits");
            {
                GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                GUILayout.Label("Primary Outfit Color ", GUILayout.Width(300));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(characterSettings.companionPrimary, 0, 35, GUILayout.Width(300)), 0);
                GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (newIndex != characterSettings.companionPrimary)
                {
                    characterSettings.companionPrimary = newIndex;
                    UpdateModel(unitEntityData.View);
                }
            }
            {
                GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                GUILayout.Label("Secondary Outfit Color ", GUILayout.Width(300));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(characterSettings.companionSecondary, 0, 35, GUILayout.Width(300)), 0);
                GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if(newIndex != characterSettings.companionSecondary)
                {
                    characterSettings.companionSecondary = newIndex;
                    UpdateModel(unitEntityData.View);
                }
            }
        }
        static int GetPrimaryColor(UnitEntityData unitEntityData)
        {
            if (unitEntityData.Descriptor.Doll == null) return -1;
            var doll = unitEntityData.Descriptor.Doll;
            foreach(var assetId in doll.EntitySecondaryRampIdices.Keys.ToList())
            {
                if (doll.EntityRampIdices.ContainsKey(assetId)) return doll.EntityRampIdices[assetId];
            }
            return -1;
        }
        static int GetSecondaryColor(UnitEntityData unitEntityData)
        {
            if (unitEntityData.Descriptor.Doll == null) return -1;
            var doll = unitEntityData.Descriptor.Doll;
            foreach (var assetId in doll.EntitySecondaryRampIdices.Keys.ToList())
            {
                return doll.EntitySecondaryRampIdices[assetId];
            }
            return -1;
        }
        static void FixColors(UnitEntityView unitEntityView)
        {
            //Probably not necessary, don't update colors if doll contains current class
            var dollEE = new List<EquipmentEntity>();
            var doll = unitEntityView.EntityData.Descriptor.Doll;
            if (doll == null) return;
            foreach (var assetId in doll.EquipmentEntityIds)
            {
                EquipmentEntity ee = ResourcesLibrary.TryGetResource<EquipmentEntity>(assetId);
                if (ee == null) continue;
                dollEE.Add(ee);
            }

            var character = unitEntityView.CharacterAvatar;
            var equipmentClass = unitEntityView.EntityData.Descriptor.Progression.GetEquipmentClass();
            var clothes = equipmentClass.LoadClothes(unitEntityView.EntityData.Descriptor.Gender, unitEntityView.EntityData.Descriptor.Progression.Race);
            var primaryIndex = GetPrimaryColor(unitEntityView.EntityData);
            var secondaryIndex = GetSecondaryColor(unitEntityView.EntityData);
            foreach(var ee in clothes)
            {
                if (dollEE.Contains(ee)) continue;
                character.SetPrimaryRampIndex(ee, primaryIndex);
                character.SetSecondaryRampIndex(ee, secondaryIndex);
            }
        }
        public static void RebuildCharacter(UnitEntityData unitEntityData)
        {
            var character = unitEntityData.View.CharacterAvatar;
            if (character == null) return; // Happens when overriding view
            if (unitEntityData.Descriptor.Doll != null)
            {
                var doll = unitEntityData.Descriptor.Doll;
                var savedEquipment = true;
                character.RemoveAllEquipmentEntities(savedEquipment);
                if (doll.RacePreset != null)
                {
                    character.Skeleton = ((doll.Gender != Gender.Male)) ? doll.RacePreset.FemaleSkeleton : doll.RacePreset.MaleSkeleton;
                    character.AddEquipmentEntities(doll.RacePreset.Skin.Load(doll.Gender, doll.RacePreset.RaceId), savedEquipment);
                }
                character.Mirror = doll.LeftHanded;
                foreach (string assetID in doll.EquipmentEntityIds)
                {
                    EquipmentEntity ee = ResourcesLibrary.TryGetResource<EquipmentEntity>(assetID);
                    character.AddEquipmentEntity(ee, savedEquipment);
                }
                doll.ApplyRampIndices(character);
                Traverse.Create(unitEntityData.View).Field("m_EquipmentClass").SetValue(null); //UpdateClassEquipment won't update if the class doesn't change
                unitEntityData.View.UpdateBodyEquipmentModel();
                unitEntityData.View.UpdateClassEquipment();
            } else
            {
                UnitEntityView viewTemplate = (!string.IsNullOrEmpty(unitEntityData.Descriptor.CustomPrefabGuid)) ? 
                    ResourcesLibrary.TryGetResource<UnitEntityView>(unitEntityData.Descriptor.CustomPrefabGuid) :
                    unitEntityData.Blueprint.Prefab.Load();
                var characterBase = viewTemplate.GetComponentInChildren<Character>();
                character.CopyEquipmentFrom(characterBase);
                //Note UpdateBodyEquipmentModel does nothing for baked characters
                IEnumerable<EquipmentEntity> ees = unitEntityData.Body.AllSlots.SelectMany(new Func<ItemSlot, IEnumerable<EquipmentEntity>>(unitEntityData.View.ExtractEquipmentEntities));
                unitEntityData.View.CharacterAvatar.AddEquipmentEntities(ees, false);
            }
        }
        static void ChangeCompanionOutfit(UnitEntityView __instance, CharacterSettings characterSettings)
        {
            /*
             * Note UpdateClassEquipment() works by removing the clothes of the old class, and loading the clothes of the new class
             * We can't do that here because we do not have a list of the companion clothes.
             */
            void FilterOutfit(string name)
            {
                __instance.CharacterAvatar.RemoveEquipmentEntities(
                   __instance.CharacterAvatar.EquipmentEntities.Where((ee) => ee.name.Contains(name)).ToArray()
                );
            }
            switch (__instance.EntityData.Blueprint.AssetGuid)
            {
                case "77c11edb92ce0fd408ad96b40fd27121": //"Linzi",
                    FilterOutfit("Bard");
                    break;
                case "5455cd3cd375d7a459ca47ea9ff2de78": //"Tartuccio",
                    FilterOutfit("Sorcerer");
                    break;
                case "54be53f0b35bf3c4592a97ae335fe765": //"Valerie",
                    FilterOutfit("Fighter");
                    break;
                case "b3f29faef0a82b941af04f08ceb47fa2": //"Amiri",
                    FilterOutfit("Barbarian");
                    break;
                case "aab03d0ab5262da498b32daa6a99b507": //"Harrim",
                    FilterOutfit("Cleric");
                    break;
                case "32d2801eddf236b499d42e4a7d34de23": //"Jaethal",
                    FilterOutfit("Inquistor");
                    break;
                case "b090918d7e9010a45b96465de7a104c3": //"Regongar",
                    FilterOutfit("Magus");
                    break;
                case "f9161aa0b3f519c47acbce01f53ee217": //"Octavia",
                    FilterOutfit("Wizard");
                    break;
                case "f6c23e93512e1b54dba11560446a9e02": //"Tristian",
                    FilterOutfit("Cleric");
                    break;
                case "d5bc1d94cd3e5be4bbc03f3366f67afc": //"Ekundayo",
                    FilterOutfit("Ranger");
                    break;
                case "3f5777b51d301524c9b912812955ee1e": //"Jubilost",
                    FilterOutfit("Alchemist");
                    break;
                case "f9417988783876044b76f918f8636455": //"Nok-Nok",
                    FilterOutfit("Rogue");
                    break;
            }
            var _class = __instance.EntityData.Descriptor.Progression.GetEquipmentClass();
            var gender = __instance.EntityData.Descriptor.Gender;
            var race = __instance.EntityData.Descriptor.Progression.Race;
            var ees = _class.LoadClothes(gender, race);
            __instance.CharacterAvatar.AddEquipmentEntities(ees);
            foreach(var ee in ees)
            {
                __instance.CharacterAvatar.SetPrimaryRampIndex(ee, characterSettings.companionPrimary);
                __instance.CharacterAvatar.SetSecondaryRampIndex(ee, characterSettings.companionSecondary);
            }
        }
        static void HideSlot(UnitEntityView __instance, ItemSlot slot, ref bool dirty)
        {
            var ee = __instance.ExtractEquipmentEntities(slot).ToList();
            if (ee.Count > 0)
            {
                __instance.CharacterAvatar.RemoveEquipmentEntities(ee);
                dirty = true;
            }
        }
        static bool OverrideEquipment(UnitEntityView __instance, ItemSlot slot, string assetId, ref bool dirty)
        {
            var kee = ResourcesLibrary.TryGetBlueprint<KingmakerEquipmentEntity>(assetId);
            if (kee == null) return false;
            var ee = kee.Load(__instance.EntityData.Descriptor.Gender, __instance.EntityData.Descriptor.Progression.Race.RaceId);
            if (ee == null) return false;
            HideSlot(__instance, slot, ref dirty);
            __instance.CharacterAvatar.AddEquipmentEntities(ee);
            dirty = true;
            return true;
        }
        public static void UpdateModel(UnitEntityView view)
        {
            if (view.CharacterAvatar == null) return;
            if (!view.EntityData.IsPlayerFaction) return;
            Settings.CharacterSettings characterSettings = settings.characterSettings.FirstOrDefault((cs) => cs.characterName == view.EntityData.CharacterName);
            if (characterSettings == null) return;            
            bool dirty = view.CharacterAvatar.IsDirty;
            if(view.EntityData.Descriptor.Doll == null && characterSettings.classOutfit != "Default")
            {
                ChangeCompanionOutfit(view, characterSettings);
            }
            if (characterSettings.hideHelmet)
            {
                HideSlot(view, view.EntityData.Body.Head, ref dirty);
            }
            if (characterSettings.hideEquipCloak)
            {
                HideSlot(view, view.EntityData.Body.Shoulders, ref dirty);
            }
            if (characterSettings.hideArmor)
            {
                HideSlot(view, view.EntityData.Body.Armor, ref dirty);
            }
            if (characterSettings.hideGloves)
            {
                HideSlot(view, view.EntityData.Body.Gloves, ref dirty);
            }
            if (characterSettings.hideBracers)
            {
                HideSlot(view, view.EntityData.Body.Wrist, ref dirty);
            }
            if (characterSettings.hideBoots)
            {
                HideSlot(view, view.EntityData.Body.Feet, ref dirty);
            }
            if(characterSettings.overrideHelm != "" && !characterSettings.hideHelmet)
            {
                if (!OverrideEquipment(view, view.EntityData.Body.Head, characterSettings.overrideHelm, ref dirty))
                {
                    characterSettings.overrideHelm = "";
                }
            }
            if (characterSettings.overrideCloak != "" && !characterSettings.hideEquipCloak)
            {
                if (!OverrideEquipment(view, view.EntityData.Body.Shoulders, characterSettings.overrideCloak, ref dirty))
                {
                    characterSettings.overrideCloak = "";
                }
            }
            if (characterSettings.overrideArmor != "" && !characterSettings.hideArmor)
            {
                if (!OverrideEquipment(view, view.EntityData.Body.Armor, characterSettings.overrideArmor, ref dirty))
                {
                    characterSettings.overrideArmor = "";
                }
            }
            if (characterSettings.overrideBracers != "" && !characterSettings.hideBracers)
            {
                if (!OverrideEquipment(view, view.EntityData.Body.Wrist, characterSettings.overrideBracers, ref dirty))
                {
                    characterSettings.overrideBracers = "";
                }
            }
            if (characterSettings.overrideGloves != "" && !characterSettings.hideGloves)
            {
                if (!OverrideEquipment(view, view.EntityData.Body.Gloves, characterSettings.overrideGloves, ref dirty))
                {
                    characterSettings.overrideGloves = "";
                }
            }
            if (characterSettings.overrideBoots != "" && !characterSettings.hideBoots)
            {
                if (!OverrideEquipment(view, view.EntityData.Body.Feet, characterSettings.overrideBoots, ref dirty))
                {
                    characterSettings.overrideBoots = "";
                }
            }
            if (characterSettings.hideBackpack)
            {
                foreach (var ee in view.CharacterAvatar.EquipmentEntities.ToArray())
                {
                    if(ee.OutfitParts.Exists((outfit) => outfit.Special == EquipmentEntity.OutfitPartSpecialType.Backpack))
                    {
                        view.CharacterAvatar.EquipmentEntities.Remove(ee);
                        dirty = true;
                    }
                }
            }
            if (characterSettings.hideCloak)
            {
                foreach (var ee in view.CharacterAvatar.EquipmentEntities.ToArray())
                {
                    if (ee.OutfitParts.Exists((outfit) => {
                        return outfit.Special == EquipmentEntity.OutfitPartSpecialType.Cloak ||
                            outfit.Special == EquipmentEntity.OutfitPartSpecialType.CloakSquashed;
                        }) && !view.ExtractEquipmentEntities(view.EntityData.Body.Shoulders).Contains(ee))
                    {
                        view.CharacterAvatar.EquipmentEntities.Remove(ee);
                        dirty = true;
                    }
                }
            }
            if (characterSettings.hideCap)
            {
                foreach (var ee in view.CharacterAvatar.EquipmentEntities.ToArray())
                {
                    if (ee.BodyParts.Exists((bodypart) => bodypart.Type == BodyPartType.Cap) &&
                        !view.ExtractEquipmentEntities(view.EntityData.Body.Head).Contains(ee))
                    {
                        view.CharacterAvatar.EquipmentEntities.Remove(ee);
                        dirty = true;
                    }
                }
            }
            if(view.EntityData.Descriptor.Doll != null) FixColors(view);
            view.CharacterAvatar.IsDirty = dirty;                
        }
        [HarmonyPatch(typeof(UnitEntityData), "CreateView")]
        static class UnitEntityData_CreateView_Patch
        {
            static bool Prefix(UnitEntityData __instance, ref UnitEntityView __result)
            {
                if (!enabled) return true;
                if (!__instance.IsPlayerFaction) return true; ;
                if (!settingsLookup.ContainsKey(__instance.CharacterName)) return true;
                var characterSettings = settingsLookup[__instance.CharacterName];
                if (characterSettings.overrideView == null || characterSettings.overrideView == "") return true;
                foreach (Fact fact in __instance.Buffs.RawFacts)
                {
                    if (fact.Active && !fact.Deactivating)
                    {
                        Buff buff = (Buff)fact;
                        if (buff.Get<Polymorph>() != null)
                        {
                            return true;
                        }
                    }
                }

                UnitEntityView unitEntityView4 = GetView(characterSettings.overrideView);
                if (unitEntityView4 == null)
                {
                    DebugLog("Overriding invalid view " + characterSettings.overrideView);
                    return true;
                }
                Quaternion rotation2 = (!unitEntityView4.ForbidRotation) ? Quaternion.Euler(0f, __instance.Orientation, 0f) : Quaternion.identity;
                __result = UnityEngine.Object.Instantiate<UnitEntityView>(unitEntityView4, __instance.Position, rotation2);
                return false;
            }
        }
        /*
         * Called by CheatsSilly.UpdatePartyNoArmor and OnDataAttached
         * Applies all EquipmentEntities from item Slots for NonBaked avatars
         * Does nothing if SCCCanSeeTheirClassSpecificClothes is enabled
         * */
        [HarmonyPatch(typeof(UnitEntityView), "UpdateBodyEquipmentModel")]
        static class UnitEntityView_UpdateBodyEquipmentModel_Patch
        {
            static void Postfix(UnitEntityView __instance)
            {
                if (!enabled) return;
                UpdateModel(__instance);
            }
        }
        /*
         * Unclear when called
         * Handles changed hand slots, usable slots
         * When item slot is changed, removes old equipment and adds new slot
         * */
        [HarmonyPatch(typeof(UnitEntityView), "HandleEquipmentSlotUpdated")]
        static class UnitEntityView_HandleEquipmentSlotUpdated_Patch
        {
            static void Postfix(UnitEntityView __instance)
            {
                if (!enabled) return;
                UpdateModel(__instance);
            }
        }
        /*
         * Called when a character levels up, or on UnitEntityView.OnDataAttached
         * Removes all equipment of current class, CheatSillyShirt.
         * Adds equipment of new class
         * Adds CheatSillyShirt back
         * Applies doll colors and saves class
         * */
        [HarmonyPatch(typeof(UnitEntityView), "UpdateClassEquipment")]
        static class UnitEntityView_UpdateClassEquipment_Patch
        {
            static void Postfix(UnitEntityView __instance)
            {
                if (!enabled) return;
                UpdateModel(__instance);
            }
        }
        [HarmonyPatch(typeof(UnitViewHandsEquipment), "UpdateVisibility")]
        static class UnitViewHandsEquipment_UpdateVisibility_Patch
        {
            static void Postfix(UnitViewHandsEquipment __instance)
            {
                if (!enabled) return;
                if (!__instance.Owner.IsPlayerFaction) return;
                Settings.CharacterSettings characterSettings = settings.characterSettings.FirstOrDefault((cs) => cs.characterName == __instance.Owner.CharacterName);
                if (characterSettings == null) return;
                if (characterSettings.hideWeapons)
                {
                    foreach (var kv in __instance.Sets)
                    {
                        if (kv.Key.PrimaryHand.Active) continue;
                        kv.Value.MainHand.ShowItem(false);
                        kv.Value.OffHand.ShowItem(false);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(UnitViewHandSlotData), "AttachModel", new Type[] { })]
        static class UnitViewHandsSlotData_AttachModel_Patch
        {
            static void Postfix(UnitViewHandSlotData __instance)
            {
                if (!enabled) return;
                if (!__instance.Owner.IsPlayerFaction) return;
                Settings.CharacterSettings characterSettings = settings.characterSettings.FirstOrDefault((cs) => cs.characterName == __instance.Owner.CharacterName);
                if (characterSettings == null) return;
                if (characterSettings.hideWeapons)
                {
                    if (!__instance.IsActiveSet)
                    {
                        __instance.ShowItem(false);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(UnitProgressionData), "GetEquipmentClass")]
        static class UnitProgressionData_GetEquipmentClass_Patch
        {
            static bool Prefix(UnitProgressionData __instance, ref BlueprintCharacterClass __result)
            {
                if (!enabled) return true;
                if(disableEquipmentClassPatch) return true;
                if (!__instance.Owner.IsPlayerFaction) return true;
                if (!settingsLookup.ContainsKey(__instance.Owner.CharacterName)) return true;
                Settings.CharacterSettings characterSettings = settingsLookup[__instance.Owner.CharacterName];
                switch (characterSettings.classOutfit)
                {
                    case "Alchemist":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["0937bec61c0dabc468428f496580c721"];
                        break;
                    case "Barbarian":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["f7d7eb166b3dd594fb330d085df41853"];
                        break;
                    case "Bard":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["772c83a25e2268e448e841dcd548235f"];
                        break;
                    case "Cleric":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["67819271767a9dd4fbfd4ae700befea0"];
                        break;
                    case "Druid":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["610d836f3a3a9ed42a4349b62f002e96"];
                        break;
                    case "Fighter":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["48ac8db94d5de7645906c7d0ad3bcfbd"];
                        break;
                    case "Inquisitor":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["f1a70d9e1b0b41e49874e1fa9052a1ce"];
                        break;
                    case "Magus":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["45a4607686d96a1498891b3286121780"];
                        break;
                    case "Monk":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["e8f21e5b58e0569468e420ebea456124"];
                        break;
                    case "Paladin":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["bfa11238e7ae3544bbeb4d0b92e897ec"];
                        break;
                    case "Ranger":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["cda0615668a6df14eb36ba19ee881af6"];
                        break;
                    case "Rogue":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["299aa766dee3cbf4790da4efb8c72484"];
                        break;
                    case "Sorcerer":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["b3a505fb61437dc4097f43c3f8f9a4cf"];
                        break;
                    case "Wizard":
                        __result = (BlueprintCharacterClass)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["ba34257984f4c41408ce1dc2004e342e"];
                        break;
                    default:
                        return true;
                }
                if (__result == null) return true;
                return false;
            }
        }

    }
    
}
