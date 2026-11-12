using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // 손잡이에 붙입니다. 상자 루트는 유지하고 닫힌 뒤 본문만 숨깁니다.
    public sealed class WarehouseDrawer:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public InventoryPanel panel;public RectTransform window,bounds,body;
        public Vector2 openedPosition=new Vector2(24,-146);
        [Min(20)]public float peekHeight=60;
        [Range(.1f,.9f)]public float closeHiddenFraction=.5f;
        [Min(1)]public float returnSpeed=12;
        public bool startClosed=true;
        bool dragging,animating,opened;Vector2 startPointer,startPosition;float targetY;
        public bool IsOpen{get{return opened;}}
        float ClosedY{get{return DrawerGeometry.ClosedY(bounds.rect.height,peekHeight);}}
        void Start()
        {
            if(panel==null||window==null||bounds==null||body==null){Debug.LogError("서랍 참조를 확인하세요.",this);enabled=false;return;}
            UiPerformanceLayers.Ensure(window.gameObject,true);opened=!startClosed;targetY=opened?openedPosition.y:ClosedY;
            window.anchoredPosition=new Vector2(openedPosition.x,targetY);body.gameObject.SetActive(opened);
        }
        public void Open(){SetTarget(true);}public void Close(){SetTarget(false);}public void Toggle(){SetTarget(!opened);}
        void SetTarget(bool value)
        {dragging=false;opened=value;targetY=value?openedPosition.y:ClosedY;animating=true;body.gameObject.SetActive(true);window.SetAsLastSibling();}
        public void OnBeginDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||panel.drag.IsDragging)return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out startPointer))return;
            startPosition=window.anchoredPosition;dragging=true;animating=false;body.gameObject.SetActive(true);window.SetAsLastSibling();
        }
        public void OnDrag(PointerEventData e)
        {
            if(!dragging)return;Vector2 point;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,e.position,panel.EventCamera,out point))return;
            window.anchoredPosition=new Vector2(openedPosition.x,Mathf.Clamp(startPosition.y+point.y-startPointer.y,ClosedY,openedPosition.y));
        }
        public void OnEndDrag(PointerEventData e)
        {if(!dragging)return;dragging=false;SetTarget(!DrawerGeometry.ShouldClose(window.anchoredPosition.y,window.rect.height,bounds.rect.height,closeHiddenFraction));}
        void Update()
        {
            // 정지 상태는 위치 계산이 없습니다. 게임 시간 정지와 별도로 동작합니다.
            if(!animating)return;float y=DrawerGeometry.Step(window.anchoredPosition.y,targetY,returnSpeed,Time.unscaledDeltaTime);
            window.anchoredPosition=new Vector2(openedPosition.x,y);if(y==targetY){animating=false;if(!opened)body.gameObject.SetActive(false);}
        }
    }
}
