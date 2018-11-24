using Harmony12;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualAdjustments
{
    static class EffectsManager
    {
        public static Dictionary<string, string> WingsLookup = new Dictionary<string, string>()
        {
            //{"25699a90ed3299e438b6fd5548930809", "WingsAngel"},
            //{"a19cda073f4c2b64ca1f8bf8fe285ece", "WingsAngelBlack"},
            //{"4113178a8d5bf4841b8f15b1b39e004f", "WingsDiabolic"},
            //{"775df52784e1d454cba0da8df5f4f59a", "WingsMovanicDeva"},
            {"d596694ff285f3f429528547f441b1c0", "BuffWingsAngel"},
            {"3c958be25ab34dc448569331488bee27", "BuffWingsDemon"},
            {"38431e32f0e210342968d3a997eb233e", "BuffWingsDevil"},
            {"ddfe6e85e1eed7a40aa911280373c228", "BuffWingsDraconicBlack"},
            {"800cde038f9e6304d95365edc60ab0a4", "BuffWingsDraconicBlue"},
            {"7f5acae38fc1e0f4c9325d8a4f4f81fc", "BuffWingsDraconicBrass"},
            {"482ee5d001527204bb86e34240e2ce65", "BuffWingsDraconicBronze"},
            {"a25d6fc69cba80548832afc6c4787379", "BuffWingsDraconicCopper"},
            {"984064a3dd0f25444ad143b8a33d7d92", "BuffWingsDraconicGold"},
            {"a4ccc396e60a00f44907e95bc8bf463f", "BuffWingsDraconicGreen"},
            {"08ae1c01155a2184db869e9ebedc758d", "BuffWingsDraconicRed"},
            {"5a791c1b0bacee3459d7f5137fa0bd5f", "BuffWingsDraconicSilver"},
            {"381a168acd79cd54baf87a17ca861d9b", "BuffWingsDraconicWhite"},
            //{"f78a249bacba9924b9595e52495cb02f", "CraneStyleWingBuff"},
            // {"9b4ab9a8c6d6bf64fb54a46c170ba80e", "SeasonedWingsAndThighsBuffCompanion"},
            //{"b624888e1271b384fa4c70faf6f64912", "SeasonedWingsAndThighsBuff"},
            //{"cca39aeac2e16414f93cc5cd7b62a0aa", "ErinyesDevilWingsBuff"},
        };
        [HarmonyPatch(typeof(UnitEntityData), "SpawnBuffsFxs")]
        static class UnitEntityData_SpawnBuffsFxs_Patch
        {
            static bool Prefix(UnitEntityData __instance)
            {
                if (!Main.enabled) return true;
                if (!__instance.IsPlayerFaction) return true;
                var characterSettings = Main.settings.GetCharacterSettings(__instance);
                if (characterSettings == null) return true;
                foreach (var buff in __instance.Buffs)
                {
                    buff.ClearParticleEffect();
                    if (characterSettings.hideWings && WingsLookup.ContainsKey(buff.Blueprint.AssetGuid))
                    {
                        Main.DebugLog($"Hiding {buff.Blueprint.name}");
                        continue;
                    }
                    buff.SpawnParticleEffect();
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(ItemEnchantment), "RespawnFx")]
        static class ItemEnchantment_RespawnFx_Patch
        {
            static bool Prefix(ItemEnchantment __instance)
            {
                if (!Main.enabled) return true;
                if (!__instance.Owner.Owner.IsPlayerFaction) return true;
                var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner.Owner.Unit);
                if (characterSettings == null) return true;
                if (!characterSettings.hideWeaponEnchantments) return true;
                return false;
            }
        }
    }
}
