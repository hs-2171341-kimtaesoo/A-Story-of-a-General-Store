using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShopItems
{
    // 아이템 정의(JSON)와 별개인 '플레이어가 실제로 보유한 재고'입니다.
    // 같은 itemId + quality는 반드시 같은 칸에 합칩니다. instanceId는 사용하지 않습니다.
    [Serializable]
    public class WarehouseStack
    {
        public int slotIndex;
        public string itemId;
        public string quality;
        public int quantity;
        public double averagePurchasePrice;
        public int sellingPrice;

        // UI에 복사본을 주어 외부에서 실수로 실제 수량을 수정하지 못하게 합니다.
        public WarehouseStack Copy() { return (WarehouseStack)MemberwiseClone(); }
    }

    [Serializable]
    public class WarehouseSaveData
    {
        public int version = 1;
        public int capacity;
        public List<WarehouseStack> stacks = new List<WarehouseStack>();
    }

    // MonoBehaviour가 아닌 순수한 창고 규칙 클래스입니다.
    // UI와 분리했기 때문에 나중에 Canvas UI로 교체해도 창고 로직은 유지할 수 있습니다.
    public class WarehouseInventory
    {
        readonly ItemManager items;
        WarehouseSaveData data;
        public event Action Changed;
        public int Capacity { get { return data.capacity; } }
        public int UsedSlots { get { return data.stacks.Count; } }

        public WarehouseInventory(ItemManager itemManager, int capacity = 40)
        {
            if (itemManager == null || !itemManager.IsLoaded)
            {
                throw new InvalidOperationException("ItemManager가 먼저 로드되어야 합니다.");
            }
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            items = itemManager;
            data = new WarehouseSaveData { capacity = capacity };
        }

        WarehouseStack Find(string itemId, string quality)
        {
            return data.stacks.Find(x => x.itemId == itemId && x.quality == quality);
        }
        WarehouseStack FindSlot(int index) { return data.stacks.Find(x => x.slotIndex == index); }
        void Notify() { if (Changed != null) Changed(); }

        public WarehouseStack GetSlot(int index)
        {
            WarehouseStack stack = FindSlot(index);
            return stack == null ? null : stack.Copy();
        }
        public int GetQuantity(string itemId, string quality)
        {
            WarehouseStack stack = Find(itemId, quality);
            return stack == null ? 0 : stack.quantity;
        }

        // 수량을 한 번에 입고합니다. 실패하면 일부만 넣지 않고 전체 요청을 거절합니다.
        // purchasePrice는 '이번에 매입한 물건 1개당 가격'입니다. 총액이 아닙니다.
        public bool TryAdd(string itemId, string quality, int quantity, int purchasePrice, out string error)
        {
            error = "";
            int defaultSellingPrice;
            if (quantity < 1 || purchasePrice < 0)
            { error = "입고 수량은 양수, 매입가는 0 이상이어야 합니다."; return false; }
            if (!items.TryGetQualityPrice(itemId, quality, out defaultSellingPrice))
            { error = "없는 아이템 또는 품질입니다."; return false; }

            WarehouseStack stack = Find(itemId, quality);
            if (stack != null)
            {
                // int 수량이 넘치는 경우 음수로 바뀌지 않게 방지합니다.
                if ((long)stack.quantity + quantity > int.MaxValue)
                { error = "한 칸에 보관 가능한 수량 범위를 초과했습니다."; return false; }

                // 예: 10개를 80에, 2개를 100에 매입 → (10×80 + 2×100) / 12
                stack.averagePurchasePrice =
                    (stack.averagePurchasePrice * stack.quantity + (double)purchasePrice * quantity)
                    / (stack.quantity + quantity);
                stack.quantity += quantity;
                // 추가 입고해도 플레이어가 정한 판매가는 유지합니다.
            }
            else
            {
                if (UsedSlots >= Capacity)
                { error = "빈 창고 칸이 없습니다."; return false; }
                int emptySlot = 0;
                while (FindSlot(emptySlot) != null) emptySlot++;
                data.stacks.Add(new WarehouseStack {
                    slotIndex = emptySlot, itemId = itemId, quality = quality,
                    quantity = quantity, averagePurchasePrice = purchasePrice,
                    sellingPrice = defaultSellingPrice
                });
            }
            Notify(); return true;
        }

        // 판매/진열 이동 때 사용할 출고 기능입니다. 돈 증가까지 처리하지는 않습니다.
        // 평균원가 방식이므로 일부를 출고해도 남은 물건의 평균 매입가는 유지합니다.
        public bool TryRemove(string itemId, string quality, int quantity, out string error)
        {
            error = "";
            WarehouseStack stack = Find(itemId, quality);
            if (quantity < 1 || stack == null || stack.quantity < quantity)
            { error = "출고할 재고가 부족하거나 수량이 잘못되었습니다."; return false; }
            stack.quantity -= quantity;
            if (stack.quantity == 0) data.stacks.Remove(stack); // 0개가 되면 칸을 비웁니다.
            Notify(); return true;
        }
        public bool TrySetSellingPrice(int slotIndex, int price)
        {
            WarehouseStack stack = FindSlot(slotIndex);
            if (stack == null || price < 0) return false;
            stack.sellingPrice = price;
            Notify(); return true;
        }

        // 종류 필터는 재고를 옮기지 않습니다. 해당하는 칸 번호만 반환합니다.
        // ""는 전체, "potion"은 포션, "food"는 음식입니다.
        public List<int> GetVisibleSlots(string typeFilter)
        {
            var result = new List<int>();
            for (int index = 0; index < Capacity; index++)
            {
                WarehouseStack stack = FindSlot(index);
                if (string.IsNullOrEmpty(typeFilter)
                    || (stack != null && items.GetItem(stack.itemId).type == typeFilter))
                    result.Add(index);
            }
            return result;
        }
        public string ToJson() { return JsonUtility.ToJson(data, true); }

        // 검증을 모두 통과한 뒤 한 번에 교체하므로, 잘못된 저장 파일은 현재 재고를 덮어쓰지 않습니다.
        public bool TryLoadJson(string json, out string error)
        {
            error = "";
            try
            {
                WarehouseSaveData loaded = JsonUtility.FromJson<WarehouseSaveData>(json);
                if (loaded == null || loaded.version != 1 || loaded.capacity < 1 || loaded.capacity > 10000
                    || loaded.stacks == null || loaded.stacks.Count > loaded.capacity)
                    throw new InvalidOperationException("지원하지 않는 창고 데이터입니다.");
                var occupied = new HashSet<int>();
                var keys = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
                foreach (WarehouseStack stack in loaded.stacks)
                {
                    if (stack == null || stack.slotIndex < 0 || stack.slotIndex >= loaded.capacity
                        || !occupied.Add(stack.slotIndex) || stack.quantity < 1 || stack.sellingPrice < 0
                        || double.IsNaN(stack.averagePurchasePrice) || double.IsInfinity(stack.averagePurchasePrice)
                        || stack.averagePurchasePrice < 0 || items.GetQuality(stack.itemId, stack.quality) == null)
                        throw new InvalidOperationException("잘못된 창고 칸 또는 아이템 정보입니다.");
                    HashSet<string> qualities;
                    if (!keys.TryGetValue(stack.itemId, out qualities))
                    { qualities = new HashSet<string>(StringComparer.Ordinal); keys.Add(stack.itemId, qualities); }
                    if (!qualities.Add(stack.quality))
                        throw new InvalidOperationException("같은 아이템/품질이 여러 칸에 중복 저장되어 있습니다.");
                }
                data = loaded;
            }
            catch (Exception e) { error = e.Message; return false; }
            Notify(); return true;
        }
    }
}
