using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.View.Equipment;
using Kingmaker.Visual.Particles;
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
        [HarmonyPatch(typeof(Buff), "SpawnParticleEffect")]
        static class Buff_SpawnParticleEffect_Patch
        {
            static bool Prefix(Buff __instance)
            {
                try
                {
                    if (!Main.enabled) return true;
                    if (!__instance.Owner.IsPlayerFaction) return true;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner.Unit);
                    if (characterSettings == null) return true;
                    if (characterSettings.hideWings && WingsLookup.ContainsKey(__instance.Blueprint.AssetGuid))
                    {
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                    return true;
                }
            }
        }
        /*[HarmonyPatch(typeof(ItemEnchantment), "WielderVisibleInInventory", MethodType.Getter)]
        static class ItemEnchantment_RecreateModel_Patch
        {
            static void Postfix(ItemEnchantment __instance, ref bool __result)
            {
                if (!Main.enabled) return;
                __result = true;
            }
        }*/
        static bool IsCustomEnchantment(ItemEnchantment enchantment)
        {
            var owner = enchantment.Owner?.Wielder?.Unit;
            if (owner == null) return false;
            var characterSettings = Main.settings.GetCharacterSettings(owner);
            if (characterSettings == null) return false;
            if (enchantment.Blueprint == null) return false;
            if (characterSettings.overrideMainWeaponEnchantments.Any(id => id == enchantment.Blueprint.AssetGuid)) return true;
            return false;
        }
        static void LogEnchantment(string prefix, ItemEnchantment enchantment)
        {
            if (enchantment.Blueprint.name != "Corrosive" && enchantment.Blueprint.name != "Agile") return;
            var weaponSlot = enchantment.Owner.HoldingSlot as WeaponSlot;
            var unit = enchantment?.Owner?.Wielder?.Unit;
            if (unit == null)
            {
                Main.DebugLog("Unit is null!");
                return;
            }
            if (!unit.IsPlayerFaction)
            {
                Main.DebugLog("Unit is not player faction!");
                return;
            }
            var weapon = enchantment.Owner;
            if (weapon == null)
            {
                Main.DebugLog("Weapon is null!");
                return;
            }
            if (!weapon.Enchantments.Any(x => IsCustomEnchantment(x)))
            {
                //Main.DebugLog("Weapon is not custom enchantment!");
                //return;
            }
            var unitView = enchantment.Owner.Wielder?.Unit?.View;
            var __instance = unitView.HandsEquipment.Sets
                .Select(kv => kv.Value)
                .SelectMany(pair => new UnitViewHandSlotData[] { pair.MainHand, pair.OffHand })
                .Where(h => h.Slot.MaybeItem == weapon)
                .FirstOrDefault();
            if (__instance == null)
            {
                Main.DebugLog("Could not find handslotdata!");
                return;
            }
            var ___m_VisibleEnchantments = Traverse.Create(__instance).Field("m_VisibleEnchantments").GetValue<List<ItemEnchantment>>();
            var ___m_Equipment = Traverse.Create(__instance).Field("m_Equipment").GetValue<UnitViewHandsEquipment>();
            var blueprint = enchantment.Blueprint as BlueprintWeaponEnchantment;
            if (blueprint.WeaponFxPrefab == null) return;
            var visibleInInventory = Traverse.Create(enchantment).Property<bool>("WielderVisibleInInventory").Value;
            var owner = enchantment.Owner;
            var prefab = blueprint.WeaponFxPrefab;

            var slot = enchantment.Owner.HoldingSlot as WeaponSlot;
            var weaponSnap = slot.FxSnapMap;
            WeaponParticlesSnapMap weaponParticlesSnapMap = __instance.VisualModel?.GetComponent<WeaponParticlesSnapMap>();
            Main.DebugLog($"{prefix, 46} enchantment {blueprint?.name,9} to {__instance?.Slot?.MaybeItem?.Name ?? "NULL"}, Active {enchantment.Active}, " +
                $"IsDollRoom {___m_Equipment.IsDollRoom}, VisibleInventory {visibleInInventory}, " +
                $"Owner {owner?.Name ?? "NULL"}({owner?.GetHashCode()}), Prefab {prefab?.name ?? "NULL",22}," +
                $"UnitView {unitView?.name ?? "NULL"}({unitView?.GetInstanceID()}, {unitView?.GetHashCode()}), WeaponSnap {weaponSnap?.name ?? "NULL"}" +
                $"Owner {owner == ___m_VisibleEnchantments[0].Owner} Unit {unitView == ___m_VisibleEnchantments[0].Owner.Wielder?.Unit?.View} " +
                $"ParticleSnapMap {weaponParticlesSnapMap == __instance.Slot.FxSnapMap} FxObject {enchantment.FxObject != null}");
        }
        [HarmonyPatch(typeof(ItemEnchantment), "RespawnFx")]
        static class ItemEnchantment_RespawnFx_Patch
        {
            static void Postfix(ItemEnchantment __instance)
            {
                LogEnchantment("ItemEnchantment.RespawnFx", __instance);
            }
        }
        [HarmonyPatch(typeof(ItemEnchantment), "Activate")]
        static class ItemEnchantment_Activate_Patch
        {
            static void Postfix(ItemEnchantment __instance)
            {
                LogEnchantment("ItemEnchantment.Activate", __instance);
            }
        }
        [HarmonyPatch(typeof(UnitViewHandSlotData), "UpdateWeaponEnchantmentFx")]
        static class UnitViewHandSlotData_UpdateWeaponEnchantmentFx_Patch
        {
            static void Postfix(UnitViewHandSlotData __instance, bool isVisible, ref List<ItemEnchantment> ___m_VisibleEnchantments, UnitViewHandsEquipment ___m_Equipment)
            {
                if (__instance.IsInHand && isVisible)
                {
                    foreach (var enchantment in ___m_VisibleEnchantments)
                    {
                        LogEnchantment("UnitViewHandSlotData.UpdateWeaponEnchantmentFx", enchantment);
                        if (IsCustomEnchantment(enchantment))
                        {
                            Main.DebugLog("'Activate enchantment'");
                            //enchantment.Activate();
                            //enchantment.RespawnFx();
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(UnitViewHandSlotData), "RecreateModel")]
        static class UnitViewHandSlotData_RecreateModel_Patch
        {
            static void Postfix(UnitViewHandSlotData __instance, ref List<ItemEnchantment> ___m_VisibleEnchantments, UnitViewHandsEquipment ___m_Equipment)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.Owner.IsPlayerFaction) return;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner);
                    if (characterSettings == null) return;
                    if (__instance.Slot.MaybeItem == null || __instance.Slot.MaybeItem.Wielder == null) return;
                    if (!__instance.IsOff && characterSettings.overrideMainWeaponEnchantments.Count > 0)
                    {
                        foreach (var enchantment in ___m_VisibleEnchantments)
                        {
                            LogEnchantment("UnitViewHandSlotData.RecreateModel Existing", enchantment);
                        }
                    }
                    if (characterSettings.hideWeaponEnchantments)
                    {
                        ___m_VisibleEnchantments.Clear();
                    }
                    if (!__instance.IsOff)
                    {
                        foreach (var id in characterSettings.overrideMainWeaponEnchantments)
                        {
                            var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponEnchantment>(id);
                            if (blueprint == null) continue;
                            if (!__instance.Slot.MaybeItem.Enchantments.Any(x => x.Blueprint == blueprint))
                            {
                                //var enchantment = __instance.Slot.MaybeItem.AddEnchantment(blueprint, null);
                                var enchantment = new ItemEnchantment(blueprint, __instance.Slot.MaybeItem);
                                AccessTools.Property(typeof(Fact), "Active").SetValue(enchantment, true);
                                enchantment.RespawnFx();
                                ___m_VisibleEnchantments.Add(enchantment);
                                LogEnchantment("UnitViewHandSlotData.RecreateModel New", enchantment);
                            }
                        }
                    }
                    else
                    {
                        foreach (var id in characterSettings.overrideOffhandWeaponEnchantments)
                        {
                            var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponEnchantment>(id);
                            if (blueprint == null) continue;
                            var enchantment = new ItemEnchantment(blueprint, __instance.Slot.MaybeItem);
                            //AccessTools.Property(typeof(Fact), "Active").SetValue(enchantment, true);
                            //Traverse.Create(enchantment).Property<bool>("Active").Value = true;
                            enchantment.Activate();
                            ___m_VisibleEnchantments.Add(enchantment);
                            LogEnchantment("UnitViewHandSlotData.RecreateModel New", enchantment);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }
    }
}
