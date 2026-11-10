using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GeneralStore.Inventory
{
    // 드래그는 그림만 옮깁니다. 실제 물건은 정상 드롭에 성공할 때만 이동합니다.
    public sealed class InventoryDragHandler : MonoBehaviour
    {
        public RectTransform dragLayer;
        InventoryPanel panel;
        InventoryItemView sourceView, ghost;
        ItemRecord selectedItem;
        string sourceContainer;
        int grabbedX, grabbedY;
        int rotation;
        bool active;
        long startedRevision;
        Vector2 pointer;
        public bool IsDragging { get { return active; } }
        public void Initialize(InventoryPanel owner) { panel = owner; }
        public void Begin(InventoryItemView view, PointerEventData e)
        {
            Cancel(false); if (panel == null) return;
            selectedItem = panel.Model.GetItem(view.Grid.containerId, view.Record.instanceId);
            if (selectedItem == null) return;
            sourceView = view; sourceContainer = view.Grid.containerId;
            startedRevision = panel.Model.Revision; rotation = selectedItem.rotation; pointer = e.position;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Rect, pointer, panel.EventCamera, out local);
            grabbedX = Mathf.Clamp(Mathf.FloorToInt(local.x / view.Grid.cellSize), 0, panel.Model.Width(selectedItem) - 1);
            grabbedY = Mathf.Clamp(Mathf.FloorToInt(-local.y / view.Grid.cellSize), 0, panel.Model.Height(selectedItem) - 1);
            if (!panel.Catalog.Shape(selectedItem.itemId).Contains(rotation, grabbedX, grabbedY)) return;
            active = true; sourceView.SetDragVisual(true);
            ghost = Instantiate(view.Grid.itemTemplate, dragLayer);
            ghost.gameObject.SetActive(true); ghost.Bind(panel, view.Grid, selectedItem.Copy()); ghost.enabled = false;
            CanvasGroup group = ghost.GetComponent<CanvasGroup>(); group.alpha = .8f; group.blocksRaycasts = false;
            ghost.Rect.anchorMin = ghost.Rect.anchorMax = new Vector2(.5f, .5f);
            panel.Select(sourceContainer, selectedItem.instanceId); panel.ShowMessage("드래그 중 R: 회전 / Esc: 취소");
            MovePointer(pointer);
        }
        void Update()
        {
            if (!active) return;
            if (panel.Model.Revision != startedRevision)
            { Cancel(false); panel.ShowMessage("재고가 변경되어 드래그를 취소했습니다."); return; }
            if (InventoryKeys.CancelPressed()) { Cancel(true); return; }
            if (InventoryKeys.RotatePressed())
            {
                ItemDefinition definition = panel.Catalog.Find(selectedItem.itemId);
                if (definition.canRotate)
                {
                    ItemFootprint shape = panel.Catalog.Shape(selectedItem.itemId);
                    int oldX = grabbedX;
                    grabbedX = shape.Height(rotation) - 1 - grabbedY; grabbedY = oldX;
                    rotation = (rotation + 1) % 4; // R 네 번이면 원래 모양/잡은 칸으로 돌아옵니다.
                }
                else panel.ShowMessage("이 물건은 회전할 수 없습니다.");
            }
            MovePointer(pointer);
        }
        InventoryGridView HoverGrid()
        {
            if (EventSystem.current == null) return null;
            var e = new PointerEventData(EventSystem.current) { position = pointer };
            var results = new List<RaycastResult>(); EventSystem.current.RaycastAll(e, results);
            // 맨 위 UI가 가리고 있으면 뒤쪽 격자로 통과해 드롭되지 않게 합니다.
            return results.Count == 0 ? null : results[0].gameObject.GetComponentInParent<InventoryGridView>();
        }
        MoveRequest CreateRequest(InventoryGridView target, Vector2 position)
        {
            int x, y; if (!target.PointerCell(position, out x, out y)) return null;
            return new MoveRequest { sourceContainer = sourceContainer, targetContainer = target.containerId,
                instanceId = selectedItem.instanceId, x = x - grabbedX, y = y - grabbedY,
                rotation = rotation, expectedRevision = startedRevision };
        }
        public void MovePointer(Vector2 position)
        {
            if (!active) return; pointer = position;
            foreach (InventoryGridView grid in panel.grids) grid.HidePreview();
            InventoryGridView target = HoverGrid(); if (target != null && target.Panel != panel) target = null;
            float cell = target == null ? sourceView.Grid.cellSize : target.cellSize;
            ghost.ApplyRotation(rotation, cell);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, pointer, panel.EventCamera, out local);
            ghost.Rect.anchoredPosition = local + new Vector2(-(grabbedX + .5f) * cell, (grabbedY + .5f) * cell);
            if (target == null) return;
            MoveRequest request = CreateRequest(target, pointer); if (request == null) return;
            PlacementPreview preview = panel.Model.Preview(request);
            target.ShowPreview(preview);
        }
        public void Drop(InventoryGridView target, Vector2 position)
        {
            if (!active || target.Panel != panel) return;
            MoveRequest request = CreateRequest(target, position);
            string error = "격자 밖에는 놓을 수 없습니다.";
            if (request != null && panel.Model.TryMove(request, out error))
            { panel.Select(target.containerId, selectedItem.instanceId); panel.ShowMessage("물건을 옮겼습니다."); }
            else panel.ShowMessage(error);
            Cancel(false);
        }
        public bool TryGetProposal(out ItemProposal proposal)
        {
            proposal = null;
            if (!active || panel.Model.Revision != startedRevision
                || panel.Model.GetItem(sourceContainer, selectedItem.instanceId) == null) return false;
            proposal = new ItemProposal { containerId = sourceContainer, instanceId = selectedItem.instanceId,
                itemId = selectedItem.itemId, quality = selectedItem.quality, revision = startedRevision };
            return true;
        }
        public void Cancel(bool showMessage)
        {
            if (!active) return; active = false;
            if (sourceView != null) sourceView.SetDragVisual(false);
            if (ghost != null) { ghost.gameObject.SetActive(false); Destroy(ghost.gameObject); }
            if (panel != null)
            {
                foreach (InventoryGridView grid in panel.grids) if (grid != null) grid.HidePreview();
                if (showMessage) panel.ShowMessage("배치를 취소했습니다. 물건은 그대로입니다.");
            }
            sourceView = ghost = null;
        }
        void OnDisable() { Cancel(false); }
        void OnApplicationFocus(bool focused) { if (!focused) Cancel(false); }
    }
    internal static class InventoryKeys
    {
        public static bool RotatePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }
        public static bool CancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
