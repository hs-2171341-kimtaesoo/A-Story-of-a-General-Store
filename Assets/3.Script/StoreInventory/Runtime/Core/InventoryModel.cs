using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    // 점유 마스크 기반 낱개 인벤토리입니다. 모양의 빈칸은 다른 물건과 공유할 수 있습니다.
    public sealed class InventoryModel
    {
        public readonly ItemCatalog Catalog;
        InventorySave state;
        public long Revision { get; private set; }
        public event Action Changed;
        public InventoryModel(ItemCatalog catalog, InventorySave initial)
        {
            Catalog = catalog ?? throw new ArgumentNullException("catalog");
            Validate(initial); state = initial.Copy();
        }
        static ContainerRecord Find(InventorySave save, string id) { return save.containers.Find(c => c.containerId == id); }
        public ContainerRecord GetContainer(string id) { ContainerRecord c = Find(state, id); return c == null ? null : c.Copy(); }
        public ItemRecord GetItem(string containerId, string instanceId)
        {
            ContainerRecord c = Find(state, containerId); ItemRecord item = c == null ? null : c.items.Find(i => i.instanceId == instanceId);
            return item == null ? null : item.Copy();
        }
        public InventorySave Snapshot() { return state.Copy(); }
        public int Width(ItemRecord item) { return Catalog.Shape(item.itemId).Width(item.rotation); }
        public int Height(ItemRecord item) { return Catalog.Shape(item.itemId).Height(item.rotation); }
        public GridCell[] Cells(ItemRecord item) { return Catalog.Shape(item.itemId).Cells(item.rotation); }

        PlacementPreview InspectPlacement(ContainerRecord container, ItemRecord item, string ignoredId)
        {
            var result = new PlacementPreview { allowed = true };
            var occupied = new HashSet<int>();
            foreach (ItemRecord other in container.items)
            {
                if (other.instanceId == ignoredId) continue;
                foreach (GridCell offset in Catalog.Shape(other.itemId).InternalCells(other.rotation))
                    occupied.Add((other.y + offset.y) * container.columns + other.x + offset.x);
            }
            foreach (GridCell offset in Catalog.Shape(item.itemId).InternalCells(item.rotation))
            {
                long x = (long)item.x + offset.x, y = (long)item.y + offset.y;
                PlacementCellState cellState;
                if (x < 0 || x >= container.columns || y < 0 || y >= container.rows) cellState = PlacementCellState.Outside;
                else cellState = occupied.Contains((int)y * container.columns + (int)x) ? PlacementCellState.Occupied : PlacementCellState.Free;
                result.cells.Add(new PlacementCell { x = x, y = y, state = cellState });
                if (cellState != PlacementCellState.Free) result.allowed = false;
            }
            if (!result.allowed) result.error = "점유 칸이 다른 물건과 겹치거나 격자 밖입니다.";
            return result;
        }
        void Validate(InventorySave save)
        {
            if (save == null || save.format != "general-store-individual" || save.version != 2 || save.containers == null
                || save.containers.Count < 1 || save.containers.Count > 32) throw new ArgumentException("모양 마스크 v2 저장 파일이 아닙니다.");
            var containers = new HashSet<string>(StringComparer.Ordinal); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ContainerRecord c in save.containers)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.containerId) || !containers.Add(c.containerId)
                    || c.columns < 1 || c.columns > 64 || c.rows < 1 || c.rows > 64 || c.items == null
                    || c.items.Count > c.columns * c.rows) throw new ArgumentException("컨테이너 정보 오류입니다.");
                foreach (ItemRecord item in c.items)
                {
                    ItemDefinition def = item == null ? null : Catalog.Find(item.itemId);
                    if (def == null || string.IsNullOrWhiteSpace(item.instanceId) || !ids.Add(item.instanceId)
                        || Catalog.FindQuality(item.itemId, item.quality) == null || item.rotation < 0 || item.rotation > 3
                        || (item.rotation != 0 && !def.canRotate) || item.purchasePrice < 0
                        || item.x < 0 || item.x >= c.columns || item.y < 0 || item.y >= c.rows)
                        throw new ArgumentException("물건 정보/회전 오류입니다.");
                }
                foreach (ItemRecord item in c.items)
                    if (!InspectPlacement(c, item, item.instanceId).allowed) throw new ArgumentException("점유 칸이 겹치거나 범위를 벗어났습니다.");
            }
        }
        void Commit(InventorySave next) { state = next; Revision++; if (Changed != null) Changed(); }
        public bool TryAdd(string containerId, string itemId, string quality, int amount, int unitCost, out string error)
        {
            error = ""; ItemDefinition def = Catalog.Find(itemId);
            if (def == null || Catalog.FindQuality(itemId, quality) == null || amount < 1 || unitCost < 0)
            { error = "아이템/품질/개수/매입가를 확인하세요."; return false; }
            InventorySave trial = state.Copy(); ContainerRecord target = Find(trial, containerId);
            if (target == null) { error = "없는 컨테이너입니다."; return false; }
            int used = 0; foreach (ItemRecord other in target.items) used += Catalog.Shape(other.itemId).CellCount;
            int free = target.columns * target.rows - used;
            if ((long)amount * Catalog.Shape(itemId).CellCount > free) { error = "필요한 점유 칸 수가 부족합니다."; return false; }
            for (int index = 0; index < amount; index++)
            {
                var item = new ItemRecord { instanceId = Guid.NewGuid().ToString("N"), itemId = itemId, quality = quality, purchasePrice = unitCost };
                bool placed = false;
                for (int rotation = 0; rotation < (def.canRotate ? 4 : 1) && !placed; rotation++)
                {
                    item.rotation = rotation;
                    for (int y = 0; y < target.rows && !placed; y++) for (int x = 0; x < target.columns && !placed; x++)
                    { item.x = x; item.y = y; placed = InspectPlacement(target, item, null).allowed; }
                }
                if (!placed) { error = "모든 물건을 놓을 수 없어 이번 입고 전체를 취소했습니다."; return false; }
                target.items.Add(item);
            }
            Commit(trial); return true;
        }
        PlacementPreview InspectRequest(InventorySave save, MoveRequest request, out ContainerRecord source, out ContainerRecord target, out ItemRecord item)
        {
            source = target = null; item = null;
            if (request == null || (request.expectedRevision >= 0 && request.expectedRevision != Revision))
                return new PlacementPreview { error = "재고가 변경되었습니다. 다시 선택하세요." };
            source = Find(save, request.sourceContainer); target = Find(save, request.targetContainer);
            item = source == null ? null : source.items.Find(i => i.instanceId == request.instanceId);
            if (item == null || target == null) return new PlacementPreview { error = "물건/목적지가 없습니다." };
            if (request.rotation < 0 || request.rotation > 3 || (request.rotation != 0 && !Catalog.Find(item.itemId).canRotate))
                return new PlacementPreview { error = "허용되지 않는 회전입니다." };
            ItemRecord candidate = item.Copy(); candidate.x = request.x; candidate.y = request.y; candidate.rotation = request.rotation;
            return InspectPlacement(target, candidate, item.instanceId);
        }
        // UI의 초록/빨강도 실제 이동도 같은 InspectRequest를 사용합니다.
        public PlacementPreview Preview(MoveRequest request)
        { ContainerRecord source, target; ItemRecord item; return InspectRequest(state, request, out source, out target, out item); }
        public bool CanMove(MoveRequest request, out string error)
        { PlacementPreview preview = Preview(request); error = preview.error; return preview.allowed; }
        public bool TryMove(MoveRequest request, out string error)
        {
            InventorySave trial = state.Copy(); ContainerRecord source, target; ItemRecord item;
            PlacementPreview preview = InspectRequest(trial, request, out source, out target, out item); error = preview.error;
            if (!preview.allowed) return false;
            ItemRecord moved = item.Copy(); moved.x = request.x; moved.y = request.y; moved.rotation = request.rotation;
            source.items.Remove(item); target.items.Add(moved); Commit(trial); return true;
        }
        public bool TryRemove(string containerId, string instanceId, out string error)
        {
            error = ""; InventorySave trial = state.Copy(); ContainerRecord c = Find(trial, containerId);
            ItemRecord item = c == null ? null : c.items.Find(i => i.instanceId == instanceId);
            if (item == null) { error = "물건이 없거나 이미 출고되었습니다."; return false; }
            c.items.Remove(item); Commit(trial); return true;
        }
        public bool TryRestore(InventorySave save, out string error)
        {
            error = ""; InventorySave next;
            try { Validate(save); next = save.Copy(); } catch (Exception e) { error = e.Message; return false; }
            Commit(next); return true;
        }
    }
}
