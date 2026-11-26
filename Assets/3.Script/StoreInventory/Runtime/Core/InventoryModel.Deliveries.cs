using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    public sealed class DawnSettlement
    {
        public long paid,dailyCost,storageCost,occupiedCells,addedDebt,workshopCost;
        public int delivered;
        public bool alreadySettled;
    }
    public sealed class TemporaryRecoveryResult
    {
        public int recovered,remaining;
        public string stoppedAtInstanceId,stopReason;
    }
    public sealed partial class InventoryModel
    {
        public const string DeliveryContainerId="delivery";
        public const int DeliveryColumns=16;
        public const string TemporaryStorageId="temporary_storage";
        public const int TemporaryStorageColumns=16;
        public const int TemporaryStorageMinimumRows=8;
        // A total order shared by the visible list and the recovery button. Never skip a blocked item.
        public static int CompareTemporaryItems(ItemRecord a,ItemRecord b)
        {
            int row=a.y.CompareTo(b.y);if(row!=0)return row;
            int column=a.x.CompareTo(b.x);return column!=0?column:string.CompareOrdinal(a.instanceId,b.instanceId);
        }
        public bool TryRecoverTemporary(long expectedRevision,out TemporaryRecoveryResult result,out string error)
        {
            result=new TemporaryRecoveryResult{remaining=TemporaryItemCount};error="";
            if(expectedRevision!=Revision){error="재고가 변경되었습니다. 다시 회수하세요.";return false;}
            if(result.remaining==0){error="회수할 아이템이 없습니다.";return false;}
            var trial=state.Copy();var temporary=Find(trial,TemporaryStorageId);
            var ordered=new List<ItemRecord>(temporary.items);ordered.Sort(CompareTemporaryItems);
            var moved=new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach(var item in ordered)
                {
                    bool placed=false;
                    if(!IsReserved(item.instanceId))foreach(var box in trial.containers)
                        if(IsWarehouse(box.containerId)&&TryPlaceRectangle(trial,box,item)){placed=true;break;}
                    if(!placed)
                    {
                        result.stoppedAtInstanceId=item.instanceId;
                        result.stopReason=IsReserved(item.instanceId)?"예약된 아이템에서 회수를 멈췄습니다.":"상자 공간이 부족해 회수를 멈췄습니다.";
                        break;
                    }
                    moved.Add(item.instanceId);
                }
                if(moved.Count==0){error=result.stopReason;return false;}
                temporary.items.RemoveAll(item=>moved.Contains(item.instanceId));
                trial.listings.RemoveAll(listing=>moved.Contains(listing.instanceId));
                TrimDynamic(trial,TemporaryStorageId);Validate(trial);
            }
            catch(ArgumentException e){error=e.Message;return false;}
            catch(OverflowException){error="회수할 아이템의 좌표 범위를 초과했습니다.";return false;}
            result.recovered=moved.Count;result.remaining=temporary.items.Count;Commit(trial);return true;
        }
        public static bool IsDynamicStorage(string id){return id==DeliveryContainerId||id==TemporaryStorageId;}
        public long TemporaryOccupiedCells{get{long n=0;foreach(var item in Find(state,TemporaryStorageId).items)n+=Catalog.Shape(item.itemId).CellCount;return n;}}
        public int TemporaryItemCount{get{return Find(state,TemporaryStorageId).items.Count;}}
        public long UnpaidCosts{get{return state.unpaidCosts;}}
        public int DeliveryItemCount{get{return Find(state,DeliveryContainerId).items.Count;}}
        // Explicit test grant: immediate, free, individual UIDs. It never creates a purchase order.
        public bool TryGrantDebugItems(string itemId,string quality,int amount,out int stored,out int delivered,out string error)
        {
            stored=delivered=0;error="";
            if(string.IsNullOrWhiteSpace(itemId)){error="아이템 ID를 입력하세요.";return false;}
            itemId=itemId.Trim();var definition=Catalog.Find(itemId);
            if(definition==null)foreach(var candidate in Catalog.AllDefinitions())
                if(string.Equals(candidate.id,itemId,StringComparison.OrdinalIgnoreCase)){definition=candidate;break;}
            if(definition==null){error="없는 아이템 ID입니다: "+itemId;return false;}
            if(amount<1||amount>100){error="받을 수량은1~100개입니다.";return false;}
            if(string.IsNullOrWhiteSpace(quality))quality=definition.qualities[0].quality;
            if(Catalog.FindQuality(definition.id,quality)==null){error="해당 아이템에 없는 품질입니다.";return false;}
            if(!CanReceiveDelivery(definition.id)){error="가로16칸 보관 공간에도 맞지 않는 물품입니다.";return false;}
            var next=state.Copy();var incoming=new List<ItemRecord>();int placed=0;
            try
            {
                for(int n=0;n<amount;n++)
                {
                    var item=new ItemRecord{instanceId=Guid.NewGuid().ToString("N"),itemId=definition.id,quality=quality,purchasePrice=0};bool fits=false;
                    foreach(var box in next.containers)
                        if(IsWarehouse(box.containerId)&&TryPlaceRectangle(next,box,item)){fits=true;placed++;break;}
                    if(!fits)incoming.Add(item);
                }
                if(incoming.Count>0)AppendTemporary(next,incoming);
                Validate(next);
            }
            catch(ArgumentException e){error=e.Message;return false;}
            catch(OverflowException){error="아이템을 넣을 좌표 범위를 초과했습니다.";return false;}
            Commit(next);stored=placed;delivered=incoming.Count;return true;
        }
        public long DeliveryOccupiedCells
        {
            get{long cells=0;foreach(var item in Find(state,DeliveryContainerId).items)cells+=Catalog.Shape(item.itemId).CellCount;return cells;}
        }
        public bool CanReceiveDelivery(string id)
        {
            var definition=Catalog.Find(id);if(definition==null)return false;
            var shape=Catalog.Shape(id);
            for(int rotation=0;rotation<(definition.canRotate?4:1);rotation++)if(shape.Width(rotation)<=DeliveryColumns)return true;
            return false;
        }
        InventorySave PrepareSave(InventorySave source)
        {
            if(source==null||source.format!="general-store-individual"||(source.version!=7&&source.version!=8&&source.version!=9&&source.version!=10&&source.version!=11&&source.version!=12&&source.version!=13)
                ||source.containers==null||source.floorItems==null||source.parcels==null||source.deliveries==null)
                throw new ArgumentException("상점 v7~v13 저장 정보가 아닙니다.");
            var save=source.Copy();
            var box=Find(save,DeliveryContainerId);
            if(box==null){box=new ContainerRecord{containerId=DeliveryContainerId,columns=DeliveryColumns,rows=5};save.containers.Add(box);}
            if(source.version==7)
            {
                var legacy=new List<ItemRecord>();
                foreach(var floor in save.floorItems){if(floor==null||floor.item==null)throw new ArgumentException("이전 바닥 물품 오류");legacy.Add(floor.item);}
                foreach(var parcel in save.parcels){if(parcel==null||parcel.items==null)throw new ArgumentException("이전 택배 오류");legacy.AddRange(parcel.items);}
                AppendDelivery(box,legacy);save.floorItems.Clear();save.parcels.Clear();save.version=8;
            }
            var temporary=Find(save,TemporaryStorageId);
            if(temporary==null){temporary=new ContainerRecord{containerId=TemporaryStorageId,columns=TemporaryStorageColumns,rows=TemporaryStorageMinimumRows};save.containers.Add(temporary);}
            temporary.rows=Math.Max(TemporaryStorageMinimumRows,temporary.rows);
            if(source.version<13)foreach(var warehouse in save.containers)if(IsWarehouse(warehouse.containerId)&&warehouse.columns==8&&warehouse.rows==5)warehouse.rows=7;
            if(source.version<10)MigrateRectangularOccupancy(save);
            if(source.version<11){var previous=new List<ItemRecord>(box.items);box.items.Clear();box.columns=DeliveryColumns;box.rows=5;AppendDynamic(temporary,previous);}
            if(source.version<12)MigrateDisplayArtworkSizes(save);if(source.version<13)MigrateScrollingDisplaySizes(save);PrepareWorkshops(save);RelocateFormerMannequinGoods(save);save.version=13;return save;
        }
        void ValidateDeliveries(InventorySave save,HashSet<string> ids)
        {
            if(save.floorItems.Count!=0||save.parcels.Count!=0||Find(save,DeliveryContainerId)==null||Find(save,TemporaryStorageId)==null)
                throw new ArgumentException("현재 저장에는 바닥/미개봉 상자 대신 택배 격자가 필요합니다.");
            var orders=new HashSet<string>(StringComparer.Ordinal);
            foreach(var order in save.deliveries)
            {
                if(order==null||string.IsNullOrWhiteSpace(order.orderId)||!orders.Add(order.orderId)||order.arrivalDay<1||order.arrivalDay>1000000
                    ||order.items==null||order.items.Count<1||order.items.Count>100)throw new ArgumentException("배송 예약 오류입니다.");
                foreach(var item in order.items)
                {
                    var definition=item==null?null:Catalog.Find(item.itemId);
                    if(definition==null||string.IsNullOrWhiteSpace(item.instanceId)||!ids.Add(item.instanceId)||Catalog.FindQuality(item.itemId,item.quality)==null
                        ||item.purchasePrice<0||item.rotation<0||item.rotation>3||(item.rotation!=0&&!definition.canRotate)||!CanReceiveDelivery(item.itemId))
                        throw new ArgumentException("배송 물품/UID/가로 폭 오류입니다.");
                }
            }
        }
        // Sixteen-column row masks avoid allocating an ever-growing rectangular bool grid.
        // Existing item coordinates stay fixed. Each incoming batch packs in row order.
        void AppendDelivery(ContainerRecord box,IList<ItemRecord> incoming){AppendDynamic(box,incoming);}
        void AppendTemporary(InventorySave save,IList<ItemRecord> incoming){AppendDynamic(Find(save,TemporaryStorageId),incoming);}
        void AppendDynamic(ContainerRecord box,IList<ItemRecord> incoming)
        {
            var masks=new Dictionary<int,int>();
            foreach(var item in box.items)foreach(var cell in Catalog.Shape(item.itemId).InternalCells(item.rotation))
            {int y=checked(item.y+cell.y),mask;masks.TryGetValue(y,out mask);masks[y]=mask|(1<<checked(item.x+cell.x));}
            int cursor=0;
            foreach(var original in incoming)
            {
                if(original==null||!CanReceiveDelivery(original.itemId))throw new ArgumentException("보관 공간 가로16칸에 맞지 않는 물품입니다.");
                var item=original.Copy();var shape=Catalog.Shape(item.itemId);
                if(item.rotation<0||item.rotation>3||(item.rotation!=0&&!Catalog.Find(item.itemId).canRotate))throw new ArgumentException("택배 회전 오류입니다.");
                if(shape.Width(item.rotation)>box.columns)
                {
                    bool fits=false;if(Catalog.Find(item.itemId).canRotate)for(int rotation=0;rotation<4;rotation++)if(shape.Width(rotation)<=box.columns){item.rotation=rotation;fits=true;break;}
                    if(!fits)throw new ArgumentException("보관 공간의 가로 폭에 맞지 않습니다.");
                }
                bool placed=false;int height=shape.Height(item.rotation),width=shape.Width(item.rotation);
                for(int y=cursor;!placed;y++)
                {
                    if(y>int.MaxValue/box.columns-height)throw new ArgumentException("택배 좌표의 수치 범위를 초과했습니다.");
                    for(int x=0;x<=box.columns-width;x++)
                    {
                        bool free=true;
                        foreach(var cell in shape.InternalCells(item.rotation))
                        {int mask;if(masks.TryGetValue(y+cell.y,out mask)&&(mask&(1<<(x+cell.x)))!=0){free=false;break;}}
                        if(!free)continue;
                        item.x=x;item.y=y;box.items.Add(item);cursor=y;placed=true;
                        foreach(var cell in shape.InternalCells(item.rotation))
                        {int row=y+cell.y,mask;masks.TryGetValue(row,out mask);masks[row]=mask|(1<<(x+cell.x));}
                        box.rows=Math.Max(box.rows,y+height);break;
                    }
                }
            }
        }
        void TrimDelivery(InventorySave save)
        {
            TrimDynamic(save,DeliveryContainerId);TrimDynamic(save,TemporaryStorageId);
        }
        void TrimDynamic(InventorySave save,string id)
        {
            var box=Find(save,id);if(box==null)return;int rows=id==TemporaryStorageId?TemporaryStorageMinimumRows:5;
            foreach(var item in box.items)rows=Math.Max(rows,item.y+Catalog.Shape(item.itemId).Height(item.rotation));box.rows=rows;
        }
        // Shared receiver for parcels, refunds, cancellations, and all workshop results.
        void ReceiveItems(InventorySave next,IList<ItemRecord> items,out int stored,out int overflow)
        {
            stored=0;var incoming=new List<ItemRecord>();
            foreach(var item in items)
            {
                bool placed=false;
                foreach(var box in next.containers)if(IsWarehouse(box.containerId)&&TryPlaceRectangle(next,box,item)){stored++;placed=true;break;}
                if(!placed)incoming.Add(item);
            }
            AppendTemporary(next,incoming);overflow=incoming.Count;
        }
        public bool TryReceiveDoorDelivery(out int stored,out int overflow,out string error)
        {
            stored=overflow=0;error="";var box=Find(state,DeliveryContainerId);
            if(box.items.Count==0){error="받을 택배가 없습니다.";return false;}
            foreach(var item in box.items)if(IsReserved(item.instanceId)){error="손님에게 예약된 물품은 수령할 수 없습니다.";return false;}
            var next=state.Copy();var incoming=new List<ItemRecord>(Find(next,DeliveryContainerId).items);
            Find(next,DeliveryContainerId).items.Clear();TrimDynamic(next,DeliveryContainerId);
            try{ReceiveItems(next,incoming,out stored,out overflow);Validate(next);}
            catch(ArgumentException e){stored=overflow=0;error=e.Message;return false;}
            catch(OverflowException){stored=overflow=0;error="임시 저장고 좌표 범위 초과";return false;}
            Commit(next);return true;
        }
        public bool TryOrderNextDawn(int currentDay,string itemId,string quality,int amount,int unitCost,out string error)
        {
            error="";
            if(currentDay<state.lastSettledDay||currentDay>=1000000||amount<1||amount>100||unitCost<0||!Catalog.CanTrade(itemId)||Catalog.FindQuality(itemId,quality)==null||!CanReceiveDelivery(itemId))
            {error="배송 주문 정보/가로 폭 오류입니다.";return false;}
            long cost=(long)amount*unitCost;if(cost>state.gold){error="주문할 돈이 부족합니다.";return false;}
            var trial=state.Copy();var order=new DeliveryOrder{orderId=Guid.NewGuid().ToString("N"),arrivalDay=currentDay+1};
            for(int n=0;n<amount;n++)order.items.Add(new ItemRecord{instanceId=Guid.NewGuid().ToString("N"),itemId=itemId,quality=quality,purchasePrice=unitCost});
            trial.gold-=cost;trial.deliveries.Add(order);Commit(trial);return true;
        }
        // Compatibility overload: callers explicitly using the old API have no storage fee.
        public bool TryBeginDawn(int day,long dailyCost,out long paid,out int delivered,out string error)
        {
            DawnSettlement settlement;bool ok=TryBeginDawn(day,dailyCost,0,out settlement,out error);
            paid=settlement.paid;delivered=settlement.delivered;return ok;
        }
        // Fee snapshot is BEFORE today's arrivals. Cash/debt/delivery/day commit atomically.
        public bool TryBeginDawn(int day,long dailyCost,long feePerCell,out DawnSettlement settlement,out string error)
        {
            settlement=new DawnSettlement();error="";
            if(day<1||day>1000000||dailyCost<0||feePerCell<0){error="새벽 정산 정보 오류입니다.";return false;}
            if(day<=state.lastSettledDay){settlement.alreadySettled=true;return true;}
            try
            {
                long workshopCost=WorkshopDue(),cells=checked(DeliveryOccupiedCells+TemporaryOccupiedCells),storage=checked(cells*feePerCell),cost=checked(dailyCost+storage+workshopCost),paid=Math.Min(state.gold,cost);
                long added=cost-paid,debt=checked(state.unpaidCosts+added);var trial=state.Copy();var arrivals=new List<ItemRecord>();
                foreach(var order in trial.deliveries)if(order.arrivalDay<=day)arrivals.AddRange(order.items);
                AppendDelivery(Find(trial,DeliveryContainerId),arrivals);
                trial.deliveries.RemoveAll(order=>order.arrivalDay<=day);
                trial.gold-=paid;trial.unpaidCosts=debt;trial.lastSettledDay=day;BeginWorkshopDay(trial,day);
                settlement=new DawnSettlement{paid=paid,dailyCost=dailyCost,workshopCost=workshopCost,storageCost=storage,occupiedCells=cells,addedDebt=added,delivered=arrivals.Count};
                Commit(trial);return true;
            }
            catch(OverflowException){error="정산 금액 또는 택배 좌표 범위를 초과했습니다.";return false;}
            catch(ArgumentException e){error=e.Message;return false;}
        }
    }
}
