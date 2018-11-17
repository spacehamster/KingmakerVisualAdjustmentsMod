using Harmony12;
using Kingmaker.View.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace VisualAdjustments
{
    class HandsEquipmentManager
    {
    }
    [HarmonyPatch(typeof(UnitViewHandsEquipment), "UpdateVisibility")]
    static class UnitViewHandsEquipment_UpdateVisibility_Patch
    {
        static void Postfix(UnitViewHandsEquipment __instance)
        {
            if (!Main.enabled) return;
            if (!__instance.Owner.IsPlayerFaction) return;
            Settings.CharacterSettings characterSettings = Main.settings.characterSettings.FirstOrDefault((cs) => cs.characterName == __instance.Owner.CharacterName);
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
            if (!Main.enabled) return;
            if (!__instance.Owner.IsPlayerFaction) return;
            Settings.CharacterSettings characterSettings = Main.settings.characterSettings.FirstOrDefault((cs) => cs.characterName == __instance.Owner.CharacterName);
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
}
