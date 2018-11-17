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
using System.Text;
using System.Threading.Tasks;
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
                if (!Main.enabled) return true;
                if (!__instance.IsPlayerFaction) return true; ;
                if (!Main.settingsLookup.ContainsKey(__instance.CharacterName)) return true;
                var characterSettings = Main.settingsLookup[__instance.CharacterName];
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
            }
        }
    }
}
