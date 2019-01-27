using Harmony12;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items.Slots;
using Kingmaker.ResourceLinks;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.View.Equipment;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.Visual.Decals;
using Kingmaker.Visual.Particles;
using Kingmaker.Visual.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace VisualAdjustments
{
    public class InfoManager
    {
        public class EquipmentEntityInfo
        {
            public string type = "Unknown";
            public string raceGenderCombos = "";
            public EquipmentEntityLink eel = null;
        }
        static private Dictionary<string, EquipmentEntityInfo> m_lookup = null;
        static Dictionary<string, EquipmentEntityInfo> lookup
        {
            get
            {
                if (m_lookup == null) BuildLookup();
                return m_lookup;
            }
        }
        static BlueprintBuff[] blueprintBuffs = new BlueprintBuff[] { };
        static bool showWeapons = false;
        static bool showCharacter = false;
        static bool showBuffs = false;
        static bool showFx = false;
        static bool showAsks = false;
        static bool showDoll = false;
        static bool showPortrait = false;
        static string GetName(EquipmentEntityLink link)
        {
            if (ResourcesLibrary.LibraryObject.ResourceNamesByAssetId.ContainsKey(link.AssetId)) return ResourcesLibrary.LibraryObject.ResourceNamesByAssetId[link.AssetId];
            return null;
        }
        static void AddLinks(EquipmentEntityLink[] links, string type, Race race, Gender gender)
        {
            foreach (var link in links)
            {
                var name = GetName(link);
                if (name == null) continue;
                if (lookup.ContainsKey(name))
                {
                    lookup[name].raceGenderCombos += ", " + race + gender;
                }
                else
                {
                    lookup[name] = new EquipmentEntityInfo
                    {
                        type = type,
                        raceGenderCombos = "" + race + gender,
                        eel = link
                    };
                }
            }
        }
        static void BuildLookup()
        {
            m_lookup = new Dictionary<string, EquipmentEntityInfo>(); ;
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
            blueprintBuffs = ResourcesLibrary.GetBlueprints<BlueprintBuff>().ToArray();
        }
        public static void ShowInfo(UnitEntityData unitEntityData)
        {;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rebuild Character"))
            {
                CharacterManager.RebuildCharacter(unitEntityData);
            }
            if (GUILayout.Button("Rebuild Outfit"))
            {
                var bakedCharacter = unitEntityData.View.CharacterAvatar.BakedCharacter;
                unitEntityData.View.CharacterAvatar.BakedCharacter = null;
                unitEntityData.View.CharacterAvatar.RebuildOutfit();
                unitEntityData.View.CharacterAvatar.BakedCharacter = bakedCharacter;
            }
            if (GUILayout.Button("Remove Outfit"))
            {
                unitEntityData.View.CharacterAvatar.RemoveOutfit();
            }
            if (GUILayout.Button("Update Class Equipment"))
            {
                var bakedCharacter = unitEntityData.View.CharacterAvatar.BakedCharacter;
                unitEntityData.View.CharacterAvatar.BakedCharacter = null;
                bool useClassEquipment = unitEntityData.Descriptor.ForcceUseClassEquipment;
                unitEntityData.Descriptor.ForcceUseClassEquipment = true;
                unitEntityData.View.UpdateClassEquipment();
                unitEntityData.Descriptor.ForcceUseClassEquipment = useClassEquipment;
                unitEntityData.View.CharacterAvatar.BakedCharacter = bakedCharacter;
            }
            if (GUILayout.Button("Update Body Equipment"))
            {
                var bakedCharacter = unitEntityData.View.CharacterAvatar.BakedCharacter;
                unitEntityData.View.CharacterAvatar.BakedCharacter = null;
                unitEntityData.View.UpdateBodyEquipmentModel();
                unitEntityData.View.CharacterAvatar.BakedCharacter = bakedCharacter;
            }
            if (GUILayout.Button("Update Model"))
            {
                CharacterManager.UpdateModel(unitEntityData.View);
            }
            if (GUILayout.Button("Update HandsEquipment"))
            {
                unitEntityData.View.HandsEquipment.UpdateAll();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Toggle Stance"))
            {
                unitEntityData.View.HandsEquipment.ForceSwitch(!unitEntityData.View.HandsEquipment.InCombat);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Original size {unitEntityData.Descriptor.OriginalSize}");
            GUILayout.Label($"Current size {unitEntityData.Descriptor.State.Size}");
            var m_OriginalScale = Traverse.Create(unitEntityData.View).Field("m_OriginalScale").GetValue<Vector3>();
            var m_Scale = Traverse.Create(unitEntityData.View).Field("m_Scale").GetValue<float>();
            var realScale = unitEntityData.View.transform.localScale;
            GUILayout.Label($"View Original {m_OriginalScale.x:0.#}");
            GUILayout.Label($"View Current {m_Scale:0.#}");
            GUILayout.Label($"View Real {realScale.x:0.#}");
            GUILayout.Label($"Disabled Scaling {unitEntityData.View.DisableSizeScaling}");
            GUILayout.EndHorizontal();
            var message =
                    unitEntityData.View == null ? "No View" :
                    unitEntityData.View.CharacterAvatar == null ? "No Character Avatar" :
                    null;
            if(message != null) GUILayout.Label(message);
            GUILayout.BeginHorizontal();
            showCharacter = GUILayout.Toggle(showCharacter, "Show Character");
            showWeapons = GUILayout.Toggle(showWeapons, "Show Weapons");
            showDoll = GUILayout.Toggle(showDoll, "Show Doll");
            showBuffs = GUILayout.Toggle(showBuffs, "Show Buffs");
            showFx = GUILayout.Toggle(showFx, "Show FX");
            showPortrait = GUILayout.Toggle(showPortrait, "Show Portrait");
            showAsks = GUILayout.Toggle(showAsks, "Show Asks");

            GUILayout.EndHorizontal();
            if (showCharacter) ShowCharacterInfo(unitEntityData);
            if (showWeapons) ShowWeaponInfo(unitEntityData);
            if (showDoll) ShowDollInfo(unitEntityData);
            if (showBuffs) ShowBuffInfo(unitEntityData);
            if (showFx) ShowFxInfo(unitEntityData);
            if (showPortrait) ShowPortraitInfo(unitEntityData);
            if (showAsks) ShowAsksInfo(unitEntityData);

        }
        static string expandedEE = null;
        static void ShowCharacterInfo(UnitEntityData unitEntityData)
        {
            var character = unitEntityData.View.CharacterAvatar;
            if (character == null) return;
            GUILayout.Label($"View: {unitEntityData.View.name}");
            GUILayout.Label($"BakedCharacter: {character.BakedCharacter?.name ?? "NULL"}");
            GUILayout.Label("Equipment");
            foreach (var ee in character.EquipmentEntities.ToArray())
            {
                GUILayout.BeginHorizontal();
                if (ee == null)
                {
                    GUILayout.Label("Null");
                } 
                else
                {
                    GUILayout.Label(
                        String.Format("{0}:{1}:{2}:P{3}:S{4}", ee.name, ee.BodyParts.Count, ee.OutfitParts.Count,
                            character.GetPrimaryRampIndex(ee), character.GetSecondaryRampIndex(ee)),
                        GUILayout.ExpandWidth(true));
                }
                if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
                {
                    character.RemoveEquipmentEntity(ee);
                }
                if(ee == null)
                {
                    GUILayout.EndHorizontal();
                    continue;
                }
                bool expanded = ee.name == expandedEE;
                if (expanded && GUILayout.Button("Shrink ", GUILayout.ExpandWidth(false))) expandedEE = null;
                if (!expanded && GUILayout.Button("Expand", GUILayout.ExpandWidth(false))) expandedEE = ee.name;
                GUILayout.EndHorizontal();
                if (expanded)
                {
                    EquipmentEntityInfo settings = lookup.ContainsKey(ee.name) ? lookup[ee.name] : new EquipmentEntityInfo();
                    GUILayout.Label($" HideFlags: {ee.HideBodyParts}");
                    var primaryIndex = character.GetPrimaryRampIndex(ee);
                    Texture2D primaryRamp = null;
                    if (primaryIndex < 0 || primaryIndex > ee.PrimaryRamps.Count - 1) primaryRamp = ee.PrimaryRamps.FirstOrDefault();
                    else primaryRamp = ee.PrimaryRamps[primaryIndex];
                    GUILayout.Label($"PrimaryRamp: {primaryRamp?.name ?? "NULL"}");

                    var secondaryIndex = character.GetSecondaryRampIndex(ee);
                    Texture2D secondaryRamp = null;
                    if (secondaryIndex < 0 || secondaryIndex > ee.PrimaryRamps.Count - 1) secondaryRamp = ee.SecondaryRamps.FirstOrDefault();
                    else secondaryRamp = ee.SecondaryRamps[secondaryIndex];
                    GUILayout.Label($"SecondaryRamp: {secondaryRamp?.name ?? "NULL"}");

                    foreach (var bodypart in ee.BodyParts.ToArray())
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(String.Format(" BP {0}:{1}", bodypart?.RendererPrefab?.name ?? "NULL", bodypart?.Type), GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("Remove", GUILayout.ExpandWidth(false)))
                        {
                            ee.BodyParts.Remove(bodypart);
                        }
                        GUILayout.EndHorizontal();
                        
                    }
                    foreach (var outfitpart in ee.OutfitParts.ToArray())
                    {
                        GUILayout.BeginHorizontal();
                        var prefab = Traverse.Create(outfitpart).Field("m_Prefab").GetValue<GameObject>();
                        GUILayout.Label(String.Format(" OP {0}:{1}", prefab?.name ?? "NULL", outfitpart?.Special), GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("Remove"))
                        {
                            ee.OutfitParts.Remove(outfitpart);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.Label("Character", GUILayout.Width(300));
            GUILayout.Label("RampIndices");
            foreach(var index in Traverse.Create(character).Field("m_RampIndices").GetValue<List<Character.SelectedRampIndices>>())
            {
                var name = index.EquipmentEntity != null ? index.EquipmentEntity.name : "NULL";
                GUILayout.Label($"  {name} - {index.PrimaryIndex}, {index.SecondaryIndex}");
            }
            GUILayout.Label("SavedRampIndices");
            foreach (var index in Traverse.Create(character).Field("m_SavedRampIndices").GetValue<List<Character.SavedSelectedRampIndices>>())
            {
                GUILayout.Label($"  {GetName(index.EquipmentEntityLink)} - {index.PrimaryIndex}, {index.SecondaryIndex}");
            }
            GUILayout.Label("SavedEquipmentEntities");
            foreach (var link in Traverse.Create(character).Field("m_SavedEquipmentEntities").GetValue<List<EquipmentEntityLink>>())
            {
                var name = GetName(link);
                GUILayout.Label($"  {name}");
            }

        }
        static void ShowAsksInfo(UnitEntityData unitEntityData)
        {
            var asks = unitEntityData.Descriptor.Asks;
            var customAsks = unitEntityData.Descriptor.CustomAsks;
            var overrideAsks = unitEntityData.Descriptor.OverrideAsks;
            GUILayout.Label($"Current Asks: {asks?.name}, Display: {asks?.DisplayName}");
            GUILayout.Label($"Current CustomAsks: {customAsks?.name}, Display: {customAsks?.DisplayName}");
            GUILayout.Label($"Current OverrideAsks: {overrideAsks?.name}, Display: {overrideAsks?.DisplayName}");
            foreach (var blueprint in ResourcesLibrary.GetBlueprints<BlueprintUnitAsksList>())
            {
                GUILayout.Label($"Asks: {blueprint}, Display: {blueprint.DisplayName}");
            }

        }
        static void ShowPortraitInfo(UnitEntityData unitEntityData)
        {
            var portrait = unitEntityData.Descriptor.Portrait;
            var portraitBP = unitEntityData.Descriptor.UISettings.PortraitBlueprint;
            var uiPortrait = unitEntityData.Descriptor.UISettings.Portrait;
            var CustomPortrait = unitEntityData.Descriptor.UISettings.CustomPortrait;
            GUILayout.Label($"Portrait Blueprint: {portraitBP}, {portraitBP?.name}");
            GUILayout.Label($"Descriptor Portrait: {portrait}, isCustom {portrait?.IsCustom}");
            GUILayout.Label($"UI Portrait: {portrait}, isCustom {portrait?.IsCustom}");
            GUILayout.Label($"Custom Portrait: {portrait}, isCustom {portrait?.IsCustom}");
            foreach (var blueprint in DollResourcesManager.Portrait.Values)
            {
                GUILayout.Label($"Portrait Blueprint: {blueprint}");
            }
        }
        static void ShowHandslotInfo(HandSlot handSlot)
        {
            GUILayout.BeginHorizontal();
            var pItem = handSlot != null && handSlot.HasItem ? handSlot.Item : null;
            GUILayout.Label(string.Format("Slot {0}, {1}, Active {2}", 
                pItem?.Name, pItem?.GetType(), handSlot?.Active), GUILayout.Width(500));
            /*if (GUILayout.Button("Active"))
            {
                Traverse.Create(handSlot).Property("Active").SetValue(!handSlot.Active);
            }
            if (GUILayout.Button("Disabled"))
            {
                Traverse.Create(handSlot).Property("Disabled").SetValue(!handSlot.Disabled);
            }*/
            if (GUILayout.Button("Remove"))
            {
                handSlot.RemoveItem();
            }
            GUILayout.EndHorizontal();
        }
            static void ShowUnitViewHandSlotData(UnitViewHandSlotData handData)
        {
            var ownerScale = handData.Owner.View.GetSizeScale() * Game.Instance.BlueprintRoot.WeaponModelSizing.GetCoeff(handData.Owner.Descriptor.OriginalSize);
            var visualScale = handData.VisualModel?.transform.localScale ?? Vector3.zero;
            var visualPosition = handData.VisualModel?.transform.localPosition ?? Vector3.zero;
            var sheathScale = handData.SheathVisualModel?.transform.localScale ?? Vector3.zero;
            var sheathPosition = handData.SheathVisualModel?.transform.localPosition ?? Vector3.zero;
            GUILayout.Label(string.Format($"weapon {ownerScale:0.#}, scale {visualScale} position {visualPosition}"), GUILayout.Width(500));
            GUILayout.Label(string.Format($"sheath {ownerScale:0.#}, scale {sheathScale} position {sheathPosition}"), GUILayout.Width(500));
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Data {0} Slot {1} Active {2}", handData?.VisibleItem?.Name, handData?.VisualSlot, handData?.IsActiveSet), GUILayout.Width(500));

            if (GUILayout.Button("Unequip"))
            {
                handData.Unequip();
            }
            if (GUILayout.Button("Swap Slot"))
            {
                handData.VisualSlot += 1;
                if(handData.VisualSlot == UnitEquipmentVisualSlotType.Quiver) handData.VisualSlot = 0;
                handData.Owner.View.HandsEquipment.UpdateAll();
            }
            if (GUILayout.Button("ShowItem 0"))
            {
                handData.ShowItem(false);
            }
            if (GUILayout.Button("ShowItem 1"))
            {
                handData.ShowItem(true);
            }
            GUILayout.EndHorizontal();
        }
        static void ShowWeaponInfo(UnitEntityData unitEntityData)
        {
            GUILayout.Label("Weapons", GUILayout.Width(300));
            var hands = unitEntityData.View.HandsEquipment;
            foreach (var kv in hands.Sets)
            {
                ShowHandslotInfo(kv.Key.PrimaryHand);
                ShowUnitViewHandSlotData(kv.Value.MainHand);
                ShowHandslotInfo(kv.Key.SecondaryHand);
                ShowUnitViewHandSlotData(kv.Value.OffHand);
            }
        }
        static int buffIndex = 0;
        static void ShowBuffInfo(UnitEntityData unitEntityData)
        {
            GUILayout.BeginHorizontal();
            buffIndex = (int)GUILayout.HorizontalSlider(buffIndex, 0, blueprintBuffs.Length - 1, GUILayout.Width(300));
            if(GUILayout.Button("Prev", GUILayout.Width(45)))
            {
                buffIndex = buffIndex == 0 ? 0 : buffIndex - 1;
            }
            if (GUILayout.Button("Next", GUILayout.Width(45)))
            {
                buffIndex = buffIndex >= blueprintBuffs.Length - 1 ? blueprintBuffs.Length - 1 : buffIndex + 1;
            }
            GUILayout.Label($"{blueprintBuffs[buffIndex].Name}, {blueprintBuffs[buffIndex].name}");
            if (GUILayout.Button("Apply"))
            {
                GameHelper.ApplyBuff(unitEntityData, blueprintBuffs[buffIndex]);
            }
            GUILayout.EndHorizontal();
            foreach(var buff in unitEntityData.Buffs)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{buff.Blueprint.name}, {buff.Name}");
                if (GUILayout.Button("Remove"))
                {
                    GameHelper.RemoveBuff(unitEntityData, buff.Blueprint);   
                }
                GUILayout.EndHorizontal();
            }
        }
        static void ShowDollInfo(UnitEntityData unitEntityData)
        {
            var doll = unitEntityData.Descriptor.Doll;
            if(doll == null)
            {
                GUILayout.Label("No Doll");
                return;
            }
            GUILayout.Label("Indices");
            foreach(var kv in doll.EntityRampIdices)
            {
                var ee = ResourcesLibrary.TryGetResource<EquipmentEntity>(kv.Key);
                GUILayout.Label($"{kv.Key} - {ee?.name} - {kv.Value}");
            }
            GUILayout.Label("EquipmentEntities");
            foreach (var id in doll.EquipmentEntityIds)
            {
                var ee = ResourcesLibrary.TryGetResource<EquipmentEntity>(id);
                GUILayout.Label($"{id} - {ee?.name}");
            }
        }
            //Refer FxHelper.SpawnFxOnGameObject
            static void ShowFxInfo(UnitEntityData unitEntityData)
        {
            /*GUILayout.Label("Global");
            for (int i = FxHelper.FxRoot.childCount - 1; i >= 0; i--)
            {
                var fx = FxHelper.FxRoot.GetChild(i);
                GUILayout.BeginHorizontal();
                GUILayout.Label("FX: " + fx.name, GUILayout.Width(400));
                if (GUILayout.Button("Destroy"))
                {
                    GameObject.Destroy(fx.gameObject);
                }
                GUILayout.EndHorizontal();
            }*/

            var spawnOnStart = unitEntityData.View.GetComponent<SpawnFxOnStart>();
            if (spawnOnStart)
            {
                GUILayout.Label("Spawn on Start");
                GUILayout.Label("FxOnStart " + spawnOnStart.FxOnStart?.Load()?.name, GUILayout.Width(400));
                GUILayout.Label("FXFxOnDeath " + spawnOnStart.FxOnStart?.Load()?.name, GUILayout.Width(400));
            }
            GUILayout.Label("Decals");
            var decals = Traverse.Create(unitEntityData.View).Field("m_Decals").GetValue<List<FxDecal>>();
            for (int i = decals.Count - 1; i >= 0; i--)
            {
                var decal = decals[i];
                GUILayout.Label("Decal: " + decal.name, GUILayout.Width(400));
                if (GUILayout.Button("Destroy"))
                {
                    GameObject.Destroy(decal.gameObject);
                    decals.RemoveAt(i);
                }
            }
            GUILayout.Label("CustomWeaponEffects");
            var dollroom = Game.Instance.UI.Common.DollRoom;
            foreach(var kv in EffectsManager.WeaponEnchantments)
            {
                GUILayout.Label($"{kv.Key.Name} - {kv.Value.Count}");
                foreach(var go in kv.Value)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  {go?.name ?? "NULL"}");
                    if (dollroom != null && GUILayout.Button("UnscaleFXTimes", GUILayout.ExpandWidth(false)))
                    {
                        Traverse.Create(dollroom).Method("UnscaleFxTimes", new object[] { go }).GetValue();
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}