using System;
using System.Collections.Generic;
using UnityEngine;
namespace GeneralStore.Inventory
{
    [Serializable] public sealed class InventorySpriteEntry
    {
        public string key;
        public Sprite sprite;
        [Tooltip("Optional lazy Resources path. A Sprite assigned above always takes priority.")]
        public string resourcePath;
    }
    // Optional authored references. Missing entries keep the existing lazy Resources fallback.
    // Configure before Play; the UI caches resolved sprites for the scene lifetime.
    [CreateAssetMenu(menuName="Store Inventory/Sprite Catalog")]
    public sealed class InventorySpriteCatalog : ScriptableObject
    {
        public List<InventorySpriteEntry> itemIcons=new List<InventorySpriteEntry>();
        [Tooltip("Auto-linked single sprites under Resources stay as paths, so opening the catalog does not preload every item texture. Manual Sprite assignments are preserved.")]
        public bool useLazyResourceIcons=true;
        public List<InventorySpriteEntry> supplierPortraits=new List<InventorySpriteEntry>();
        public Sprite FindItem(string key){return Find(itemIcons,key);}
        public Sprite FindSupplier(string key){return Find(supplierPortraits,key);}
        static Sprite Find(List<InventorySpriteEntry> entries,string key)
        {
            if(entries==null||string.IsNullOrEmpty(key))return null;
            foreach(var entry in entries)
                if(entry!=null&&string.Equals(entry.key,key,StringComparison.Ordinal))return entry.sprite!=null?entry.sprite:string.IsNullOrEmpty(entry.resourcePath)?null:Resources.Load<Sprite>(entry.resourcePath);
            return null;
        }
    }
}
