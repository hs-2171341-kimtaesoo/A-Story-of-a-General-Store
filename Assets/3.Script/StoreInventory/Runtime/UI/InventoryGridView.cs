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
        [Min(16)] public float cellSize = 78;
        public Color cellColor = new Color(.16f, .14f, .1f);
        public Sprite cellSprite;
        public RectTransform itemLayer;
        public InventoryItemView itemTemplate;
        public InventoryPanel Panel { get; private set; }
        readonly List<GameObject> cellObjects = new List<GameObject>();
        readonly List<GameObject> itemObjects = new List<GameObject>();
        int columns, rows;
        PlacementPreviewGraphic previewGraphic;
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
            Rect.sizeDelta = new Vector2(container.columns * cellSize, container.rows * cellSize);
            if (columns != container.columns || rows != container.rows)
            {
                foreach (GameObject go in cellObjects) { go.SetActive(false); Destroy(go); }
                cellObjects.Clear(); columns = container.columns; rows = container.rows;
                for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
                {
                    var go = new GameObject("Cell " + x + "," + y, typeof(RectTransform), typeof(global::UnityEngine.UI.Image));
                    go.transform.SetParent(transform, false);
                    RectTransform cell = TopLeft(go); cell.sizeDelta = Vector2.one * (cellSize - 1);
                    cell.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                    global::UnityEngine.UI.Image image = go.GetComponent<global::UnityEngine.UI.Image>(); image.color = cellColor; image.sprite = cellSprite; image.raycastTarget = false;
                    cellObjects.Add(go);
                }
                itemLayer.SetAsLastSibling();
            }
            foreach (GameObject go in itemObjects) { go.SetActive(false); Destroy(go); }
            itemObjects.Clear();
            foreach (ItemRecord record in container.items)
            {
                InventoryItemView view = Instantiate(itemTemplate, itemLayer);
                TopLeft(view.gameObject); view.gameObject.SetActive(true); view.Bind(Panel, this, record);
                itemObjects.Add(view.gameObject);
            }
            HidePreview();
        }
        public bool PointerCell(Vector2 screen, out int x, out int y)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screen, Panel.EventCamera, out local))
            { x = y = 0; return false; }
            x = Mathf.FloorToInt(local.x / cellSize); y = Mathf.FloorToInt(-local.y / cellSize);
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
            previewGraphic.rectTransform.sizeDelta=Rect.sizeDelta;previewGraphic.transform.SetAsLastSibling();
            previewGraphic.Show(preview,cellSize);
        }
        public void HidePreview() { if(previewGraphic!=null)previewGraphic.ClearPreview(); }
        public void OnDrop(PointerEventData e) { if (Panel != null) Panel.drag.Drop(this, e.position); }
    }
}
