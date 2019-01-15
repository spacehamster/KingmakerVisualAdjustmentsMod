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
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.Blueprints.Root;
using Kingmaker.Utility;
namespace VisualAdjustments
{

    public class Main
    {
        const float DefaultLabelWidth = 200f;
        const float DefaultSliderWidth = 300f;
        public static UnityModManager.ModEntry.ModLogger logger;
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugLog(string msg)
        {
            if (logger != null) logger.Log(msg);
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
            "Kineticist",
            "Magus",
            "Monk",
            "Paladin",
            "Ranger",
            "Rogue",
            "Sorcerer",
            "Wizard",
            "None"
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
            catch (Exception e)
            {
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
                if (Game.Instance.Player.ControllableCharacters == null) return;
                foreach (UnitEntityData unitEntityData in Game.Instance.Player.ControllableCharacters)
                {
                    Settings.CharacterSettings characterSettings = settings.GetCharacterSettings(unitEntityData);
                    if (characterSettings == null)
                    {
                        characterSettings = new CharacterSettings();
                        characterSettings.characterName = unitEntityData.CharacterName;
                        settings.AddCharacterSettings(unitEntityData, characterSettings);
                    }
                    if (unitEntityData.Descriptor.IsPet)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(string.Format("{0}", unitEntityData.CharacterName), "box", GUILayout.Width(DefaultLabelWidth));
                        characterSettings.showOverrideSelection = GUILayout.Toggle(characterSettings.showOverrideSelection, "Show Override Selection", GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();
                        if (characterSettings.showOverrideSelection) ChooseEquipmentOverride(unitEntityData, characterSettings);
                        continue;
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("{0}", unitEntityData.CharacterName), "box", GUILayout.Width(DefaultLabelWidth));
                    characterSettings.showClassSelection = GUILayout.Toggle(characterSettings.showClassSelection, "Select Outfit", GUILayout.ExpandWidth(false));
                    if (unitEntityData.Descriptor.Doll != null)
                    {
                        characterSettings.showDollSelection = GUILayout.Toggle(characterSettings.showDollSelection, "Select Doll", GUILayout.ExpandWidth(false));
                    }
                    else
                    {
                        characterSettings.showDollSelection = GUILayout.Toggle(characterSettings.showDollSelection, "Select Doll", GUILayout.ExpandWidth(false));
                    }
                    characterSettings.showEquipmentSelection = GUILayout.Toggle(characterSettings.showEquipmentSelection, "Select Equipment", GUILayout.ExpandWidth(false));
                    characterSettings.showOverrideSelection = GUILayout.Toggle(characterSettings.showOverrideSelection, "Select Overrides", GUILayout.ExpandWidth(false));
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
            }
            catch (Exception e)
            {
                DebugLog(e.ToString() + " " + e.StackTrace);
            }
        }
        static void ChooseClassOutfit(CharacterSettings characterSettings, UnitEntityData unitEntityData)
        {
            var focusedStyle = new GUIStyle(GUI.skin.button);
            focusedStyle.normal.textColor = Color.yellow;
            focusedStyle.focused.textColor = Color.yellow;
            GUILayout.BeginHorizontal();
            foreach (var _class in classes)
            {
                if (_class == "Magus")
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                if (_class == "Kineticist" && !BlueprintRoot.Instance.DlcSettings.Tieflings.Enabled)
                    continue;
                var style = characterSettings.classOutfit == _class ? focusedStyle : GUI.skin.button;
                if (GUILayout.Button(_class, style))
                {
                    characterSettings.classOutfit = _class;
                    CharacterManager.RebuildCharacter(unitEntityData);
                    unitEntityData.View.UpdateClassEquipment();
                }
            }
            GUILayout.EndHorizontal();
        }
        static void ChoosePortrait(UnitEntityData unitEntityData)
        {
            if (unitEntityData.Portrait.IsCustom)
            {
                var key = unitEntityData.Descriptor.UISettings.CustomPortrait.CustomId;
                var currentIndex = DollResourcesManager.CustomPortraits.IndexOf(key);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Portrait:  ", GUILayout.Width(DefaultLabelWidth));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, DollResourcesManager.CustomPortraits.Count, GUILayout.Width(DefaultSliderWidth)), 0);
                if (GUILayout.Button("Prev", GUILayout.Width(45)) && currentIndex >= 0)
                {
                    newIndex = currentIndex - 1;
                }
                if (GUILayout.Button("Next", GUILayout.Width(45)) && currentIndex < DollResourcesManager.CustomPortraits.Count - 1)
                {
                    newIndex = currentIndex + 1;
                }
                if (GUILayout.Button("Use Normal"))
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(
                        (BlueprintPortrait)ResourcesLibrary.LibraryObject.BlueprintsByAssetId["621ada02d0b4bf64387babad3a53067b"]);
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                    return;
                }
                var value = newIndex >= 0 && newIndex < DollResourcesManager.CustomPortraits.Count ? DollResourcesManager.CustomPortraits[newIndex] : null;
                GUILayout.Label(" " + value, GUILayout.ExpandWidth(false));

                GUILayout.EndHorizontal();
                if (newIndex != currentIndex && value != null)
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(new PortraitData(value));
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                }
            }
            else
            {
                var key = unitEntityData.Descriptor.UISettings.PortraitBlueprint?.name;
                var currentIndex = DollResourcesManager.Portrait.IndexOfKey(key ?? "");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Portrait ", GUILayout.Width(DefaultLabelWidth));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, DollResourcesManager.Portrait.Count, GUILayout.Width(DefaultSliderWidth)), 0);
                if (GUILayout.Button("Prev", GUILayout.Width(45)) && currentIndex >= 0)
                {
                    newIndex = currentIndex - 1;
                }
                if (GUILayout.Button("Next", GUILayout.Width(45)) && currentIndex < DollResourcesManager.Portrait.Count - 1)
                {
                    newIndex = currentIndex + 1;
                }
                if (GUILayout.Button("Use Custom", GUILayout.ExpandWidth(false)))
                {
                    unitEntityData.Descriptor.UISettings.SetPortrait(CustomPortraitsManager.Instance.CreateNewOrLoadDefault());
                    EventBus.RaiseEvent<IUnitPortraitChangedHandler>(delegate (IUnitPortraitChangedHandler h)
                    {
                        h.HandlePortraitChanged(unitEntityData);
                    });
                    return;
                }
                var value = newIndex >= 0 && newIndex < DollResourcesManager.Portrait.Count ? DollResourcesManager.Portrait.Values[newIndex] : null;
                GUILayout.Label(" " + value, GUILayout.ExpandWidth(false));

                GUILayout.EndHorizontal();
                if (newIndex != currentIndex && value != null)
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
            int currentIndex = -1;
            if (unitEntityData.Descriptor.CustomAsks != null)
            {
                currentIndex = DollResourcesManager.Asks.IndexOfKey(unitEntityData.Descriptor.CustomAsks.name);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Custom Voice ", GUILayout.Width(DefaultLabelWidth));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, -1, DollResourcesManager.Asks.Count - 1, GUILayout.Width(DefaultSliderWidth)), 0);
            if (GUILayout.Button("Prev", GUILayout.Width(45)) && currentIndex >= 0)
            {
                newIndex = currentIndex - 1;
            }
            if (GUILayout.Button("Next", GUILayout.Width(45)) && currentIndex < DollResourcesManager.Asks.Count)
            {
                newIndex = currentIndex + 1;
            }
            var value = (newIndex >= 0 && newIndex < DollResourcesManager.Asks.Count) ? DollResourcesManager.Asks.Values[newIndex] : null;
            if (GUILayout.Button("Preview", GUILayout.ExpandWidth(false)))
            {
                var component = value?.GetComponent<UnitAsksComponent>();
                if (component != null && component.PreviewSound != "")
                {
                    component.PlayPreview();
                }
                else if (component != null && component.Selected.HasBarks)
                {
                    var bark = component.Selected.Entries.Random();
                    AkSoundEngine.PostEvent(bark.AkEvent, unitEntityData.View.gameObject);
                }
            }
            GUILayout.Label(" " + (value?.name ?? "None"), GUILayout.ExpandWidth(false));


            GUILayout.EndHorizontal();
            if (newIndex != currentIndex)
            {
                unitEntityData.Descriptor.CustomAsks = value;
                unitEntityData.View?.UpdateAsks();
            }
        }
        static void ChooseFromList<T>(string label, IReadOnlyList<T> list, ref int currentIndex, Action onChoose)
        {
            if (list.Count == 0) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " ", GUILayout.Width(DefaultLabelWidth));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, 0, list.Count - 1, GUILayout.Width(DefaultSliderWidth)), 0);
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
            if (!unitEntityData.IsMainCharacter && !unitEntityData.IsCustomCompanion() && GUILayout.Button("Destroy Doll", GUILayout.Width(DefaultLabelWidth)))
            {
                unitEntityData.Descriptor.Doll = null;
                unitEntityData.Descriptor.ForcceUseClassEquipment = false;
                CharacterManager.RebuildCharacter(unitEntityData);
            }
            var doll = DollResourcesManager.GetDoll(unitEntityData);
            var race = unitEntityData.Descriptor.Progression.Race;
            var gender = unitEntityData.Gender;
            CustomizationOptions customizationOptions = gender != Gender.Male ? race.FemaleOptions : race.MaleOptions;
            ChooseEEL(unitEntityData, doll, "Face", customizationOptions.Heads, doll.Head, (EquipmentEntityLink ee) => doll.SetHead(ee));
            ChooseEEL(unitEntityData, doll, "Hair", customizationOptions.Hair, doll.Hair, (EquipmentEntityLink ee) => doll.SetHair(ee));
            ChooseEEL(unitEntityData, doll, "Beards", customizationOptions.Beards, doll.Beard, (EquipmentEntityLink ee) => doll.SetBeard(ee));
            if (BlueprintRoot.Instance.DlcSettings.Tieflings.Enabled)
            {
                ChooseEEL(unitEntityData, doll, "Horns", customizationOptions.Horns, doll.Horn, (EquipmentEntityLink ee) => doll.SetHorn(ee));
            }
            ChooseRamp(unitEntityData, doll, "Hair Color", doll.GetHairRamps(), doll.HairRampIndex, (int index) => doll.SetHairColor(index));
            ChooseRamp(unitEntityData, doll, "Skin Color", doll.GetSkinRamps(), doll.SkinRampIndex, (int index) => doll.SetSkinColor(index));
            if (BlueprintRoot.Instance.DlcSettings.Tieflings.Enabled)
            {
                ChooseRamp(unitEntityData, doll, "Horn Color", doll.GetHornsRamps(), doll.HornsRampIndex, (int index) => doll.SetHornsColor(index));
            }
            ChooseRamp(unitEntityData, doll, "Primary Outfit Color", doll.GetOutfitRampsPrimary(), doll.EquipmentRampIndex, (int index) => doll.SetEquipColors(index, doll.EquipmentRampIndexSecondary));
            ChooseRamp(unitEntityData, doll, "Secondary Outfit Color", doll.GetOutfitRampsSecondary(), doll.EquipmentRampIndexSecondary, (int index) => doll.SetEquipColors(doll.EquipmentRampIndex, index));
            ChooseVisualPreset(unitEntityData, doll, "Body Type", doll.Race.Presets, doll.RacePreset);
            if (unitEntityData.Descriptor.Doll.LeftHanded && GUILayout.Button("Set Right Handed", GUILayout.Width(DefaultLabelWidth)))
            {
                unitEntityData.Descriptor.LeftHandedOverride = false;
                doll.SetLeftHanded(false);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                ViewManager.ReplaceView(unitEntityData, null);
                unitEntityData.View.HandsEquipment.HandleEquipmentSetChanged();
            }
            else if (!unitEntityData.Descriptor.Doll.LeftHanded && GUILayout.Button("Set Left Handed", GUILayout.Width(DefaultLabelWidth)))
            {
                unitEntityData.Descriptor.LeftHandedOverride = true;
                doll.SetLeftHanded(true);
                unitEntityData.Descriptor.Doll = doll.CreateData();
                ViewManager.ReplaceView(unitEntityData, null);
                unitEntityData.View.HandsEquipment.HandleEquipmentSetChanged();
            }
            ChoosePortrait(unitEntityData);
            if (unitEntityData.IsMainCharacter || unitEntityData.IsCustomCompanion()) ChooseAsks(unitEntityData);
        }
        static void ChooseCompanionColor(CharacterSettings characterSettings, UnitEntityData unitEntityData)
        {
            if (GUILayout.Button("Create Doll", GUILayout.Width(DefaultLabelWidth)))
            {
                var race = unitEntityData.Descriptor.Progression.Race;
                var options = unitEntityData.Descriptor.Gender == Gender.Male ? race.MaleOptions : race.FemaleOptions;
                var dollState = new DollState();
                dollState.SetRace(unitEntityData.Descriptor.Progression.Race); //Race must be set before class
                                                                               //This is a hack to work around harmony not allowing calls to the unpatched method
                CharacterManager.disableEquipmentClassPatch = true;
                dollState.SetClass(unitEntityData.Descriptor.Progression.GetEquipmentClass());
                CharacterManager.disableEquipmentClassPatch = false;
                dollState.SetGender(unitEntityData.Descriptor.Gender);
                dollState.SetRacePreset(race.Presets[0]);
                dollState.SetLeftHanded(false);
                if (options.Hair.Length > 0) dollState.SetHair(options.Hair[0]);
                if (options.Heads.Length > 0) dollState.SetHead(options.Hair[0]);
                if (options.Beards.Length > 0) dollState.SetBeard(options.Hair[0]);
                dollState.Validate();
                unitEntityData.Descriptor.Doll = dollState.CreateData();
                unitEntityData.Descriptor.ForcceUseClassEquipment = true;
                CharacterManager.RebuildCharacter(unitEntityData);
            }
            GUILayout.Label("Note: Colors only applies to non-default outfits, the default companion custom voice is None");
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Primary Outfit Color ", GUILayout.Width(DefaultLabelWidth));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(characterSettings.companionPrimary, -1, 35, GUILayout.Width(DefaultSliderWidth)), 0);
                GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (newIndex != characterSettings.companionPrimary)
                {
                    characterSettings.companionPrimary = newIndex;
                    CharacterManager.UpdateModel(unitEntityData.View);
                }
            }
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Secondary Outfit Color ", GUILayout.Width(DefaultLabelWidth));
                var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(characterSettings.companionSecondary, -1, 35, GUILayout.Width(DefaultSliderWidth)), 0);
                GUILayout.Label(" " + newIndex, GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
                if (newIndex != characterSettings.companionSecondary)
                {
                    characterSettings.companionSecondary = newIndex;
                    CharacterManager.UpdateModel(unitEntityData.View);
                }
            }
            ChoosePortrait(unitEntityData);
            ChooseAsks(unitEntityData);
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
            void onHideEquipment()
            {
                CharacterManager.RebuildCharacter(unitEntityData);
                CharacterManager.UpdateModel(unitEntityData.View);
            }
            void onHideBuff()
            {
                foreach (var buff in unitEntityData.Buffs) buff.ClearParticleEffect();
                unitEntityData.SpawnBuffsFxs();
            }
            void onWeaponChanged()
            {
                unitEntityData.View.HandsEquipment.UpdateAll();
            }
            ChooseToggle("Hide Cap", ref characterSettings.hideCap, onHideEquipment);
            ChooseToggle("Hide Backpack", ref characterSettings.hideBackpack, onHideEquipment);
            ChooseToggle("Hide Class Cloak", ref characterSettings.hideClassCloak, onHideEquipment);
            ChooseToggle("Hide Helmet", ref characterSettings.hideHelmet, onHideEquipment);
            ChooseToggle("Hide Item Cloak", ref characterSettings.hideItemCloak, onHideEquipment);
            ChooseToggle("Hide Armor", ref characterSettings.hideArmor, onHideEquipment);
            ChooseToggle("Hide Bracers", ref characterSettings.hideBracers, onHideEquipment);
            ChooseToggle("Hide Gloves", ref characterSettings.hideGloves, onHideEquipment);
            ChooseToggle("Hide Boots", ref characterSettings.hideBoots, onHideEquipment);
            ChooseToggle("Hide Inactive Weapons", ref characterSettings.hideWeapons, onWeaponChanged);
            ChooseToggle("Hide Belt Slots", ref characterSettings.hideBeltSlots, onWeaponChanged);
            ChooseToggle("Hide Weapon Enchantments", ref characterSettings.hideWeaponEnchantments, onWeaponChanged);
            ChooseToggle("Hide Wings", ref characterSettings.hideWings, onHideBuff);
            if (BlueprintRoot.Instance.DlcSettings.Tieflings.Enabled)
            {
                ChooseToggle("Hide Horns", ref characterSettings.hideHorns, onHideEquipment);
                ChooseToggle("Hide Tail", ref characterSettings.hideTail, onHideEquipment);
            }
        }
        static void ChooseSlider(string name, UnorderedList<string, string> items, ref string currentItem, Action onChoose)
        {
            var currentIndex = currentItem == null ? -1 : items.IndexOfKey(currentItem);
            GUILayout.BeginHorizontal();
            GUILayout.Label(name + " ", GUILayout.Width(DefaultLabelWidth));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, -1, items.Count - 1, GUILayout.Width(DefaultSliderWidth)), 0);
            if (GUILayout.Button("Prev", GUILayout.Width(45)) && currentIndex >= 0)
            {
                newIndex = currentIndex - 1;
            }
            if (GUILayout.Button("Next", GUILayout.Width(45)) && currentIndex < items.Count - 1)
            {
                newIndex = currentIndex + 1;
            }
            var displayText = newIndex == -1 ? "None" : items.Values[newIndex];
            GUILayout.Label(" " + displayText, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            if (currentIndex != newIndex)
            {
                currentItem = newIndex == -1 ? "" : items.Keys[newIndex];
                onChoose();
            }
        }
        /*
         * m_Size is updated from GetSizeScale (EntityData.Descriptor.State.Size) and 
         * is with m_OriginalScale to adjust the transform.localScale 
         * Adjusting GetSizeScale will effect character corpulence and cause gameplay sideeffects
         * Changing m_OriginalScale will effect ParticlesSnapMap.AdditionalScale
         */
        static void ChooseSizeAdditive(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Additive Scale Factor", GUILayout.Width(300));
            var sizeModifier = (int)GUILayout.HorizontalSlider(characterSettings.additiveScaleFactor, -4, 4, GUILayout.Width(DefaultSliderWidth));
            characterSettings.additiveScaleFactor = sizeModifier;
            var sign = sizeModifier >= 0 ? "+" : "";
            GUILayout.Label($" {sign}{sizeModifier}", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
        static void ChooseSizeOverride(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Override Scale Factor", GUILayout.Width(300));
            var sizeModifier = (int)GUILayout.HorizontalSlider(characterSettings.overrideScaleFactor, 0, 8, GUILayout.Width(DefaultSliderWidth));
            characterSettings.overrideScaleFactor = sizeModifier;
            GUILayout.Label($" {(Size)(sizeModifier)}", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
        static void ChooseSliderList(string name, UnorderedList<string, string> items, List<string> saved, int savedIndex, Action onChoose)
        {
            var currentItem = saved[savedIndex];
            var currentIndex = currentItem == null ? -1 : items.IndexOfKey(currentItem);
            GUILayout.BeginHorizontal();
            GUILayout.Label(name + " ", GUILayout.Width(DefaultLabelWidth));
            var newIndex = (int)Math.Round(GUILayout.HorizontalSlider(currentIndex, -1, items.Count - 1, GUILayout.Width(DefaultSliderWidth)), 0);
            if (GUILayout.Button("Prev", GUILayout.Width(45)) && currentIndex >= 0)
            {
                newIndex = currentIndex - 1;
            }
            if (GUILayout.Button("Next", GUILayout.Width(45)) && currentIndex < items.Count - 1)
            {
                newIndex = currentIndex + 1;
            }
            if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
            {
                saved.RemoveAt(savedIndex);
                onChoose();
                return;
            }
            var displayText = newIndex == -1 ? "None" : items.Values[newIndex];
            GUILayout.Label(" " + displayText, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            if (currentIndex != newIndex)
            {
                currentItem = newIndex == -1 ? "" : items.Keys[newIndex];
                saved[savedIndex] = currentItem;
                onChoose();
            }
        }
        static void ChooseEquipmentOverride(UnitEntityData unitEntityData, CharacterSettings characterSettings)
        {
            void onEquipment()
            {
                CharacterManager.RebuildCharacter(unitEntityData);
                CharacterManager.UpdateModel(unitEntityData.View);
            }
            GUILayout.Label("Equipment", "box", GUILayout.Width(DefaultLabelWidth));
            void onView() => ViewManager.ReplaceView(unitEntityData, characterSettings.overrideView);
            ChooseSlider("Override Helm", EquipmentResourcesManager.Helm, ref characterSettings.overrideHelm, onEquipment);
            ChooseSlider("Override Cloak", EquipmentResourcesManager.Cloak, ref characterSettings.overrideCloak, onEquipment);
            ChooseSlider("Override Armor", EquipmentResourcesManager.Armor, ref characterSettings.overrideArmor, onEquipment);
            ChooseSlider("Override Bracers", EquipmentResourcesManager.Bracers, ref characterSettings.overrideBracers, onEquipment);
            ChooseSlider("Override Gloves", EquipmentResourcesManager.Gloves, ref characterSettings.overrideGloves, onEquipment);
            ChooseSlider("Override Boots", EquipmentResourcesManager.Boots, ref characterSettings.overrideBoots, onEquipment);
            ChooseSlider("Override Tattoos", EquipmentResourcesManager.Tattoos, ref characterSettings.overrideTattoo, onEquipment);
            GUILayout.Label("Weapons", "box", GUILayout.Width(DefaultLabelWidth));
            foreach (var kv in EquipmentResourcesManager.Weapons)
            {
                var animationStyle = kv.Key;
                var weaponLookup = kv.Value;
                characterSettings.overrideWeapons.TryGetValue(animationStyle, out string currentValue);
                void onWeapon()
                {
                    characterSettings.overrideWeapons[animationStyle] = currentValue;
                    unitEntityData.View.HandsEquipment.UpdateAll();
                }
                ChooseSlider($"Override {animationStyle} ", weaponLookup, ref currentValue, onWeapon);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Main Weapon Enchantments", "box", GUILayout.Width(DefaultLabelWidth));
            if (GUILayout.Button("Add Enchantment", GUILayout.ExpandWidth(false)))
            {
                characterSettings.overrideMainWeaponEnchantments.Add("");
            }
            GUILayout.EndHorizontal();
            void onWeaponEnchantment()
            {
                unitEntityData.View.HandsEquipment.UpdateAll();
            }
            for (int i = 0; i < characterSettings.overrideMainWeaponEnchantments.Count; i++) {
                ChooseSliderList($"Override Main Hand", EquipmentResourcesManager.WeaponEnchantments, 
                    characterSettings.overrideMainWeaponEnchantments, i, onWeaponEnchantment);
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Offhand Weapon Enchantments", "box", GUILayout.Width(DefaultLabelWidth));
            if (GUILayout.Button("Add Enchantment", GUILayout.ExpandWidth(false)))
            {
                characterSettings.overrideOffhandWeaponEnchantments.Add("");
            }
            GUILayout.EndHorizontal();
            for (int i = 0; i < characterSettings.overrideOffhandWeaponEnchantments.Count; i++)
            {
                ChooseSliderList($"Override Off Hand", EquipmentResourcesManager.WeaponEnchantments,
                    characterSettings.overrideOffhandWeaponEnchantments, i, onWeaponEnchantment);
            }
            GUILayout.Label("View", "box", GUILayout.Width(DefaultLabelWidth));
            ChooseSlider("Override View", EquipmentResourcesManager.Units, ref characterSettings.overrideView, onView);
            void onChooseScale()
            {
                Traverse.Create(unitEntityData.View).Field("m_Scale").SetValue(unitEntityData.View.GetSizeScale() + 0.01f);
            }
            GUILayout.Label("Scale", "box", GUILayout.Width(DefaultLabelWidth));
            GUILayout.BeginHorizontal();
            ChooseToggle("Enable Override Scale", ref characterSettings.overrideScale, onChooseScale);
            ChooseToggle("Restrict to polymorph", ref characterSettings.overrideScaleShapeshiftOnly, onChooseScale);
            ChooseToggle("Use Additive Factor", ref characterSettings.overrideScaleAdditive, onChooseScale);
            ChooseToggle("Use Cheat Mode", ref characterSettings.overrideScaleCheatMode, onChooseScale);
            GUILayout.EndHorizontal();
            if (characterSettings.overrideScale && characterSettings.overrideScaleAdditive) ChooseSizeAdditive(unitEntityData, characterSettings);
            if (characterSettings.overrideScale && !characterSettings.overrideScaleAdditive) ChooseSizeOverride(unitEntityData, characterSettings);
        }
    }

}
