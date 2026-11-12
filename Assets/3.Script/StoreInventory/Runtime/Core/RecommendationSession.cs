using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    public sealed class PendingOffer
    {
        public string containerId, instanceId;
        public int price;
        public bool customerPicked;
        public PendingOffer Copy() { return (PendingOffer)MemberwiseClone(); }
    }
    // 손님 추천 → 수락 → 재고/돈 확정의 작은 데모 규칙입니다. 스토리/취향 AI는 아닙니다.
    public sealed class RecommendationSession
    {
        readonly InventoryModel model;
        ICustomerPurchasePolicy purchasePolicy=new DefaultCustomerPurchasePolicy();
        CustomerDefinition activeProfile;
        public void SetPurchasePolicy(ICustomerPurchasePolicy policy)
        {if(CustomerPresent)throw new InvalidOperationException("방문 중에는 구매 정책을 바꿀 수 없습니다.");purchasePolicy=policy??throw new ArgumentNullException("policy");}
        // 캐릭터 JSON과 구매 판단을 분리합니다. UI는 프로필만 전달합니다.
        public bool TryEnterShopping(CustomerDefinition profile,out string error)
        {
            error="";if(profile==null){error="손님 프로필이 없습니다.";return false;}
            try{profile.Validate();}catch(ArgumentException e){error=e.Message;return false;}
            if(!profile.enabled){error="방문할 수 없는 손님입니다.";return false;}
            return EnterProfile(profile.Copy(),out error);
        }
        readonly List<PendingOffer> offers = new List<PendingOffer>();
        readonly List<PendingOffer> picked = new List<PendingOffer>();
        public List<PendingOffer> Purchases()
        { var result = new List<PendingOffer>(); foreach (PendingOffer p in picked) result.Add(p.Copy()); return result; }
        // 테스트 선택 규칙: 선호 itemId 순서 → 진열 순서로 최대 5개. 품질이 달라도 낱개 UID로 예약합니다.
        public bool TryEnterShopping(string name,long customerBudget,double ratio,string[] preferredIds,int pickLimit,out string error)
        {
            return TryEnterShopping(new CustomerDefinition{id="test",displayName=name,wallet=customerBudget,
                spendingFraction=1,maxPriceRatio=ratio,preferredItemIds=preferredIds,maxPickCount=pickLimit},out error);
        }
        bool EnterProfile(CustomerDefinition profile,out string error)
        {
            error="";
            if(CustomerPresent){error="이미 손님이 있습니다.";return false;}
            long customerBudget=purchasePolicy.SpendingBudget(profile);
            if(customerBudget<0||customerBudget>profile.wallet){error="구매 정책의 사용 예산이 잘못되었습니다.";return false;}
            string[] preferredIds=profile.preferredItemIds;int pickLimit=profile.maxPickCount;
            var candidates=new List<PendingOffer>(); var used=new HashSet<string>(); long sum=0;
            var displayContainers=model.DisplayContainers();
            foreach (string preferred in preferredIds)
                foreach (ContainerRecord c in displayContainers)
                    if (InventoryModel.IsDisplay(c.containerId)) foreach (ItemRecord item in c.items)
                    {
                        int price;
                        if (candidates.Count>=pickLimit || item.itemId!=preferred || used.Contains(item.instanceId)
                            || model.IsReserved(item.instanceId) || !model.TryGetOfferPrice(c.containerId,item.instanceId,out price)
                            || !purchasePolicy.AcceptItem(profile,item,price,model.Catalog.ReferencePrice(item.itemId,item.quality)) || price>customerBudget-sum) continue;
                        candidates.Add(new PendingOffer { containerId=c.containerId,instanceId=item.instanceId,price=price,customerPicked=true });
                        used.Add(item.instanceId); sum+=price;
                    }
            if (candidates.Count==0) { error="선호 품목 중 가격과 예산에 맞는 진열 상품이 없어 돌아갔습니다."; return false; }
            if (!model.TryReserve(candidates,out error)) return false;
            picked.Clear();picked.AddRange(candidates);offers.Clear();CustomerPresent=true;
            ConversationStarted=false;activeProfile=profile;CustomerName=string.IsNullOrWhiteSpace(profile.displayName)?"테스트 손님":profile.displayName;
            budget=customerBudget;allowedPriceRatio=profile.maxPriceRatio;Notify();return true;
        }
        long budget;
        double allowedPriceRatio;
        public bool CustomerPresent { get; private set; }
        public bool ConversationStarted { get; private set; }
        public bool BeginConversation(out string error)
        {error="";if(!CustomerPresent){error="손님이 없습니다.";return false;}ConversationStarted=true;Notify();return true;}
        public string CustomerName { get; private set; }
        public event Action Changed;
        public RecommendationSession(InventoryModel model) { this.model = model; }
        void Notify() { if (Changed != null) Changed(); }
        public List<PendingOffer> Offers()
        { var result = new List<PendingOffer>(); foreach (PendingOffer offer in offers) result.Add(offer.Copy()); return result; }
        // 이전 추천 전용 동작을 연결한 코드의 호환용 API입니다. 새 데모 방문은 TryEnterShopping을 사용합니다.
        public bool TryEnter(string name, long customerBudget, double maxPriceRatio, bool requireDisplay, out string error)
        {
            error = "";
            if (CustomerPresent) { error = "이미 손님이 있습니다."; return false; }
            if (customerBudget < 0 || double.IsNaN(maxPriceRatio) || double.IsInfinity(maxPriceRatio) || maxPriceRatio <= 0)
            { error = "손님 예산/가격 허용 배율 오류입니다."; return false; }
            if (requireDisplay && model.DisplayListingCount == 0)
            { error = "판매 등록된 진열 상품이 없어 손님이 돌아갔습니다."; return false; }
            ConversationStarted=true;activeProfile=null; offers.Clear(); CustomerPresent = true; CustomerName = string.IsNullOrWhiteSpace(name) ? "손님" : name;
            budget = customerBudget; allowedPriceRatio = maxPriceRatio; Notify(); return true;
        }
        public bool TryRecommend(string containerId, string instanceId, out string error)
        {
            error = "";
            if (!CustomerPresent) { error = "먼저 손님을 맞이하세요."; return false; }
            if(!ConversationStarted){error="이야기하기를 누른 뒤 드래그로 추천하세요.";return false;}
            if (model.IsReserved(instanceId)) { error = "손님이 이미 구매할 물건으로 골랐습니다."; return false; }
            if (offers.Exists(o => o.instanceId == instanceId)) { error = "이미 추천 목록에 있는 물건입니다."; return false; }
            int price;
            if (!model.TryGetOfferPrice(containerId, instanceId, out price))
            { error = "물건이 없거나 진열 가격이 등록되지 않았습니다."; return false; }
            offers.Add(new PendingOffer { containerId = containerId, instanceId = instanceId, price = price });
            Notify(); return true;
        }
        public void Remove(string instanceId) { if (offers.RemoveAll(o => o.instanceId == instanceId) > 0) Notify(); }
        public void Leave() { ConversationStarted=false;activeProfile=null;model.ReleaseReservations(); picked.Clear(); CustomerPresent = false; CustomerName = ""; offers.Clear(); Notify(); }
        public bool TrySubmit(out long revenue, out string error)
        {return Submit(false,out revenue,out error);}
        public bool TrySubmitPurchasesOnly(out long revenue,out string error)
        {return Submit(true,out revenue,out error);}
        bool Submit(bool purchasesOnly,out long revenue,out string error)
        {
            revenue = 0; error = "";
            if (!CustomerPresent || (purchasesOnly?picked.Count:offers.Count + picked.Count) == 0) { error = "손님과 추천 물건이 필요합니다."; return false; }
            long total = 0; var lines = new List<SaleLine>();
            var combined = Purchases(); if(!purchasesOnly)combined.AddRange(Offers());
            foreach (PendingOffer offer in combined)
            {
                ItemRecord item = model.GetItem(offer.containerId, offer.instanceId); int quote;
                if (item == null || !model.TryGetOfferPrice(offer.containerId, offer.instanceId, out quote) || quote != offer.price)
                { error = "추천 물건이 이동/소진되었거나 가격이 바뀌었습니다. 제거한 뒤 다시 추천하세요."; return false; }
                if (activeProfile!=null ? !purchasePolicy.AcceptItem(activeProfile,item,offer.price,model.Catalog.ReferencePrice(item.itemId,item.quality))
                    : offer.price > model.Catalog.ReferencePrice(item.itemId, item.quality) * allowedPriceRatio)
                { error = "손님이 가격이 높다고 제안을 거절했습니다."; return false; }
                total += offer.price;
                lines.Add(new SaleLine { containerId = offer.containerId, instanceId = offer.instanceId, price = offer.price });
            }
            if (total > budget) { error = "손님 예산을 초과해서 제안을 거절했습니다."; return false; }
            var customerIds = new HashSet<string>(); foreach (PendingOffer p in picked) customerIds.Add(p.instanceId);
            if (!model.TrySellCustomer(lines, model.Revision, customerIds, out revenue, out error)) return false;
            // 한 번 판매하면 손님/목록을 종료합니다. 연타해도 다시 판매할 수 없습니다.
            ConversationStarted=false;activeProfile=null; picked.Clear(); offers.Clear(); CustomerPresent = false; CustomerName = ""; Notify(); return true;
        }
    }
}
