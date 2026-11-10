using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    // 루트 RectTransform은 바깥 사각형이지만, 표시/클릭은 실제 점유 칸만 사용합니다.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InventoryItemView : MonoBehaviour, ICanvasRaycastFilter,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // icon은 Sprite 설정을 받을 템플릿이며 화면에는 칸별로 잘라 표시합니다.
        public global::UnityEngine.UI.Image icon;
        public Text nameLabel;
        public InventoryPanel Panel { get; private set; }
        public InventoryGridView Grid { get; private set; }
        public ItemRecord Record { get; private set; }
        public RectTransform Rect { get { return (RectTransform)transform; } }
        readonly List<GameObject> fragments = new List<GameObject>();
        Sprite displayedSprite, tileSprite;
        Color tileColor;
        int visualRotation = -1;
        float visualCellSize;
        bool styleCaptured;

        public void Bind(InventoryPanel panel, InventoryGridView grid, ItemRecord record)
        {
            Panel = panel; Grid = grid; Record = record;
            QualityDefinition quality = panel.Catalog.FindQuality(record.itemId, record.quality);
            displayedSprite = panel.FindIcon(quality.iconKey);
            var rootImage = GetComponent<global::UnityEngine.UI.Image>();
            if (!styleCaptured) { tileColor = rootImage.color; tileSprite = rootImage.sprite; styleCaptured = true; }
            // 투명한 루트는 포인터를 받을 용도입니다. 실제 배경은 점유 칸별로 그립니다.
            // 투명 메시 제거를 꺼서 루트의 Raycast가 유지되도록 합니다.
            rootImage.color = new Color(1, 1, 1, 0); rootImage.sprite = null;
            rootImage.canvasRenderer.cullTransparentMesh = false;
            rootImage.alphaHitTestMinimumThreshold = 0; rootImage.raycastTarget = true;
            icon.gameObject.SetActive(false); visualRotation = -1;
            ApplyRotation(record.rotation, grid.cellSize);
            Rect.anchoredPosition = new Vector2(record.x * grid.cellSize, -record.y * grid.cellSize);
        }
        static RectTransform Child(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        public void ApplyRotation(int rotation, float cellSize)
        {
            if (visualRotation == rotation && visualCellSize == cellSize) return;
            visualRotation = rotation; visualCellSize = cellSize;
            foreach (GameObject fragment in fragments) { fragment.SetActive(false); Destroy(fragment); }
            fragments.Clear();
            ItemFootprint shape = Panel.Catalog.Shape(Record.itemId);
            ItemDefinition definition = Panel.Catalog.Find(Record.itemId);
            int width = shape.Width(rotation), height = shape.Height(rotation);
            Rect.sizeDelta = new Vector2(width * cellSize, height * cellSize);
            GridCell[] cells = shape.Cells(rotation);
            foreach (GridCell cell in cells)
            {
                RectTransform mask = Child("Occupied Cell", transform, Vector2.one * cellSize,
                    new Vector2(cell.x * cellSize, -cell.y * cellSize));
                mask.gameObject.AddComponent<RectMask2D>(); fragments.Add(mask.gameObject);
                RectTransform background = Child("Tile", mask, Vector2.one * (cellSize - 2), new Vector2(1, -1));
                var tile = background.gameObject.AddComponent<global::UnityEngine.UI.Image>();
                tile.sprite = tileSprite; tile.color = tileColor; tile.raycastTarget = false;
                if (displayedSprite == null) continue;
                // 원본은 한 장의 Sprite입니다. 같은 전체 이미지를 각 점유 칸의 마스크로 잘라 그립니다.
                RectTransform imageRect = Child("Clipped Icon", mask,
                    new Vector2(definition.width * cellSize - 6, definition.height * cellSize - 6),
                    new Vector2((width * .5f - cell.x) * cellSize, (-height * .5f + cell.y) * cellSize));
                imageRect.pivot = new Vector2(.5f, .5f);
                imageRect.localRotation = Quaternion.Euler(0, 0, -90 * rotation);
                var image = imageRect.gameObject.AddComponent<global::UnityEngine.UI.Image>();
                image.sprite = displayedSprite; image.preserveAspect = true; image.raycastTarget = false;
            }
            // 빈 부분에 글자까지 걸리지 않도록 첫 점유 칸 안에만 대체 이름을 표시합니다.
            nameLabel.text = displayedSprite == null ? Panel.Catalog.FindQuality(Record.itemId, Record.quality).name : "";
            nameLabel.rectTransform.anchorMin = nameLabel.rectTransform.anchorMax = nameLabel.rectTransform.pivot = new Vector2(0, 1);
            nameLabel.rectTransform.anchoredPosition = new Vector2(cells[0].x * cellSize + 3, -cells[0].y * cellSize - 3);
            nameLabel.rectTransform.sizeDelta = Vector2.one * (cellSize - 6); nameLabel.transform.SetAsLastSibling();
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (Panel == null || Record == null || visualRotation < 0) return false;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screenPoint, eventCamera, out local)) return false;
            int x = Mathf.FloorToInt(local.x / visualCellSize), y = Mathf.FloorToInt(-local.y / visualCellSize);
            // false이면 이 카드의 빈 부분을 통과해 뒤쪽 물건/격자가 포인터를 받을 수 있습니다.
            return Panel.Catalog.Shape(Record.itemId).Contains(visualRotation, x, y);
        }
        public void SetDragVisual(bool dragging)
        { CanvasGroup group = GetComponent<CanvasGroup>(); group.alpha = dragging ? .3f : 1; group.blocksRaycasts = !dragging; }
        public void OnPointerClick(PointerEventData e)
        { if (e.button == PointerEventData.InputButton.Left) Panel.Select(Grid.containerId, Record.instanceId); }
        public void OnBeginDrag(PointerEventData e)
        { if (e.button == PointerEventData.InputButton.Left) Panel.drag.Begin(this, e); }
        public void OnDrag(PointerEventData e) { Panel.drag.MovePointer(e.position); }
        public void OnEndDrag(PointerEventData e) { Panel.drag.Cancel(false); }
    }
}
