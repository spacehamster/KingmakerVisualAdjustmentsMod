using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.View.Equipment;
using Kingmaker.Visual.Particles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        static GameObject RespawnFx(GameObject prefab, ItemEntity item)
        {
            WeaponSlot weaponSlot = item.HoldingSlot as WeaponSlot;
            var weaponSnap = weaponSlot?.FxSnapMap;
            var unit = item.Wielder.Unit?.View;
            return FxHelper.SpawnFxOnWeapon(prefab, unit, weaponSnap);
        }
        static void DestroyFx(GameObject FxObject)
        {
            if (FxObject)
            {
                FxHelper.Destroy(FxObject);
                FxObject = null;
            }
        }
        [HarmonyPatch(typeof(DollRoom), "UpdateAvatarRenderers")]
        static class DollRoom_UpdateAvatarRenderers_Patch
        {
            static FastInvoker<DollRoom, GameObject, object> UnscaleFxTimes;
            static bool Prepare()
            {
                UnscaleFxTimes = Accessors.CreateInvoker<DollRoom, GameObject, object>("UnscaleFxTimes");
                return true;
            }
            static void Postfix(DollRoom __instance, UnitViewHandsEquipment ___m_AvatarHands, UnitEntityData ___m_Unit)
            {
                try
                {
                    if (___m_Unit == null) return;
                    var characterSettings = Main.settings.GetCharacterSettings(___m_Unit);
                    if (characterSettings == null) return;
                    foreach (var isOffhand in new bool[] { true, false })
                    {
                        WeaponParticlesSnapMap weaponParticlesSnapMap = ___m_AvatarHands?.GetWeaponModel(isOffhand)?.GetComponent<WeaponParticlesSnapMap>();
                        if (weaponParticlesSnapMap)
                        {
                            UnityEngine.Object x = weaponParticlesSnapMap;
                            UnityEngine.Object y = isOffhand ?
                                ___m_Unit?.Body?.SecondaryHand.FxSnapMap :
                                ___m_Unit?.Body?.PrimaryHand.FxSnapMap;
                            if (x == y)
                            {
                                var weapon = isOffhand ?
                                        ___m_Unit?.Body?.SecondaryHand?.MaybeItem :
                                        ___m_Unit?.Body?.PrimaryHand?.MaybeItem;
                                WeaponEnchantments.TryGetValue(weapon, out List<GameObject> fxObjects);
                                if (fxObjects != null)
                                {
                                    foreach (var fxObject in fxObjects)
                                    {
                                        UnscaleFxTimes(__instance, fxObject);
                                    }
                                }
                            }
                        }
                    }
                } catch(Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }
        public static Dictionary<ItemEntity, List<GameObject>> WeaponEnchantments = new Dictionary<ItemEntity, List<GameObject>>();
        [HarmonyPatch(typeof(UnitViewHandSlotData), "UpdateWeaponEnchantmentFx")]
        static class UnitViewHandSlotData_UpdateWeaponEnchantmentFx_Patch
        {
            static void Postfix(UnitViewHandSlotData __instance, bool isVisible, ref List<ItemEnchantment> ___m_VisibleEnchantments, UnitViewHandsEquipment ___m_Equipment)
            {
                try {
                    if (!Main.enabled) return;
                    if (!__instance.Owner.IsPlayerFaction) return;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner);
                    if (characterSettings == null) return;
                    if (__instance.Slot.MaybeItem == null) return;
                    if (WeaponEnchantments.ContainsKey(__instance.Slot.MaybeItem))
                    {
                        foreach (var fxObject in WeaponEnchantments[__instance.Slot.MaybeItem])
                        {
                            DestroyFx(fxObject);
                        }
                        WeaponEnchantments[__instance.Slot.MaybeItem].Clear();
                    }
                    if (__instance.IsInHand && isVisible)
                    {
                        if (!WeaponEnchantments.ContainsKey(__instance.Slot.MaybeItem)) WeaponEnchantments[__instance.Slot.MaybeItem] = new List<GameObject>();
                        var enchantments = __instance.IsOff ?
                            characterSettings.overrideOffhandWeaponEnchantments :
                            characterSettings.overrideMainWeaponEnchantments;
                        foreach (var enchantmentId in enchantments) {
                            var blueprint = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponEnchantment>(enchantmentId);
                            if (blueprint == null || blueprint.WeaponFxPrefab == null) continue;
                            var fxObject = RespawnFx(blueprint.WeaponFxPrefab, __instance.Slot.MaybeItem);
                            WeaponEnchantments[__instance.Slot.MaybeItem].Add(fxObject);
                        }
                    }
                } catch(Exception ex)
                {
                    Main.DebugError(ex);
                }
    }
        }
        [HarmonyPatch(typeof(UnitViewHandSlotData), "DestroyModel")]
        static class UnitViewHandSlotData_DestroyModel_Patch
        {
            static void Postfix(UnitViewHandSlotData __instance, ref List<ItemEnchantment> ___m_VisibleEnchantments, UnitViewHandsEquipment ___m_Equipment)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.Owner.IsPlayerFaction) return;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.Owner);
                    if (characterSettings == null) return;
                    if (__instance.Slot.MaybeItem == null) return;
                    if (!WeaponEnchantments.ContainsKey(__instance.Slot.MaybeItem)) return;
                    foreach (var fxObject in WeaponEnchantments[__instance.Slot.MaybeItem])
                    {
                        DestroyFx(fxObject);
                    }
                    WeaponEnchantments[__instance.Slot.MaybeItem].Clear();
                    WeaponEnchantments.Remove(__instance.Slot.MaybeItem);
                } catch(Exception ex)
                {
                    Main.DebugError(ex);
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
                    if (__instance.Slot.MaybeItem == null) return;
                    if (characterSettings.hideWeaponEnchantments)
                    {
                        ___m_VisibleEnchantments.Clear();
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
