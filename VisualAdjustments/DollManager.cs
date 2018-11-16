using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Visual.Sound;
using System.Collections.Generic;

namespace VisualAdjustments
{
    class DollManager
    {
        static private Dictionary<string, EquipmentEntityLink> head = new Dictionary<string, EquipmentEntityLink>();
        static private Dictionary<string, EquipmentEntityLink> hair = new Dictionary<string, EquipmentEntityLink>();
        static private Dictionary<string, EquipmentEntityLink> beard = new Dictionary<string, EquipmentEntityLink>();
        static private Dictionary<string, EquipmentEntityLink> eyebrows = new Dictionary<string, EquipmentEntityLink>();
        static private Dictionary<string, EquipmentEntityLink> skin = new Dictionary<string, EquipmentEntityLink>();
        static private Dictionary<string, EquipmentEntityLink> classOutfits = new Dictionary<string, EquipmentEntityLink>();
        static private SortedList<string, BlueprintPortrait> portraits = new SortedList<string, BlueprintPortrait>();
        static private SortedList<string, BlueprintUnitAsksList> asks = new SortedList<string, BlueprintUnitAsksList>();
        static private Dictionary<string, DollState> characterDolls = new Dictionary<string, DollState>();
        static public SortedList<string, BlueprintPortrait> Portrait
        {
            get
            {
                if (!loaded) Init();
                return portraits;
            }
        }
        static public SortedList<string, BlueprintUnitAsksList> Asks
        {
            get
            {
                if(!loaded) Init();
                return asks;
            }
        }
        static private bool loaded = false;
        static private void AddLinks(Dictionary<string, EquipmentEntityLink> dict, EquipmentEntityLink[] links)
        {
            foreach (var eel in links)
            {
                dict[eel.AssetId] = eel;
            }
        }
        static private void Init()
        {
            var races = ResourcesLibrary.GetBlueprints<BlueprintRace>();
            var racePresets = ResourcesLibrary.GetBlueprints<BlueprintRaceVisualPreset>();
            var classes = ResourcesLibrary.GetBlueprints<BlueprintCharacterClass>();
            foreach (var race in races)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    CustomizationOptions customizationOptions = gender != Gender.Male ? race.FemaleOptions : race.MaleOptions;
                    AddLinks(head, customizationOptions.Heads);
                    AddLinks(hair, customizationOptions.Hair);
                    AddLinks(beard, customizationOptions.Beards);
                    AddLinks(eyebrows, customizationOptions.Eyebrows);
                }
            }
            foreach (var racePreset in racePresets)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    var raceSkin = racePreset.Skin;
                    if (raceSkin == null) continue;
                    AddLinks(skin, raceSkin.GetLinks(gender, racePreset.RaceId));
                }
            }
            foreach (var _class in classes)
            {
                foreach (var race in races)
                {
                    foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                    {
                        AddLinks(classOutfits, _class.GetClothesLinks(gender, race.RaceId).ToArray());
                    }
                }
            }
            foreach(var bp in ResourcesLibrary.GetBlueprints<BlueprintPortrait>())
            {
                //Note there are two wolf portraits
                if(!portraits.ContainsKey(bp.name) && bp.name != "CustomPortrait") portraits.Add(bp.name, bp);
            }
            foreach (var bp in ResourcesLibrary.GetBlueprints<BlueprintUnitAsksList>())
            {
                if(bp.DisplayName != "" || bp.name == "PC_None_Barks") asks.Add(bp.name, bp);
            }
            loaded = true;
        }
        static public DollState GetDoll(UnitEntityData unitEntityData)
        {
            if (!loaded) Init();
            if (unitEntityData.Descriptor.Doll == null) return null;
            if (!characterDolls.ContainsKey(unitEntityData.CharacterName))
            {
                characterDolls[unitEntityData.CharacterName] = CreateDollState(unitEntityData);
            }
            return characterDolls[unitEntityData.CharacterName];
        }
        static public string GetType(string assetID)
        {
            if (!loaded) Init();
            if (head.ContainsKey(assetID)) return "Head";
            if (hair.ContainsKey(assetID)) return "Hair";
            if (beard.ContainsKey(assetID)) return "Beard";
            if (eyebrows.ContainsKey(assetID)) return "Eyebrows";
            if (skin.ContainsKey(assetID)) return "Skin";
            if (classOutfits.ContainsKey(assetID)) return "ClassOutfit";
            return "Unknown";
        }
        static private DollState CreateDollState(UnitEntityData unitEntityData)
        {
            var dollState = new DollState();
            var dollData = unitEntityData.Descriptor.Doll;
            dollState.SetRace(unitEntityData.Descriptor.Progression.Race); //Race must be set before class
            //This is a hack to work around harmony not allowing calls to the unpatched method
            Main.disableEquipmentClassPatch = true; 
            dollState.SetClass(unitEntityData.Descriptor.Progression.GetEquipmentClass()); 
            Main.disableEquipmentClassPatch = false;
            dollState.SetGender(dollData.Gender);
            dollState.SetRacePreset(dollData.RacePreset);
            dollState.SetLeftHanded(dollData.LeftHanded);
            foreach(var assetID in dollData.EquipmentEntityIds)
            {
                if (head.ContainsKey(assetID))
                {
                    dollState.SetHead(head[assetID]);
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetSkinColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (hair.ContainsKey(assetID))
                {
                    dollState.SetHair(hair[assetID]);
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetHairColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (beard.ContainsKey(assetID))
                {
                    dollState.SetBeard(beard[assetID]);
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetHairColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (skin.ContainsKey(assetID))
                {
                    if (dollData.EntityRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetSkinColor(dollData.EntityRampIdices[assetID]);
                    }
                }
                if (classOutfits.ContainsKey(assetID))
                {
                    if (dollData.EntityRampIdices.ContainsKey(assetID) &&
                        dollData.EntitySecondaryRampIdices.ContainsKey(assetID))
                    {
                        dollState.SetEquipColors(dollData.EntityRampIdices[assetID], dollData.EntitySecondaryRampIdices[assetID]);
                    }
                }
            }
            dollState.Validate();
            return dollState;
        }
    }

}
