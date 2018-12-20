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
        /*
         *Fix sword hover outside sheath bug
        * Note: Only setting the sheath position to the weapon position is required to
        * fix the sheath bug, but I have fixed the scale incase there are 
        * weapons with chirality in the game
        */
        public static void FixSheath(UnitViewHandSlotData __instance)
        {
            if (__instance.SheathVisualModel == null) return;
            if (__instance.VisualModel == null) return;
            var sign = __instance.Owner.Descriptor.IsLeftHanded ? -1 : 1;
            __instance.VisualModel.transform.localScale = new Vector3(
                sign * Mathf.Abs(__instance.VisualModel.transform.localScale.x),
                __instance.VisualModel.transform.localScale.y,
                __instance.VisualModel.transform.localScale.z);
            __instance.SheathVisualModel.transform.localPosition = __instance.VisualModel.transform.localPosition;
            __instance.SheathVisualModel.transform.localScale = __instance.VisualModel.transform.localScale;
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
}
