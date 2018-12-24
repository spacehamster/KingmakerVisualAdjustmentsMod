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
                } catch(Exception ex)
                {
                    Main.DebugError(ex);
                }

            }
    }
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
                } catch(Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }
        /*
        *Fix sword hover outside sheath bug
        * This bug is caused by the line this.VisualModel.transform.SetParent(transform, true);
        * in UnitViewHandSlotData. SetParent causes the scale of left handed weapons to 
        * flip from (-1, 1, 1) to (1, 1, 1), so when the EquipmentOffsets are applied,
        * They are mirrored from their intended value along the x axis.
        */
        public static void FixSheath(UnitViewHandSlotData __instance)
        {
            if (__instance.VisualModel == null) return;
            if (!__instance.Owner.Descriptor.IsLeftHanded) return;
            var sign = __instance.Owner.Descriptor.IsLeftHanded ? -1 : 1;
            __instance.VisualModel.transform.localScale = new Vector3(
                sign * Mathf.Abs(__instance.VisualModel.transform.localScale.x),
                __instance.VisualModel.transform.localScale.y,
                __instance.VisualModel.transform.localScale.z);
            EquipmentOffsets component = __instance.VisualModel.GetComponent<EquipmentOffsets>();
            if (component)
            {
                var visibleItemBlueprint = __instance.VisibleItem.Blueprint as BlueprintItemEquipmentHand;
                var inCombat = Traverse.Create(__instance).Field("m_Equipment").GetValue<UnitViewHandsEquipment>().InCombat;
                bool isTorch = __instance.IsActiveSet && visibleItemBlueprint && visibleItemBlueprint.VisualParameters.IsTorch && !inCombat;
                var slot = (!__instance.IsInHand && !isTorch) ? __instance.VisualSlot : UnitEquipmentVisualSlotType.None;
                component.Apply(slot, __instance.IsOff || visibleItemBlueprint.VisualParameters.IsBow, __instance.Character, null);
            }
        }
    }
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
                HandsEquipmentManager.FixSheath(__instance);
            } catch(Exception ex)
            {
                Main.DebugError(ex);
            }
        }
    }
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
                characterSettings.overrideWeapons.TryGetValue(animationStyle, out string blueprintId);
                if (blueprintId == null || blueprintId == "") return;
                var newBlueprint = ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipmentHand>(blueprintId);
                if (newBlueprint == null) return;
                __result = newBlueprint;
            } catch (Exception ex)
            {
                Main.DebugError(ex);
            }
        }
    }
}
