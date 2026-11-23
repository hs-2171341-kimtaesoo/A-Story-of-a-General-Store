using System;
using System.Linq;
namespace GeneralStore.Inventory.Verification
{
    // 실제 C# 모델 검사입니다. Unity의 Run Model Checks에 연결되며 여기서는 실행하지 못했습니다.
    public static class CounterCheckoutChecks
    {
        static int checks;
        static void Check(bool value,string name){checks++;if(!value)throw new Exception("카운터 검사 실패: "+name);}
        static InventoryModel Model(long gold=100,int listedPrice=80)
        {
            var save=new InventorySave{gold=gold};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});save.containers.Add(new ContainerRecord{containerId="display_preview",columns=8,rows=5});
            var model=new InventoryModel(InventoryModelChecks.Fixture(),save);string error;
            Check(model.TryAdd("warehouse","C001","common",3,50,out error),"테스트 상품 입고");
            var items=model.GetContainer("warehouse").items;
            for(int n=0;n<2;n++)Check(model.TryListOnDisplay(new MoveRequest{sourceContainer="warehouse",targetContainer="display_preview",instanceId=items[n].instanceId,x=n*2,y=0,rotation=0,expectedRevision=model.Revision},listedPrice,out error),"진열가 등록");
            return model;
        }
        static RecommendationSession Enter(InventoryModel model)
        {
            var session=new RecommendationSession(model);string error;
            Check(session.TryEnterShopping(new CustomerDefinition{id="counter",displayName="손님",preferredItemIds=new[]{"C001"},wallet=1000,spendingFraction=1,maxPriceRatio=1.5,maxPickCount=2},out error),"손님 방문");
            Check(session.PresentOnCounter(out error)&&session.CounterPresented,"카운터 제시");return session;
        }
        public static int Run()
        {
            checks=0;string error;long revenue;
            CheckBrowsingAndReturns();
            CheckReturnPreviews();
            var model=Model();var session=Enter(model);var ids=session.Purchases();
            Check(ids.Count==2&&ids.Sum(x=>x.price)==160,"등록 판매가 합계");
            long revision=model.Revision;
            Check(!session.TrySubmitAtTotal(-1,out revenue,out error)&&model.Revision==revision,"음수 거절 원본 유지");
            Check(!session.TrySubmitAtTotal(400,out revenue,out error)&&model.Gold==100&&model.GetContainer("display_preview").items.Count==2,"과도한 제안 거절/물건 유지");
            Check(session.TrySubmitAtTotal(120,out revenue,out error)&&revenue==120&&model.Gold==220,"협의 총액 한 번만 지급");
            Check(model.GetContainer("display_preview").items.Count==0&&!session.CustomerPresent,"상품과 세션 종료");
            Check(!session.TrySubmitAtTotal(120,out revenue,out error)&&model.Gold==220,"연타 중복 판매 거절");
            model=Model();session=Enter(model);Check(session.BeginConversation(out error),"추가 대화");
            var spare=model.GetContainer("warehouse").items[0];
            Check(session.TryRecommend("warehouse",spare.instanceId,out error)&&model.IsReserved(spare.instanceId),"추천 물품 카운터 예약");
            Check(!model.TryRemove("warehouse",spare.instanceId,out error),"카운터 추천품 우회 반출 차단");
            session.Remove(spare.instanceId);Check(!model.IsReserved(spare.instanceId)&&model.GetItem("warehouse",spare.instanceId)!=null,"추천 취소 원래 위치 복구");
            Check(session.TryRecommend("warehouse",spare.instanceId,out error),"추천 재추가");
            Check(session.TrySubmit(out revenue,out error)&&revenue==260&&model.GetContainer("warehouse").items.Count==0,"기존가 구매+추천 원자적 판매");
            model=Model();session=Enter(model);var returnedIds=session.Purchases();session.Leave();
            Check(model.GetContainer("display_preview").items.Count==2&&model.Gold==100&&returnedIds.All(x=>!model.IsReserved(x.instanceId)),"거절 시 원래 진열 보존");
            model=Model(long.MaxValue-5);session=Enter(model);revision=model.Revision;
            Check(!session.TrySubmitAtTotal(120,out revenue,out error)&&model.Revision==revision&&model.GetContainer("display_preview").items.Count==2,"소지금 오버플로 판매 롤백");
            model=Model(100,0);session=Enter(model);
            Check(!session.TrySubmitAtTotal(1,out revenue,out error),"무료 상품 무근거 증액 거절");
            Check(session.TrySubmitAtTotal(0,out revenue,out error)&&revenue==0&&model.Gold==100,"0G 결제 원자적 처리");
            return checks;
        }
        static CustomerDefinition Browser()
        {return new CustomerDefinition{id="browser",displayName="방문 손님",preferredItemIds=new[]{"C001"},wallet=1000,spendingFraction=1,maxPriceRatio=1.5,maxPickCount=2,situationText="회복 물약이 필요합니다."};}
        static void CheckBrowsingAndReturns()
        {
            string error;long revenue;var model=Model();var session=new RecommendationSession(model);
            Check(session.BeginBrowsing(Browser(),out error)&&session.Browsing&&session.CustomerPresent&&session.Purchases().Count==0,"구매품 선정 전 먼저 등장");
            Check(!session.PresentOnCounter(out error)&&!session.BeginConversation(out error),"둘러보는 중 카운터/대화 우회 금지");
            foreach(var item in model.GetContainer("display_preview").items)Check(model.TryRemove("display_preview",item.instanceId,out error),"둘러보는 중 재고 변경 가능");
            Check(!session.FinishBrowsing(out error)&&!session.CustomerPresent&&!session.CounterPresented,"빈 진열장 방문 후 카운터 없이 귀가");
            Check(session.BeginBrowsing(Browser(),out error),"빈 진열장에도 다음 손님 방문");
            var spare=model.GetContainer("warehouse").items[0];
            Check(model.TryListOnDisplay(new MoveRequest{sourceContainer="warehouse",targetContainer="display_preview",instanceId=spare.instanceId,x=0,y=0,expectedRevision=model.Revision},80,out error),"둘러보는 중 새 진열");
            Check(session.FinishBrowsing(out error)&&session.Purchases().Count==1&&model.IsReserved(spare.instanceId),"둘러본 뒤 최신 재고로 선택/예약");session.Leave();
            model=Model();session=Enter(model);spare=model.GetContainer("warehouse").items[0];
            Check(!session.ConversationStarted&&session.TryRecommend("warehouse",spare.instanceId,out error),"대화 없이 바로 추천");
            long revision=model.Revision;
            Check(!session.TryReturnRecommendation(spare.instanceId,"warehouse",7,4,0,out error)&&model.Revision==revision&&model.IsReserved(spare.instanceId)&&session.Offers().Count==1,"실패한 회수는 재고/예약/추천 보존");
            Check(session.TryReturnRecommendation(spare.instanceId,"warehouse",0,0,0,out error)&&!model.IsReserved(spare.instanceId)&&session.Offers().Count==0&&model.GetItem("warehouse",spare.instanceId).purchasePrice==50,"드래그 회수는 UID/매입가 유지 및 예약 해제");
            Check(!session.TryReturnRecommendation("missing-cart-uid","warehouse",4,0,0,out error),"카운터에 없는 UID의 회수 금지");
            Check(session.BeginConversation(out error)&&session.BeginConversation(out error)&&session.DialogueText!=null,"대화는 반복해서 단서만 확인");
            Check(model.TryAdd("warehouse","E001","none",1,10,out error),"비선호 추천 준비");var unwanted=model.GetContainer("warehouse").items.Find(item=>item.itemId=="E001");
            Check(session.TryRecommend("warehouse",unwanted.instanceId,out error),"비선호 물건도 추천 동작 허용");revision=model.Revision;
            Check(!session.TrySubmit(out revenue,out error)&&model.Revision==revision&&model.Gold==100&&session.CustomerPresent,"비선호 제안 거절 시 판매/소모 없음");
            session.Remove(unwanted.instanceId);Check(session.TrySubmit(out revenue,out error)&&revenue==160,"추천을 거둔 뒤 원래 구매품 거래 가능");
            model=Model();session=Enter(model);spare=model.GetContainer("warehouse").items[0];Check(session.TryRecommend("warehouse",spare.instanceId,out error),"원래 물건만 계산 경로 준비");
            Check(session.TrySubmitPurchasesOnly(out revenue,out error)&&!model.IsReserved(spare.instanceId)&&model.GetItem("warehouse",spare.instanceId)!=null,"남은 추천품은 계산 종료 후 잠금 해제");
        }
        static void CheckReturnPreviews()
        {
            string error;var model=Model();var session=Enter(model);var spare=model.GetContainer("warehouse").items[0];
            Check(session.TryRecommend("warehouse",spare.instanceId,out error),"배치 미리보기용 추천 준비");
            var request=new MoveRequest{sourceContainer="warehouse",targetContainer="warehouse",instanceId=spare.instanceId,x=0,y=0,expectedRevision=model.Revision};
            long revision=model.Revision;var preview=session.PreviewCounterReturn(request);
            Check(preview.allowed&&preview.cells.Count==4&&model.Revision==revision&&model.IsReserved(spare.instanceId),"회수 미리보기4칸/예약 및 리비전 보존");
            Check(!model.Preview(request).allowed,"일반 이동 API는 여전히 예약을 우회하지 못함");
            request.x=7;request.y=4;Check(!session.PreviewCounterReturn(request).allowed&&model.Revision==revision,"경계 밖 미리보기는 거절");
            request.targetContainer="display_preview";request.x=4;request.y=0;
            Check(session.PreviewCounterReturn(request).allowed,"예약된 추천 물품도 진열장 미리보기 허용");
            Check(session.TryReturnRecommendation(spare.instanceId,"display_preview",4,0,0,out error)&&session.Offers().Count==0&&!model.IsReserved(spare.instanceId),"추천 물품을 진열장으로 이동 및 잠금 해제");
            int price;Check(model.TryGetOfferPrice("display_preview",spare.instanceId,out price)&&price==100&&model.GetItem("display_preview",spare.instanceId).purchasePrice==50,"기존 제안가 및 매입가 유지");
            var first=session.Purchases()[0];Check(session.TryReturnRecommendation(first.instanceId,"warehouse",0,0,0,out error)&&session.Purchases().Count==1,"손님 선택품도 상자로 회수 및 계산 목록 제외");
            var last=session.Purchases()[0];Check(session.TryReturnRecommendation(last.instanceId,"display_preview",0,0,0,out error)&&session.Purchases().Count==0,"손님 선택품을 다른 진열 칸으로 돌려놓기");
            Check(model.TryGetOfferPrice("display_preview",last.instanceId,out price)&&price==80&&model.Gold==100,"진열 등록가 보존 및 회수 금액 부과 없음");
            revision=model.Revision;Check(!session.TryReturnRecommendation(last.instanceId,"warehouse",4,0,0,out error)&&model.Revision==revision,"회수한 UID 중복 수령 금지");
        }
    }
}
