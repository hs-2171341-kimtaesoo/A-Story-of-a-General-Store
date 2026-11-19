using UnityEngine;
namespace GeneralStore.Inventory
{
    // 빈 칸의 입력용 투명 면은 아이템을 들고 있을 때만 렌더/레이캐스트에 등록합니다.
    public sealed class DragOnlyRaycastSurface:MonoBehaviour
    {
        public InventoryPanel panel;public global::UnityEngine.UI.Image image;
        InventoryDragHandler source;
        void OnEnable(){Connect();}void Start(){Connect();}
        void Connect()
        {
            if(panel==null||panel.drag==null)return;
            if(image==null)image=GetComponent<global::UnityEngine.UI.Image>();
            if(source==null){source=panel.drag;source.DraggingChanged+=Render;}Render(source.IsDragging);
        }
        void Render(bool held)
        {
            if(image==null)return;image.raycastTarget=held;image.canvasRenderer.cullTransparentMesh=!held;
            if(held)image.canvasRenderer.cull=false;image.SetVerticesDirty();
        }
        void OnDisable(){if(source!=null)source.DraggingChanged-=Render;source=null;}
    }
}
