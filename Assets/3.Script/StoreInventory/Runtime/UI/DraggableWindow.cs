using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // 타이틀 바에만 붙입니다. 아이템/탭/닫기 버튼 드래그와 겹치지 않습니다. Update는 없습니다.
    public sealed class DraggableWindow : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerDownHandler,IPointerUpHandler
    {
        public RectTransform window,bounds;
        public InventoryPanel panel;
        [Min(1)]public float returnSpeed=12;
        Vector2 startPointer,startPosition,returnTarget;bool dragging,returning;
        void Start(){if(window!=null)UiPerformanceLayers.Ensure(window.gameObject,true);}
        // 손잡이를 누른 순간부터 막아 드래그 시작 임계값 전에도 뒤 창이 반응하지 않습니다.
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button==PointerEventData.InputButton.Left&&panel!=null&&window!=null&&bounds!=null&&!panel.drag.IsDragging)
            {returning=false;panel.BeginWindowDrag(this,window,bounds);}
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
            window.anchoredPosition=target;
        }
        public void OnEndDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!dragging)return;FinishDrag();
        }
        void FinishDrag()
        {
            dragging=false;if(panel!=null)panel.EndWindowDrag(this);
            var p=WindowDragGeometry.Clamp(window.anchoredPosition.x,window.anchoredPosition.y,window.rect.width,window.rect.height,bounds.rect.width,bounds.rect.height);
            returnTarget=new Vector2(p.x,p.y);returning=true;
        }
        void OnApplicationFocus(bool focused){if(!focused&&dragging&&panel!=null)FinishDrag();}
        void Update()
        {
            if(!returning)return;
            float x=DrawerGeometry.Step(window.anchoredPosition.x,returnTarget.x,returnSpeed,Time.unscaledDeltaTime);
            float y=DrawerGeometry.Step(window.anchoredPosition.y,returnTarget.y,returnSpeed,Time.unscaledDeltaTime);
            window.anchoredPosition=new Vector2(x,y);if(x==returnTarget.x&&y==returnTarget.y)returning=false;
        }
        void OnDisable(){dragging=false;returning=false;if(panel!=null)panel.EndWindowDrag(this);}
    }
}
