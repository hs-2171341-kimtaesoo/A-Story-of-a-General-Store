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
            var save=new InventorySave{gold=gold};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=5});save.containers.Add(new ContainerRecord{containerId="display_preview",columns=8,rows=5});
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
    }
}
