using System;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class FurnitureSceneLayout{public FurnitureSceneEntry[] entries;}
    [Serializable]public sealed class FurnitureSceneEntry
    {
        public int number,drawOrder;public string typeId,title,spriteKey,notes;
        // 1920×1080 Canvas의 왼쪽 위가 원점입니다. Unity anchoredPosition에서는 y의 부호를 바꿉니다.
        public float x,y,width,height;
    }
}
