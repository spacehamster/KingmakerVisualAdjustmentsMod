using UnityEngine;
using Harmony12;
using UnityModManagerNet;
using Kingmaker.Blueprints;
using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.ResourceLinks;
using static VisualAdjustments.Settings;
using Kingmaker.PubSubSystem;
using Kingmaker.Visual.Sound;

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
        public static void DebugError(Exception ex)
        {
            if (logger != null) logger.Log(ex.ToString() + "\n" + ex.StackTrace);
        }
        public static bool enabled;
        public static Settings settings;
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
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                settings = Settings.Load(modEntry);
                var harmony = HarmonyInstance.Create(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
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
                    Settings.CharacterSettings characterSettings = settings.GetCharacterSettings(unitEntityData);
                    if(characterSettings == null)
                    {
                        characterSettings = new Settings.CharacterSettings();
                        characterSettings.characterName = unitEntityData.CharacterName;
                        settings.AddCharacterSettings(unitEntityData, characterSettings);
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
                if (characterSettings.classOutfit == _class)
                {
                    GUI.skin.button.normal.textColor = Color.yellow;
                    GUI.skin.button.focused.textColor = Color.yellow;
                }
                else
                {
                    GUI.skin.button.normal.textColor = normalColor;
                    GUI.skin.button.focused.textColor = focusedColor;
                }
                if (GUILayout.Button(_class, Array.Empty<GUILayoutOption>()))
                {
                    characterSettings.classOutfit = _class;
                    CharacterManager.RebuildCharacter(unitEntityData);
                    unitEntityData.View.UpdateClassEquipment();
                }
            }
            GUI.skin.button.normal.textColor = normalColor;
            GUI.skin.button.focused.textColor = focusedColor;
            GUILayout.EndHorizontal();
        }
        static void ChoosePortrait(UnitEntityData unitEntityData)
        {
            if (unitEntityData.Portrait.IsCustom) {
                var key = unitEntityData.Descriptor.UISettings.CustomPortrait.CustomId;
                var oldIndex = DollResourcesManager.CustomPortraits.IndexOf(key);
                GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                GUILayout.Label("Portrait:  ", GUILayout.Width(300));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(oldIndex, 0, DollResourcesManager.CustomPortraits.Count, GUILayout.Width(300)), 0);
                var value = newIndex >= 0 && newIndex < DollResourcesManager.CustomPortraits.Count ? DollResourcesManager.CustomPortraits[newIndex] : null;
                GUILayout.Label(" " + value, GUILayout.ExpandWidth(false));
                if(GUILayout.Button("Use Normal"))
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(
                        (BlueprintPortrait)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["621ada02d0b4bf64387babad3a53067b"]);
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                    return;
                }
                GUILayout.EndHorizontal();
                if (newIndex != oldIndex && value != null)
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(new PortraitData(value));
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                }
            } else
            {
                var key = unitEntityData.Descriptor.UISettings.PortraitBlueprint?.name;
                var oldIndex = DollResourcesManager.Portrait.IndexOfKey(key != null ? key : "");
                GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                GUILayout.Label("Portrait ", GUILayout.Width(300));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(oldIndex, 0, DollResourcesManager.Portrait.Count, GUILayout.Width(300)), 0);
                var value = newIndex >= 0 && newIndex < DollResourcesManager.Portrait.Count ? DollResourcesManager.Portrait.Values[newIndex] : null;
                GUILayout.Label(" " + value, GUILayout.ExpandWidth(false));
                if (GUILayout.Button("Use Custom"))
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(CustomPortraitsManager.Instance.CreateNewOrLoadDefault());
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                    return;
                }
                GUILayout.EndHorizontal();
                if (newIndex != oldIndex && value != null)
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(value);
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                }
            }
        }
        static void ChooseAsks(UnitEntityData unitEntityData)
        {
            var oldIndex = DollResourcesManager.Asks.IndexOfKey(unitEntityData.Descriptor.CustomAsks.name);
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label("Voice  ", GUILayout.Width(300));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(oldIndex, 0, DollResourcesManager.Asks.Count, GUILayout.Width(300)), 0);
            var value = (newIndex >= 0 && newIndex < DollResourcesManager.Asks.Count) ? DollResourcesManager.Asks.Values[newIndex] : null;
            GUILayout.Label(" " + value, GUILayout.ExpandWidth(false));
            if (GUILayout.Button("Preview", GUILayout.ExpandWidth(false)))
            {
                var asks = value?.GetComponent<UnitAsksComponent>();
                if (asks != null) asks.PlayPreview();
                else DebugLog("Missing Asks");
            }
            GUILayout.EndHorizontal();
            if (newIndex != oldIndex && value != null)
            {
                unitEntityData.Descriptor.CustomAsks = value;
                unitEntityData.View?.UpdateAsks();
            }
        }
        static void ChooseFromList<T>(string label, IReadOnlyList<T> list, ref int currentIndex, Action onChoose)
        {
            if (list.Count == 0) return;
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label(label + " ", GUILayout.Width(300));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, list.Count - 1, GUILayout.Width(300)), 0);
            GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (newIndex != currentIndex && newIndex < list.Count)
            {
                currentIndex = newIndex;
                onChoose();
            }
        }
        static void ChooseEEL(UnitEntityData unitEntityData, DollState doll, string label, EquipmentEntityLink[] links, EquipmentEntityLink link, Action<EquipmentEntityLink> setter)
        {
            var index = links.ToList().FindIndex((eel) => eel.AssetId == link.AssetId);
            ChooseFromList(label, links, ref index, () => {
                setter(links[index]);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                CharacterManager.RebuildCharacter(unitEntityData);
            });
        }
        static void ChooseRamp(UnitEntityData unitEntityData, DollState doll, string label, List<Texture2D> textures, int currentRamp, Action<int> setter)
        {
            ChooseFromList(label, textures, ref currentRamp, () => {
                setter(currentRamp);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                CharacterManager.RebuildCharacter(unitEntityData);
            });
        }
        static void ChooseVisualPreset(UnitEntityData unitEntityData, DollState doll, string label, BlueprintRaceVisualPreset[] presets,
            BlueprintRaceVisualPreset currentPreset)
        {
            var index = Array.FindIndex(presets, (vp) => vp == currentPreset); 
            ChooseFromList(label, presets, ref index, () => {
                doll.SetRacePreset(presets[index]);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                CharacterManager.RebuildCharacter(unitEntityData);
            });
        }
        static void ChooseDoll(UnitEntityData unitEntityData)
        {
            var doll = DollResourcesManager.GetDoll(unitEntityData);
            var race = unitEntityData.Descriptor.Progression.Race;
            var gender = unitEntityData.Gender;
            CustomizationOptions customizationOptions = gender != Gender.Male ? race.FemaleOptions : race.MaleOptions;
            ChooseEEL(unitEntityData, doll, "Face", customizationOptions.Heads, doll.Head, (EquipmentEntityLink ee) => doll.SetHead(ee));
            ChooseEEL(unitEntityData, doll, "Hair", customizationOptions.Hair, doll.Hair,  (EquipmentEntityLink ee) => doll.SetHair(ee));
            ChooseEEL(unitEntityData, doll, "Beards", customizationOptions.Beards, doll.Beard,  (EquipmentEntityLink ee) => doll.SetBeard(ee));
            ChooseRamp(unitEntityData, doll, "Hair Color", doll.GetHairRamps(), doll.HairRampIndex,  (int index) => doll.SetHairColor(index));
            ChooseRamp(unitEntityData, doll, "Skin Color", doll.GetSkinRamps(), doll.SkinRampIndex,  (int index) => doll.SetSkinColor(index));
            ChooseRamp(unitEntityData, doll, "Primary Outfit Color", doll.GetOutfitRampsPrimary(), doll.EquipmentRampIndex,  (int index) => doll.SetEquipColors(index, doll.EquipmentRampIndexSecondary));
            ChooseRamp(unitEntityData, doll, "Secondary Outfit Color", doll.GetOutfitRampsSecondary(), doll.EquipmentRampIndexSecondary,  (int index) => doll.SetEquipColors(doll.EquipmentRampIndex, index));
            ChooseVisualPreset(unitEntityData, doll, "Body Type", doll.Race.Presets, doll.RacePreset);
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
                    CharacterManager.UpdateModel(unitEntityData.View);
                }
            }
            {
                GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                GUILayout.Label("Secondary Outfit Color ", GUILayout.Width(300));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(characterSettings.companionSecondary, 0, 35, GUILayout.Width(300)), 0);
                GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (newIndex != characterSettings.companionSecondary)
                {
                    characterSettings.companionSecondary = newIndex;
                    CharacterManager.UpdateModel(unitEntityData.View);
                }
            }
            ChoosePortrait(unitEntityData);
        }
        static void ChooseToggle(string label, ref bool currentValue, Action onChoose)
        {
            bool newValue = GUILayout.Toggle(currentValue, label, GUILayout.ExpandWidth(false));
            if (newValue != currentValue)
            {
                currentValue = newValue;
                onChoose();
            }
        }
        static void ChooseEquipment(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            Action onHideEquipment = () =>
            {
                CharacterManager.RebuildCharacter(unitEntityData);
                CharacterManager.UpdateModel(unitEntityData.View);
            };
            Action onHideWeapon = () =>
            {
                unitEntityData.View.HandsEquipment.HandleEquipmentSetChanged();
            };
            Action onHideBuff = () =>
            {
                foreach (var buff in unitEntityData.Buffs) buff.ClearParticleEffect();
                unitEntityData.SpawnBuffsFxs();
            };
            Action onHideWeaponEnchantment = () =>
            {
                unitEntityData.View.HandsEquipment.HandleEquipmentSetChanged();
            };
            ChooseToggle("Hide Cap", ref characterSettings.hideCap, onHideEquipment);
            ChooseToggle("Hide Backpack", ref characterSettings.hideBackpack, onHideEquipment);
            ChooseToggle("Hide Class Cloak", ref characterSettings.hideClassCloak, onHideEquipment);
            ChooseToggle("Hide Helmet", ref characterSettings.hideHelmet, onHideEquipment);
            ChooseToggle("Hide Item Cloak", ref characterSettings.hideItemCloak, onHideEquipment);
            ChooseToggle("Hide Armor", ref characterSettings.hideArmor, onHideEquipment);
            ChooseToggle("Hide Bracers", ref characterSettings.hideBracers, onHideEquipment);
            ChooseToggle("Hide Gloves", ref characterSettings.hideGloves, onHideEquipment);
            ChooseToggle("Hide Boots", ref characterSettings.hideBoots, onHideEquipment);
            ChooseToggle("Hide Inactive Weapons", ref characterSettings.hideWeapons, onHideWeapon);
            ChooseToggle("Hide Weapon Enchantments", ref characterSettings.hideWeaponEnchantments, onHideWeaponEnchantment);
            ChooseToggle("Hide Wings", ref characterSettings.hideWings, onHideBuff);
        }
        static void ChooseSlider(string name, SortedList<string, string> items, ref string currentItem, Action onChoose)
        {
            var currentIndex = currentItem == null ? -1 : items.IndexOfKey(currentItem);
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label(name + " ", GUILayout.Width(300));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, -1, items.Count - 1, GUILayout.Width(300)), 0);
            var displayText = newIndex == -1 ? "None" : items.Values[newIndex];
            GUILayout.Label(" " + displayText, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            if (currentIndex != newIndex)
            {
                currentItem = newIndex == -1 ? "" : items.Keys[newIndex];
                onChoose();
            }
        }
        static void ChooseEquipmentOverride(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            Action onEquipment = () =>
            {
                CharacterManager.RebuildCharacter(unitEntityData);
                CharacterManager.UpdateModel(unitEntityData.View);
            };
            GUILayout.Label("Equipment", "box", GUILayout.Width(300f));
            Action onView = () => ViewManager.ReplaceView(unitEntityData, characterSettings.overrideView);
            ChooseSlider("Override Helm", EquipmentResourcesManager.Helm, ref characterSettings.overrideHelm, onEquipment);
            ChooseSlider("Override Cloak ", EquipmentResourcesManager.Cloak, ref characterSettings.overrideCloak, onEquipment);
            ChooseSlider("Override Armor ", EquipmentResourcesManager.Armor, ref characterSettings.overrideArmor, onEquipment);
            ChooseSlider("Override Bracers ", EquipmentResourcesManager.Bracers, ref characterSettings.overrideBracers, onEquipment);
            ChooseSlider("Override Gloves ", EquipmentResourcesManager.Gloves, ref characterSettings.overrideGloves, onEquipment);
            ChooseSlider("Override Boots ", EquipmentResourcesManager.Boots, ref characterSettings.overrideBoots, onEquipment);
            GUILayout.Label("Weapons", "box", GUILayout.Width(300f));
            foreach (var kv in EquipmentResourcesManager.Weapons)
            {
                var animationStyle = kv.Key;
                var weaponLookup = kv.Value;
                string currentValue = null;
                characterSettings.overrideWeapons.TryGetValue(animationStyle, out currentValue);
                Action onWeapon = () =>
                {
                    characterSettings.overrideWeapons[animationStyle] = currentValue;
                    unitEntityData.View.HandsEquipment.UpdateAll();
                };
                ChooseSlider($"Override {animationStyle} ", weaponLookup, ref currentValue, onWeapon);
            }

            GUILayout.Label("View", "box", GUILayout.Width(300f));
            GUILayout.BeginHorizontal();
            ChooseSlider("Override View", EquipmentResourcesManager.Units, ref characterSettings.overrideView, onView);
            if (GUILayout.Button("Prev", GUILayout.Width(45)))
            {
                var currentIndex = EquipmentResourcesManager.Units.IndexOfKey(characterSettings.overrideView);
                if (currentIndex == 0)
                {
                    characterSettings.overrideView = "";
                }
                else
                {
                    characterSettings.overrideView = EquipmentResourcesManager.Units.Keys[currentIndex - 1];
                }
                ViewManager.ReplaceView(unitEntityData, characterSettings.overrideView);
            }
            if (GUILayout.Button("Next", GUILayout.Width(45)))
            {
                var currentIndex = EquipmentResourcesManager.Units.IndexOfKey(characterSettings.overrideView);
                if (currentIndex == EquipmentResourcesManager.Units.Count) return;
                characterSettings.overrideView = EquipmentResourcesManager.Units.Keys[currentIndex + 1];
                ViewManager.ReplaceView(unitEntityData, characterSettings.overrideView);
            }
            GUILayout.EndHorizontal();
#if (DEBUG)
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
#endif
        }
    }
    
}
