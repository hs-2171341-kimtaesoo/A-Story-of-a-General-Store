using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InventoryItemView : MonoBehaviour, ICanvasRaycastFilter,
        IPointerEnterHandler,IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public InventoryIconGraphic icon;
        public Text nameLabel;
        public InventoryPanel Panel { get; private set; }
        public InventoryGridView Grid { get; private set; }
        public ItemRecord Record { get; private set; }
        public RectTransform Rect { get { return (RectTransform)transform; } }
        Sprite sprite;
        int visualRotation=-1;
        Vector2 visualCellSize;bool previousLocked;
        public void Bind(InventoryPanel panel,InventoryGridView grid,ItemRecord record)
        {
            bool locked=panel.Model.IsReserved(record.instanceId);
            if(Panel==panel&&Grid==grid&&Record!=null&&Record.instanceId==record.instanceId&&Record.itemId==record.itemId&&Record.quality==record.quality
                &&Record.x==record.x&&Record.y==record.y&&visualRotation==record.rotation&&Record.rotation==record.rotation&&Record.purchasePrice==record.purchasePrice&&previousLocked==locked
                &&visualCellSize.x==grid.cellWidth&&visualCellSize.y==grid.cellHeight){Rect.anchoredPosition=new Vector2((record.x-grid.startColumn)*grid.cellWidth,-(record.y-grid.startRow)*grid.cellHeight);return;}
            Panel=panel;Grid=grid;if(Record==null)Record=record.Copy();else Record.CopyFrom(record);previousLocked=locked;
            sprite=panel.FindItemIcon(record.itemId);
            var hitArea=GetComponent<global::UnityEngine.UI.Image>();
            hitArea.color=new Color(1,1,1,0);hitArea.sprite=null;hitArea.raycastTarget=false;
            hitArea.alphaHitTestMinimumThreshold=0;hitArea.canvasRenderer.cullTransparentMesh=true;
            // 아이콘은 하나뿐입니다. 점유 칸마다 자식 오브젝트를 만들지 않습니다.
            icon.gameObject.SetActive(true);icon.raycastTarget=true;panel.ApplyQuality(icon,record.quality);
            RectTransform iconRect=icon.rectTransform;iconRect.anchorMin=Vector2.zero;iconRect.anchorMax=Vector2.one;
            iconRect.offsetMin=iconRect.offsetMax=Vector2.zero;iconRect.localRotation=Quaternion.Euler(0,0,0);
            visualRotation=-1;ApplyRotation(record.rotation,grid.CellSize);
            GetComponent<CanvasGroup>().alpha=locked?.55f:1;
            if(locked) nameLabel.text="손님 선택 · 잠금";
            Rect.anchoredPosition=new Vector2((record.x-grid.startColumn)*grid.cellWidth,-(record.y-grid.startRow)*grid.cellHeight);
        }
        public void ApplyRotation(int rotation,Vector2 cellSize)
        {
            if(visualRotation==rotation && visualCellSize.x==cellSize.x && visualCellSize.y==cellSize.y) return;
            visualRotation=rotation;visualCellSize=cellSize;ItemFootprint shape=Panel.Catalog.Shape(Record.itemId);
            Rect.sizeDelta=new Vector2(shape.Width(rotation)*cellSize.x,shape.Height(rotation)*cellSize.y);
            icon.Configure(shape,sprite,rotation,cellSize.x/cellSize.y);
            nameLabel.text=sprite==null?Panel.Catalog.FindQuality(Record.itemId,Record.quality).name:"";
            var label=nameLabel.rectTransform;label.anchorMin=label.anchorMax=label.pivot=new Vector2(0,1);
            label.anchoredPosition=new Vector2(3,-3);
            label.sizeDelta=new Vector2(cellSize.x-6,cellSize.y-6);nameLabel.transform.SetAsLastSibling();
        }
        public bool IsRaycastLocationValid(Vector2 point,Camera camera)
        {
            if(Panel==null || Record==null || visualRotation<0) return false;Vector2 local;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect,point,camera,out local)) return false;
            return Panel.Catalog.Shape(Record.itemId).Contains(visualRotation,Mathf.FloorToInt(local.x/visualCellSize.x),Mathf.FloorToInt(-local.y/visualCellSize.y));
        }
        public void SetDragVisual(bool dragging)
        { CanvasGroup group=GetComponent<CanvasGroup>();group.alpha=dragging?.3f:1;group.blocksRaycasts=!dragging; }
        public void ReleaseVisual(){Record=null;sprite=null;visualRotation=-1;icon.Configure(null,null,0,1);nameLabel.text="";}
        // 상위 창의 배경 PointerDown이 아이템 클릭을 가로채지 않게 같은 오브젝트에서 받습니다.
        public void OnPointerEnter(PointerEventData e){if(Panel?.tooltip!=null)Panel.tooltip.Enter(this);}
        public void OnPointerExit(PointerEventData e){if(Panel?.tooltip!=null)Panel.tooltip.Exit(this);}
        void OnDisable(){if(Panel?.tooltip!=null)Panel.tooltip.Exit(this);}
        public void OnPointerDown(PointerEventData e){}
        public void OnPointerUp(PointerEventData e){}
        public void OnPointerClick(PointerEventData e)
        {
            // Consume the click without creating a persistent selection.
        }
        public void OnBeginDrag(PointerEventData e) { if(Panel.IsWindowDragging)return;if(Panel.Model.IsReserved(Record.instanceId)){Panel.ShowMessage("손님 선택 상품은 이동할 수 없습니다.");return;} if(e.button==PointerEventData.InputButton.Left)Panel.drag.Begin(this,e); }
        public void OnDrag(PointerEventData e) { Panel.drag.MovePointer(e.position); }
        public void OnEndDrag(PointerEventData e) { Panel.drag.Cancel(false); }
    }
}
