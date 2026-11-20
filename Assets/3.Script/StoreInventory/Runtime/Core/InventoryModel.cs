using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    // 직사각형 낱개 인벤토리입니다. 가로×세로 전체를 점유합니다.
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
        internal bool TryReserveCounterOffers(IList<PendingOffer> lines,out string error)
        {
            error="";var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var line in lines)
            {
                int price;if(line==null||string.IsNullOrEmpty(line.instanceId)||!ids.Add(line.instanceId)||IsReserved(line.instanceId)
                    ||!TryGetOfferPrice(line.containerId,line.instanceId,out price)||price!=line.price)
                {error="추가 추천 물건이 이동했거나 가격이 바뀌었습니다.";return false;}
            }
            if(ids.Count==0)return true;foreach(string id in ids)reserved.Add(id);Revision++;if(Changed!=null)Changed();return true;
        }
        internal void ReleaseCounterOffer(string uid)
        {if(!reserved.Remove(uid))return;Revision++;if(Changed!=null)Changed();}
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
        public InventoryModel(ItemCatalog catalog, InventorySave initial,SupplierCatalog suppliers=null,DisplayCatalog displays=null,WorkshopCatalog workshops=null)
        {
            Catalog = catalog ?? throw new ArgumentNullException("catalog");
            Suppliers=suppliers;Displays=displays;Workshops=workshops;state=PrepareSave(initial);Validate(state);
        }
        static ContainerRecord Find(InventorySave save, string id)
        {
            return save.containers.Find(c => c.containerId == id);
        }
        void RemoveItem(InventorySave save,string containerId,string instanceId)
        {
            Find(save,containerId).items.RemoveAll(i=>i.instanceId==instanceId);
            if(containerId==DeliveryContainerId)TrimDelivery(save);
        }
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

        PlacementPreview InspectPlacement(InventorySave save,ContainerRecord container, ItemRecord item, string ignoredId,HashSet<int> cachedOccupancy=null)
        {
            var result = new PlacementPreview { allowed = true };
            var occupied = cachedOccupancy??new HashSet<int>();
            if(cachedOccupancy==null)foreach (ItemRecord other in container.items)
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
            // 드래그 미리보기·확정·자동 입고·저장 복원이 같은 칸막이 검사를 사용합니다.
            var installation=save.displays==null?null:save.displays.Find(d=>d!=null&&d.containerId==container.containerId);
            var definition=installation==null||Displays==null?null:Displays.Type(installation.typeId);
            if(definition!=null&&!DisplayPartitionRules.Fits(definition,Catalog.Shape(item.itemId).InternalCells(item.rotation),item.x,item.y))
            {
                result.allowed=false;result.error="선반의 층/칸막이를 가로질러 놓을 수 없습니다.";
                for(int n=0;n<result.cells.Count;n++){var cell=result.cells[n];if(cell.state==PlacementCellState.Free){cell.state=PlacementCellState.Blocked;result.cells[n]=cell;}}
            }
            return result;
        }
        void Validate(InventorySave save)
        {
            if (save == null || save.format != "general-store-individual" || save.version != 10 || save.gold < 0 || save.unpaidCosts < 0 || save.lastSettledDay<1 || save.lastSettledDay>1000000 || save.displays == null || save.parcels == null || save.floorItems == null || save.deliveries == null || save.listings == null || save.containers == null
                || save.containers.Count < 1 || save.containers.Count > 64) throw new ArgumentException("상점 v10 저장 파일이 아닙니다.");
            var containers = new HashSet<string>(StringComparer.Ordinal); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ContainerRecord c in save.containers)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.containerId) || c.containerId=="floor" || !containers.Add(c.containerId)
                    || c.columns < 1 || c.columns > 64 || c.rows < 1 || (c.containerId==DeliveryContainerId?(c.columns!=DeliveryColumns||c.rows>int.MaxValue/DeliveryColumns):c.rows>64) || c.items == null
                    || c.items.Count > (long)c.columns * c.rows) throw new ArgumentException("컨테이너 정보 오류입니다.");
                foreach (ItemRecord item in c.items)
                {
                    ItemDefinition def = item == null ? null : Catalog.Find(item.itemId);
                    if (def == null || string.IsNullOrWhiteSpace(item.instanceId) || !ids.Add(item.instanceId)
                        || Catalog.FindQuality(item.itemId, item.quality) == null || item.rotation < 0 || item.rotation > 3
                        || (item.rotation != 0 && !def.canRotate) || item.purchasePrice < 0
                        || item.x < 0 || item.x >= c.columns || item.y < 0 || item.y >= c.rows)
                        throw new ArgumentException("물건 정보/회전 오류입니다.");
                }
                var occupied=new HashSet<long>();
                foreach(var item in c.items)
                {
                    foreach(var offset in Catalog.Shape(item.itemId).InternalCells(item.rotation))
                    {
                        long x=(long)item.x+offset.x,y=(long)item.y+offset.y;
                        if(x<0||x>=c.columns||y<0||y>=c.rows||!occupied.Add(y*c.columns+x))throw new ArgumentException("점유 칸 겹침/범위 오류입니다.");
                    }
                }
            }
            foreach(var c in save.containers)if(IsWarehouse(c.containerId)&&(c.columns!=WarehouseColumns||c.rows!=WarehouseRows))
                throw new ArgumentException("상자는 가로8칸, 세로5칸 고정입니다.");
            ValidateDisplays(save);
            ValidateSuppliers(save);
            ValidateDeliveries(save,ids);ValidateWorkshops(save,ids);
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
        // Only pre-v10 saves are repaired. Current saves still reject overlap/corruption.
        // The input is already a deep copy, so any failure leaves both the caller and live model intact.
        void MigrateRectangularOccupancy(InventorySave save)
        {
            var displaced=new List<ItemRecord>();var origins=new List<ContainerRecord>();
            foreach(var container in save.containers)
            {
                if(container==null||container.items==null)throw new ArgumentException("이전 보관함 정보 오류입니다.");
                var original=new List<ItemRecord>(container.items);container.items.Clear();var occupied=new HashSet<int>();
                foreach(var item in original)
                {
                    var def=item==null?null:Catalog.Find(item.itemId);
                    if(def==null||string.IsNullOrWhiteSpace(item.instanceId)||Catalog.FindQuality(item.itemId,item.quality)==null||item.rotation<0||item.rotation>3||!def.canRotate&&item.rotation!=0||item.purchasePrice<0)
                        throw new ArgumentException("이전 물품 정보가 유효하지 않습니다.");
                    if(InspectPlacement(save,container,item,null,occupied).allowed)
                    {
                        container.items.Add(item);
                        foreach(var cell in Catalog.Shape(item.itemId).InternalCells(item.rotation))occupied.Add((item.y+cell.y)*container.columns+item.x+cell.x);
                    }
                    else{displaced.Add(item);origins.Add(container);}
                }
            }
            var incoming=new List<ItemRecord>();
            for(int n=0;n<displaced.Count;n++)
            {
                var item=displaced[n];var origin=origins[n];
                if(origin.containerId==DeliveryContainerId){incoming.Add(item);continue;}
                if(TryPlaceRectangle(save,origin,item))continue;
                bool placed=false;
                foreach(var container in save.containers)
                    if(container!=origin&&IsWarehouse(container.containerId)&&TryPlaceRectangle(save,container,item)){placed=true;break;}
                if(!placed)
                {
                    if(!CanReceiveDelivery(item.itemId))throw new ArgumentException("직사각형 전환 중 물품을 보관할 공간이 없습니다: "+item.itemId+". 기존 저장은 변경하지 않았습니다.");
                    incoming.Add(item);
                }
                // Listings belong only to goods that remain on a display.
                save.listings.RemoveAll(x=>x.instanceId==item.instanceId);
            }
            AppendDelivery(Find(save,DeliveryContainerId),incoming);TrimDelivery(save);
        }
        bool TryPlaceRectangle(InventorySave save,ContainerRecord container,ItemRecord original)
        {
            var item=original.Copy();var definition=Catalog.Find(item.itemId);
            int rotations=definition.canRotate?4:1;
            for(int n=0;n<rotations;n++)
            {
                item.rotation=definition.canRotate?(original.rotation+n)%4:0;
                for(int y=0;y<container.rows;y++)for(int x=0;x<container.columns;x++)
                {
                    item.x=x;item.y=y;
                    if(InspectPlacement(save,container,item,null).allowed){container.items.Add(item);return true;}
                }
            }
            return false;
        }
        void Commit(InventorySave next) { state = next; Revision++; if (Changed != null) Changed(); }
        public bool TryAdd(string containerId, string itemId, string quality, int amount, int unitCost, out string error)
        {
            error = ""; ItemDefinition def = Catalog.Find(itemId);
            if (def == null || Catalog.FindQuality(itemId, quality) == null || amount < 1 || unitCost < 0)
            { error = "아이템/품질/개수/매입가를 확인하세요."; return false; }
            InventorySave trial = state.Copy(); ContainerRecord target = Find(trial, containerId);
            if (target == null || containerId==DeliveryContainerId) { error = "배송 전용 공간에는 직접 입고할 수 없습니다."; return false; }
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
                    { item.x = x; item.y = y; placed = InspectPlacement(trial,target, item, null).allowed; }
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
            if(request.targetContainer=="floor"||request.targetContainer==DeliveryContainerId)
                return new PlacementPreview{error="택배는 입고 전용입니다. 빈 배경에는 놓을 수 없습니다."};
            if (item == null || target == null) return new PlacementPreview { error = "물건/목적지가 없습니다." };
            if (request.rotation < 0 || request.rotation > 3 || (request.rotation != 0 && !Catalog.Find(item.itemId).canRotate))
                return new PlacementPreview { error = "허용되지 않는 회전입니다." };
            ItemRecord candidate = item.Copy(); candidate.x = request.x; candidate.y = request.y; candidate.rotation = request.rotation;
            var preview=InspectPlacement(save,target,candidate,item.instanceId);string typeError;
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
        internal bool TrySellCustomerAtTotal(IList<SaleLine> lines,long expectedRevision,HashSet<string> customerIds,long agreedTotal,out long revenue,out string error)
        {return Sell(lines,expectedRevision,customerIds,out revenue,out error,agreedTotal);}
        bool Sell(IList<SaleLine> lines, long expectedRevision, HashSet<string> customerIds, out long revenue, out string error,long? agreedTotal=null)
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
            if(agreedTotal.HasValue){if(agreedTotal.Value<0){error="거래 금액은 0 이상이어야 합니다.";return false;}total=agreedTotal.Value;}
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
            if(customerIds!=null&&customerIds.Count>0){trial.successfulCustomerSales=Math.Min(1000000000,trial.successfulCustomerSales+1);UnlockEarnedWorkshops(trial);}
            trial.gold = nextGold; Commit(trial); revenue = total; return true;
        }
        public bool TryRestore(InventorySave save, out string error)
        {
            error = ""; InventorySave next;
            try { next=PrepareSave(save);Validate(next); } catch (Exception e) { error = e.Message; return false; }
            reserved.Clear(); Commit(next); return true;
        }
    }
}
