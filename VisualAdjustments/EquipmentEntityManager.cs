using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.Visual.CharacterSystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using VisualAdjustments;

public static class EquipmentEntityManager {
    public class EquipmentEntityInfo
    {
        public string type = "Unknown";
        public string raceGenderCombos = "";
        public EquipmentEntityLink eel = null;
        public bool expanded = false;
    }
    static readonly Dictionary<string, EquipmentEntityInfo> lookup = new Dictionary<string, EquipmentEntityInfo>();
    static bool loaded = false;
    static void AddLinks(EquipmentEntityLink[] links, string type, Race race, Gender gender)
    {
        foreach(var link in links)
        {
            var ee = link.Load();
            if (lookup.ContainsKey(ee.name))
            {
                lookup[ee.name].raceGenderCombos += ", " + race + gender;
            } else
            {
                lookup[ee.name] = new EquipmentEntityInfo
                {
                    type = type,
                    raceGenderCombos = "" + race + gender,
                    eel = link

                };
            }
        }
    }
    static void Init()
    {
        var races = ResourcesLibrary.GetBlueprints<BlueprintRace>();
        var racePresets = ResourcesLibrary.GetBlueprints<BlueprintRaceVisualPreset>();
        var classes = ResourcesLibrary.GetBlueprints<BlueprintCharacterClass>();
        foreach (var race in races)
        {
            foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
            {
                CustomizationOptions customizationOptions = gender != Gender.Male ? race.FemaleOptions : race.MaleOptions;
                AddLinks(customizationOptions.Heads, "Head", race.RaceId, gender);
                AddLinks(customizationOptions.Hair, "Hair", race.RaceId, gender);
                AddLinks(customizationOptions.Beards, "Beards", race.RaceId, gender);
                AddLinks(customizationOptions.Eyebrows, "Eyebrows", race.RaceId, gender);
            }
        }
        foreach (var racePreset in racePresets)
        {
            foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
            {
                var raceSkin = racePreset.Skin;
                if (raceSkin == null) continue;
                AddLinks(raceSkin.GetLinks(gender, racePreset.RaceId), "Skin", racePreset.RaceId, gender);
            }
        }
        foreach (var _class in classes)
        {
            foreach (var race in races)
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    AddLinks(_class.GetClothesLinks(gender, race.RaceId).ToArray(), "ClassOutfit", race.RaceId, gender);
                }
            }
        }
        var gear = ResourcesLibrary.GetBlueprints<KingmakerEquipmentEntity>();
        foreach (var race in races)
        {
            foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
            {
                foreach (var kee in gear)
                {
                    AddLinks(kee.GetLinks(gender, race.RaceId), "Armor", race.RaceId, gender);
                }
            }
        }
        loaded = true;
    }
    public static void ShowInfo(UnitEntityData unitEntityData)
    {
        if (!loaded) Init();
        
        var character = unitEntityData.View.CharacterAvatar;
        if (GUILayout.Button("Rebuild Doll"))
        {
            Main.UpdateDoll(unitEntityData);
        }
        if (GUILayout.Button("Mark Dirty"))
        {
            unitEntityData.View.CharacterAvatar.IsDirty = true;
        }
        GUILayout.Label("Info", GUILayout.Width(300));
        foreach (var ee in character.EquipmentEntities.ToArray())
        {
            EquipmentEntityInfo settings = lookup.ContainsKey(ee.name) ? lookup[ee.name] : new EquipmentEntityInfo();
            GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
            GUILayout.Label(
                    String.Format("{0}:{1}:{2}:{3}", ee.name, settings.type, ee.BodyParts.Count, ee.OutfitParts.Count), 
                    GUILayout.ExpandWidth(false));
            if (GUILayout.Button("Remove"))
            {
                character.RemoveEquipmentEntity(ee);
            }
            if (GUILayout.Button("Log Parts"))
            {
                LogEquipmentEntity(ee, settings.raceGenderCombos);
            }
            if (GUILayout.Button("Reload Equipment"))
            {
                
                character.RemoveEquipmentEntity(ee);
                var eel = lookup[ee.name].eel;
                character.AddEquipmentEntity(eel.Load());
            }

            settings.expanded = GUILayout.Toggle(settings.expanded, "Expand", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            if (settings.expanded)
            {
                foreach(var bodypart in ee.BodyParts.ToArray())
                {
                    GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                    GUILayout.Label(String.Format(" BP {0}:{1}", bodypart.ToString(), bodypart.Type), GUILayout.ExpandWidth(false));
                    if (GUILayout.Button("Remove"))
                    {
                        ee.BodyParts.Remove(bodypart);
                    }
                    GUILayout.EndHorizontal();
                }
                foreach (var outfitpart in ee.OutfitParts.ToArray())
                {
                    GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
                    GUILayout.Label(String.Format(" OP {0}:{1}", outfitpart.ToString(), outfitpart.Special), GUILayout.ExpandWidth(false));
                    if (GUILayout.Button("Remove"))
                    {
                        ee.OutfitParts.Remove(outfitpart);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
    static void LogEquipmentEntity(EquipmentEntity ee, string raceGenderCombo)
    {
        Main.DebugLog(string.Format("\tee: {0}", ee.name, raceGenderCombo));
        foreach (var bodypart in ee.BodyParts)
        {
            var renderer = bodypart.SkinnedRenderer;
            Main.DebugLog(string.Format("\t\tbodypart: {0}:{1}:{2}", bodypart.ToString(), bodypart.Type, renderer?.ToString()));
        }
        foreach (var outfitpart in ee.OutfitParts)
        {
            var go = Traverse.Create(outfitpart).Field("m_Prefab").GetValue<GameObject>();
            Main.DebugLog(string.Format("\t\toutfitpart: {0}:{1}:{2}", outfitpart.ToString(), outfitpart.Special, go));
        }
        //Main.DebugLog(JsonUtility.ToJson(ee));
    }
}