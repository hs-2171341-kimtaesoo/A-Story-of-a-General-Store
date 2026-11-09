using System;

namespace ShopItems
{
    
    [Serializable]
    public class ItemDatabase
    {
        public ItemDefinition[] items;
    }

    [Serializable]
    public class ItemDefinition
    {
        public string id;
        public string type;
        public int basePrice;
        public ItemQualityData[] qualities;
    }

    [Serializable]
    public class ItemQualityData
    {
        // 현재 데이터 종류: poor, common, superior, perfect, masterpiece, legendary, none
        public string quality;
        public string name;
        public float priceMultiplier;
        public string iconKey;
        public string explanation;
    }
}
