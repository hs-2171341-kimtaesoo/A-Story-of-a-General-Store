using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GeneralStore.Inventory
{
    // 드래그는 그림만 옮깁니다. 실제 물건은 정상 드롭에 성공할 때만 이동합니다.
    public sealed class InventoryDragHandler : MonoBehaviour,IDragHandler,IEndDragHandler
    {
        public RectTransform dragLayer;
        InventoryPanel panel;
        InventoryItemView sourceView, ghost;
        InventoryItemView ghostTemplate;
        readonly List<RaycastResult> rayResults=new List<RaycastResult>(32);
        readonly HoverSwitchTimer hoverTimer=new HoverSwitchTimer();
        readonly HoverSwitchTimer displayHoverTimer=new HoverSwitchTimer();
        PointerEventData rayPointer;EventSystem raySystem;
        InventoryGridView previewGrid;
        bool previewValid;
        int previewX,previewY,previewRotation;
        ItemRecord selectedItem;
        string sourceContainer;
        int grabbedX, grabbedY;
        int rotation;
        bool active;
        long startedRevision;
        Vector2 pointer;
        public event System.Action<bool> DraggingChanged;
        void NotifyDragging(bool value){var callback=DraggingChanged;if(callback!=null)callback(value);}
        public bool IsDragging { get { return active; } }
        public void Initialize(InventoryPanel owner) { panel = owner;UiPerformanceLayers.Ensure(dragLayer.gameObject,false); }
        void PrepareGhost(InventoryItemView template)
        {
            if(ghost!=null&&ghostTemplate!=template){Destroy(ghost.gameObject);ghost=null;}
            if(ghost==null){ghost=Instantiate(template,dragLayer);ghostTemplate=template;}
            ghost.gameObject.SetActive(true);ghost.enabled=false;previewValid=false;
        }
        public void Begin(InventoryItemView view, PointerEventData e)
        {
            Cancel(false); if (panel == null) return;
            selectedItem = panel.Model.GetItem(view.Grid.containerId, view.Record.instanceId);
            if (selectedItem == null) return;
            if (panel.Model.IsReserved(selectedItem.instanceId)) { panel.ShowMessage("손님 선택 상품은 이동할 수 없습니다."); return; }
            sourceView = view; sourceContainer = view.Grid.containerId;
            startedRevision = panel.Model.Revision; rotation = selectedItem.rotation; pointer = e.position;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Rect, pointer, panel.EventCamera, out local);
            grabbedX = Mathf.Clamp(Mathf.FloorToInt(local.x / view.Grid.cellWidth), 0, panel.Model.Width(selectedItem) - 1);
            grabbedY = Mathf.Clamp(Mathf.FloorToInt(-local.y / view.Grid.cellHeight), 0, panel.Model.Height(selectedItem) - 1);
            active = true;NotifyDragging(true);e.pointerDrag=gameObject; sourceView.SetDragVisual(true);
            PrepareGhost(view.Grid.itemTemplate);ghost.Bind(panel, view.Grid, selectedItem.Copy()); ghost.enabled = false;
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
        }
        void LateUpdate(){if(active)RefreshPointer();}
        InventoryGridView HoverGrid()
        {
            if (EventSystem.current == null) return null;
            if(rayPointer==null||raySystem!=EventSystem.current)
            {raySystem=EventSystem.current;rayPointer=new PointerEventData(raySystem);}
            rayPointer.position=pointer;rayResults.Clear();using(StoreProfiler.DragRaycast.Auto())EventSystem.current.RaycastAll(rayPointer,rayResults);
            // 맨 위 UI가 가리고 있으면 뒤쪽 격자로 통과해 드롭되지 않게 합니다.
            var tab=rayResults.Count==0?null:rayResults[0].gameObject.GetComponentInParent<WarehouseTabTarget>();
            string id=tab!=null&&tab.warehouse.SelectedId!=tab.containerId?tab.containerId:null;
            if(hoverTimer.Tick(id,Time.unscaledDeltaTime,tab==null?1:tab.warehouse.hoverSwitchSeconds))tab.warehouse.SelectWhileDragging(id);
            var displayButton=rayResults.Count==0?null:rayResults[0].gameObject.GetComponentInParent<LeftDisplayButtonView>();
            string displayId=displayButton==null?null:displayButton.containerId;
            if(displayHoverTimer.Tick(displayId,Time.unscaledDeltaTime,displayButton==null?1:displayButton.menu.hoverOpenSeconds))
                displayButton.menu.OpenFromHover(displayId);
            return rayResults.Count == 0 ? null : rayResults[0].gameObject.GetComponentInParent<InventoryGridView>();
        }
        MoveRequest CreateRequest(InventoryGridView target, Vector2 position)
        {
            int x, y; if (!target.PointerCell(position, out x, out y)) return null;
            return new MoveRequest { sourceContainer = sourceContainer, targetContainer = target.containerId,
                instanceId = selectedItem.instanceId, x = x - grabbedX, y = y - grabbedY,
                rotation = rotation, expectedRevision = startedRevision };
        }
        // 원본 카드가 탭 변경으로 파괴되어도 계속 살아 있는 이 처리기가 포인터를 받습니다.
        public void OnDrag(PointerEventData e){MovePointer(e.position);}public void OnEndDrag(PointerEventData e){Cancel(false);}
        public void InvalidatePreview(){HideCachedPreview();}
        public void MovePointer(Vector2 position){if(active)pointer=position;}
        void HideCachedPreview()
        {if(previewGrid!=null)previewGrid.HidePreview();previewGrid=null;previewValid=false;}
        // 작업창이 축소되어도 최상위 Drag Layer의 물건 그림은 화면상 칸 크기와 일치합니다.
        Vector2 CellSizeInDragLayer(InventoryGridView grid)
        {
            Vector3 sourceScale=grid.transform.lossyScale,layerScale=dragLayer.lossyScale;
            return new Vector2(grid.cellWidth*Mathf.Abs(sourceScale.x)/Mathf.Max(.0001f,Mathf.Abs(layerScale.x)),
                grid.cellHeight*Mathf.Abs(sourceScale.y)/Mathf.Max(.0001f,Mathf.Abs(layerScale.y)));
        }
        void RefreshPointer()
        {
            if(panel.Model.Revision!=startedRevision){Cancel(false);return;}
            InventoryGridView target=HoverGrid();if(target!=null&&target.Panel!=panel)target=null;
            InventoryGridView visualGrid=target!=null?target:(sourceView!=null?sourceView.Grid:panel.grids[0]);
            Vector2 cell=CellSizeInDragLayer(visualGrid);
            var counterDrop=rayResults.Count==0?null:rayResults[0].gameObject.GetComponentInParent<RecommendationDropArea>();
            if(target==null&&counterDrop!=null&&counterDrop.counter!=null)cell=counterDrop.counter.CellSizeIn(dragLayer);
            var counterItem=rayResults.Count==0?null:rayResults[0].gameObject.GetComponentInParent<CounterItemView>();
            if(target==null&&counterItem!=null&&counterItem.Trade!=null)cell=counterItem.Trade.CellSizeIn(dragLayer);
            ghost.ApplyRotation(rotation,cell);Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer,pointer,panel.EventCamera,out local);
            Vector2 pos=local+new Vector2(-(grabbedX+.5f)*cell.x,(grabbedY+.5f)*cell.y);
            if(ghost.Rect.anchoredPosition.x!=pos.x||ghost.Rect.anchoredPosition.y!=pos.y)ghost.Rect.anchoredPosition=pos;
            if(target==null){HideCachedPreview();return;}
            int x,y;if(!target.PointerCell(pointer,out x,out y)){HideCachedPreview();return;}
            x-=grabbedX;y-=grabbedY;
            if(previewValid&&previewGrid==target&&previewX==x&&previewY==y&&previewRotation==rotation)return;
            if(previewGrid!=target)HideCachedPreview();
            previewGrid=target;previewX=x;previewY=y;previewRotation=rotation;previewValid=true;
            using(StoreProfiler.Preview.Auto())target.ShowPreview(panel.Model.Preview(new MoveRequest{sourceContainer=sourceContainer,targetContainer=target.containerId,
                instanceId=selectedItem.instanceId,x=x,y=y,rotation=rotation,expectedRevision=startedRevision}));
        }
        public void Drop(InventoryGridView target, Vector2 position)
        {
            if (!active || target.Panel != panel) return;
            MoveRequest request = CreateRequest(target, position);
            panel.HandleDrop(request);
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
            if (!active) return; active = false;NotifyDragging(false);hoverTimer.Reset();displayHoverTimer.Reset();
            if (sourceView != null) sourceView.SetDragVisual(false);
            if (ghost != null)ghost.gameObject.SetActive(false);
            HideCachedPreview();
            if (panel != null)
            {
                foreach (InventoryGridView grid in panel.grids) if (grid != null) grid.HidePreview();
                if (showMessage) panel.ShowMessage("배치를 취소했습니다. 물건은 그대로입니다.");
            }
            sourceView = null;
        }
        void OnDestroy(){if(ghost!=null)Destroy(ghost.gameObject);}
        void OnDisable() { Cancel(false); }
        void OnApplicationFocus(bool focused) { if (!focused) Cancel(false); }
    }
    internal static class InventoryKeys
    {
        public static bool TryPointerPosition(out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM
            if(Mouse.current==null){position=Vector2.zero;return false;}position=Mouse.current.position.ReadValue();return true;
#else
            var p=Input.mousePosition;position=new Vector2(p.x,p.y);return true;
#endif
        }
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
