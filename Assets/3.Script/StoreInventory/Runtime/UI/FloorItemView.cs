using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FloorItemView : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public global::UnityEngine.UI.Image icon;
        public Text fallbackName;
        public FloorDropArea Area {get;private set;}
        public ItemRecord Record {get;private set;}
        public void Bind(FloorDropArea area,FloorItemRecord floor)
        {
            Area=area;Record=floor.item;
            var q=area.panel.Catalog.FindQuality(Record.itemId,Record.quality);
            icon.sprite=area.panel.FindIcon(q.iconKey);icon.preserveAspect=true;
            icon.color=icon.sprite==null?new Color(.7f,.6f,.4f,.8f):Color.white;
            fallbackName.text=icon.sprite==null?q.name:"";
        }
        public void OnBeginDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Area.panel.drag.BeginFloor(this,e);}
        public void OnDrag(PointerEventData e){Area.panel.drag.MovePointer(e.position);}
        public void OnEndDrag(PointerEventData e){Area.panel.drag.Cancel(false);}
        public void SetDragVisual(bool active){GetComponent<CanvasGroup>().alpha=active?.3f:1;}
    }
}
