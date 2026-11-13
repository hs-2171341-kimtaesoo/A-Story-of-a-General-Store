using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // 손잡이에 붙입니다. 열린 상자는 자유 배치, 반 이상 내리면 처음의 하단 자리로 복귀합니다.
    public sealed class WarehouseDrawer:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerDownHandler,IPointerUpHandler
    {
        public InventoryPanel panel;public RectTransform window,bounds,body;
        // 닫힐 때 돌아갈 가로 위치이자 처음 열리는 위치입니다. 자유 이동 중에는 변경하지 않습니다.
        public Vector2 openedPosition=new Vector2(24,-146);
        [Min(20)]public float peekHeight=60;
        [Range(.1f,.9f)]public float closeHiddenFraction=.5f;
        [Min(1)]public float returnSpeed=12;
        public bool startClosed=true;
        bool dragging,animating,opened;
        Vector2 startPointer,startPosition,targetPosition,lastOpenedPosition;
        public bool IsOpen{get{return opened;}}
        float ClosedY{get{return DrawerGeometry.ClosedY(bounds.rect.height,peekHeight);}}
        Vector2 ClampPosition(Vector2 point)
        {
            var p=DrawerGeometry.ClampFree(point.x,point.y,window.rect.width,bounds.rect.width,bounds.rect.height,peekHeight);
            return new Vector2(p.x,p.y);
        }
        Vector2 ClosedPosition{get{return ClampPosition(new Vector2(openedPosition.x,ClosedY));}}
        void Start()
        {
            if(panel==null||window==null||bounds==null||body==null){Debug.LogError("서랍 참조를 확인하세요.",this);enabled=false;return;}
            UiPerformanceLayers.Ensure(window.gameObject,true);opened=!startClosed;
            lastOpenedPosition=ClampPosition(openedPosition);targetPosition=opened?lastOpenedPosition:ClosedPosition;
            window.anchoredPosition=targetPosition;body.gameObject.SetActive(opened);
        }
        public void Open(){SetTarget(true);}public void Close(){SetTarget(false);}public void Toggle(){SetTarget(!opened);}
        void SetTarget(bool value)
        {
            panel.EndWindowDrag(this);
            if(!value)panel.ClearWarehouseSelection();
            dragging=false;opened=value;targetPosition=value?ClampPosition(lastOpenedPosition):ClosedPosition;
            animating=true;body.gameObject.SetActive(true);window.SetAsLastSibling();
        }
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
            if(e.button!=PointerEventData.InputButton.Left||panel.drag.IsDragging)return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out startPointer))return;
            if(!panel.BeginWindowDrag(this,window,bounds))return;
            startPosition=window.anchoredPosition;dragging=true;animating=false;body.gameObject.SetActive(true);window.SetAsLastSibling();
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!dragging||!panel.IsWindowDragging)return;Vector2 point;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out point))return;
            // 위/양옆은 막고 아래만 손잡이가 남는 위치까지 허용합니다.
            window.anchoredPosition=ClampPosition(new Vector2(startPosition.x+point.x-startPointer.x,startPosition.y+point.y-startPointer.y));
        }
        public void OnEndDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!dragging)return;dragging=false;panel.EndWindowDrag(this);
            if(DrawerGeometry.ShouldClose(window.anchoredPosition.y,window.rect.height,bounds.rect.height,closeHiddenFraction))SetTarget(false);
            else
            {
                // 열린 상태는 놓은 자리를 유지합니다. 초기 열린 자리로 끌어당기지 않습니다.
                opened=true;animating=false;lastOpenedPosition=window.anchoredPosition;
            }
        }
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
