using System;
using UnityEngine;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class FurnitureArt{public string typeId;public Sprite thumbnail;public string description;}
    // 구매/관리의 썸네일은 공통 ID로 연결합니다. 가구 재고 데이터와 분리합니다.
    public sealed class FurnitureArtCatalog:MonoBehaviour
    {
        public FurnitureArt[] entries;
        public FurnitureArt Find(string id){if(entries!=null)foreach(var a in entries)if(a!=null&&a.typeId==id)return a;return null;}
    }
    public static class DisplayMenuOrder
    {
        static readonly string[] ids={"single_stand","weapon_rack","aisle_left","aisle_right","wall_shelf_lower","wall_shelf","mannequin","drawer_cabinet","cubby_cabinet","table","shelf","side_shelf"};
        public static int Rank(string typeId)
        {for(int n=0;n<ids.Length;n++)if(ids[n]==typeId)return n;return ids.Length;}
    }
}
