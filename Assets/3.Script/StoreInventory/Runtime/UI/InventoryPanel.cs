using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    // 모델과 Canvas를 연결합니다. 이름/설명은 Legacy Text, 아이콘은 Image입니다.
    public sealed class InventoryPanel : MonoBehaviour
    {
        public InventoryRuntime inventory;
        public Canvas canvas;
        public InventoryDragHandler drag;
        public InventoryGridView[] grids;
        public InventoryDetailsView[] detailViews;
        public ShopfrontUI shop;
        public Text titleLabel, messageLabel;
        public Font koreanFont;
        public InventoryModel Model { get { return inventory.Model; } }
        public ItemCatalog Catalog { get { return inventory.Catalog; } }
        public Camera EventCamera { get { return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera; } }
        readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        bool initialized, dirty;
        string selectedContainer, selectedInstance;
        public string SelectedContainerId { get { return selectedContainer; } }
        public string SelectedInstanceId { get { return selectedInstance; } }
        void Start()
        {
            if (inventory == null || canvas == null || drag == null || drag.dragLayer == null || grids == null || grids.Length == 0
                || shop == null || titleLabel == null || messageLabel == null || detailViews == null || detailViews.Length == 0)
            { Debug.LogError("InventoryPanel의 Inspector 참조를 확인하세요.", this); enabled = false; return; }
            foreach (InventoryGridView grid in grids)
            {
                if (grid == null || grid.itemLayer == null || grid.itemTemplate == null
                    || grid.itemTemplate.icon == null || grid.itemTemplate.nameLabel == null
                    || grid.itemTemplate.GetComponent<global::UnityEngine.UI.Image>() == null)
                { Debug.LogError("격자/아이템 템플릿 참조를 확인하세요.", this); enabled = false; return; }
            }
            foreach (InventoryDetailsView detail in detailViews)
                if (detail == null || detail.icon == null || detail.nameLabel == null || detail.bodyLabel == null)
                { Debug.LogError("상세창 참조를 확인하세요.", this); enabled = false; return; }
            string error; if (!inventory.TryInitialize(out error)) { Debug.LogError(error, this); enabled = false; return; }
            foreach (InventoryGridView grid in grids)
                if (Model.GetContainer(grid.containerId) == null)
                { Debug.LogError("존재하지 않는 컨테이너: " + grid.containerId, this); enabled = false; return; }
            if (koreanFont != null)
                foreach (Text text in GetComponentsInChildren<Text>(true)) text.font = koreanFont;
            drag.Initialize(this);
            foreach (InventoryGridView grid in grids) grid.Initialize(this);
            initialized = true; Model.Changed += MarkDirty;
            RefreshDetails(); ShowMessage("드래그: 물건 1개 이동 / R: 회전 / Esc: 취소 / 빈 배경: 바닥에 내려놓기");
        }
        void MarkDirty() { dirty = true; }
        void LateUpdate()
        {
            if (!initialized || !dirty) return; dirty = false;
            // 마우스 이벤트 처리 중 원본 UI를 지우지 않고, 프레임 끝에 갱신합니다.
            drag.Cancel(false);
            foreach (InventoryGridView grid in grids) if (grid.gameObject.activeInHierarchy) grid.Refresh();
            RefreshDetails();
        }
        // 처음 창을 만들 때만 등록하고 배열을 교체합니다. 매 프레임 할당하지 않습니다.
        public void RegisterWindowGrid(InventoryGridView grid,InventoryDetailsView detail)
        {
            var v=new List<InventoryGridView>(grids);v.Add(grid);grids=v.ToArray();var d=new List<InventoryDetailsView>(detailViews);d.Add(detail);detailViews=d.ToArray();
            if(koreanFont!=null)foreach(Text text in grid.GetComponentInParent<DisplayWindowView>().GetComponentsInChildren<Text>(true))text.font=koreanFont;grid.Initialize(this);
        }
        public void UnregisterWindowGrid(InventoryGridView grid,InventoryDetailsView detail)
        {var v=new List<InventoryGridView>(grids);v.Remove(grid);grids=v.ToArray();var d=new List<InventoryDetailsView>(detailViews);d.Remove(detail);detailViews=d.ToArray();}
        public Sprite FindIcon(string iconKey)
        {
            Sprite sprite;
            if (!icons.TryGetValue(iconKey, out sprite))
            {
                sprite = Resources.Load<Sprite>("ItemIcons/" + iconKey); icons.Add(iconKey, sprite);
                if (sprite == null) Debug.LogWarning("아이콘 없음: Resources/ItemIcons/" + iconKey + " (이름으로 표시)", this);
            }
            return sprite;
        }
        public void Select(string containerId, string instanceId)
        { selectedContainer = containerId; selectedInstance = instanceId; if (initialized) RefreshDetails(); }
        public void ShowMessage(string message) { if (messageLabel != null) messageLabel.text = message; }
        void RefreshDetails()
        {
            ContainerRecord warehouse = Model.GetContainer(grids[0].containerId);
            int usedCells = 0;
            if (warehouse != null) foreach (ItemRecord record in warehouse.items) usedCells += Catalog.Shape(record.itemId).CellCount;
            titleLabel.text = warehouse == null ? "격자 인벤토리" : "보관함 " + warehouse.items.Count + "개 / " + usedCells
                + "칸 사용 (전체 " + warehouse.columns * warehouse.rows + "칸)";
            ItemRecord item = Model.GetItem(selectedContainer, selectedInstance);
            foreach (InventoryDetailsView detail in detailViews) detail.Render(this, item);

        }
        public void HandleDrop(MoveRequest request)
        {
            if (request == null) { ShowMessage("격자 밖에는 놓을 수 없습니다."); return; }
            string validationError;
            if(!Model.CanMove(request,out validationError)){ShowMessage(validationError);return;}
            if (InventoryModel.IsDisplay(request.targetContainer) && request.sourceContainer != request.targetContainer)
            { shop.RequestDisplayPlacement(request); return; }
            string error;
            if (Model.TryMove(request, out error)) { Select(request.targetContainer, request.instanceId); ShowMessage("물건을 옮겼습니다."); }
            else ShowMessage(error);
        }
        public void SaveInventory()
        {
            if (!initialized) return;
            if (drag.IsDragging) { ShowMessage("드래그를 마친 뒤 저장하세요."); return; }
            string error; ShowMessage(inventory.Save(out error) ? "저장 완료" : "저장 실패: " + error);
        }
        public void LoadInventory()
        {
            if (!initialized) return; drag.Cancel(false); shop.CancelDisplayPrice();
            string error; ShowMessage(inventory.Load(out error) ? "불러오기 완료" : "불러오기 실패: " + error);
        }
        void OnDisable() { if (drag != null) drag.Cancel(false); }
        void OnDestroy() { if (initialized && inventory != null && inventory.Model != null) inventory.Model.Changed -= MarkDirty; }
    }
}
