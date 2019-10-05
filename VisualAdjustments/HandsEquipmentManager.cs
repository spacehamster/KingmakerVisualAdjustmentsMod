using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.View.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VisualAdjustments
{
    class HandsEquipmentManager
    {
        [HarmonyPatch(typeof(UnitViewHandsEquipment), "UpdateVisibility")]
        static class UnitViewHandsEquipment_UpdateVisibility_Patch
        {
            static void Postfix(UnitViewHandsEquipment __instance)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.Owner.IsPlayerFaction) return;
                    Settings.CharacterSettings characterSettings = Main.settings.GetCharacterSettings(__instance.Owner);
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
                catch (Exception ex)
                {
                    Main.Error(ex);
                }

            }
        }
        /*
         * Hide Belt Slots and fix belt item scale
         * 
         */
        [HarmonyPatch(typeof(UnitViewHandsEquipment), "UpdateBeltPrefabs")]
        static class UnitViewHandsEquipment_UpdateBeltPrefabs_Patch
        {
            static void Postfix(UnitViewHandsEquipment __instance, GameObject[] ___m_ConsumableSlots)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.Owner.IsPlayerFaction) return;
                    Settings.CharacterSettings characterSettings = Main.settings.GetCharacterSettings(__instance.Owner);
                    if (characterSettings == null) return;
                    if (characterSettings.hideBeltSlots)
                    {
                        foreach (var go in ___m_ConsumableSlots) go?.SetActive(false);
                    }
                    if (characterSettings.overrideScale && !__instance.Character.PeacefulMode)
                    {
                        foreach (var go in ___m_ConsumableSlots)
                        {
                            if (go == null) continue;
                            go.transform.localScale *= ViewManager.GetRealSizeScale(__instance.Owner.View, characterSettings);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Main.Error(ex);
                }
            }
        }
        /*
         * Hide Quiver
         */ 
        [HarmonyPatch(typeof(UnitViewHandSlotData), "ReattachSheath")]
        static class UnitViewHandsSlotData_ReattachSheath_Patch
        {
            static FastInvoker<UnitViewHandSlotData, object> DestroySheathModelInvoker = null;
            static bool Prepare()
            {
                DestroySheathModelInvoker = Accessors.CreateInvoker<UnitViewHandSlotData, object>("DestroySheathModel");
                return true;
            }
            static bool HasQuiver(UnitViewHandSlotData slotData)
            {
                if (slotData.VisibleItem == null) return false;
                var blueprint = slotData.VisibleItem.Blueprint as BlueprintItemEquipmentHand;
                if (blueprint == null) return false;
                return blueprint.VisualParameters.HasQuiver;
            }
            static bool Prefix(UnitViewHandSlotData __instance, UnitViewHandsEquipment ___m_Equipment)
            {
                try
                {
                    if (!Main.enabled) return true;
                    if (!__instance.Owner.IsPlayerFaction) return true;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner);
                    if (characterSettings == null) return true;
                    if (!HasQuiver(__instance)) return true;
                    if (characterSettings.hideQuiver)
                    {
                        UnitViewHandSlotData unitViewHandSlotData = ___m_Equipment.QuiverHandSlot;
                        if (unitViewHandSlotData == null) return true;
                        if (unitViewHandSlotData == __instance) return false;
                        if(unitViewHandSlotData.IsActiveSet || unitViewHandSlotData.SheathVisualModel == null || !HasQuiver(unitViewHandSlotData))
                        {
                            DestroySheathModelInvoker(unitViewHandSlotData);
                            unitViewHandSlotData = null;
                        }
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Main.Error(ex);
                }
                return true;
            }
        }
        /*
         * Hide Weapon Models 
         */
        [HarmonyPatch(typeof(UnitViewHandSlotData), "AttachModel", new Type[] { })]
        static class UnitViewHandsSlotData_AttachModel_Patch
        {
            static void Postfix(UnitViewHandSlotData __instance)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.Owner.IsPlayerFaction) return;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner);
                    if (characterSettings == null) return;
                    if (characterSettings.hideWeapons)
                    {
                        if (!__instance.IsActiveSet)
                        {
                            __instance.ShowItem(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Main.Error(ex);
                }
            }
        }
        /*
         * Override Weapon Model
         * 
         */
        [HarmonyPatch(typeof(UnitViewHandSlotData), "VisibleItemBlueprint", MethodType.Getter)]
        static class UnitViewHandsSlotData_VisibleItemBlueprint_Patch
        {
            static void Postfix(UnitViewHandSlotData __instance, ref BlueprintItemEquipmentHand __result)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.Owner.IsPlayerFaction) return;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner);
                    if (characterSettings == null) return;
                    if (__instance.VisibleItem == null) return;
                    var blueprint = __instance.VisibleItem.Blueprint as BlueprintItemEquipmentHand;
                    var animationStyle = blueprint.VisualParameters.AnimStyle.ToString();
                    characterSettings.overrideWeapons.TryGetValue(animationStyle, out BlueprintRef blueprintId);
                    if (blueprintId == null || blueprintId == "") return;
                    var newBlueprint = ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipmentHand>(blueprintId);
                    if (newBlueprint == null) return;
                    __result = newBlueprint;
                }
                catch (Exception ex)
                {
                    Main.Error(ex);
                }
            }
        }
        /*
         * Use real size scaling for weapons
         */
        [HarmonyPatch(typeof(UnitViewHandSlotData), "OwnerWeaponScale", MethodType.Getter)]
        static class UnitViewHandsSlotData_OwnerWeaponScale_Patch
        {
            static void Postfix(UnitViewHandSlotData __instance, ref float __result)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.Owner.IsPlayerFaction) return;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner);
                    if (characterSettings == null) return;
                    if (!characterSettings.overrideScale) return;
                    var realScale = ViewManager.GetRealSizeScale(__instance.Owner.View, characterSettings);
                }
                catch (Exception ex)
                {
                    Main.Error(ex);
                }
            }
        }
    }
}
