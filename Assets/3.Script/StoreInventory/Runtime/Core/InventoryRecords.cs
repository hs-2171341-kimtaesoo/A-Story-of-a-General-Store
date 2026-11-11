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
        public int version = 3;
        public long gold;
        public List<DisplayListing> listings = new List<DisplayListing>();
        public List<ContainerRecord> containers = new List<ContainerRecord>();
        public InventorySave Copy()
        {
            var result = new InventorySave { format = format, version = version, gold = gold };
            foreach (DisplayListing listing in listings) result.listings.Add(listing == null ? null : listing.Copy());
            foreach (ContainerRecord container in containers) result.containers.Add(container == null ? null : container.Copy());
            return result;
        }
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
