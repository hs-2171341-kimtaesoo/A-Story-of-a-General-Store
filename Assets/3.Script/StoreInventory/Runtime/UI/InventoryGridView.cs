using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    // 여러 칸을 차지하므로 GridLayoutGroup이 아니라 좌표로 카드를 배치합니다.
    public sealed class InventoryGridView : MonoBehaviour, IDropHandler
    {
        public bool acceptsDrop=true,recycleItems=true;
        [Range(0,128)]public int maximumPooledItems=32;
        readonly Stack<InventoryItemView> itemPool=new Stack<InventoryItemView>();
        public string containerId = "warehouse";
        [Min(16)] public float cellWidth = 78;
        [Min(16)] public float cellHeight = 78;
        [Range(0,8)]public float cellGap=2;
        public bool squareCells=true,renderCells=true,renderPartitions=true;
        [Min(0)]public int startColumn,startRow,viewColumns,viewRows; // 0 크기는 전체 컨테이너. 좌표는 재고의 전역 좌표입니다.
        readonly PlacementPreview localPreview=new PlacementPreview();
        public float ContentOrigin{get{return cellGap*.5f;}}
        public Vector2 LogicalSize{get{return new Vector2(columns*cellWidth,rows*cellHeight);}}
        public Vector2 CellSize { get { return new Vector2(cellWidth,cellHeight); } }
        public Color cellColor = new Color(.16f, .14f, .1f);
        public Sprite cellSprite;
        public RectTransform itemLayer;
        public InventoryItemView itemTemplate;
        public InventoryPanel Panel { get; private set; }
        readonly List<GameObject> cellObjects = new List<GameObject>();
        readonly Dictionary<string,InventoryItemView> itemObjects = new Dictionary<string,InventoryItemView>();
        readonly HashSet<string> seen = new HashSet<string>();
        readonly List<string> removed = new List<string>();
        float renderedWidth,renderedHeight,renderedGap=-1;bool previousRenderCells;
        int columns, rows;
        PlacementPreviewGraphic previewGraphic;
        DisplayPartitionGraphic partitionGraphic;ReservedSlotGraphic reservedGraphic;
        readonly List<ItemRecord> reservedItems=new List<ItemRecord>();
        long shownVersion=-2;string shownContainer;bool shownCounter;
        int shownStartColumn=-1,shownStartRow=-1,shownViewColumns=-1,shownViewRows=-1;
        public Color dividerColor=new Color(.5f,.29f,.1f,1);
        [Range(2,16)]public float dividerThickness=8;
        public bool externalRefresh;
        public int LiveItemViewCount{get{return itemObjects.Count;}}
        public RectTransform Rect { get { return (RectTransform)transform; } }
        public void Initialize(InventoryPanel panel) { Panel = panel; if(gameObject.activeInHierarchy&&!externalRefresh) Refresh(); }
        void OnEnable() { if(Panel!=null&&!externalRefresh) Refresh(); }
        static RectTransform TopLeft(GameObject go)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); return rect;
        }
        public void Refresh(){Refresh(null);}
        public void Refresh(ContainerRecord snapshot)
        {
            long version=Panel.Model.ContainerVersion(containerId);bool counter=Panel.inventory.Sales.CounterPresented;
            if(snapshot==null&&shownVersion==version&&shownContainer==containerId&&shownCounter==counter&&shownStartColumn==startColumn&&shownStartRow==startRow&&shownViewColumns==viewColumns&&shownViewRows==viewRows&&renderedWidth==cellWidth&&renderedHeight==(squareCells?cellWidth:cellHeight)&&renderedGap==cellGap&&previousRenderCells==renderCells)return;
            ContainerRecord container = snapshot??Panel.GridData(containerId);
            if (container == null) { Debug.LogError("없는 컨테이너: " + containerId, this); return; }
                        // 1:1 고정 칸. 바깥 테두리도 타일 사이와 같은 간격입니다.
            if(squareCells)cellHeight=cellWidth;
            int visibleColumns=viewColumns>0?viewColumns:container.columns,visibleRows=viewRows>0?viewRows:container.rows;
            if(startColumn<0||startRow<0||visibleColumns<1||visibleRows<1||startColumn+visibleColumns>container.columns||startRow+visibleRows>container.rows){Debug.LogError("진열 부분 격자 범위가 재고 범위를 벗어났습니다.",this);return;}
            Rect.sizeDelta=new Vector2(visibleColumns*cellWidth+cellGap,visibleRows*cellHeight+cellGap);
            itemLayer.anchorMin=itemLayer.anchorMax=itemLayer.pivot=new Vector2(0,1);
            itemLayer.anchoredPosition=new Vector2(ContentOrigin,-ContentOrigin);
            itemLayer.sizeDelta=new Vector2(visibleColumns*cellWidth,visibleRows*cellHeight);
            if (columns != visibleColumns || rows != visibleRows || renderedWidth!=cellWidth || renderedHeight!=cellHeight||renderedGap!=cellGap||previousRenderCells!=renderCells)
            {
                columns = visibleColumns; rows = visibleRows;renderedWidth=cellWidth;renderedHeight=cellHeight;renderedGap=cellGap;previousRenderCells=renderCells;
                int needed=renderCells?columns*rows:0;
                // 가구 전환 시 칸을 파괴하지 않습니다. 지금까지 필요한 최대 수만 보관하고 재사용합니다.
                while(cellObjects.Count<needed)
                {
                    var go=new GameObject("Cell",typeof(RectTransform),typeof(global::UnityEngine.UI.Image));
                    go.transform.SetParent(transform,false);cellObjects.Add(go);
                }
                for(int n=0;n<cellObjects.Count;n++)
                {
                    var go=cellObjects[n];go.SetActive(n<needed);if(n>=needed)continue;
                    RectTransform cell=TopLeft(go);cell.sizeDelta=new Vector2(cellWidth-cellGap,cellHeight-cellGap);
                    cell.anchoredPosition=new Vector2(cellGap+(n%columns)*cellWidth,-cellGap-(n/columns)*cellHeight);
                    var image=go.GetComponent<global::UnityEngine.UI.Image>();image.color=cellColor;image.sprite=cellSprite;image.raycastTarget=false;
                }
                itemLayer.SetAsLastSibling();
            }
            // 돈/예약 변경 때마다 모든 카드를 파괴하지 않고 UID별로 재사용합니다.
            seen.Clear();reservedItems.Clear();
            foreach (ItemRecord record in container.items)
            {
                if(!IsVisible(record))continue;
                if(Panel.inventory.Sales.CounterPresented&&Panel.Model.IsReserved(record.instanceId)){reservedItems.Add(record);continue;}
                seen.Add(record.instanceId);InventoryItemView view;
                if(!itemObjects.TryGetValue(record.instanceId,out view))
                {
                    view=recycleItems&&itemPool.Count>0?itemPool.Pop():Instantiate(itemTemplate,itemLayer);TopLeft(view.gameObject);view.gameObject.SetActive(true);
                    itemObjects.Add(record.instanceId,view);
                }
                view.Bind(Panel,this,record);
            }
            removed.Clear();foreach(var pair in itemObjects)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(var id in removed){var view=itemObjects[id];view.gameObject.SetActive(false);if(recycleItems&&itemPool.Count<maximumPooledItems){view.ReleaseVisual();itemPool.Push(view);}else Destroy(view.gameObject);itemObjects.Remove(id);}
            RefreshReservations();RefreshPartitions();HidePreview();
            shownVersion=version;shownContainer=containerId;shownCounter=counter;shownStartColumn=startColumn;shownStartRow=startRow;shownViewColumns=viewColumns;shownViewRows=viewRows;
        }
        void RefreshReservations()
        {
            if(reservedGraphic==null&&reservedItems.Count==0)return;
            if(reservedGraphic==null){var go=new GameObject("Reserved Return Slots",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(transform,false);TopLeft(go);reservedGraphic=go.AddComponent<ReservedSlotGraphic>();}
            reservedGraphic.rectTransform.sizeDelta=LogicalSize;reservedGraphic.rectTransform.anchoredPosition=new Vector2(ContentOrigin,-ContentOrigin);reservedGraphic.Show(reservedItems,this);reservedGraphic.transform.SetAsLastSibling();
        }
        public bool IsVisible(ItemRecord item){return item!=null&&item.x>=startColumn&&item.y>=startRow&&item.x+Panel.Model.Width(item)<=startColumn+columns&&item.y+Panel.Model.Height(item)<=startRow+rows;}
        void RefreshPartitions()
        {
            var installation=Panel.Model.Installation(containerId);
            var definition=installation==null||Panel.Model.Displays==null?null:Panel.Model.Displays.Type(installation.typeId);
            bool any=renderPartitions&&startColumn==0&&startRow==0&&viewColumns==0&&viewRows==0&&definition!=null&&((definition.horizontalDividers!=null&&definition.horizontalDividers.Length>0)||(definition.verticalDividers!=null&&definition.verticalDividers.Length>0)||(definition.lowerVerticalDividers!=null&&definition.lowerVerticalDividers.Length>0));
            if(!any){if(partitionGraphic!=null)partitionGraphic.gameObject.SetActive(false);return;}
            if(partitionGraphic==null)
            {
                var go=new GameObject("Shelf Partitions",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(transform,false);
                TopLeft(go);partitionGraphic=go.AddComponent<DisplayPartitionGraphic>();partitionGraphic.raycastTarget=false;
            }
            partitionGraphic.gameObject.SetActive(true);
            partitionGraphic.rectTransform.sizeDelta=LogicalSize;partitionGraphic.rectTransform.anchoredPosition=new Vector2(ContentOrigin,-ContentOrigin);
            partitionGraphic.Configure(definition,CellSize,dividerThickness,dividerColor);partitionGraphic.transform.SetAsLastSibling();
        }
        public bool PointerCell(Vector2 screen, out int x, out int y)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screen, Panel.EventCamera, out local))
            { x = y = 0; return false; }
            int cx=Mathf.FloorToInt((local.x-ContentOrigin)/cellWidth),cy=Mathf.FloorToInt((-local.y-ContentOrigin)/cellHeight);
            x=cx+startColumn;y=cy+startRow;return cx>=0&&cx<columns&&cy>=0&&cy<rows;
        }
        public void ShowPreview(PlacementPreview preview)
        {
            if(previewGraphic==null)
            {
                var go=new GameObject("Placement Preview Mesh",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(transform,false);
                RectTransform rect=TopLeft(go);rect.sizeDelta=Rect.sizeDelta;
                previewGraphic=go.AddComponent<PlacementPreviewGraphic>();previewGraphic.raycastTarget=false;
            }
            previewGraphic.rectTransform.sizeDelta=LogicalSize;previewGraphic.rectTransform.anchoredPosition=new Vector2(ContentOrigin,-ContentOrigin);previewGraphic.transform.SetAsLastSibling();
            if(startColumn!=0||startRow!=0||viewColumns>0||viewRows>0)
            {
                localPreview.allowed=preview.allowed;localPreview.error=preview.error;localPreview.cells.Clear();
                foreach(var original in preview.cells){var cell=original;cell.x-=startColumn;cell.y-=startRow;if(cell.x>=0&&cell.x<columns&&cell.y>=0&&cell.y<rows)localPreview.cells.Add(cell);}
                previewGraphic.Show(localPreview,CellSize,cellGap);
            }
            else previewGraphic.Show(preview,CellSize,cellGap);
        }
        public void HidePreview() { if(previewGraphic!=null)previewGraphic.ClearPreview(); }
        public void OnDrop(PointerEventData e)
        {
            if(Panel==null||!acceptsDrop)return;
            var counter=e.pointerDrag==null?null:e.pointerDrag.GetComponent<CounterItemView>();
            if(counter!=null){counter.TryReturnTo(this,e);return;}Panel.drag.Drop(this,e.position);
        }
    }
}
