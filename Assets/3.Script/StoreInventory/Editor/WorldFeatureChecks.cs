using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory.Verification
{
    public static class WorldFeatureChecks
    {
        static int count;
        static void Check(bool value,string rule){count++;if(!value)throw new Exception("월드 검사 실패: "+rule);}
        static InventoryModel Model(long gold=1000)
        {
            var save=new InventorySave{gold=gold};
            save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=8});
            save.containers.Add(new ContainerRecord{containerId="display_1",columns=6,rows=4});
            save.containers.Add(new ContainerRecord{containerId="display_2",columns=6,rows=4});
            return new InventoryModel(InventoryModelChecks.Fixture(),save);
        }
        static MoveRequest Move(InventoryModel m,string src,string uid,string dst,int x=0,int y=0,int rotation=0)
        {return new MoveRequest{sourceContainer=src,targetContainer=dst,instanceId=uid,x=x,y=y,rotation=rotation,expectedRevision=m.Revision};}
        static CustomerDefinition Customer()
        {return new CustomerDefinition{id="tester",displayName="테스트",wallet=400,spendingFraction=.5,preferredItemIds=new[]{"C001"}};}
        sealed class HalfBudgetPolicy : ICustomerPurchasePolicy
        {
            public long SpendingBudget(CustomerDefinition c){return 100;}
            public bool AcceptItem(CustomerDefinition c,ItemRecord item,int price,int reference){return price<=100;}
        }
        public static int Run()
        {
            count=0;string error;long revenue;
            var model=Model();model.TryAdd("warehouse","C001","common",1,80,out error);
            var item=model.GetContainer("warehouse").items[0];string uid=item.instanceId;
            long revision=model.Revision;
            Check(!model.TryDropToFloor("warehouse",uid,0,.5f,.5f,revision-1,out error),"오래된 바닥 드롭 거절");
            Check(!model.TryDropToFloor("warehouse",uid,0,float.NaN,.5f,revision,out error),"비정상 좌표 거절");
            Check(!model.TryDropToFloor("warehouse",uid,0,1.1f,.5f,revision,out error),"바닥 범위 초과 거절");
            Check(model.TryListOnDisplay(Move(model,"warehouse",uid,"display_1"),100,out error),"진열 준비");
            Check(model.TryDropToFloor("display_1",uid,1,.2f,.3f,model.Revision,out error),"진열에서 바닥으로 이동");
            Check(model.Snapshot().listings.Count==0&&model.GetContainer("display_1").items.Count==0,"바닥 이동 시 진열/가격 해제");
            Check(model.FloorItems().Count==1&&model.GetItem("floor",uid).purchasePrice==80,"바닥에서 동일 uid/매입가 보존");
            Check(model.TryDropToFloor("floor",uid,2,.7f,.6f,model.Revision,out error)&&model.FloorItems().Count==1,"바닥 재배치 복제 없음");
            var save=model.Snapshot();var restored=Model();
            Check(restored.TryRestore(save,out error)&&restored.FloorItems()[0].normalizedX==.7f,"바닥 위치 저장 복원");
            var leaked=restored.FloorItems();leaked[0].item.purchasePrice=999;leaked.Clear();
            Check(restored.GetItem("floor",uid).purchasePrice==80,"바닥 조회 복사본 보호");
            var bad=save.Copy();bad.floorItems.Add(bad.floorItems[0].Copy());
            Check(!restored.TryRestore(bad,out error),"바닥 uid 중복 저장 거절");
            bad=save.Copy();bad.floorItems[0].normalizedY=float.PositiveInfinity;
            Check(!restored.TryRestore(bad,out error),"비정상 바닥 저장 거절");
            Check(model.TryMove(Move(model,"floor",uid,"warehouse"),out error)&&model.FloorItems().Count==0,"바닥에서 창고 회수");
            // 수백 번 바닥/격자 사이를 왕복하며 UID/품질/매입가와 총 개수를 확인합니다.
            var random=new Random(482);
            for(int n=0;n<400;n++)
            {
                int rotation=n%4;
                Check(model.TryDropToFloor("warehouse",uid,rotation,(float)random.NextDouble(),(float)random.NextDouble(),model.Revision,out error),"왕복 내려놓기");
                Check(model.GetContainer("warehouse").items.Count==0&&model.FloorItems().Count==1,"바닥/창고 한쪽에만 존재");
                Check(model.TryMove(Move(model,"floor",uid,"warehouse",0,0,rotation),out error),"왕복 회수");
                var actual=model.GetItem("warehouse",uid);
                Check(actual!=null&&actual.itemId==item.itemId&&actual.quality==item.quality&&actual.purchasePrice==80&&actual.rotation==rotation,"왕복 낱개 정보 보존");
            }
            model.TryDropToFloor("warehouse",uid,0,.5f,.5f,model.Revision,out error);
            Check(model.TryRemove("floor",uid,out error)&&model.GetItem("floor",uid)==null,"도난은 정확한 바닥 물건만 제거");

            model=Model();model.TryAdd("warehouse","C001","common",16,80,out error);
            uid=model.GetContainer("warehouse").items[0].instanceId;
            model.TryDropToFloor("warehouse",uid,0,.1f,.1f,model.Revision,out error);
            model.TryAdd("warehouse","C001","common",1,80,out error);
            Check(!model.TryMove(Move(model,"floor",uid,"warehouse"),out error),"창고가 가득 차면 바닥 회수 실패");
            Check(model.GetItem("floor",uid)!=null&&model.GetContainer("warehouse").items.Count==16,"회수 실패 시 양쪽 원본 유지");
            Check(!model.TryListOnDisplay(Move(model,"floor",uid,"display_1"),-1,out error)&&model.GetItem("floor",uid)!=null,"바닥 출발 진열가 실패 시 원본 유지");
            Check(model.TryListOnDisplay(Move(model,"floor",uid,"display_1"),125,out error)&&model.FloorItems().Count==0,"바닥에서 가격 등록하며 진열");
            int groundQuote;Check(model.TryGetOfferPrice("display_1",uid,out groundQuote)&&groundQuote==125,"바닥 출발 새 진열 가격 적용");

            model=Model();model.TryAdd("warehouse","C001","common",3,80,out error);
            var items=model.GetContainer("warehouse").items;
            model.TryListOnDisplay(Move(model,"warehouse",items[0].instanceId,"display_1"),100,out error);
            var session=new RecommendationSession(model);var profile=Customer();
            Check(new DefaultCustomerPurchasePolicy().SpendingBudget(profile)==200,"소지금의 50퍼센트만 예산");
            Check(session.TryEnterShopping(profile,out error)&&!session.ConversationStarted,"처음 방문은 가져온 물건 화면");
            uid=session.Purchases()[0].instanceId;
            Check(!model.TryDropToFloor("display_1",uid,0,.5f,.5f,model.Revision,out error),"예약한 진열 물건 바닥 이동 차단");
            Check(!session.TryRecommend("warehouse",items[1].instanceId,out error),"이야기 전 추천 직접 API 차단");
            Check(session.BeginConversation(out error)&&session.ConversationStarted,"이야기하기 단계 전환");
            Check(session.TryRecommend("warehouse",items[1].instanceId,out error),"이야기 후 추가 추천");
            Check(session.TryRecommend("warehouse",items[2].instanceId,out error),"추가 추천 두 개");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==1000,"소지금 전체가 아닌 사용 비율 예산 적용");
            Check(session.TrySubmitPurchasesOnly(out revenue,out error)&&revenue==100&&model.Gold==1100,"가져온 물건만 판매 경로");
            Check(model.GetContainer("warehouse").items.Count==2&&!session.CustomerPresent&&!session.ConversationStarted,"추천 제외 결제 후 상태 정리");
            Check(!session.TrySubmitPurchasesOnly(out revenue,out error)&&model.Gold==1100,"기본 판매 연타 중복 지급 차단");
            model.TryListOnDisplay(Move(model,"warehouse",items[1].instanceId,"display_1"),120,out error);
            session.SetPurchasePolicy(new HalfBudgetPolicy());
            Check(!session.TryEnterShopping(Customer(),out error),"교체한 판단 정책으로 고가 선택 거절");
            model.TryListOnDisplay(Move(model,"display_1",items[1].instanceId,"display_1"),100,out error);
            Check(session.TryEnterShopping(Customer(),out error),"교체한 정책으로 허용 상품 방문");
            bool threw=false;try{session.SetPurchasePolicy(new DefaultCustomerPurchasePolicy());}catch(InvalidOperationException){threw=true;}
            Check(threw,"진행 중 손님의 구매 정책 변경 차단");session.Leave();
            profile=Customer();profile.spendingFraction=1.1;
            Check(!session.TryEnterShopping(profile,out error),"100퍼센트 초과 사용 비율 거절");
            profile=Customer();profile.enabled=false;
            Check(!session.TryEnterShopping(profile,out error),"비활성 손님 직접 방문 차단");
            var profiles=new List<CustomerDefinition>();
            for(int n=0;n<24;n++){var c=Customer();c.id="customer_"+n;profiles.Add(c);}
            var catalog=new CustomerCatalog(new CustomerDatabase{customers=profiles.ToArray()});
            Check(catalog.All().Count==24,"24명 데이터만으로 등록");
            var copied=catalog.All();copied[0].preferredItemIds[0]="changed";
            Check(catalog.All()[0].preferredItemIds[0]=="C001","손님 데이터 외부 수정 차단");
            profiles[1].id=profiles[0].id;threw=false;
            try{new CustomerCatalog(new CustomerDatabase{customers=profiles.ToArray()});}catch(ArgumentException){threw=true;}
            Check(threw,"중복 캐릭터 id 거절");

            model=Model(200);
            Check(model.TryOrderNextDawn(1,"C001","common",2,80,out error)&&model.Gold==40,"주문 시 매입비 한 번 차감");
            var order=model.Snapshot().deliveries[0];uid=order.items[0].instanceId;
            Check(order.arrivalDay==2&&order.items.Count==2&&model.FloorItems().Count==0,"당일 도착 금지 및 배송 중 낱개 ID 생성");
            Check(!model.TryOrderNextDawn(1,"C001","common",1,80,out error)&&model.Gold==40,"잔액 부족 주문 전체 거절");
            restored=Model();Check(restored.TryRestore(model.Snapshot(),out error)&&restored.Snapshot().deliveries[0].items[0].instanceId==uid,"배송 예약과 UID 저장 복원");
            long paid;int delivered;
            Check(model.TryBeginDawn(1,60,out paid,out delivered,out error)&&paid==0&&delivered==0,"현재 날짜 새벽 중복 정산 없음");
            Check(model.TryBeginDawn(2,60,out paid,out delivered,out error)&&paid==40&&delivered==2,"다음 새벽 비용과 배송 함께 확정");
            Check(model.Gold==0&&model.UnpaidCosts==20&&model.Parcels()[0].items[0].instanceId==uid&&model.FloorItems().Count==0&&model.Snapshot().deliveries.Count==0,"음수 잔액 대신 미납 및 상자 속 배송 UID 유지");
            Check(model.TryBeginDawn(2,60,out paid,out delivered,out error)&&paid==0&&delivered==0&&model.UnpaidCosts==20,"새벽 처리 재호출 이중 정산/배송 차단");
            Check(model.TryOpenParcel(model.Parcels()[0].parcelId,.2f,.5f,out error),"도착 상자 개봉 후 바닥으로 이동");
            bad=model.Snapshot();bad.deliveries.Add(new DeliveryOrder{orderId="dup",arrivalDay=3,items=new List<ItemRecord>{bad.floorItems[0].item.Copy()}});
            Check(!restored.TryRestore(bad,out error),"바닥과 배송 중 물건 중복 거절");
            model=Model();bad=model.Snapshot();bad.unpaidCosts=long.MaxValue;
            Check(model.TryRestore(bad,out error)&&model.TryBeginDawn(2,60,out paid,out delivered,out error)&&model.UnpaidCosts==long.MaxValue,"전액 지불 가능한 경우 기존 최대 미납액 보존");
            Check(!model.TryBeginDawn(3,long.MaxValue,out paid,out delivered,out error)&&model.Gold==940,"미납 오버플로 시 정산 전체 취소");

            var settings=new ShopClockSettings();var clock=new ShopClock(settings,new ShopClockState());revision=model.Revision;
            Check(clock.Advance(539).Count==0&&clock.Snapshot().phase==ShopPhase.Day,"낮 판매 시간 경계 전");
            Check(model.Revision==revision,"시계 흐름은 재고 버전을 바꾸지 않음");
            var transitions=clock.Advance(1);Check(transitions.Count==1&&transitions[0].phase==ShopPhase.Night&&clock.Snapshot().day==1,"낮 종료 후 밤 시작");
            transitions=clock.Advance(720);Check(transitions.Count==1&&clock.Snapshot().phase==ShopPhase.Dawn&&clock.Snapshot().day==2,"밤 종료 후 다음 날 새벽");
            transitions=clock.Advance(180);Check(transitions.Count==1&&clock.Snapshot().phase==ShopPhase.Day,"새벽 종료 후 낮");
            var old=clock.Snapshot();clock.Advance(42);clock.Restore(old);
            Check(clock.Snapshot().elapsedMinutes==0&&clock.Snapshot().day==2,"시계 저장 상태 복원");
            transitions=clock.Advance(1440);Check(transitions.Count==3&&clock.Snapshot().day==3&&clock.Snapshot().phase==ShopPhase.Day,"여러 단계 건너뛰기 순서");
            threw=false;try{clock.Advance(double.NaN);}catch(ArgumentException){threw=true;}
            Check(threw&&clock.Snapshot().day==3,"비정상 시간 거절 및 상태 유지");
            var wrong=clock.Snapshot();wrong.elapsedMinutes=540;threw=false;
            try{clock.Restore(wrong);}catch(ArgumentException){threw=true;}
            Check(threw&&clock.Snapshot().elapsedMinutes==0,"시간 범위가 잘못된 저장 거절");
            return count;
        }
    }
}
