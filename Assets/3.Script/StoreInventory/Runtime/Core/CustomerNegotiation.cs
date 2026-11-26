using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    public sealed partial class RecommendationSession
    {
        bool liveVisit,askStock,sellAfter;
        int visitNumber,failures;
        string requestId;
        public bool EventVisit {get;private set;}
        public bool Selling {get;private set;}
        public int SellerPrice {get{return activeProfile==null?0:activeProfile.sellPrice;}}
        public string Speech {get;private set;}
        public long CounterPrice {get;private set;}=-1;
        public event Action<string,string> Returned;
        public bool StoryVisit {get{return liveVisit;}}
        public long ProposedPrice {get;private set;}=-1;
        public void SetOffer(long amount){
            ProposedPrice=amount;CounterPrice=-1;Notify();
        }

        public void ConfigureVisit(int number,int purpose,double stockRoll)
        {
            liveVisit=true;visitNumber=number;var p=activeProfile;
            askStock=stockRoll<Math.Min(1,p.inquiryChance+(number-1)*p.inquiryPerVisit);
            sellAfter=purpose==1&&!string.IsNullOrEmpty(p.sellItemId)&&model.Catalog.CanTrade(p.sellItemId);
            EventVisit=purpose==2&&!string.IsNullOrWhiteSpace(p.eventText);
            Speech=EventVisit?p.eventText:p.visitStories!=null&&p.visitStories.Length>0
                ?p.visitStories[Math.Min(number-1,p.visitStories.Length-1)]:DialogueText;
        }
        void ResetVisit()
        {
            liveVisit=askStock=sellAfter=Selling=EventVisit=false;
            failures=0;requestId=null;Speech=null;ProposedPrice=CounterPrice=-1;
        }
        string SaleDescription(){
            return "계산은 됐고, 제가 가져온 "+model.Catalog.Find(activeProfile.sellItemId).qualities[0].name+"도 "+SellerPrice+" G에 매입하시겠어요?";
        }
        public bool AcceptSeller(out string message){
            if(!Selling){message="판매 제안이 없습니다.";return false;}
            if(!model.BuyCustomerItem(activeProfile.sellItemId,SellerPrice,out message)){Speech=message;Notify();return false;}
            Leave();return true;
        }
        void ReturnLine(PendingOffer line){
            string destination=model.StoreCounterReturn(line);
            picked.RemoveAll(x=>x.instanceId==line.instanceId);offers.RemoveAll(x=>x.instanceId==line.instanceId);
            model.ReleaseCounterOffer(line.instanceId);
            if(Returned!=null)Returned(line.instanceId,destination);
        }
        public void Decline(){
            if(liveVisit&&CounterPresented){
                foreach(var line in Purchases())ReturnLine(line);
                foreach(var line in Offers())ReturnLine(line);
            }
            Leave();
        }
        bool Evaluate(List<PendingOffer> lines,long? proposal,out string error){
            error="";long total=0;decimal ceiling=decimal.MaxValue;bool requested=false;
            bool rejected=false;
            foreach(var line in lines){
                var item=model.GetItem(line.containerId,line.instanceId);
                if(item==null){error="물건이 이동했습니다.";return false;}
                bool wanted=requestId==null?Array.IndexOf(activeProfile.preferredItemIds,item.itemId)>=0:item.itemId==requestId&&!requested;
                if(!wanted){
                    ReturnLine(line);ProposedPrice=CounterPrice=-1;
                    error=Speech=requestId==null?"그건 이번 여행에 필요 없어요. 빼 주세요.":"제가 찾는 물건 하나만 부탁해요.";
                    rejected=true;continue;
                }
                requested=true;total+=line.price;
            }
            if(rejected){Notify();return false;}
            long ask=proposal??total;if(ask<0){error="가격을 다시 입력해 주세요.";return false;}
            // The negotiated total is allocated in proportion to displayed prices, as at checkout.
            foreach(var line in lines){
                var item=model.GetItem(line.containerId,line.instanceId);
                int fair=model.Catalog.ReferencePrice(item.itemId,item.quality);
                if(line.price>0)ceiling=Math.Min(ceiling,(decimal)fair*(decimal)activeProfile.ValueRatio(item.itemId)*total/line.price);
            }
            long wallet=activeProfile.wallet;
            decimal flexible=Math.Floor(wallet*(decimal)Math.Min(1,activeProfile.spendingFraction+activeProfile.negotiationRoom));
            decimal limit=Math.Min(ceiling,flexible);
            if(ask<=limit)return true;
            failures++;
            string reason=ask>ceiling?"그 물건의 시세에 비해 너무 비싸요.":ask>wallet?"가진 돈으로는 어려워요.":"오늘 쓰려던 금액보다 높네요.";
            if(failures>=activeProfile.patience){error=reason+" 다음에 올게요.";Decline();return false;}
            CounterPrice=(long)Math.Min(limit,Math.Min((decimal)budget,Math.Floor(ceiling*.9m)));
            Speech=error=reason+" "+CounterPrice+" G면 어떨까요?";Notify();return false;
        }
    }

    public sealed partial class InventoryModel
    {
        internal string StoreCounterReturn(PendingOffer line){
            if(IsWarehouse(line.containerId))return line.containerId;
            var next=state.Copy();var source=Find(next,line.containerId);
            var item=source.items.Find(x=>x.instanceId==line.instanceId);
            if(item==null)return line.containerId;
            source.items.Remove(item);next.listings.RemoveAll(x=>x.instanceId==line.instanceId);
            string destination=PackCustomerItem(next,item);Validate(next);Commit(next);return destination;
        }
        string PackCustomerItem(InventorySave next,ItemRecord item){
            foreach(var box in next.containers)
                if(IsWarehouse(box.containerId)&&TryPlaceRectangle(next,box,item))return box.containerId;
            AppendTemporary(next,new[]{item});return TemporaryStorageId;
        }
        internal bool BuyCustomerItem(string id,int price,out string message){
            var def=Catalog.Find(id);
            if(!Catalog.CanTrade(id)||price<0||price>Gold||def==null||!CanReceiveDelivery(id)){message="매입할 수 없습니다. 소지금과 물품을 확인하세요.";return false;}
            var next=state.Copy();var item=new ItemRecord{instanceId=Guid.NewGuid().ToString("N"),itemId=id,quality=def.qualities[0].quality,purchasePrice=price};
            string destination=PackCustomerItem(next,item);next.gold-=price;Validate(next);Commit(next);
            message=destination==TemporaryStorageId?"매입 완료 · 임시저장고에 보관했습니다.":"매입 완료 · 상자에 보관했습니다.";return true;
        }
    }
}
