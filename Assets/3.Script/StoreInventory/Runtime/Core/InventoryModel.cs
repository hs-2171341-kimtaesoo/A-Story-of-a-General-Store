using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    // 점유 마스크 기반 낱개 인벤토리입니다. 모양의 빈칸은 다른 물건과 공유할 수 있습니다.
    public sealed partial class InventoryModel
    {
        public readonly ItemCatalog Catalog;
        InventorySave state;
        // 예약은 임시 상태입니다. 실제 물건은 원래 진열 칸에 그대로 남습니다.
        readonly HashSet<string> reserved = new HashSet<string>(StringComparer.Ordinal);
        public bool IsReserved(string instanceId) { return instanceId != null && reserved.Contains(instanceId); }
        internal bool TryReserve(IList<PendingOffer> picks, out string error)
        {
            error = "";
            foreach (PendingOffer pick in picks)
            {
                int price;
                if (IsReserved(pick.instanceId) || !IsDisplay(pick.containerId)
                    || !TryGetOfferPrice(pick.containerId,pick.instanceId,out price) || price != pick.price)
                { error = "손님 선택 상품을 예약할 수 없습니다."; return false; }
            }
            foreach (PendingOffer pick in picks) reserved.Add(pick.instanceId);
            Revision++; if (Changed != null) Changed(); return true;
        }
        internal void ReleaseReservations()
        {
            if (reserved.Count == 0) return;
            reserved.Clear(); Revision++; if (Changed != null) Changed();
        }
        public long Revision { get; private set; }
        public int DisplayListingCount {get{return state.listings.Count;}}
        public List<ContainerRecord> DisplayContainers()
        {var list=new List<ContainerRecord>();foreach(var c in state.containers)if(IsDisplay(c.containerId))list.Add(c.Copy());return list;}
        public long Gold { get { return state.gold; } }
        public event Action Changed;
        public InventoryModel(ItemCatalog catalog, InventorySave initial,SupplierCatalog suppliers=null,DisplayCatalog displays=null)
        {
            Catalog = catalog ?? throw new ArgumentNullException("catalog");
            Suppliers=suppliers;Displays=displays;Validate(initial); state = initial.Copy(); MergeParcels(state);
        }
        static ContainerRecord Find(InventorySave save, string id)
        {
            if(id=="floor")
            {var c=new ContainerRecord{containerId="floor"};foreach(var f in save.floorItems)c.items.Add(f.item);return c;}
            return save.containers.Find(c => c.containerId == id);
        }
        static void RemoveItem(InventorySave save,string containerId,string instanceId)
        {
            if(containerId=="floor")save.floorItems.RemoveAll(f=>f.item.instanceId==instanceId);
            else Find(save,containerId).items.RemoveAll(i=>i.instanceId==instanceId);
        }
        public ContainerRecord GetContainer(string id) { ContainerRecord c = Find(state, id); return c == null ? null : c.Copy(); }
        public ItemRecord GetItem(string containerId, string instanceId)
        {
            if(containerId=="floor"){var f=state.floorItems.Find(v=>v.item.instanceId==instanceId);return f==null?null:f.item.Copy();}
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
            if (save == null || save.format != "general-store-individual" || save.version != 7 || save.gold < 0 || save.unpaidCosts < 0 || save.lastSettledDay<1 || save.lastSettledDay>1000000 || save.displays == null || save.parcels == null || save.floorItems == null || save.deliveries == null || save.listings == null || save.containers == null
                || save.containers.Count < 1 || save.containers.Count > 64) throw new ArgumentException("상점 v7 저장 파일이 아닙니다.");
            var containers = new HashSet<string>(StringComparer.Ordinal); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ContainerRecord c in save.containers)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.containerId) || c.containerId=="floor" || !containers.Add(c.containerId)
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
            foreach(var c in save.containers)if(IsWarehouse(c.containerId)&&(c.columns!=8||c.rows>10))
                throw new ArgumentException("보관함은 가로8칸, 세로 최대10칸입니다.");
            ValidateDisplays(save);
            ValidateSuppliers(save);
            ValidateLooseItems(save,ids);
            var listingIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DisplayListing listing in save.listings)
            {
                if (listing == null || string.IsNullOrEmpty(listing.instanceId) || !listingIds.Add(listing.instanceId) || listing.price < 0)
                    throw new ArgumentException("진열 가격 등록 오류입니다.");
                bool found = false;
                foreach (ContainerRecord c in save.containers)
                    if (IsDisplay(c.containerId) && c.items.Exists(i => i.instanceId == listing.instanceId)) found = true;
                if (!found) throw new ArgumentException("진열되지 않은 물건의 가격 등록입니다.");
            }
        }
        void Commit(InventorySave next) { state = next; Revision++; if (Changed != null) Changed(); }
        public bool TryAdd(string containerId, string itemId, string quality, int amount, int unitCost, out string error)
        {
            error = ""; ItemDefinition def = Catalog.Find(itemId);
            if (def == null || Catalog.FindQuality(itemId, quality) == null || amount < 1 || unitCost < 0)
            { error = "아이템/품질/개수/매입가를 확인하세요."; return false; }
            InventorySave trial = state.Copy(); ContainerRecord target = Find(trial, containerId);
            if (target == null || containerId=="floor") { error = "입고 격자가 없습니다. 바닥 생성은 전용 API를 사용하세요."; return false; }
            if(!AcceptDisplay(trial,containerId,itemId,out error))return false;
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
            if (IsReserved(request.instanceId)) return new PlacementPreview { error = "손님이 고른 물건은 계산 전까지 변경할 수 없습니다." };
            source = Find(save, request.sourceContainer); target = Find(save, request.targetContainer);
            item = source == null ? null : source.items.Find(i => i.instanceId == request.instanceId);
            if(request.targetContainer=="floor")return new PlacementPreview{error="바닥 이동은 TryDropToFloor를 사용하세요."};
            if (item == null || target == null) return new PlacementPreview { error = "물건/목적지가 없습니다." };
            if (request.rotation < 0 || request.rotation > 3 || (request.rotation != 0 && !Catalog.Find(item.itemId).canRotate))
                return new PlacementPreview { error = "허용되지 않는 회전입니다." };
            ItemRecord candidate = item.Copy(); candidate.x = request.x; candidate.y = request.y; candidate.rotation = request.rotation;
            var preview=InspectPlacement(target,candidate,item.instanceId);string typeError;
            if(!AcceptDisplay(save,target.containerId,item.itemId,out typeError)){preview.allowed=false;preview.error=typeError;}
            return preview;
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
            RemoveItem(trial,source.containerId,item.instanceId); target.items.Add(moved);
            if (!IsDisplay(target.containerId)) trial.listings.RemoveAll(l => l.instanceId == item.instanceId);
            Commit(trial); return true;
        }
        public bool TryRemove(string containerId, string instanceId, out string error)
        {
            error = ""; if (IsReserved(instanceId)) { error = "손님이 고른 물건입니다."; return false; }
            InventorySave trial = state.Copy(); ContainerRecord c = Find(trial, containerId);
            ItemRecord item = c == null ? null : c.items.Find(i => i.instanceId == instanceId);
            if (item == null) { error = "물건이 없거나 이미 출고되었습니다."; return false; }
            RemoveItem(trial,containerId,instanceId); trial.listings.RemoveAll(l => l.instanceId == instanceId); Commit(trial); return true;
        }

        public static bool IsDisplay(string containerId)
        { return containerId != null && containerId.StartsWith("display_", StringComparison.Ordinal); }
        // 진열 시에만 가격을 받습니다. 창고 자체에는 판매가 수정 API가 없습니다.
        public bool TryListOnDisplay(MoveRequest request, int price, out string error)
        {
            error = "";
            if (request == null || !IsDisplay(request.targetContainer) || price < 0)
            { error = "진열 공간과 0 이상의 판매가를 지정하세요."; return false; }
            InventorySave trial = state.Copy(); ContainerRecord source, target; ItemRecord item;
            PlacementPreview preview = InspectRequest(trial, request, out source, out target, out item);
            if (!preview.allowed) { error = preview.error; return false; }
            ItemRecord moved = item.Copy(); moved.x = request.x; moved.y = request.y; moved.rotation = request.rotation;
            RemoveItem(trial,source.containerId,item.instanceId); target.items.Add(moved);
            trial.listings.RemoveAll(l => l.instanceId == moved.instanceId);
            trial.listings.Add(new DisplayListing { instanceId = moved.instanceId, price = price });
            Commit(trial); return true;
        }
        public bool TryGetOfferPrice(string containerId, string instanceId, out int price)
        {
            price = 0; ItemRecord item = GetItem(containerId, instanceId); if (item == null) return false;
            if (IsDisplay(containerId))
            {
                DisplayListing listing = state.listings.Find(l => l.instanceId == instanceId);
                if (listing == null) return false;
                price = listing.price; return true;
            }
            // 창고에서 바로 추천할 때는 기준가를 읽기만 합니다. 추후 여기서 시세 공급자를 사용하면 됩니다.
            price = Catalog.ReferencePrice(item.itemId, item.quality); return true;
        }
        // 재고 차감 + 진열 해제 + 돈 지급을 한 스냅샷으로 확정합니다.
        public bool TrySell(IList<SaleLine> lines, long expectedRevision, out long revenue, out string error)
        { return Sell(lines, expectedRevision, null, out revenue, out error); }
        // 예약 상품의 판매는 손님 세션에서만 요청합니다. 일반 TrySell은 예약을 우회하지 못합니다.
        internal bool TrySellCustomer(IList<SaleLine> lines, long expectedRevision, HashSet<string> customerIds, out long revenue, out string error)
        { return Sell(lines, expectedRevision, customerIds, out revenue, out error); }
        bool Sell(IList<SaleLine> lines, long expectedRevision, HashSet<string> customerIds, out long revenue, out string error)
        {
            revenue = 0; error = "";
            if (lines == null || lines.Count == 0 || (expectedRevision >= 0 && expectedRevision != Revision))
            { error = "판매할 물건이 없거나 재고가 변경되었습니다."; return false; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            long total = 0;
            foreach (SaleLine line in lines)
            {
                int currentPrice;
                if (line != null && IsReserved(line.instanceId) && (customerIds == null || !customerIds.Contains(line.instanceId)))
                { error = "손님 예약 상품은 해당 손님의 계산으로만 판매할 수 있습니다."; return false; }
                if (line == null || string.IsNullOrEmpty(line.instanceId) || !ids.Add(line.instanceId)
                    || line.price < 0 || !TryGetOfferPrice(line.containerId, line.instanceId, out currentPrice) || line.price != currentPrice)
                { error = "물건이 이동/소진되었거나 제안 가격이 변경되었습니다. 목록을 다시 확인하세요."; return false; }
                try { total = checked(total + line.price); }
                catch (OverflowException) { error = "판매 금액 범위를 초과했습니다."; return false; }
            }
            long nextGold;
            try { nextGold = checked(state.gold + total); }
            catch (OverflowException) { error = "보유 금액 범위를 초과했습니다."; return false; }
            InventorySave trial = state.Copy();
            foreach (SaleLine line in lines)
            {
                RemoveItem(trial,line.containerId,line.instanceId);
                trial.listings.RemoveAll(l => l.instanceId == line.instanceId);
            }
            foreach (SaleLine line in lines) reserved.Remove(line.instanceId);
            trial.gold = nextGold; Commit(trial); revenue = total; return true;
        }
        public bool TryRestore(InventorySave save, out string error)
        {
            error = ""; InventorySave next;
            try { Validate(save); next = save.Copy(); } catch (Exception e) { error = e.Message; return false; }
            MergeParcels(next); reserved.Clear(); Commit(next); return true;
        }
    }
}
