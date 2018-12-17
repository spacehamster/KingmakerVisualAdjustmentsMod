using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.View;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VisualAdjustments
{
    class ViewManager
    {
        static UnitEntityView GetView(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return ResourcesLibrary.TryGetResource<UnitEntityView>(id);
        }
        public static void ReplaceView(UnitEntityData unit, string id)
        {
            var original = unit.View;
            foreach (Buff buff in unit.Buffs)
            {
                buff.ClearParticleEffect();
            }
            UnitEntityView template = GetView(id);
            if (template == null) template = unit.Blueprint.Prefab.Load();
            var instance = UnityEngine.Object.Instantiate<UnitEntityView>(template).GetComponent<UnitEntityView>();
            instance.UniqueId = unit.UniqueId;
            instance.transform.SetParent(original.transform.parent);
            instance.transform.position = original.transform.position;
            instance.transform.rotation = original.transform.rotation;
            if (id != null && id != "") instance.DisableSizeScaling = true;
            instance.Blueprint = unit.Blueprint;
            unit.AttachToViewOnLoad(instance);
            unit.Commands.InterruptAll((UnitCommand cmd) => !(cmd is UnitMoveTo));
            SelectionManager selectionManager = Game.Instance.UI.SelectionManager;
            if (selectionManager != null)
            {
                selectionManager.ForceCreateMarks();
            }
            UnityEngine.Object.Destroy(original.gameObject);
        }

        [HarmonyPatch(typeof(UnitEntityData), "CreateView")]
        static class UnitEntityData_CreateView_Patch
        {
            static bool Prefix(UnitEntityData __instance, ref UnitEntityView __result)
            {
                try
                {
                    if (!Main.enabled) return true;
                    if (!__instance.IsPlayerFaction) return true;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance);
                    if (characterSettings == null) return true;
                    if (characterSettings.overrideView == null || characterSettings.overrideView == "") return true;
                    foreach (Fact fact in __instance.Buffs.RawFacts)
                    {
                        if (fact.Active && !fact.Deactivating)
                        {
                            Buff buff = (Buff)fact;
                            if (buff.Get<Polymorph>() != null)
                            {
                                return true;
                            }
                        }
                    }
                    UnitEntityView template = GetView(characterSettings.overrideView);
                    if (template == null)
                    {
                        Main.DebugLog("Overriding invalid view " + characterSettings.overrideView);
                        return true;
                    }
                    Quaternion rotation = (!template.ForbidRotation) ? Quaternion.Euler(0f, __instance.Orientation, 0f) : Quaternion.identity;
                    __result = UnityEngine.Object.Instantiate<UnitEntityView>(template, __instance.Position, rotation);
                    return false;
                } catch(Exception ex)
                {
                    Main.DebugError(ex);
                    return true;
                }
            }
        }
        [HarmonyPatch(typeof(UnitEntityView), "GetSizeScale")]
        static class UnitEntityView_GetSizeScale_Patch
        {
            static void Postfix(UnitEntityView __instance, ref float __result)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.EntityData.IsPlayerFaction) return;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.EntityData);
                    if (characterSettings == null) return;
                    if (!characterSettings.overrideScale || !characterSettings.overrideScaleCheatMode) return;
                    if (characterSettings.overrideScaleShapeshiftOnly &&
                        !__instance.EntityData.Body.IsPolymorphed)
                    {
                        return;
                    }
                    int sizeDiff = 0;
                    if (characterSettings.overrideScaleAdditive) sizeDiff = __instance.EntityData.Descriptor.State.Size + characterSettings.additiveScaleFactor - __instance.EntityData.Descriptor.OriginalSize;
                    else sizeDiff = characterSettings.overrideScaleFactor - (int)__instance.EntityData.Descriptor.OriginalSize;
                    var newScaleFactor = Math.Pow(1 / 0.66, sizeDiff);
                    __result = (float)newScaleFactor;
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }
        [HarmonyPatch(typeof(UnitEntityView), "GetSpeedAnimationCoeff")]
        static class UnitEntityView_GetSpeedAnimationCoeff_Patch
        {
            static void Postfix(UnitEntityView __instance, ref float __result)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.EntityData.IsPlayerFaction) return;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.EntityData);
                    if (characterSettings == null) return;
                    if (!characterSettings.overrideScale || characterSettings.overrideScaleCheatMode) return;
                    if (characterSettings.overrideScaleShapeshiftOnly &&
                        !__instance.EntityData.Body.IsPolymorphed)
                    {
                        return;
                    }
                    __result *= __instance.GetSizeScale();
                    var sizeDiff = 0;
                    if (characterSettings.overrideScaleAdditive) sizeDiff = __instance.EntityData.Descriptor.State.Size + characterSettings.additiveScaleFactor - __instance.EntityData.Descriptor.OriginalSize;
                    else sizeDiff = characterSettings.overrideScaleFactor - (int)__instance.EntityData.Descriptor.OriginalSize;
                    var newScaleFactor = Math.Pow(1 / 0.66, sizeDiff);
                    __result /= (float)newScaleFactor;
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }
        [HarmonyPatch(typeof(UnitEntityView), "LateUpdate")]
        static class UnitEntityView_LateUpdate_Patch
        {
            static void Postfix(UnitEntityView __instance)
            {
                try
                {
                    if (!Main.enabled) return;
                    if (!__instance.EntityData.IsPlayerFaction) return;
                    var characterSettings = Main.settings.GetCharacterSettings(__instance.EntityData);
                    if (characterSettings == null) return;
                    if (!characterSettings.overrideScale || characterSettings.overrideScaleCheatMode) return;
                    if (characterSettings.overrideScaleShapeshiftOnly &&
                        !__instance.EntityData.Body.IsPolymorphed)
                    {
                        return;
                    }
                    var originalScale = __instance.GetSizeScale();
                    float sizeScale = 1;
                    if (characterSettings.overrideScaleAdditive) sizeScale = originalScale * (float)Math.Pow(1 / 0.66, characterSettings.additiveScaleFactor);
                    else sizeScale = (float)Math.Pow(1 / 0.66, characterSettings.overrideScaleFactor - (int)__instance.EntityData.Descriptor.OriginalSize);
                    var m_OriginalScale = Traverse.Create(__instance).Field("m_OriginalScale").GetValue<Vector3>();
                    var m_Scale = __instance.transform.localScale.x / m_OriginalScale.x;
                    if (!sizeScale.Equals(m_Scale) && !__instance.DoNotAdjustScale)
                    {
                        float scaleDelta = sizeScale - m_Scale;
                        float deltaTime = Game.Instance.TimeController.DeltaTime;
                        float scaleStep = scaleDelta * deltaTime * 2f;
                        m_Scale = (scaleDelta <= 0f) ? Math.Max(sizeScale, m_Scale + scaleStep) : Math.Min(sizeScale, m_Scale + scaleStep);
                        __instance.transform.localScale = m_OriginalScale * m_Scale;
                    }
                    if (__instance.ParticlesSnapMap)
                    {
                        //Is this necessary?
                        __instance.ParticlesSnapMap.AdditionalScale = __instance.transform.localScale.x / m_OriginalScale.x;
                    }
                    //Prevent fighting m_Scale to set transform scale
                    Traverse.Create(__instance).Field("m_Scale").SetValue(__instance.GetSizeScale());
                }
                catch (Exception ex)
                {
                    Main.DebugError(ex);
                }
            }
        }
    }
}
