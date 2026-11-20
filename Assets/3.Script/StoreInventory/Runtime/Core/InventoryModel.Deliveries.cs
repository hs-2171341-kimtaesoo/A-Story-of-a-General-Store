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
    public sealed partial class InventoryModel
    {
        public const string DeliveryContainerId="delivery";
        public const int DeliveryColumns=8;
        public long UnpaidCosts{get{return state.unpaidCosts;}}
        public int DeliveryItemCount{get{return Find(state,DeliveryContainerId).items.Count;}}
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
            if(source==null||source.format!="general-store-individual"||(source.version!=7&&source.version!=8&&source.version!=9&&source.version!=10)
                ||source.containers==null||source.floorItems==null||source.parcels==null||source.deliveries==null)
                throw new ArgumentException("상점 v7/v8/v9/v10 저장 정보가 아닙니다.");
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
            PrepareWorkshops(save);if(source.version<10)MigrateRectangularOccupancy(save);save.version=10;return save;
        }
        void ValidateDeliveries(InventorySave save,HashSet<string> ids)
        {
            if(save.floorItems.Count!=0||save.parcels.Count!=0||Find(save,DeliveryContainerId)==null)
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
        // Eight-bit row masks avoid allocating an ever-growing rectangular bool grid.
        // Existing item coordinates stay fixed. Each incoming batch packs in row order.
        void AppendDelivery(ContainerRecord box,IList<ItemRecord> incoming)
        {
            var masks=new Dictionary<int,int>();
            foreach(var item in box.items)foreach(var cell in Catalog.Shape(item.itemId).InternalCells(item.rotation))
            {int y=checked(item.y+cell.y),mask;masks.TryGetValue(y,out mask);masks[y]=mask|(1<<checked(item.x+cell.x));}
            int cursor=0;
            foreach(var original in incoming)
            {
                if(original==null||!CanReceiveDelivery(original.itemId))throw new ArgumentException("택배 가로8칸에 맞지 않는 물품입니다.");
                var item=original.Copy();var shape=Catalog.Shape(item.itemId);
                if(item.rotation<0||item.rotation>3||(item.rotation!=0&&!Catalog.Find(item.itemId).canRotate))throw new ArgumentException("택배 회전 오류입니다.");
                if(shape.Width(item.rotation)>DeliveryColumns)
                    for(int rotation=0;rotation<4;rotation++)if(shape.Width(rotation)<=DeliveryColumns){item.rotation=rotation;break;}
                bool placed=false;int height=shape.Height(item.rotation),width=shape.Width(item.rotation);
                for(int y=cursor;!placed;y++)
                {
                    if(y>int.MaxValue/DeliveryColumns-height)throw new ArgumentException("택배 좌표의 수치 범위를 초과했습니다.");
                    for(int x=0;x<=DeliveryColumns-width;x++)
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
            var box=Find(save,DeliveryContainerId);if(box==null)return;int rows=5;
            foreach(var item in box.items)rows=Math.Max(rows,item.y+Catalog.Shape(item.itemId).Height(item.rotation));box.rows=rows;
        }
        public bool TryOrderNextDawn(int currentDay,string itemId,string quality,int amount,int unitCost,out string error)
        {
            error="";
            if(currentDay<state.lastSettledDay||currentDay>=1000000||amount<1||amount>100||unitCost<0||Catalog.FindQuality(itemId,quality)==null||!CanReceiveDelivery(itemId))
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
                long workshopCost=WorkshopDue(),cells=DeliveryOccupiedCells,storage=checked(cells*feePerCell),cost=checked(dailyCost+storage+workshopCost),paid=Math.Min(state.gold,cost);
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
