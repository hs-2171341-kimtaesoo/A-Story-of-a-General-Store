using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // 상자 루트에 붙입니다. 열린 상자는 자유 배치, 반 이상 내리면 처음의 하단 자리로 복귀합니다.
    public sealed class WarehouseDrawer:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerDownHandler,IPointerUpHandler
    {
        public InventoryPanel panel;public RectTransform window,bounds,body;
        // 닫힐 때 돌아갈 가로 위치이자 처음 열리는 위치입니다. 자유 이동 중에는 변경하지 않습니다.
        public Vector2 openedPosition=new Vector2(24,-146);
        [Min(20)]public float peekHeight=60;
        [Range(0,.4f)]public float homeRaiseRatio=0; // 이전.1보다 화면 높이10%만큼 더 아래로 내립니다.
        [Range(.1f,.9f)]public float closeHiddenFraction=.5f;
        [Min(1)]public float returnSpeed=12;
        public bool startClosed=true;
        bool dragging,animating,opened;
        Vector2 startPointer,startPosition,targetPosition;
        public bool IsOpen{get{return opened;}}
        public event System.Action<bool> StateChanged;
        void NotifyState(){var callback=StateChanged;if(callback!=null)callback(opened);}
        float ClosedY{get{return DrawerGeometry.ClosedY(bounds.rect.height,peekHeight+bounds.rect.height*homeRaiseRatio);}}
        Vector2 ClampPosition(Vector2 point)
        {
            var p=WindowDragGeometry.Clamp(point.x,point.y,window.rect.width,window.rect.height,bounds.rect.width,bounds.rect.height);
            return new Vector2(p.x,p.y);
        }
        Vector2 ClosedPosition{get{return new Vector2(Mathf.Clamp(openedPosition.x,0,Mathf.Max(0,bounds.rect.width-window.rect.width)),ClosedY);}}
        void Start()
        {
            if(panel==null||window==null||bounds==null||body==null){Debug.LogError("서랍 참조를 확인하세요.",this);enabled=false;return;}
            UiPerformanceLayers.Ensure(window.gameObject,true);opened=!startClosed;
            targetPosition=opened?ClampPosition(openedPosition):ClosedPosition;
            window.anchoredPosition=targetPosition;body.gameObject.SetActive(opened);NotifyState();
        }
        public void Open(){SetTarget(true);}public void Close(){SetTarget(false);}public void Toggle(){SetTarget(!opened);}
        void SetTarget(bool value)
        {
            if(panel==null||window==null||bounds==null||body==null)return;
            if(panel.drag!=null)panel.drag.Cancel(false);
            panel.EndWindowDrag(this);
            if(!value)panel.ClearWarehouseSelection();
            dragging=false;opened=value;targetPosition=value?ClampPosition(openedPosition):ClosedPosition;
            animating=true;body.gameObject.SetActive(true);window.SetAsLastSibling();NotifyState();
        }
        // 손잡이를 누른 순간부터 막아 드래그 시작 임계값 전에도 뒤 창이 반응하지 않습니다.
        bool CanDrag(PointerEventData e)
        {
            var hit=e.pointerPressRaycast.gameObject;if(hit==null)hit=e.pointerCurrentRaycast.gameObject;
            return hit!=null&&hit.GetComponentInParent<WarehouseDrawer>()==this
                &&hit.GetComponentInParent<InventoryGridView>()==null&&hit.GetComponentInParent<UnityEngine.UI.Selectable>()==null
                &&hit.GetComponentInParent<UnityEngine.UI.ScrollRect>()==null&&hit.GetComponentInParent<WindowDragExclusion>()==null;
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(CanDrag(e)&&e.button==PointerEventData.InputButton.Left&&panel!=null&&window!=null&&bounds!=null&&!panel.drag.IsDragging)
            {animating=false;panel.BeginWindowDrag(this,window,bounds);}
        }
        public void OnPointerUp(PointerEventData e)
        {if(e.button==PointerEventData.InputButton.Left&&!dragging&&panel!=null){panel.EndWindowDrag(this);animating=true;}}
        // 토글 손잡이를 끄는 경우에도 배경 드래그는 그대로 사용할 수 있습니다.
        public void PressHandle(PointerEventData e)
        {
            if(e.button==PointerEventData.InputButton.Left&&panel!=null&&window!=null&&bounds!=null&&panel.drag!=null&&!panel.drag.IsDragging)
            {animating=false;panel.BeginWindowDrag(this,window,bounds);}
        }
        public void OnBeginDrag(PointerEventData e){if(CanDrag(e))BeginHandleDrag(e);}
        public bool BeginHandleDrag(PointerEventData e)
        {
            if(panel==null||panel.drag==null||window==null||bounds==null||body==null||e.button!=PointerEventData.InputButton.Left||panel.drag.IsDragging)return false;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out startPointer))return false;
            if(!panel.BeginWindowDrag(this,window,bounds))return false;
            startPosition=window.anchoredPosition;dragging=true;animating=false;body.gameObject.SetActive(true);window.SetAsLastSibling();return true;
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!dragging||!panel.IsWindowDragging)return;Vector2 point;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out point))return;
            // 이동 중에는 사방으로 벗어날 수 있습니다. 놓는 순간 복귀 목표만 계산합니다.
            window.anchoredPosition=new Vector2(startPosition.x+point.x-startPointer.x,startPosition.y+point.y-startPointer.y);
        }
        public void OnEndDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&dragging)FinishDrag();}
        void FinishDrag()
        {
            dragging=false;panel.EndWindowDrag(this);
            if(DrawerGeometry.ShouldClose(window.anchoredPosition.y,window.rect.height,bounds.rect.height,closeHiddenFraction))SetTarget(false);
            else{opened=true;targetPosition=ClampPosition(window.anchoredPosition);animating=true;NotifyState();}
        }
        public void ReleaseHandle(){if(dragging&&panel!=null)FinishDrag();else if(panel!=null)panel.EndWindowDrag(this);}
        void OnApplicationFocus(bool focused)
        {if(!focused&&panel!=null){if(dragging)FinishDrag();else{panel.EndWindowDrag(this);animating=true;}}}
        void OnDisable(){dragging=false;if(panel!=null)panel.EndWindowDrag(this);}
        void Update()
        {
            if(!animating)return;
            float x=DrawerGeometry.Step(window.anchoredPosition.x,targetPosition.x,returnSpeed,Time.unscaledDeltaTime);
            float y=DrawerGeometry.Step(window.anchoredPosition.y,targetPosition.y,returnSpeed,Time.unscaledDeltaTime);
            window.anchoredPosition=new Vector2(x,y);
            if(x==targetPosition.x&&y==targetPosition.y){animating=false;if(!opened)body.gameObject.SetActive(false);}
        }
    }
}
