using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.CharacterSystem;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VisualAdjustments
{
#if DEBUG
    public class ResourceInfo
    {
        //[Harmony12.HarmonyPatch(typeof(ResourcesLibrary), "CleanupLoadedCache")]
        class ResourcesLibrary_CleanupLoadedCache_Patch
        {
            static void Log()
            {
                var loadedResources = Traverse.Create(typeof(ResourcesLibrary)).Field("s_LoadedResources").GetValue<IDictionary>();
                if (loadedResources == null)
                {
                    Main.Log("Can't get s_LoadedResources");
                    return;
                }
                foreach (var key in loadedResources.Keys)
                {
                    var LoadedResource = loadedResources[key];
                    var resource = Traverse.Create(LoadedResource).Field("Resource").GetValue<UnityEngine.Object>();
                    int requestCounter = Traverse.Create(LoadedResource).Field("RequestCounter").GetValue<int>();
                    Main.Log($"Resource {resource?.name ?? "NULL"} RequestCounter {requestCounter} Key {key}");
                }
            }
            static bool Prefix()
            {
                try
                {
                    Main.Log("CleanupLoadedCache.Prefix");
                    Log();
                }
                catch(Exception ex)
                {
                    Main.Error(ex);
                }
                return true;
            }
            static void Postfix()
            {
                try
                {
                    Main.Log("CleanupLoadedCache.Postfix");
                    Log();
                }
                catch (Exception ex)
                {
                    Main.Error(ex);
                }
            }
        }
        static Dictionary<string, string> RequestLookup = new Dictionary<string, string>();
        static string GetRequestCounter(EquipmentEntity obj)
        {
            var loadedResources = Traverse.Create(typeof(ResourcesLibrary)).Field("s_LoadedResources").GetValue<Dictionary<string, object>>();
            if (loadedResources == null)
            {
                throw new Exception("Can't get s_LoadedResources");
            }
            object LoadedResource = null;
            if (RequestLookup.ContainsKey(obj.name))
            {
                LoadedResource = loadedResources[RequestLookup[obj.name]];
            }
            else
            {
                foreach (var kv in loadedResources)
                {
                    var resource = Traverse.Create(kv.Value).Field("Resource").GetValue<UnityEngine.Object>();
                    if (resource == obj)
                    {
                        LoadedResource = kv.Value;
                        RequestLookup[kv.Key] = obj.name;
                        break;
                    }
                    return "NULL";
                }
            }
            int RequestCounter = Traverse.Create(LoadedResource).Field("RequestCounter").GetValue<int>();
            return RequestCounter.ToString();
        }
    }
#endif
}