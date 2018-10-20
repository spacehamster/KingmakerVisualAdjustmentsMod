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

namespace VisualAdjustmentsMod
{

    public class Main
    {
        public static UnityModManager.ModEntry.ModLogger logger;
        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugLog(string msg)
        {
            Debug.WriteLine(nameof(VisualAdjustmentsMod) + ": " + msg);
            if(logger != null) logger.Log(msg);
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
        static Dictionary<string, Settings.CharacterSettings> settingsLookup = new Dictionary<string, Settings.CharacterSettings>();
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                Debug.Listeners.Add(new TextWriterTraceListener("Mods/VisualAdjustmentsMod/VisualAdjustmentsMod.log"));
                Debug.AutoFlush = true;

                var harmony = HarmonyInstance.Create(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
                foreach(var characterSettings in settings.characterSettings)
                {
                    settingsLookup[characterSettings.characterName] = characterSettings;
                }
                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                modEntry.Logger.Log("Loaded VisualAdjustmentsMod");
                logger = modEntry.Logger;

            }
            catch (Exception e){
                modEntry.Logger.Log(e.ToString());
            }
            return true;
        }
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    

        // Called when the mod is turned to on/off.
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
        {
            enabled = value;
            return true; // Permit or not.
        }
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (Main.enabled)
            {
                foreach (UnitEntityData unitEntityData in Game.Instance.Player.ControllableCharacters)
                {
                    Settings.CharacterSettings characterSettings;
                    if (settingsLookup.ContainsKey(unitEntityData.CharacterName))
                    {
                        characterSettings = settingsLookup[unitEntityData.CharacterName];
                    } else
                    {
                        characterSettings = new Settings.CharacterSettings();
                        characterSettings.characterName = unitEntityData.CharacterName;
                        settings.characterSettings.Add(characterSettings);
                        settingsLookup[characterSettings.characterName] = characterSettings;
                    }
                   
                    GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                    GUILayout.Label(string.Format("{0}:", unitEntityData.CharacterName), "box", new GUILayoutOption[] { GUILayout.Width(300f) });
                    if(unitEntityData.Descriptor.Doll != null)
                    {
                        characterSettings.showClassSelection = GUILayout.Toggle(characterSettings.showClassSelection, "Select Character Outfit", Array.Empty<GUILayoutOption>());
                    }
                    characterSettings.hideBackpack = GUILayout.Toggle(characterSettings.hideBackpack, "Hide Backpack", Array.Empty<GUILayoutOption>());
                    characterSettings.hideHelmet = GUILayout.Toggle(characterSettings.hideHelmet, "Hide Helmet", Array.Empty<GUILayoutOption>());
                    GUILayout.EndHorizontal();
                    if (unitEntityData.Descriptor.Doll != null && characterSettings.showClassSelection)
                    {
                        foreach (var _class in classes)
                        {
                            if(GUILayout.Button(_class, Array.Empty<GUILayoutOption>()))
                            {
                                characterSettings.classOutfit = _class;
                                characterSettings.showClassSelection = false;
                                unitEntityData.View.UpdateClassEquipment();
                            }
                        }
                    }

                }
            }
        }
        static void UpdateModel(UnitEntityView __instance)
        {
            if (__instance.CharacterAvatar == null) return;
            if (!__instance.EntityData.IsPlayerFaction) return;
            Settings.CharacterSettings characterSettings = settings.characterSettings.FirstOrDefault((cs) => cs.characterName == __instance.EntityData.CharacterName);
            if (characterSettings == null) return;            
            bool dirty = false;
            if (characterSettings.hideBackpack)
            {
                for (int i = __instance.CharacterAvatar.EquipmentEntities.Count - 1; i >= 0; i--)
                {
                    var ee = __instance.CharacterAvatar.EquipmentEntities[i];
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
            __instance.CharacterAvatar.IsDirty = dirty;                
        }
        [HarmonyPatch(typeof(UnitEntityView), "UpdateBodyEquipmentModel")]
        static class UnitEntityView_UpdateBodyEquipmentModel_Patch
        {
            static void Postfix(UnitEntityView __instance)
            {
                UpdateModel(__instance);
            }
        }
        [HarmonyPatch(typeof(UnitEntityView), "HandleEquipmentSlotUpdated")]
        static class UnitEntityView_HandleEquipmentSlotUpdated_Patch
        {
            static void Postfix(UnitEntityView __instance)
            {
                UpdateModel(__instance);
            }
        }
        [HarmonyPatch(typeof(UnitEntityView), "UpdateClassEquipment")]
        static class UnitEntityView_UpdateClassEquipment_Patch
        {
            static void Postfix(UnitEntityView __instance)
            {
                UpdateModel(__instance);
            }
        }
        [HarmonyPatch(typeof(UnitProgressionData), "GetEquipmentClass")]
        static class UnitProgressionData_GetEquipmentClass_Patch
        {
            static bool Prefix(UnitProgressionData __instance, ref BlueprintCharacterClass __result)
            {
                Settings.CharacterSettings characterSettings = settings.characterSettings.FirstOrDefault((cs) => cs.characterName == __instance.Owner.CharacterName);
                if (!__instance.Owner.IsPlayerFaction) return true;
                if (characterSettings == null) return true;
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
                return false;
            }
        }

    }
    
}
