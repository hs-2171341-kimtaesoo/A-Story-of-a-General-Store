using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InventoryItemView : MonoBehaviour, ICanvasRaycastFilter,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public InventoryIconGraphic icon;
        public Text nameLabel;
        public InventoryPanel Panel { get; private set; }
        public InventoryGridView Grid { get; private set; }
        public ItemRecord Record { get; private set; }
        public RectTransform Rect { get { return (RectTransform)transform; } }
        Sprite sprite;
        int visualRotation=-1;
        float visualCellSize;
        public void Bind(InventoryPanel panel,InventoryGridView grid,ItemRecord record)
        {
            Panel=panel;Grid=grid;Record=record;
            sprite=panel.FindIcon(panel.Catalog.FindQuality(record.itemId,record.quality).iconKey);
            var hitArea=GetComponent<global::UnityEngine.UI.Image>();
            hitArea.color=new Color(1,1,1,0);hitArea.sprite=null;hitArea.raycastTarget=true;
            hitArea.alphaHitTestMinimumThreshold=0;hitArea.canvasRenderer.cullTransparentMesh=false;
            // 아이콘은 하나뿐입니다. 점유 칸마다 자식 오브젝트를 만들지 않습니다.
            icon.gameObject.SetActive(true);icon.raycastTarget=false;
            RectTransform iconRect=icon.rectTransform;iconRect.anchorMin=Vector2.zero;iconRect.anchorMax=Vector2.one;
            iconRect.offsetMin=iconRect.offsetMax=Vector2.zero;iconRect.localRotation=Quaternion.Euler(0,0,0);
            visualRotation=-1;ApplyRotation(record.rotation,grid.cellSize);
            Rect.anchoredPosition=new Vector2(record.x*grid.cellSize,-record.y*grid.cellSize);
        }
        public void ApplyRotation(int rotation,float cellSize)
        {
            if(visualRotation==rotation && visualCellSize==cellSize) return;
            visualRotation=rotation;visualCellSize=cellSize;ItemFootprint shape=Panel.Catalog.Shape(Record.itemId);
            Rect.sizeDelta=new Vector2(shape.Width(rotation)*cellSize,shape.Height(rotation)*cellSize);
            icon.Configure(shape,sprite,rotation);
            GridCell first=shape.Cells(rotation)[0];
            nameLabel.text=sprite==null?Panel.Catalog.FindQuality(Record.itemId,Record.quality).name:"";
            var label=nameLabel.rectTransform;label.anchorMin=label.anchorMax=label.pivot=new Vector2(0,1);
            label.anchoredPosition=new Vector2(first.x*cellSize+3,-first.y*cellSize-3);
            label.sizeDelta=Vector2.one*(cellSize-6);nameLabel.transform.SetAsLastSibling();
        }
        public bool IsRaycastLocationValid(Vector2 point,Camera camera)
        {
            if(Panel==null || Record==null || visualRotation<0) return false;Vector2 local;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect,point,camera,out local)) return false;
            return Panel.Catalog.Shape(Record.itemId).Contains(visualRotation,Mathf.FloorToInt(local.x/visualCellSize),Mathf.FloorToInt(-local.y/visualCellSize));
        }
        public void SetDragVisual(bool dragging)
        { CanvasGroup group=GetComponent<CanvasGroup>();group.alpha=dragging?.3f:1;group.blocksRaycasts=!dragging; }
        public void OnPointerClick(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left)Panel.Select(Grid.containerId,Record.instanceId); }
        public void OnBeginDrag(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left)Panel.drag.Begin(this,e); }
        public void OnDrag(PointerEventData e) { Panel.drag.MovePointer(e.position); }
        public void OnEndDrag(PointerEventData e) { Panel.drag.Cancel(false); }
    }
}
