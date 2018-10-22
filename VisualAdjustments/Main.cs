using UnityEngine;
using Harmony12;
using UnityModManagerNet;
using Kingmaker.Blueprints;
using System.Reflection;
using System;
using Debug = System.Diagnostics.Debug;
using System.Diagnostics;
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
using Kingmaker.Blueprints.Root;

namespace VisualAdjustments
{

    public class Main
    {
        public static UnityModManager.ModEntry.ModLogger logger;
        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugLog(string msg)
        {
            Debug.WriteLine(nameof(VisualAdjustments) + ": " + msg);
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
        static DollManager dollManager = new DollManager();
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                Debug.Listeners.Add(new TextWriterTraceListener("Mods/VisualAdjustments/VisualAdjustments.log"));
                Debug.AutoFlush = true;
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
                modEntry.Logger.Log("Loaded VisualAdjustments");
                logger = modEntry.Logger;
            }
            catch (Exception e){
                modEntry.Logger.Log(e.ToString());
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

                    GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                    GUILayout.Label(string.Format("{0}", unitEntityData.CharacterName), "box", GUILayout.Width(300f));
                    if (unitEntityData.Descriptor.Doll != null)
                    {
                        characterSettings.showClassSelection = GUILayout.Toggle(characterSettings.showClassSelection, "Select Outfit", GUILayout.ExpandWidth(false));
                        characterSettings.showDollSelection = GUILayout.Toggle(characterSettings.showDollSelection, "Select Doll", GUILayout.ExpandWidth(false));
                        characterSettings.hideCap = GUILayout.Toggle(characterSettings.hideCap, "Hide Cap", GUILayout.ExpandWidth(false));
                    }
                    characterSettings.hideBackpack = GUILayout.Toggle(characterSettings.hideBackpack, "Hide Backpack", GUILayout.ExpandWidth(false));
                    characterSettings.hideHelmet = GUILayout.Toggle(characterSettings.hideHelmet, "Hide Helmet", GUILayout.ExpandWidth(false));
                    characterSettings.hideCloak = GUILayout.Toggle(characterSettings.hideCloak, "Hide Cloak", GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                    if (unitEntityData.Descriptor.Doll != null && characterSettings.showClassSelection)
                    {
                        GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                        foreach (var _class in classes)
                        {
                            if (GUILayout.Button(_class, Array.Empty<GUILayoutOption>()))
                            {
                                characterSettings.classOutfit = _class;
                                characterSettings.showClassSelection = false;
                                unitEntityData.View.UpdateClassEquipment();
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (unitEntityData.Descriptor.Doll != null && characterSettings.showDollSelection)
                    {
                        ChooseDoll(unitEntityData);
                    }
                }
            } catch(Exception e)
            {
                DebugLog(e.ToString() + " " + e.StackTrace);
            }
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
                UpdateDoll(unitEntityData);
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
                UpdateDoll(unitEntityData);
            }
        }
        static void ChooseDoll(UnitEntityData unitEntityData)
        {
            var doll = dollManager.GetDoll(unitEntityData);
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
            //ChooseEELRamp(unitEntityData, doll, (new int[] { 0, 1 }).ToList(), doll.LeftHanded ? 1 : 0, "Left Handed", (int value) => doll.SetLeftHanded(value > 0));
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
        static void UpdateDoll(UnitEntityData unitEntityData)
        {
            var character = unitEntityData.View.CharacterAvatar;
            var doll = unitEntityData.Descriptor.Doll;
            var savedEquipment = true;
            character.RemoveAllEquipmentEntities(savedEquipment);
            if (doll.RacePreset != null)
            {
                character.Skeleton = ((doll.Gender != Gender.Male)) ? doll.RacePreset.FemaleSkeleton : doll.RacePreset.MaleSkeleton;
                character.AddEquipmentEntities(doll.RacePreset.Skin.Load(doll.Gender, doll.RacePreset.RaceId), savedEquipment);
            }
            character.Mirror = doll.LeftHanded;
            foreach(string assetID in doll.EquipmentEntityIds)
            {
                EquipmentEntity ee = ResourcesLibrary.TryGetResource<EquipmentEntity>(assetID);
                character.AddEquipmentEntity(ee, savedEquipment);
            }
            doll.ApplyRampIndices(character);
            Traverse.Create(unitEntityData.View).Field("m_EquipmentClass").SetValue(null); //UpdateClassEquipment won't update if the class doesn't change
            unitEntityData.View.UpdateBodyEquipmentModel();
            unitEntityData.View.UpdateClassEquipment();
        }
        static void UpdateModel(UnitEntityView __instance)
        {
            if (__instance.CharacterAvatar == null) return;
            if (!__instance.EntityData.IsPlayerFaction) return;
            Settings.CharacterSettings characterSettings = settings.characterSettings.FirstOrDefault((cs) => cs.characterName == __instance.EntityData.CharacterName);
            if (characterSettings == null) return;            
            bool dirty = __instance.CharacterAvatar.IsDirty;
            if (characterSettings.hideBackpack)
            {
                foreach (var ee in __instance.CharacterAvatar.EquipmentEntities)
                {
                    for (int j = ee.OutfitParts.Count - 1; j >= 0; j--)
                    {
                        var outfit = ee.OutfitParts[j];
                        if (outfit.Special == EquipmentEntity.OutfitPartSpecialType.Backpack)
                        {
                            ee.OutfitParts.Remove(outfit);
                            dirty = true;
                        }
                    }
                }
            }
            if (characterSettings.hideHelmet)
            {
                var helmetEE = __instance.ExtractEquipmentEntities(__instance.EntityData.Body.Head).ToList();
                if (helmetEE.Count > 0)
                {
                    __instance.CharacterAvatar.RemoveEquipmentEntities(helmetEE);
                    dirty = true;
                }
            }
            if (characterSettings.hideCloak)
            {
                foreach (var ee in __instance.CharacterAvatar.EquipmentEntities)
                {
                    for (int j = ee.OutfitParts.Count - 1; j >= 0; j--)
                    {
                        var outfit = ee.OutfitParts[j];
                        if (outfit.Special == EquipmentEntity.OutfitPartSpecialType.Cloak || outfit.Special == EquipmentEntity.OutfitPartSpecialType.CloakSquashed)
                        {
                            ee.OutfitParts.Remove(outfit);
                            dirty = true;
                        }
                    }
                }
            }
            if (characterSettings.hideCap)
            {
                foreach (var ee in __instance.CharacterAvatar.EquipmentEntities)
                {
                    for (int j = ee.BodyParts.Count - 1; j >= 0; j--)
                    {
                        var bodypart = ee.BodyParts[j];
                        if (bodypart.Type == BodyPartType.Cap)
                        {
                            ee.BodyParts.Remove(bodypart);
                            dirty = true;
                        }
                    }
                    ee.HideBodyParts &= ~(BodyPartType.Ears | BodyPartType.Hair | BodyPartType.Hair2 | BodyPartType.HeadTop); //Show ears, hair, headtop
                }
            }
            FixColors(__instance);
            __instance.CharacterAvatar.IsDirty = dirty;                
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
