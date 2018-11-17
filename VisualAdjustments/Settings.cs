using System.Collections.Generic;
using UnityModManagerNet;

namespace VisualAdjustments
{
    public class Settings : UnityModManager.ModSettings
    {
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

            public string overrideHelm = "";
            public string overrideCloak = "";
            public string overrideArmor = "";
            public string overrideBracers = "";
            public string overrideGloves = "";
            public string overrideBoots = "";
            public string overrideView = "";

            public bool hideWeapons = false;
#if (DEBUG)
            public bool showInfo = false;
#endif
            public string classOutfit = "Default";
            public int companionPrimary = 0;
            public int companionSecondary = 0;

        }
        public List<CharacterSettings> characterSettings = new List<CharacterSettings>();
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
