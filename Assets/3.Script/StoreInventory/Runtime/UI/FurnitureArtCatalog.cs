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
}
