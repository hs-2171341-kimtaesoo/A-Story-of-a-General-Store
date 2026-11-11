using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    public sealed class PendingOffer
    {
        public string containerId, instanceId;
        public int price;
        public PendingOffer Copy() { return (PendingOffer)MemberwiseClone(); }
    }
    // 손님 추천 → 수락 → 재고/돈 확정의 작은 데모 규칙입니다. 스토리/취향 AI는 아닙니다.
    public sealed class RecommendationSession
    {
        readonly InventoryModel model;
        readonly List<PendingOffer> offers = new List<PendingOffer>();
        long budget;
        double allowedPriceRatio;
        public bool CustomerPresent { get; private set; }
        public string CustomerName { get; private set; }
        public event Action Changed;
        public RecommendationSession(InventoryModel model) { this.model = model; }
        void Notify() { if (Changed != null) Changed(); }
        public List<PendingOffer> Offers()
        { var result = new List<PendingOffer>(); foreach (PendingOffer offer in offers) result.Add(offer.Copy()); return result; }
        public bool TryEnter(string name, long customerBudget, double maxPriceRatio, bool requireDisplay, out string error)
        {
            error = "";
            if (CustomerPresent) { error = "이미 손님이 있습니다."; return false; }
            if (customerBudget < 0 || double.IsNaN(maxPriceRatio) || double.IsInfinity(maxPriceRatio) || maxPriceRatio <= 0)
            { error = "손님 예산/가격 허용 배율 오류입니다."; return false; }
            if (requireDisplay && model.Snapshot().listings.Count == 0)
            { error = "판매 등록된 진열 상품이 없어 손님이 돌아갔습니다."; return false; }
            offers.Clear(); CustomerPresent = true; CustomerName = string.IsNullOrWhiteSpace(name) ? "손님" : name;
            budget = customerBudget; allowedPriceRatio = maxPriceRatio; Notify(); return true;
        }
        public bool TryRecommend(string containerId, string instanceId, out string error)
        {
            error = "";
            if (!CustomerPresent) { error = "먼저 손님을 맞이하세요."; return false; }
            if (offers.Exists(o => o.instanceId == instanceId)) { error = "이미 추천 목록에 있는 물건입니다."; return false; }
            int price;
            if (!model.TryGetOfferPrice(containerId, instanceId, out price))
            { error = "물건이 없거나 진열 가격이 등록되지 않았습니다."; return false; }
            offers.Add(new PendingOffer { containerId = containerId, instanceId = instanceId, price = price });
            Notify(); return true;
        }
        public void Remove(string instanceId) { if (offers.RemoveAll(o => o.instanceId == instanceId) > 0) Notify(); }
        public void Leave() { CustomerPresent = false; CustomerName = ""; offers.Clear(); Notify(); }
        public bool TrySubmit(out long revenue, out string error)
        {
            revenue = 0; error = "";
            if (!CustomerPresent || offers.Count == 0) { error = "손님과 추천 물건이 필요합니다."; return false; }
            long total = 0; var lines = new List<SaleLine>();
            foreach (PendingOffer offer in offers)
            {
                ItemRecord item = model.GetItem(offer.containerId, offer.instanceId); int quote;
                if (item == null || !model.TryGetOfferPrice(offer.containerId, offer.instanceId, out quote) || quote != offer.price)
                { error = "추천 물건이 이동/소진되었거나 가격이 바뀌었습니다. 제거한 뒤 다시 추천하세요."; return false; }
                if (offer.price > model.Catalog.ReferencePrice(item.itemId, item.quality) * allowedPriceRatio)
                { error = "손님이 가격이 높다고 제안을 거절했습니다."; return false; }
                total += offer.price;
                lines.Add(new SaleLine { containerId = offer.containerId, instanceId = offer.instanceId, price = offer.price });
            }
            if (total > budget) { error = "손님 예산을 초과해서 제안을 거절했습니다."; return false; }
            if (!model.TrySell(lines, model.Revision, out revenue, out error)) return false;
            // 한 번 판매하면 손님/목록을 종료합니다. 연타해도 다시 판매할 수 없습니다.
            offers.Clear(); CustomerPresent = false; CustomerName = ""; Notify(); return true;
        }
    }
}
