using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    // Header/border drag only. Item gestures and the viewport's own ScrollRect stay independent.
    public sealed class TemporaryStorageWindowDrag:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,
        IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public DeliveryUI delivery;public InventoryPanel panel;public RectTransform window,bounds;
        bool dragging,pressed;Vector2 startPointer,startPosition;
        public bool IsDragging{get{return dragging;}}
        public bool CanDrag(PointerEventData pointer)
        {
            if(pointer==null||pointer.button!=PointerEventData.InputButton.Left||panel==null||panel.drag==null||window==null||bounds==null||delivery==null
                ||panel.IsWindowDragging&&!pressed||panel.drag.IsDragging||(panel.shop!=null&&(panel.shop.HasPendingPlacement||(panel.shop.counterTrade!=null&&panel.shop.counterTrade.IsItemDragging))))return false;
            var motion=window.GetComponent<ManagementWindowMotion>();if(motion!=null&&(motion.IsTransitioning||motion.IsClosing))return false;
            var hit=pointer.pointerPressRaycast.gameObject;if(hit==null)hit=pointer.pointerCurrentRaycast.gameObject;
            return hit!=null&&(hit.transform==window||hit.transform.IsChildOf(window))
                &&hit.GetComponentInParent<InventoryGridView>()==null&&hit.GetComponentInParent<Selectable>()==null
                &&hit.GetComponentInParent<ScrollRect>()==null&&hit.GetComponentInParent<WindowDragExclusion>()==null;
        }
        public void OnPointerDown(PointerEventData pointer)
        {if(CanDrag(pointer))pressed=panel.BeginWindowDrag(this,window,bounds);}
        public void OnBeginDrag(PointerEventData pointer)
        {
            if(!CanDrag(pointer)||!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,pointer.position,panel.EventCamera,out startPointer))return;
            if(!panel.BeginWindowDrag(this,window,bounds))return;
            pressed=dragging=true;startPosition=window.anchoredPosition;window.SetAsLastSibling();
        }
        public void OnDrag(PointerEventData pointer)
        {
            if(!dragging||!panel.IsWindowDragging||pointer.button!=PointerEventData.InputButton.Left)return;Vector2 position;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,pointer.position,panel.EventCamera,out position))return;
            delivery.SetDraggedPosition(startPosition+position-startPointer);
        }
        public void OnPointerUp(PointerEventData pointer){if(pointer.button==PointerEventData.InputButton.Left&&!dragging)Release();}
        public void OnEndDrag(PointerEventData pointer){if(pointer.button==PointerEventData.InputButton.Left)Release();}
        void Release(){dragging=pressed=false;if(panel!=null)panel.EndWindowDrag(this);}
        void OnDisable(){Release();}
        void OnApplicationFocus(bool focused){if(!focused)Release();}
    }
}
