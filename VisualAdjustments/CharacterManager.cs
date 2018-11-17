using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.View;
using Kingmaker.Visual.CharacterSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static VisualAdjustments.Settings;

namespace VisualAdjustments
{
    class CharacterManager
    {
        public static bool disableEquipmentClassPatch;
        static int GetPrimaryColor(UnitEntityData unitEntityData)
        {
            if (unitEntityData.Descriptor.Doll == null) return -1;
            var doll = unitEntityData.Descriptor.Doll;
            foreach (var assetId in doll.EntitySecondaryRampIdices.Keys.ToList())
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
            foreach (var ee in clothes)
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
            }
            else
            {
                UnitEntityView viewTemplate = (!string.IsNullOrEmpty(unitEntityData.Descriptor.CustomPrefabGuid)) ?
                    ResourcesLibrary.TryGetResource<UnitEntityView>(unitEntityData.Descriptor.CustomPrefabGuid) :
                    unitEntityData.Blueprint.Prefab.Load();
                var characterBase = viewTemplate.GetComponentInChildren<Character>();
                character.CopyEquipmentFrom(characterBase);
                //Note UpdateBodyEquipmentModel does nothing for baked characters
                IEnumerable<EquipmentEntity> ees = unitEntityData.Body.AllSlots.SelectMany(
                    new Func<ItemSlot, IEnumerable<EquipmentEntity>>(unitEntityData.View.ExtractEquipmentEntities));
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
            foreach (var ee in ees)
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
        static void FixRangerCloak(UnitEntityView view)
        {
            foreach(var ee in view.CharacterAvatar.EquipmentEntities)
            {
                if(ee.name == "EE_Ranger_M_Cape")
                {
                    ee.HideBodyParts &= ~(BodyPartType.Hair | BodyPartType.Hair2);
                }
            }
        }
        public static void UpdateModel(UnitEntityView view)
        {
            if (view.CharacterAvatar == null) return;
            if (!view.EntityData.IsPlayerFaction) return;
            Settings.CharacterSettings characterSettings = Main.settings.characterSettings.FirstOrDefault((cs) => cs.characterName == view.EntityData.CharacterName);
            if (characterSettings == null) return;
            bool dirty = view.CharacterAvatar.IsDirty;
            if (view.EntityData.Descriptor.Doll == null && characterSettings.classOutfit != "Default")
            {
                ChangeCompanionOutfit(view, characterSettings);
            }
            if (characterSettings.hideHelmet)
            {
                HideSlot(view, view.EntityData.Body.Head, ref dirty);
            }
            if (characterSettings.hideItemCloak)
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
            if (characterSettings.overrideHelm != "" && !characterSettings.hideHelmet)
            {
                if (!OverrideEquipment(view, view.EntityData.Body.Head, characterSettings.overrideHelm, ref dirty))
                {
                    characterSettings.overrideHelm = "";
                }
            }
            if (characterSettings.overrideCloak != "" && !characterSettings.hideItemCloak)
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
                    if (ee.OutfitParts.Exists((outfit) => outfit.Special == EquipmentEntity.OutfitPartSpecialType.Backpack))
                    {
                        view.CharacterAvatar.EquipmentEntities.Remove(ee);
                        dirty = true;
                    }
                }
            }
            if (characterSettings.hideClassCloak)
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
            if (view.EntityData.Descriptor.Progression.GetEquipmentClass().Name == "Ranger")
            {
                FixRangerCloak(view);
            }
            if (view.EntityData.Descriptor.Doll != null) FixColors(view);
            view.CharacterAvatar.IsDirty = dirty;
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
                if (!Main.enabled) return;
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
                if (!Main.enabled) return;
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
                if (!Main.enabled) return;
                UpdateModel(__instance);
            }
        }
        [HarmonyPatch(typeof(UnitProgressionData), "GetEquipmentClass")]
        static class UnitProgressionData_GetEquipmentClass_Patch
        {
            static bool Prefix(UnitProgressionData __instance, ref BlueprintCharacterClass __result)
            {
                if (!Main.enabled) return true;
                if (disableEquipmentClassPatch) return true;
                if (!__instance.Owner.IsPlayerFaction) return true;
                if (!Main.settingsLookup.ContainsKey(__instance.Owner.CharacterName)) return true;
                Settings.CharacterSettings characterSettings = Main.settingsLookup[__instance.Owner.CharacterName];
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
