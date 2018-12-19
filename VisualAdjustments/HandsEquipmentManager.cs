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
        public static void FixSheath(UnitViewHandSlotData __instance)
        {
            if (__instance.SheathVisualModel == null) return;
            if (__instance.VisualModel == null) return;
            if (__instance.Owner.Descriptor.IsLeftHanded)
            {
                return;
                __instance.SheathVisualModel.transform.localPosition = __instance.VisualModel.transform.localPosition;
                //Still sometimes has weapons on wrong side, try right to positive and left negative for both weapon and sheath
                //return;
                var sign = InfoManager.WeaponScale ? 1 : -1;
                var weaponScale = sign * Mathf.Abs(__instance.VisualModel.transform.localScale.x);
                var sheathScale = sign * Mathf.Abs(__instance.SheathVisualModel.transform.localScale.x);
                __instance.VisualModel.transform.localScale = new Vector3(
                    weaponScale,
                    __instance.VisualModel.transform.localScale.y,
                    __instance.VisualModel.transform.localScale.z);
                __instance.SheathVisualModel.transform.localScale = new Vector3(
                    sheathScale,
                    __instance.SheathVisualModel.transform.localScale.y,
                    __instance.SheathVisualModel.transform.localScale.z);
                Main.DebugLog($"Set weapon to {__instance.VisualModel.transform.localScale.x} {__instance.SheathVisualModel.transform.localScale.x}");
            }

        }
    }
    [HarmonyPatch(typeof(UnitViewHandsEquipment), "UpdateVisibility")]
    static class UnitViewHandsEquipment_UpdateVisibility_Patch
    {
        static void Postfix(UnitViewHandsEquipment __instance)
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
                string blueprintId;
                characterSettings.overrideWeapons.TryGetValue(animationStyle, out blueprintId);
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
    /*
     * Fix sword hover outside sheath bug
     * Note: this patch has to be MatchVisuals because something is stomping the transfrom values
     * of VisualModel and SheathVisualModel after RecreateModel returns and before MatchVisuals returns     * 
     */
    [HarmonyPatch(typeof(UnitViewHandSlotData), "MatchVisuals")]
    static class UnitViewHandSlotData_MatchVisuals_Patch
    {
        static void Postfix(UnitViewHandSlotData __instance)
        {
            try
            {
                if (!Main.enabled) return;
                if (!__instance.Owner.IsPlayerFaction) return;
                //HandsEquipmentManager.FixSheath(__instance);
            } catch(Exception ex)
            {
                Main.DebugError(ex);
            }
        }
    }
}
