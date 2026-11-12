using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    // 실제 물건 한 개입니다. 같은 포션도 instanceId가 각각 다릅니다.
    // quantity가 없으므로 이 객체 하나에 여러 물건을 저장할 수 없습니다.
    [Serializable] public sealed class ItemRecord
    {
        public string instanceId;
        public string itemId;
        public string quality;
        public int x, y;
        // 0/1/2/3 = 시계 방향 0/90/180/270도입니다.
        public int rotation;
        public int purchasePrice;
        public ItemRecord Copy() { return (ItemRecord)MemberwiseClone(); }
    }
    [Serializable] public sealed class ContainerRecord
    {
        public string containerId;
        public int columns, rows;
        public List<ItemRecord> items = new List<ItemRecord>();
        public ContainerRecord Copy()
        {
            var result = new ContainerRecord { containerId = containerId, columns = columns, rows = rows };
            foreach (ItemRecord item in items) result.items.Add(item == null ? null : item.Copy());
            return result;
        }
    }
    [Serializable] public sealed class InventorySave
    {
        // 이전 중첩 저장과 혼동하지 않도록 새 포맷을 명시합니다.
        public string format = "general-store-individual";
        public int version = 7;
        public List<DisplayInstallation> displays = new List<DisplayInstallation>();
        public List<SupplierState> suppliers = new List<SupplierState>();
        public long gold;
        public long unpaidCosts;
        public int lastSettledDay=1;
        public List<FloorItemRecord> floorItems = new List<FloorItemRecord>();
        public List<ParcelRecord> parcels = new List<ParcelRecord>();
        public List<DeliveryOrder> deliveries = new List<DeliveryOrder>();
        public List<DisplayListing> listings = new List<DisplayListing>();
        public List<ContainerRecord> containers = new List<ContainerRecord>();
        public InventorySave Copy()
        {
            var result = new InventorySave { format = format, version = version, gold = gold, unpaidCosts = unpaidCosts, lastSettledDay = lastSettledDay };
            foreach(var display in displays)result.displays.Add(display==null?null:display.Copy());
            foreach(var supplier in suppliers)result.suppliers.Add(supplier==null?null:supplier.Copy());
            foreach (FloorItemRecord floor in floorItems) result.floorItems.Add(floor == null ? null : floor.Copy());
            foreach (ParcelRecord parcel in parcels) result.parcels.Add(parcel == null ? null : parcel.Copy());
            foreach (DeliveryOrder order in deliveries) result.deliveries.Add(order == null ? null : order.Copy());
            foreach (DisplayListing listing in listings) result.listings.Add(listing == null ? null : listing.Copy());
            foreach (ContainerRecord container in containers) result.containers.Add(container == null ? null : container.Copy());
            return result;
        }
    }
    // 바닥은 격자가 아닙니다. 0~1 정규화 위치만 저장하므로 해상도가 달라져도 유지됩니다.
    [Serializable] public sealed class FloorItemRecord
    {
        public ItemRecord item;
        public float normalizedX,normalizedY;
        public float velocityX,velocityY;
        public bool airborne;
        public FloorItemRecord Copy(){return new FloorItemRecord{item=item==null?null:item.Copy(),normalizedX=normalizedX,normalizedY=normalizedY,velocityX=velocityX,velocityY=velocityY,airborne=airborne};}
    }
    [Serializable] public sealed class DeliveryOrder
    {
        public string orderId;
        public int arrivalDay;
        public List<ItemRecord> items=new List<ItemRecord>();
        public DeliveryOrder Copy()
        {var c=new DeliveryOrder{orderId=orderId,arrivalDay=arrivalDay};foreach(var i in items)c.items.Add(i==null?null:i.Copy());return c;}
    }
    [Serializable] public sealed class ParcelRecord
    {
        public string parcelId;
        public float normalizedX,normalizedY;
        public List<ItemRecord> items=new List<ItemRecord>();
        public ParcelRecord Copy()
        {var p=new ParcelRecord{parcelId=parcelId,normalizedX=normalizedX,normalizedY=normalizedY};foreach(var i in items)p.items.Add(i==null?null:i.Copy());return p;}
    }
    public struct ParcelSummary
    {
        public readonly string id;public readonly int count;
        public ParcelSummary(string id,int count){this.id=id;this.count=count;}
    }
    // 판매가는 물건 자체가 아니라 진열 등록에 속합니다.
    [Serializable] public sealed class DisplayListing
    {
        public string instanceId;
        public int price;
        public DisplayListing Copy() { return (DisplayListing)MemberwiseClone(); }
    }
    public sealed class SaleLine
    {
        public string containerId, instanceId;
        public int price;
    }
    public sealed class MoveRequest
    {
        public string sourceContainer;
        public string targetContainer;
        public string instanceId;
        public int x, y;
        // 0/1/2/3 = 시계 방향 0/90/180/270도입니다.
        public int rotation;
        // 드래그 시작 당시 모델 버전입니다. 재고가 바뀌었다면 오래된 요청을 거절합니다.
        public long expectedRevision = -1;
    }
    [Serializable] public sealed class ItemProposal
    {
        public string containerId, instanceId, itemId, quality;
        public long revision;
        // 추천 후보 전달용입니다. 전달만으로 예약/판매/차감되지 않습니다.
    }
}
