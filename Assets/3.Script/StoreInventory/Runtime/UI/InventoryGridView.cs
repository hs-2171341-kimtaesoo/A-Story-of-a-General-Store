using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    // 여러 칸을 차지하므로 GridLayoutGroup이 아니라 좌표로 카드를 배치합니다.
    public sealed class InventoryGridView : MonoBehaviour, IDropHandler
    {
        public string containerId = "warehouse";
        [Min(16)] public float cellWidth = 78;
        [Min(16)] public float cellHeight = 78;
        [Range(0,8)]public float cellGap=2;
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
        float renderedWidth,renderedHeight,renderedGap=-1;
        int columns, rows;
        public Color selectionColor=new Color(1,.75f,.15f,1);
        [Min(1)]public float selectionThickness=3;
        ItemSelectionGraphic selectionGraphic;
        PlacementPreviewGraphic previewGraphic;
        DisplayPartitionGraphic partitionGraphic;
        public Color dividerColor=new Color(.5f,.29f,.1f,1);
        [Range(2,16)]public float dividerThickness=8;
        public RectTransform Rect { get { return (RectTransform)transform; } }
        public void Initialize(InventoryPanel panel) { Panel = panel; if(gameObject.activeInHierarchy) Refresh(); }
        void OnEnable() { if(Panel!=null) Refresh(); }
        static RectTransform TopLeft(GameObject go)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); return rect;
        }
        public void Refresh()
        {
            ContainerRecord container = Panel.Model.GetContainer(containerId);
            if (container == null) { Debug.LogError("없는 컨테이너: " + containerId, this); return; }
                        // 1:1 고정 칸. 바깥 테두리도 타일 사이와 같은 간격입니다.
            cellHeight=cellWidth;
            Rect.sizeDelta=new Vector2(container.columns*cellWidth+cellGap,container.rows*cellHeight+cellGap);
            itemLayer.anchorMin=itemLayer.anchorMax=itemLayer.pivot=new Vector2(0,1);
            itemLayer.anchoredPosition=new Vector2(ContentOrigin,-ContentOrigin);
            itemLayer.sizeDelta=new Vector2(container.columns*cellWidth,container.rows*cellHeight);
            if (columns != container.columns || rows != container.rows || renderedWidth!=cellWidth || renderedHeight!=cellHeight||renderedGap!=cellGap)
            {
                columns = container.columns; rows = container.rows;renderedWidth=cellWidth;renderedHeight=cellHeight;renderedGap=cellGap;
                int needed=columns*rows;
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
            seen.Clear();
            foreach (ItemRecord record in container.items)
            {
                seen.Add(record.instanceId);InventoryItemView view;
                if(!itemObjects.TryGetValue(record.instanceId,out view))
                {
                    view=Instantiate(itemTemplate,itemLayer);TopLeft(view.gameObject);view.gameObject.SetActive(true);
                    itemObjects.Add(record.instanceId,view);
                }
                view.Bind(Panel,this,record);
            }
            removed.Clear();foreach(var pair in itemObjects)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(var id in removed){itemObjects[id].gameObject.SetActive(false);Destroy(itemObjects[id].gameObject);itemObjects.Remove(id);}
            RefreshPartitions();HidePreview();RefreshSelection();
        }
        void RefreshPartitions()
        {
            var installation=Panel.Model.Installation(containerId);
            var definition=installation==null||Panel.Model.Displays==null?null:Panel.Model.Displays.Type(installation.typeId);
            bool any=definition!=null&&((definition.horizontalDividers!=null&&definition.horizontalDividers.Length>0)||(definition.verticalDividers!=null&&definition.verticalDividers.Length>0));
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
        public void RefreshSelection()
        {
            if(Panel==null)return;var item=Panel.Model.GetItem(containerId,Panel.Selection.Get(containerId));
            if(item==null&&selectionGraphic==null)return;
            if(selectionGraphic==null)
            {
                var go=new GameObject("Selected Item Outline",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(transform,false);
                TopLeft(go);selectionGraphic=go.AddComponent<ItemSelectionGraphic>();selectionGraphic.raycastTarget=false;
            }
            selectionGraphic.rectTransform.sizeDelta=LogicalSize;selectionGraphic.rectTransform.anchoredPosition=new Vector2(ContentOrigin,-ContentOrigin);selectionGraphic.color=selectionColor;
            selectionGraphic.Show(item,item==null?null:Panel.Catalog.Shape(item.itemId),CellSize,selectionThickness);selectionGraphic.transform.SetAsLastSibling();
        }
        public bool PointerCell(Vector2 screen, out int x, out int y)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screen, Panel.EventCamera, out local))
            { x = y = 0; return false; }
            x = Mathf.FloorToInt((local.x-ContentOrigin) / cellWidth); y = Mathf.FloorToInt((-local.y-ContentOrigin) / cellHeight);
            return x >= 0 && x < columns && y >= 0 && y < rows;
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
            previewGraphic.Show(preview,CellSize,cellGap);
        }
        public void HidePreview() { if(previewGraphic!=null)previewGraphic.ClearPreview(); }
        public void OnDrop(PointerEventData e) { if (Panel != null) Panel.drag.Drop(this, e.position); }
    }
}
