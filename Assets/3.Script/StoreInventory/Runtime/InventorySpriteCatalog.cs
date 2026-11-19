using System;
using System.Collections.Generic;
using UnityEngine;
namespace GeneralStore.Inventory
{
    [Serializable] public sealed class InventorySpriteEntry
    {
        public string key;
        public Sprite sprite;
    }
    // Optional authored references. Missing entries keep the existing lazy Resources fallback.
    // Configure before Play; the UI caches resolved sprites for the scene lifetime.
    [CreateAssetMenu(menuName="Store Inventory/Sprite Catalog")]
    public sealed class InventorySpriteCatalog : ScriptableObject
    {
        public List<InventorySpriteEntry> itemIcons=new List<InventorySpriteEntry>();
        public List<InventorySpriteEntry> supplierPortraits=new List<InventorySpriteEntry>();
        public Sprite FindItem(string key){return Find(itemIcons,key);}
        public Sprite FindSupplier(string key){return Find(supplierPortraits,key);}
        static Sprite Find(List<InventorySpriteEntry> entries,string key)
        {
            if(entries==null||string.IsNullOrEmpty(key))return null;
            foreach(var entry in entries)
                if(entry!=null&&string.Equals(entry.key,key,StringComparison.Ordinal)&&entry.sprite!=null)return entry.sprite;
            return null;
        }
    }
}
