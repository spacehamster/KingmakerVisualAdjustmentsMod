using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;

namespace VisualAdjustments
{
    public class Settings : UnityModManager.ModSettings
    {
        public class CharacterSettings
        {
            public string characterName = "";
            public bool showClassSelection = false;
            public bool showColorSelection = false;
            public bool hideHelmet = false;
            public bool hideBackpack = false;
            public bool hideCap = false;
            public bool hideCloak = false;
            public string classOutfit = "Default";
        }
        public List<CharacterSettings> characterSettings = new List<CharacterSettings>();
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
