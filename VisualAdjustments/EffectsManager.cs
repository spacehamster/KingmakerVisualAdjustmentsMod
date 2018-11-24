using Harmony12;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualAdjustments
{
    static class EffectsManager
    {
        public static Dictionary<string, string> WingsLookup = new Dictionary<string, string>()
        {
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
        };
        [HarmonyPatch(typeof(UnitEntityData), "SpawnBuffsFxs")]
        static class UnitEntityData_SpawnBuffsFxs_Patch
        {
            static bool Prefix(UnitEntityData __instance)
            {
                try
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
                } catch(Exception ex)
                {
                    Main.DebugError(ex);
                    return true;
                }
            }
        }
        [HarmonyPatch(typeof(ItemEnchantment), "RespawnFx")]
        static class ItemEnchantment_RespawnFx_Patch
        {
            static bool Prefix(ItemEnchantment __instance)
            {
                try
                {
                    if (!Main.enabled) return true;
                    if (!__instance.Owner.Wielder.IsPlayerFaction) return true;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner.Wielder.Unit);
                    if (characterSettings == null) return true;
                    if (!characterSettings.hideWeaponEnchantments) return true;
                    return false;
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                    return true;
                }
            }
        }
    }
}
