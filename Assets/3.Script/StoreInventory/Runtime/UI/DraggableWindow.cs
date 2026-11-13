using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // 타이틀 바에만 붙입니다. 아이템/탭/닫기 버튼 드래그와 겹치지 않습니다. Update는 없습니다.
    public sealed class DraggableWindow : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerDownHandler,IPointerUpHandler
    {
        public RectTransform window,bounds;
        public InventoryPanel panel;
        public bool keepInsideScreen=true;
        Vector2 startPointer,startPosition;bool dragging;
        void Start(){if(window!=null)UiPerformanceLayers.Ensure(window.gameObject,true);}
        // 손잡이를 누른 순간부터 막아 드래그 시작 임계값 전에도 뒤 창이 반응하지 않습니다.
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button==PointerEventData.InputButton.Left&&panel!=null&&window!=null&&bounds!=null&&!panel.drag.IsDragging)
                panel.BeginWindowDrag(this,window,bounds);
        }
        public void OnPointerUp(PointerEventData e)
        {if(e.button==PointerEventData.InputButton.Left&&!dragging&&panel!=null)panel.EndWindowDrag(this);}
        public void OnBeginDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||window==null||bounds==null||panel==null||panel.drag.IsDragging)return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out startPointer))return;
            if(!panel.BeginWindowDrag(this,window,bounds))return;
            startPosition=window.anchoredPosition;dragging=true;
            // Floating Windows 안에서만 앞에 둡니다. 가격/거래처/업그레이드 모달 위로 올라가지 않습니다.
            window.SetAsLastSibling();
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!dragging||!panel.IsWindowDragging)return;Vector2 current;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out current))return;
            Vector2 target=new Vector2(startPosition.x+current.x-startPointer.x,startPosition.y+current.y-startPointer.y);
            if(keepInsideScreen)
            {var p=WindowDragGeometry.Clamp(target.x,target.y,window.rect.width,window.rect.height,bounds.rect.width,bounds.rect.height);target=new Vector2(p.x,p.y);}
            window.anchoredPosition=target;
        }
        public void OnEndDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;dragging=false;if(panel!=null)panel.EndWindowDrag(this);}
        void OnDisable(){dragging=false;if(panel!=null)panel.EndWindowDrag(this);}
    }
}
