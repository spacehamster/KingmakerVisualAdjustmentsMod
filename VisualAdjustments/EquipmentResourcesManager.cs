using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using static Kingmaker.UI.Common.ItemsFilter;

namespace VisualAdjustments
{
    public class EquipmentResourcesManager
    {
        public static UnorderedList<string, string> Helm
        {
            get
            {
                if (!loaded) Init();
                return m_Helm;
            }
        }
        public static UnorderedList<string, string> Cloak
        {
            get
            {
                if (!loaded) Init();
                return m_Cloak;
            }
        }
        public static UnorderedList<string, string> Armor
        {
            get
            {
                if (!loaded) Init();
                return m_Armor;
            }
        }
        public static UnorderedList<string, string> Bracers
        {
            get
            {
                if (!loaded) Init();
                return m_Bracers;
            }
        }
        public static UnorderedList<string, string> Gloves
        {
            get
            {
                if (!loaded) Init();
                return m_Gloves;
            }
        }
        public static UnorderedList<string, string> Boots
        {
            get
            {
                if (!loaded) Init();
                return m_Boots;
            }
        }
        public static UnorderedList<string, string> Units
        {
            get
            {
                if (!loaded) Init();
                return m_Units; ;
            }
        }
        public static SortedList<string, UnorderedList<string, string>> Weapons
        {
            get
            {
                if (!loaded) Init();
                return m_Weapons;
            }
        }
        public static UnorderedList<string, string> Tattoos
        {
            get
            {
                if(m_Tattoo.Count == 0)
                {
                    m_Tattoo["326c1affb2a6a26489921bf588f717b6"] = "EE_KineticistTattooWind_U";
                    m_Tattoo["23b9e367a73b5534d918675405de5aa0"] = "EE_KineticistTattooEarth_U";
                    m_Tattoo["c4aee0b105e3e7e45994f4d8619a5974"] = "EE_KineticistTattooFire_U";
                    m_Tattoo["5dcf740907a3ec94bb4deeac33f0c2b3"] = "EE_KineticistTattooWater_U";
                }
                return m_Tattoo;
            }
        }
        private static UnorderedList<string, string> m_Helm = new UnorderedList<string, string>();
        private static UnorderedList<string, string> m_Cloak = new UnorderedList<string, string>();
        private static UnorderedList<string, string> m_Armor = new UnorderedList<string, string>();
        private static UnorderedList<string, string> m_Bracers = new UnorderedList<string, string>();
        private static UnorderedList<string, string> m_Gloves = new UnorderedList<string, string>();
        private static UnorderedList<string, string> m_Boots = new UnorderedList<string, string>();
        private static UnorderedList<string, string> m_Tattoo = new UnorderedList<string, string>();
        private static UnorderedList<string, string> m_Units = new UnorderedList<string, string>();
        private static SortedList<string, UnorderedList<string, string>> m_Weapons = new SortedList<string, UnorderedList<string, string>>();
        private static bool loaded = false;
        static void Init()
        {
            var blueprints = ResourcesLibrary.GetBlueprints<BlueprintItemEquipment>()
                .Where(bp => bp.EquipmentEntity != null)
                .OrderBy(bp => bp.EquipmentEntity.name);
            foreach (var bp in blueprints)
            {
                switch (bp.ItemType)
                {
                    case ItemType.Head:
                        if (m_Helm.ContainsKey(bp.EquipmentEntity.AssetGuid)) break;
                        m_Helm[bp.EquipmentEntity.AssetGuid] = bp.EquipmentEntity.name;
                        break;
                    case ItemType.Shoulders:
                        if (m_Cloak.ContainsKey(bp.EquipmentEntity.AssetGuid)) break;
                        m_Cloak[bp.EquipmentEntity.AssetGuid] = bp.EquipmentEntity.name;
                        break;
                    case ItemType.Armor:
                        if (m_Armor.ContainsKey(bp.EquipmentEntity.AssetGuid)) break;
                        m_Armor[bp.EquipmentEntity.AssetGuid] = bp.EquipmentEntity.name;
                        break;
                    case ItemType.Wrist:
                        if (m_Bracers.ContainsKey(bp.EquipmentEntity.AssetGuid)) break;
                        m_Bracers[bp.EquipmentEntity.AssetGuid] = bp.EquipmentEntity.name;
                        break;
                    case ItemType.Gloves:
                        if (m_Gloves.ContainsKey(bp.EquipmentEntity.AssetGuid)) break;
                        m_Gloves[bp.EquipmentEntity.AssetGuid] = bp.EquipmentEntity.name;
                        break;
                    case ItemType.Feet:
                        if (m_Boots.ContainsKey(bp.EquipmentEntity.AssetGuid)) break;
                        m_Boots[bp.EquipmentEntity.AssetGuid] = bp.EquipmentEntity.name;
                        break;
                    default:
                        break;
                }
            }
            var weapons = ResourcesLibrary.GetBlueprints<BlueprintItemEquipmentHand>().OrderBy((bp) => bp.name);
            foreach (var bp in weapons)
            {
                var visualParameters = bp.VisualParameters;
                var animationStyle = visualParameters.AnimStyle.ToString();
                if (bp.VisualParameters.Model == null) continue;
                UnorderedList<string, string> eeList = null;
                if (!m_Weapons.ContainsKey(animationStyle))
                {
                    eeList = new UnorderedList<string, string>();
                    m_Weapons[animationStyle] = eeList;
                } else
                {
                    eeList = m_Weapons[animationStyle];
                }
                if (eeList.ContainsKey(bp.AssetGuid))
                {
                    continue;
                }
                eeList[bp.AssetGuid] = bp.name;
            }
            string getViewName(BlueprintUnit bp)
            {
                if (!ResourcesLibrary.LibraryObject.ResourceNamesByAssetId.ContainsKey(bp.Prefab.AssetId)) return "NULL";
                var path = ResourcesLibrary.LibraryObject.ResourceNamesByAssetId[bp.Prefab.AssetId].Split('/');
                return path[path.Length - 1];
            }
            var units = ResourcesLibrary.GetBlueprints<BlueprintUnit>().OrderBy(getViewName);
            foreach (var bp in units)
            {
                if (bp.Prefab.AssetId == "") continue;
                if (!ResourcesLibrary.LibraryObject.ResourceNamesByAssetId.ContainsKey(bp.Prefab.AssetId)) continue;             
                if (m_Units.ContainsKey(bp.Prefab.AssetId)) continue;
                m_Units[bp.Prefab.AssetId] = getViewName(bp);
            }
            loaded = true;
        }
    }
}
