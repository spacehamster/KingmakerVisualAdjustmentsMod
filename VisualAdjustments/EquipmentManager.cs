using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using System.Collections.Generic;
using static Kingmaker.UI.Common.ItemsFilter;

namespace VisualAdjustments
{
    public class EquipmentManager
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
        private static SortedList<string, string> m_Helm = new SortedList<string, string>();
        private static SortedList<string, string> m_Cloak = new SortedList<string, string>();
        private static SortedList<string, string> m_Armor = new SortedList<string, string>();
        private static SortedList<string, string> m_Bracers = new SortedList<string, string>();
        private static SortedList<string, string> m_Gloves = new SortedList<string, string>();
        private static SortedList<string, string> m_Boots = new SortedList<string, string>();
        private static SortedList<string, string> m_Units = new SortedList<string, string>();
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
            var units = ResourcesLibrary.GetBlueprints<BlueprintUnit>();
            foreach (var bp in units)
            {
                if (bp.Prefab.AssetId == "") continue;
                m_Units[bp.Prefab.AssetId] = bp.name;
            }
            /*foreach (var kv in ResourcesLibrary.LibraryObject.BlueprintsByAssetId)
            {
                var resource = ResourcesLibrary.TryGetResource<UnitEntityView>(kv.Key);
                if (resource == null) continue;
                m_Units[kv.Key] = resource.name;
            }*/
            Main.DebugLog($"Loaded {m_Units.Count}");
            loaded = true;
        }
    }
}
