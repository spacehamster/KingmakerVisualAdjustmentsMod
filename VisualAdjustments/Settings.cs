using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;

namespace VisualAdjustments
{
    public class Settings : UnityModManager.ModSettings
    {
        internal bool rebuildCharacters = false;
        public class CharacterSettings
        {
            public string characterName = "";
            public bool showClassSelection = false;
            public bool showDollSelection = false;
            public bool showEquipmentSelection = false;
            public bool showOverrideSelection = false;
            public bool hideBackpack = false;
            public bool hideCap = false;
            public bool hideClassCloak = false;
            public bool hideHelmet = false;
            public bool hideItemCloak = false;
            public bool hideArmor = false;
            public bool hideBracers = false;
            public bool hideGloves = false;
            public bool hideBoots = false;
            public bool hideWings = false;
            public bool hideWeaponEnchantments = false;
            public bool hideTail;
            public bool hideHorns;

            public string overrideHelm = "";
            public string overrideCloak = "";
            public string overrideArmor = "";
            public string overrideBracers = "";
            public string overrideGloves = "";
            public string overrideBoots = "";
            public string overrideTattoo = "";
            public string overrideView = "";
            public List<string> overrideMainWeaponEnchantments = new List<string>();
            public List<string> overrideOffhandWeaponEnchantments = new List<string>();
            public bool overrideScale = false;
            public bool overrideScaleShapeshiftOnly = false;
            public bool overrideScaleAdditive = false;
            public bool overrideScaleCheatMode = false;
            public bool overrideScaleFloatMode = false;
            public float overrideScaleFactor = 4;
            public float additiveScaleFactor = 0;
            public Dictionary<string, string> overrideWeapons = new Dictionary<string, string>();

            public bool hideWeapons = false;
            public bool hideBeltSlots = false;
#if (DEBUG)
            public bool showInfo = false;
#endif
            public string classOutfit = "Default";
            public int companionPrimary = -1;
            public int companionSecondary = -1;
        }
        [JsonProperty]
        private Dictionary<string, CharacterSettings> characterSettings = new Dictionary<string, CharacterSettings>();
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            var filepath = Path.Combine(modEntry.Path, "Settings.json");
            try
            {
                JsonSerializer serializer = new JsonSerializer();
#if (DEBUG)
                serializer.Formatting = Formatting.Indented;
#endif
                using (StreamWriter sw = new StreamWriter(filepath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex)
            {
                modEntry.Logger.Error($"Can't save {filepath}.");
                modEntry.Logger.Error(ex.ToString());
            }
        }
        public CharacterSettings GetCharacterSettings(UnitEntityData unitEntityData)
        {
            characterSettings.TryGetValue(unitEntityData.UniqueId, out CharacterSettings result);
            return result;
        }
        public void AddCharacterSettings(UnitEntityData unitEntityData, CharacterSettings newSettings)
        {
            characterSettings[unitEntityData.UniqueId] = newSettings;
        }
        public static Settings Load(ModEntry modEntry)
        {
            var filepath = Path.Combine(modEntry.Path, "Settings.json");
            if (File.Exists(filepath))
            {
                try
                {
                    JsonSerializer serializer = new JsonSerializer();
                    using (StreamReader sr = new StreamReader(filepath))
                    using (JsonTextReader reader = new JsonTextReader(sr))
                    {
                        Settings result = serializer.Deserialize<Settings>(reader);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    modEntry.Logger.Error($"Can't read {filepath}.");
                    modEntry.Logger.Error(ex.ToString());
                }
            }
            return new Settings();
        }
    }
}
