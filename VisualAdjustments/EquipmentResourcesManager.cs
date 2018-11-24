using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using System.Collections.Generic;
using static Kingmaker.UI.Common.ItemsFilter;

namespace VisualAdjustments
{
    public class EquipmentResourcesManager
    {
        public static SortedList<string, string> Helm
        {
            get
            {
                if (!loaded) Init();
                return m_Helm;
            }
        }
        public static SortedList<string, string> Cloak
        {
            get
            {
                if (!loaded) Init();
                return m_Cloak;
            }
        }
        public static SortedList<string, string> Armor
        {
            get
            {
                if (!loaded) Init();
                return m_Armor;
            }
        }
        public static SortedList<string, string> Bracers
        {
            get
            {
                if (!loaded) Init();
                return m_Bracers;
            }
        }
        public static SortedList<string, string> Gloves
        {
            get
            {
                if (!loaded) Init();
                return m_Gloves;
            }
        }
        public static SortedList<string, string> Boots
        {
            get
            {
                if (!loaded) Init();
                return m_Boots;
            }
        }
        public static SortedList<string, string> Units
        {
            get
            {
                if (!loaded) Init();
                return m_Units; ;
            }
        }
        public static SortedList<string, SortedList<string, string>> Weapons
        {
            get
            {
                if (!loaded) Init();
                return m_Weapons;
            }
        }
        private static SortedList<string, string> m_Helm = new SortedList<string, string>();
        private static SortedList<string, string> m_Cloak = new SortedList<string, string>();
        private static SortedList<string, string> m_Armor = new SortedList<string, string>();
        private static SortedList<string, string> m_Bracers = new SortedList<string, string>();
        private static SortedList<string, string> m_Gloves = new SortedList<string, string>();
        private static SortedList<string, string> m_Boots = new SortedList<string, string>();
        private static SortedList<string, string> m_Units = new SortedList<string, string>();
        private static SortedList<string, SortedList<string, string>> m_Weapons = new SortedList<string, SortedList<string, string>>();
        private static bool loaded = false;
        static void Init()
        {
            var blueprints = ResourcesLibrary.GetBlueprints<BlueprintItemEquipment>();
            foreach(var bp in blueprints)
            {
                if (bp.EquipmentEntity == null) continue;
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
            var weapons = ResourcesLibrary.GetBlueprints<BlueprintItemEquipmentHand>();
            foreach (var bp in weapons)
            {
                var visualParameters = bp.VisualParameters;
                var animationStyle = visualParameters.AnimStyle.ToString();
                if (bp.VisualParameters.Model == null) continue;
                SortedList<string, string> eeList = null;
                if (!m_Weapons.ContainsKey(animationStyle))
                {
                    eeList = new SortedList<string, string>();
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
            var units = ResourcesLibrary.GetBlueprints<BlueprintUnit>();
            foreach (var bp in units)
            {
                if (bp.Prefab.AssetId == "") continue;
                m_Units[bp.Prefab.AssetId] = bp.name;
            }
            loaded = true;
        }
    }
}
