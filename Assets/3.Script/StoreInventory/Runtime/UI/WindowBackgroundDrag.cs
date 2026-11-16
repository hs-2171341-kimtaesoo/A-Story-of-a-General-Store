using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // 창 루트에 붙입니다. 격자/탭/버튼은 제외하고 배경 입력만 기존 이동 처리기에 전달합니다.
    public sealed class WindowBackgroundDrag:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public WarehouseDrawer drawer;public DraggableWindow displayDrag;
        bool forwarding;PointerEventData activeEvent;
        bool CanDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left)return false;
            var hit=e.pointerPressRaycast.gameObject;if(hit==null)hit=e.pointerCurrentRaycast.gameObject;if(hit==null)return false;
            if(hit.GetComponentInParent<WindowBackgroundDrag>()!=this)return false;
            // 빈 격자/칸 사이도 포함해 격자 전체를 제외합니다. 아이템 드래그가 우선입니다.
            if(hit.GetComponentInParent<InventoryGridView>()!=null||hit.GetComponentInParent<Selectable>()!=null
                ||hit.GetComponentInParent<ScrollRect>()!=null||hit.GetComponentInParent<WindowDragExclusion>()!=null)return false;
            return drawer!=null||displayDrag!=null;
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(!CanDrag(e))return;activeEvent=e;
            if(drawer!=null)drawer.OnPointerDown(e);else displayDrag.OnPointerDown(e);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(activeEvent==null)return;
            if(drawer!=null)drawer.OnPointerUp(e);else displayDrag.OnPointerUp(e);
            if(!forwarding)activeEvent=null;
        }
        public void OnBeginDrag(PointerEventData e)
        {
            // Button은 자체 PointerDown을 받으므로 여기서도 원래 누른 대상을 다시 검사합니다.
            if(!CanDrag(e))return;forwarding=true;activeEvent=e;
            if(drawer!=null)drawer.OnBeginDrag(e);else displayDrag.OnBeginDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {if(!forwarding)return;if(drawer!=null)drawer.OnDrag(e);else displayDrag.OnDrag(e);}
        public void OnEndDrag(PointerEventData e)
        {
            if(!forwarding)return;
            if(drawer!=null)drawer.OnEndDrag(e);else displayDrag.OnEndDrag(e);
            forwarding=false;activeEvent=null;
        }
        void OnDisable()
        {
            if(activeEvent!=null){OnPointerUp(activeEvent);if(forwarding)OnEndDrag(activeEvent);}
            forwarding=false;activeEvent=null;
        }
    }
}
