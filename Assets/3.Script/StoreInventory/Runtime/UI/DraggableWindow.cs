using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // 타이틀 바에만 붙입니다. 아이템/탭/닫기 버튼 드래그와 겹치지 않습니다. Update는 없습니다.
    public sealed class DraggableWindow : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public RectTransform window,bounds;
        public InventoryPanel panel;
        public bool keepInsideScreen=true;
        Vector2 startPointer,startPosition;bool dragging;
        void Start(){if(window!=null)UiPerformanceLayers.Ensure(window.gameObject,true);}
        public void OnBeginDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||window==null||bounds==null||panel==null||panel.drag.IsDragging)return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out startPointer))return;
            startPosition=window.anchoredPosition;dragging=true;
            // Floating Windows 안에서만 앞에 둡니다. 가격/거래처/업그레이드 모달 위로 올라가지 않습니다.
            window.SetAsLastSibling();
        }
        public void OnDrag(PointerEventData e)
        {
            if(!dragging)return;Vector2 current;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out current))return;
            Vector2 target=new Vector2(startPosition.x+current.x-startPointer.x,startPosition.y+current.y-startPointer.y);
            if(keepInsideScreen)
            {var p=WindowDragGeometry.Clamp(target.x,target.y,window.rect.width,window.rect.height,bounds.rect.width,bounds.rect.height);target=new Vector2(p.x,p.y);}
            window.anchoredPosition=target;
        }
        public void OnEndDrag(PointerEventData e){dragging=false;}
        void OnDisable(){dragging=false;}
    }
}
