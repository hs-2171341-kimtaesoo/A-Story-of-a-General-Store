#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;

// ShopFeatureChecks.cs
namespace GeneralStore.Inventory.Verification
{
    // 판매/돈/개별 물건 보존과, UI 메시의 순수 좌표 계산을 검증합니다.
    public static class ShopFeatureChecks
    {
        static int count;
        static void Check(bool value,string rule){count++;if(!value)throw new Exception("상점 검사 실패: "+rule);}
        static InventoryModel Model(long gold=100)
        {
            var save=new InventorySave { gold=gold };
            save.containers.Add(new ContainerRecord{containerId="test_storage",columns=8,rows=8});
            save.containers.Add(new ContainerRecord{containerId="display_1",columns=6,rows=4});
            save.containers.Add(new ContainerRecord{containerId="display_2",columns=6,rows=4});
            return new InventoryModel(InventoryModelChecks.Fixture(),save);
        }
        static MoveRequest Place(InventoryModel model,string source,string uid,int x=0,int y=0)
        {return new MoveRequest{sourceContainer=source,targetContainer="display_1",instanceId=uid,x=x,y=y,rotation=0,expectedRevision=model.Revision};}
        public static int Run()
        {
            count=0;string error;long revenue;
            InventoryModel model=Model();var session=new RecommendationSession(model);
            Check(!session.TryEnter("손님",1000,1.5,true,out error),"진열 없으면 방문 거절");
            Check(model.TryAdd("test_storage","C001","common",2,80,out error),"물건 두 개 준비");
            var items=model.GetContainer("test_storage").items;string first=items[0].instanceId,second=items[1].instanceId;
            Check(model.TryListOnDisplay(Place(model,"test_storage",first),150,out error),"진열과 가격을 함께 확정");
            int quote;Check(model.TryGetOfferPrice("display_1",first,out quote)&&quote==150,"진열가 조회");
            Check(session.TryEnter("손님",1000,1.5,true,out error),"진열 후 방문");
            Check(session.TryRecommend("display_1",first,out error),"진열 물건 추천");
            Check(!session.TryRecommend("display_1",first,out error),"동일 물건 중복 추천 차단");
            Check(session.TryRecommend("test_storage",second,out error),"창고 물건 추천");
            Check(session.Offers()[1].price==100,"창고 추천은 읽기 전용 기준가");
            long before=model.Revision;
            Check(session.TrySubmit(out revenue,out error)&&revenue==250,"손님 수락과 판매");
            Check(model.Gold==350&&model.Revision==before+1,"돈과 재고를 한 번만 확정");
            Check(model.GetItem("display_1",first)==null&&model.GetItem("test_storage",second)==null,"정확한 두 uid만 차감");
            Check(model.Snapshot().listings.Count==0&&!session.CustomerPresent&&session.Offers().Count==0,"판매 후 진열/손님/목록 정리");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==350,"버튼 연타 이중 지급 차단");

            model=Model();session=new RecommendationSession(model);model.TryAdd("test_storage","C001","common",1,80,out error);
            first=model.GetContainer("test_storage").items[0].instanceId;
            Check(model.TryListOnDisplay(Place(model,"test_storage",first),300,out error),"고가 진열");
            session.TryEnter("손님",1000,1.5,true,out error);session.TryRecommend("display_1",first,out error);
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==100&&model.GetItem("display_1",first)!=null,"가격 거절 시 유지");
            var moveBack=new MoveRequest{sourceContainer="display_1",targetContainer="test_storage",instanceId=first,x=0,y=0,rotation=0,expectedRevision=model.Revision};
            Check(model.TryMove(moveBack,out error)&&model.Snapshot().listings.Count==0,"창고 회수 시 진열 가격 해제");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==100,"이동한 추천 물건 판매 거절");
            Check(model.TryGetOfferPrice("test_storage",first,out quote)&&quote==100,"창고 회수 후 기준가 조회");
            Check(!model.TryListOnDisplay(Place(model,"test_storage",first),-1,out error),"음수 진열가 차단");
            Check(model.GetItem("test_storage",first)!=null,"진열 실패 시 원래 물건 유지");

            model=Model();model.TryAdd("test_storage","C001","common",2,80,out error);items=model.GetContainer("test_storage").items;
            first=items[0].instanceId;second=items[1].instanceId;model.TryListOnDisplay(Place(model,"test_storage",first),100,out error);
            session=new RecommendationSession(model);session.TryEnter("손님",150,2,true,out error);
            session.TryRecommend("display_1",first,out error);session.TryRecommend("test_storage",second,out error);
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==100,"예산 초과 시 판매 거절");
            session.Remove(second);Check(session.TrySubmit(out revenue,out error)&&model.Gold==200,"목록 수정 후 판매 가능");
            Check(model.GetItem("test_storage",second)!=null,"목록에서 뺀 다른 물건은 유지");
            var snapshot=model.Snapshot();var restored=Model(0);
            Check(restored.TryRestore(snapshot,out error)&&restored.Gold==200,"돈과 재고 함께 저장 복원");

            var badSave=restored.Snapshot();badSave.gold=-1;
            Check(!restored.TryRestore(badSave,out error)&&restored.Gold==200,"음수 돈 저장 거절");
            badSave=restored.Snapshot();badSave.listings.Add(new DisplayListing{instanceId="missing",price=100});
            Check(!restored.TryRestore(badSave,out error),"없는 물건 진열 등록 거절");
            model=Model();model.TryAdd("test_storage","C001","common",1,80,out error);first=model.GetContainer("test_storage").items[0].instanceId;
            model.TryListOnDisplay(Place(model,"test_storage",first),100,out error);session=new RecommendationSession(model);
            session.TryEnter("손님",1000,1.5,true,out error);session.TryRecommend("display_1",first,out error);
            Check(model.TryListOnDisplay(Place(model,"display_1",first),110,out error),"진열 다시 확인으로 가격 변경");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==100,"추천 후 가격 변경도 판매 거절");
            session.Remove(first);session.TryRecommend("display_1",first,out error);
            Check(session.TrySubmit(out revenue,out error)&&model.Gold==210,"새 가격으로 재추천 후 판매 가능");
            model=Model(long.MaxValue-50);model.TryAdd("test_storage","C001","common",1,80,out error);first=model.GetContainer("test_storage").items[0].instanceId;
            Check(!model.TrySell(new[]{new SaleLine{containerId="test_storage",instanceId=first,price=100}},model.Revision,out revenue,out error),"돈 범위 초과 거절");
            Check(model.Gold==long.MaxValue-50&&model.GetItem("test_storage",first)!=null,"금액 실패 시 재고도 유지");
            model=Model();model.TryAdd("test_storage","C001","common",1,80,out error);first=model.GetContainer("test_storage").items[0].instanceId;
            var line=new SaleLine{containerId="test_storage",instanceId=first,price=100};
            Check(!model.TrySell(new[]{line,line},model.Revision,out revenue,out error)&&model.GetItem("test_storage",first)!=null,"같은 uid 중복 판매 차단");
            Check(!model.TrySell(new[]{new SaleLine{containerId="test_storage",instanceId=first,price=999}},model.Revision,out revenue,out error),"임의 가격 변조 차단");

            // 예약은 물건을 옮기지 않습니다. 같은 품목이어도 UID별로 선택/미선택을 구분합니다.
            model=Model();session=new RecommendationSession(model);
            Check(model.TryAdd("test_storage","C001","common",8,80,out error),"예약 검사 재고 준비");
            items=model.GetContainer("test_storage").items;
            for(int n=0;n<6;n++) Check(model.TryListOnDisplay(Place(model,"test_storage",items[n].instanceId,(n%3)*2,(n/3)*2),100,out error),"여섯 낱개 진열");
            Check(!session.TryEnterShopping("손님",1000,1.5,new[]{"missing"},5,out error)&&!session.CustomerPresent,"선호 품목 없으면 방문 종료");
            Check(!session.TryEnterShopping("손님",99,1.5,new[]{"C001"},5,out error),"예산 이하 후보 없으면 방문 종료");
            Check(session.TryEnterShopping("손님",1000,1.5,new[]{"C001","C001"},5,out error),"선호 진열 상품 선택");
            Check(session.Purchases().Count==5&&session.Offers().Count==0,"최대 다섯 낱개 선택 및 선호 목록 중복 배제");
            first=session.Purchases()[0].instanceId;second=items[5].instanceId;
            Check(model.GetContainer("display_1").items.Count==6&&model.IsReserved(first)&&!model.IsReserved(second),"진열 위치 유지 및 선택한 UID만 잠금");
            session.BeginConversation(out error);
            Check(!session.TryRecommend("display_1",first,out error),"구매 예정 물건 재추천 차단");
            Check(!model.TryRemove("display_1",first,out error),"예약 상품 일반 출고 차단");
            var blocked=new MoveRequest{sourceContainer="display_1",targetContainer="test_storage",instanceId=first,x=0,y=0,expectedRevision=model.Revision};
            Check(!model.Preview(blocked).allowed&&!model.TryMove(blocked,out error),"미리보기와 이동 모두 예약 차단");
            Check(!model.TryListOnDisplay(Place(model,"display_1",first),120,out error),"예약 상품 재가격 설정 차단");
            Check(!model.TrySell(new[]{new SaleLine{containerId="display_1",instanceId=first,price=100}},model.Revision,out revenue,out error),"일반 판매 API로 예약 우회 차단");
            session.Remove(first);Check(session.Purchases().Count==5&&model.IsReserved(first),"추천 제거로 구매 예정 상품을 해제하지 않음");
            Check(session.TryRecommend("display_1",second,out error),"같은 품목의 미선택 UID는 추천 가능");
            Check(session.TrySubmit(out revenue,out error)&&revenue==600&&model.Gold==700,"구매 다섯 개와 추천 한 개를 함께 한 번만 결제");
            Check(model.GetContainer("display_1").items.Count==0&&!model.IsReserved(first)&&model.GetContainer("test_storage").items.Count==2,"판매 물건만 제거 및 예약 정리");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==700,"예약 결제 연타 방지");
            Check(model.TryListOnDisplay(Place(model,"test_storage",items[6].instanceId),100,out error),"다음 손님용 진열");
            Check(session.TryEnterShopping("손님",1000,1.5,new[]{"C001"},1,out error),"한 개 선택 설정");
            first=session.Purchases()[0].instanceId;
            long savedGold=model.Gold;session.Leave();
            Check(!model.IsReserved(first)&&model.GetItem("display_1",first)!=null&&model.Gold==savedGold,"거래 거절 시 위치와 돈 유지하며 잠금만 해제");
            Check(session.TryEnterShopping("손님",1000,1.5,new[]{"C001"},1,out error)&&session.TrySubmit(out revenue,out error)&&revenue==100,"추천 없이 구매 예정 물건만 계산 가능");
            Check(model.TryListOnDisplay(Place(model,"test_storage",items[7].instanceId),100,out error),"불러오기 검사 진열");
            session.TryEnterShopping("손님",100,1.5,new[]{"C001"},1,out error);
            first=session.Purchases()[0].instanceId;
            var reservedSnapshot=model.Snapshot();
            var isolated=Model();Check(isolated.TryRestore(reservedSnapshot,out error)&&!isolated.IsReserved(first),"예약은 저장 파일에 포함되지 않음");
            Check(model.TryAdd("test_storage","C001","common",1,80,out error),"예산 초과 추천 준비");
            string extra=model.GetContainer("test_storage").items[0].instanceId;
            session.BeginConversation(out error);
            Check(session.TryRecommend("test_storage",extra,out error)&&!session.TrySubmit(out revenue,out error),"구매 예정 포함 총예산으로 추천 거절");
            Check(model.IsReserved(first)&&model.GetItem("display_1",first)!=null,"추천 거절 시 기존 구매 예정은 유지");
            session.Remove(extra);
            Check(session.TrySubmit(out revenue,out error)&&revenue==100,"거절된 추천 제거 후 기존 구매 결제");

            var catalog=InventoryModelChecks.Fixture();var random=new Random(81);
            foreach(string id in new[]{"C001","sample_rope","sample_lframe","sample_cframe"})
                for(int rotation=0;rotation<4;rotation++)
                {
                    ItemFootprint shape=catalog.Shape(id);var placeholder=IconMeshGeometry.Build(shape,rotation,false,1);
                    double area=0;foreach(IconQuad quad in placeholder)area+=(quad.x1-quad.x0)*(quad.y1-quad.y0);
                    Check(placeholder.Count==1&&Math.Abs(area-shape.CellCount)<.0001,"단일 사각형 메시 면적 보존");
                    for(int attempt=0;attempt<25;attempt++)
                    {
                        float aspect=(float)(.2+random.NextDouble()*4);
                        foreach(IconQuad quad in IconMeshGeometry.Build(shape,rotation,true,aspect))
                        {
                            Check(shape.Contains(rotation,(int)Math.Floor((quad.x0+quad.x1)*.5f),(int)Math.Floor((quad.y0+quad.y1)*.5f)),"메시가 물품 사각형 내부에 있음");
                            foreach(IconPoint uv in new[]{quad.topLeft,quad.topRight,quad.bottomRight,quad.bottomLeft})
                                Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"회전/비율 변경 후 UV 범위");
                        }
                    }
                }
            return count;
        }
    }
}

// CounterCheckoutChecks.cs
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
            CheckReservationVersions();
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
        static void CheckReservationVersions()
        {
            var model=Model();var session=Enter(model);var item=model.GetContainer("warehouse").items[0];string error;long before=model.ContainerVersion("warehouse");
            Check(session.TryRecommend("warehouse",item.instanceId,out error)&&model.ContainerVersion("warehouse")!=before,"추천 잠금이 원본 격자 캐시를 무효화");
            before=model.ContainerVersion("warehouse");Check(session.TryReturnRecommendation(item.instanceId,"warehouse",item.x,item.y,item.rotation,out error)&&model.ContainerVersion("warehouse")!=before,"원래 자리로 회수해도 예약 표시 해제");
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

// DeliveryAndCustomerChecks.cs
// Consolidated from DeliveryFeatureChecks.cs
namespace GeneralStore.Inventory.Verification
{
    public static class DeliveryFeatureChecks
    {
        static int count;
        static void Check(bool value,string note){count++;if(!value)throw new Exception("택배 검사 실패: "+note);}
        static InventoryModel Model(long gold=10000)
        {
            var save=new InventorySave{gold=gold};
            save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});
            save.containers.Add(new ContainerRecord{containerId="display_1",columns=8,rows=5});
            return new InventoryModel(InventoryModelChecks.Fixture(),save);
        }
        static MoveRequest Withdraw(InventoryModel model,string uid,int x=0,int y=0)
        {return new MoveRequest{sourceContainer=InventoryModel.DeliveryContainerId,targetContainer="warehouse",instanceId=uid,x=x,y=y,expectedRevision=model.Revision};}
        public static int Run()
        {
            count=0;string error;DawnSettlement bill;var model=Model();
            Check(model.TryOrderNextDawn(1,"sample_lframe","common",1,30,out error),"직사각형 물품 주문");
            string uid=model.Snapshot().deliveries[0].items[0].instanceId;
            Check(model.TryBeginDawn(2,0,10,out bill,out error)&&bill.storageCost==0&&bill.delivered==1,"도착 당일 부과 없음");
            Check(model.DeliveryOccupiedCells==9,"3x3 물품은 전체9칸");
            Check(model.TryOrderNextDawn(2,"C001","common",2,20,out error),"기존 잔량 위에 후속 주문");
            long before=model.Gold;
            Check(model.TryBeginDawn(3,60,10,out bill,out error)&&bill.occupiedCells==9&&bill.storageCost==90&&bill.paid==150&&bill.delivered==2,"기존 잔량만 계산 후 새 배송 추가");
            Check(model.Gold==before-150&&model.DeliveryItemCount==3&&model.DeliveryOccupiedCells==17,"금액/누적 낱개 보존");
            long revision=model.Revision;
            Check(model.TryBeginDawn(3,60,10,out bill,out error)&&bill.alreadySettled&&model.Revision==revision,"같은 새벽 이중 과금/배송 없음");
            Check(model.TryMove(Withdraw(model,uid),out error)&&model.GetItem("warehouse",uid).purchasePrice==30&&model.DeliveryOccupiedCells==8,"상자 회수 시 UID/매입가 유지 및 보관 칸 감소");
            before=model.Gold;
            Check(model.TryBeginDawn(4,0,10,out bill,out error)&&bill.storageCost==80&&model.Gold==before-80,"남은 물품만 다음 날 과금");
            var copy=model.GetContainer(InventoryModel.DeliveryContainerId);copy.items.Clear();
            Check(model.DeliveryItemCount==2,"UI 조회 복사본으로 원본 변경 불가");
            var restored=Model();Check(restored.TryRestore(model.Snapshot(),out error)&&restored.DeliveryOccupiedCells==8,"v10 배송 잔량 저장 복원");
            Check(restored.TryBeginDawn(4,0,10,out bill,out error)&&bill.alreadySettled,"로드 후 같은 새벽 재부과 없음");
            var back=Withdraw(model,uid);back.sourceContainer="warehouse";back.targetContainer=InventoryModel.DeliveryContainerId;
            Check(!model.TryMove(back,out error)&&model.GetItem("warehouse",uid)!=null,"보관료 회피용 역입고 금지");
            string parcelUid=model.GetContainer(InventoryModel.DeliveryContainerId).items[0].instanceId;
            var direct=Withdraw(model,parcelUid);direct.targetContainer="display_1";
            Check(model.TryListOnDisplay(direct,100,out error)&&model.GetItem("display_1",parcelUid)!=null,"택배에서 기존 판매가 등록으로 직접 진열 가능");
            parcelUid=model.GetContainer(InventoryModel.DeliveryContainerId).items[0].instanceId;
            Check(!model.TryMove(Withdraw(model,parcelUid,0,0),out error)&&model.GetItem(InventoryModel.DeliveryContainerId,parcelUid)!=null,"겹치는 상자 칸으로 이동 실패 시 택배 보존");
            var stale=Withdraw(model,parcelUid,5,0);stale.expectedRevision--;
            Check(!model.TryMove(stale,out error),"오래된 드래그 거절");
            var session=new RecommendationSession(model);var customer=new CustomerDefinition{id="delivery-counter",displayName="택배 추천",wallet=1000,spendingFraction=1,preferredItemIds=new[]{"C001"}};
            Check(session.TryEnterShopping(customer,out error)&&session.PresentOnCounter(out error)&&session.BeginConversation(out error)&&session.TryRecommend(InventoryModel.DeliveryContainerId,parcelUid,out error),"택배 물건도 카운터 추가 추천 가능");
            Check(model.TryBeginDawn(5,0,10,out bill,out error)&&bill.storageCost==40&&model.IsReserved(parcelUid),"거래 대기 중 물품도 실제 판매 전까지 남은 칸으로 과금");session.Leave();
            model=Model(0);Check(model.TryOrderNextDawn(1,"C001","common",1,0,out error)&&model.TryBeginDawn(2,0,10,out bill,out error),"무료 테스트 배송");
            Check(model.TryBeginDawn(3,0,10,out bill,out error)&&bill.storageCost==40&&bill.paid==0&&bill.addedDebt==40&&model.Gold==0&&model.UnpaidCosts==40,"잔액 부족은 음수 대신 미납");
            revision=model.Revision;
            Check(!model.TryBeginDawn(4,0,long.MaxValue,out bill,out error)&&model.Revision==revision&&model.Snapshot().lastSettledDay==3,"요금 오버플로 시 재고/날짜/돈 전체 유지");
            Check(model.TryBeginDawn(4,0,0,out bill,out error)&&bill.storageCost==0,"요율0 설정 지원");
            // Old floor and unopened parcels are migration-only DTOs, never live floor objects.
            var old=Model().Snapshot();old.version=7;old.containers.RemoveAll(c=>c.containerId==InventoryModel.DeliveryContainerId);
            old.floorItems.Add(new FloorItemRecord{item=new ItemRecord{instanceId="legacy-floor",itemId="C001",quality="common",purchasePrice=17}});
            old.parcels.Add(new ParcelRecord{parcelId="legacy-box",items=new List<ItemRecord>{new ItemRecord{instanceId="legacy-parcel",itemId="E001",quality="none",purchasePrice=19}}});
            restored=Model();Check(restored.TryRestore(old,out error)&&restored.TemporaryItemCount==2,"v7 바닥/택배를 새 보관함으로 이동");
            Check(restored.GetItem(InventoryModel.TemporaryStorageId,"legacy-floor").purchasePrice==17&&restored.Snapshot().floorItems.Count==0&&restored.Snapshot().parcels.Count==0,"마이그레이션 UID/비용 유지 및 구형 목록 비움");
            old.parcels[0].items.Add(old.floorItems[0].item.Copy());revision=restored.Revision;
            Check(!restored.TryRestore(old,out error)&&restored.Revision==revision&&restored.TemporaryItemCount==2,"중복 UID 구형 저장은 원자적으로 거절");
            // Beyond the old 4096-item/64-row limits. No item/row game cap is applied.
            model=Model(0);
            for(int order=0;order<50;order++)Check(model.TryOrderNextDawn(1,"C001","common",100,0,out error),"연속 주문");
            Check(model.TryBeginDawn(2,0,10,out bill,out error)&&bill.delivered==5000&&model.DeliveryItemCount==5000,"5000개 누적 입고");
            var container=model.GetContainer(InventoryModel.DeliveryContainerId);Check(container.columns==16&&container.rows>64,"8열 고정·행 자동 증가");
            var occupied=new HashSet<long>();var ids=new HashSet<string>();
            foreach(var item in container.items)
            {
                Check(ids.Add(item.instanceId),"대량 배송 UID 중복 없음");
                foreach(var cell in model.Cells(item))Check(item.x+cell.x<16&&occupied.Add((long)(item.y+cell.y)*16+item.x+cell.x),"대량 점유 겹침 없음");
            }
            restored=Model();Check(restored.TryRestore(model.Snapshot(),out error)&&restored.DeliveryItemCount==5000,"대량 저장 재검증");
            return count;
        }
    }
}

// Consolidated from WorldFeatureChecks.cs
namespace GeneralStore.Inventory.Verification
{
    public static class WorldFeatureChecks
    {
        static int count;
        static void Check(bool value,string rule){count++;if(!value)throw new Exception("월드 검사 실패: "+rule);}
        static InventoryModel Model(long gold=1000)
        {
            var save=new InventorySave{gold=gold};
            save.containers.Add(new ContainerRecord{containerId="test_storage",columns=8,rows=8});
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
            var model=Model();long revision;bool threw;
            model.TryAdd("test_storage","C001","common",3,80,out error);
            var items=model.GetContainer("test_storage").items;
            model.TryListOnDisplay(Move(model,"test_storage",items[0].instanceId,"display_1"),100,out error);
            var session=new RecommendationSession(model);var profile=Customer();
            Check(new DefaultCustomerPurchasePolicy().SpendingBudget(profile)==200,"소지금의 50퍼센트만 예산");
            Check(session.TryEnterShopping(profile,out error)&&!session.ConversationStarted,"처음 방문은 가져온 물건 화면");
            Check(session.TryRecommend("test_storage",items[1].instanceId,out error),"대화 전에도 추천 가능");session.Remove(items[1].instanceId);
            Check(session.BeginConversation(out error)&&session.ConversationStarted,"이야기하기 단계 전환");
            Check(session.TryRecommend("test_storage",items[1].instanceId,out error),"이야기 후 추가 추천");
            Check(session.TryRecommend("test_storage",items[2].instanceId,out error),"추가 추천 두 개");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==1000,"소지금 전체가 아닌 사용 비율 예산 적용");
            Check(session.TrySubmitPurchasesOnly(out revenue,out error)&&revenue==100&&model.Gold==1100,"가져온 물건만 판매 경로");
            Check(model.GetContainer("test_storage").items.Count==2&&!session.CustomerPresent&&!session.ConversationStarted,"추천 제외 결제 후 상태 정리");
            Check(!session.TrySubmitPurchasesOnly(out revenue,out error)&&model.Gold==1100,"기본 판매 연타 중복 지급 차단");
            model.TryListOnDisplay(Move(model,"test_storage",items[1].instanceId,"display_1"),120,out error);
            session.SetPurchasePolicy(new HalfBudgetPolicy());
            Check(!session.TryEnterShopping(Customer(),out error),"교체한 판단 정책으로 고가 선택 거절");
            model.TryListOnDisplay(Move(model,"display_1",items[1].instanceId,"display_1"),100,out error);
            Check(session.TryEnterShopping(Customer(),out error),"교체한 정책으로 허용 상품 방문");
            threw=false;try{session.SetPurchasePolicy(new DefaultCustomerPurchasePolicy());}catch(InvalidOperationException){threw=true;}
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

            var settings=new ShopClockSettings();var clock=new ShopClock(settings,new ShopClockState());revision=model.Revision;
            Check(clock.Advance(539).Count==0&&clock.Snapshot().phase==ShopPhase.Day,"낮 판매 시간 경계 전");
            Check(model.Revision==revision,"시계 흐름은 재고 버전을 바꾸지 않음");
            var transitions=clock.Advance(1);Check(transitions.Count==1&&transitions[0].phase==ShopPhase.Night&&clock.Snapshot().day==1,"낮 종료 후 밤 시작");
            transitions=clock.Advance(settings.nightMinutes);Check(transitions.Count==1&&clock.Snapshot().phase==ShopPhase.Dawn&&clock.Snapshot().day==2,"밤 종료 후 다음 날 새벽");
            transitions=clock.Advance(180);Check(transitions.Count==1&&clock.Snapshot().phase==ShopPhase.Day,"새벽 종료 후 낮");
            var old=clock.Snapshot();clock.Advance(42);clock.Restore(old);
            Check(clock.Snapshot().elapsedMinutes==0&&clock.Snapshot().day==2,"시계 저장 상태 복원");
            transitions=clock.Advance(settings.dayMinutes+settings.nightMinutes+settings.dawnMinutes);Check(transitions.Count==3&&clock.Snapshot().day==3&&clock.Snapshot().phase==ShopPhase.Day,"여러 단계 건너뛰기 순서");
            threw=false;try{clock.Advance(double.NaN);}catch(ArgumentException){threw=true;}
            Check(threw&&clock.Snapshot().day==3,"비정상 시간 거절 및 상태 유지");
            var wrong=clock.Snapshot();wrong.elapsedMinutes=540;threw=false;
            try{clock.Restore(wrong);}catch(ArgumentException){threw=true;}
            Check(threw&&clock.Snapshot().elapsedMinutes==0,"시간 범위가 잘못된 저장 거절");
            clock.MarkVisit("guest");
            var brokenHistory=clock.Snapshot();
            brokenHistory.visitCounts.Clear();
            threw=false;
            try{clock.Restore(brokenHistory);}catch(ArgumentException){threw=true;}
            Check(threw&&clock.NextVisit("guest")==2,"손상된 방문 기록 복원 시 원래 상태 유지");
            var legacyClock=new ShopClockState{visitorIds=null,visitCounts=null,visitDays=null};
            clock.Restore(legacyClock);
            Check(!clock.HasVisited("guest")&&clock.NextVisit("guest")==1,"구버전 방문 기록 호환");
            return count;
        }
    }
}

// Consolidated from CounterFeatureChecks.cs
namespace GeneralStore.Inventory.Verification
{
    public static class CounterFeatureChecks
    {
        static int count;
        static void Check(bool value,string rule){count++;if(!value)throw new Exception("카운터 검사 실패: "+rule);}
        static InventoryModel Model()
        {
            var s=new InventorySave{gold=1000};
            s.containers.Add(new ContainerRecord{containerId="test_storage",columns=8,rows=8});
            s.containers.Add(new ContainerRecord{containerId="display_1",columns=6,rows=4});
            s.containers.Add(new ContainerRecord{containerId="display_2",columns=6,rows=4});
            return new InventoryModel(InventoryModelChecks.Fixture(),s);
        }
        public static int Run()
        {
            count=0;var catalog=InventoryModelChecks.Fixture();float cellAspect=1/1.1f;
            foreach(string id in new[]{"C001","sample_rope","sample_lframe","sample_cframe"})
            for(int rotation=0;rotation<4;rotation++)
            foreach(float spriteAspect in new[]{.3f,.7f,1f,1.6f,3f})
            {
                var shape=catalog.Shape(id);var quads=IconMeshGeometry.Build(shape,rotation,true,spriteAspect,cellAspect);
                Check(quads.Count==1,"아이콘은 단일 사각형 메시");
                foreach(var q in quads)
                {
                    Check(shape.Contains(rotation,(int)Math.Floor((q.x0+q.x1)/2),(int)Math.Floor((q.y0+q.y1)/2)),"이미지는 사각형 범위 안에 맞춤");
                    foreach(var uv in new[]{q.topLeft,q.topRight,q.bottomRight,q.bottomLeft})
                        Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"직사각형 셀/회전 UV 범위");
                }
                if(id=="C001"||id=="sample_rope")
                {
                    float x0=float.MaxValue,y0=float.MaxValue,x1=float.MinValue,y1=float.MinValue;
                    foreach(var q in quads){x0=Math.Min(x0,q.x0);y0=Math.Min(y0,q.y0);x1=Math.Max(x1,q.x1);y1=Math.Max(y1,q.y1);}
                    float actual=(x1-x0)*78/((y1-y0)*85.8f),expected=rotation%2==0?spriteAspect:1/spriteAspect;
                    Check(Math.Abs(actual-expected)<.0001f,"그림의 실제 픽셀 종횡비 보존");
                }
            }
            return count;
        }
    }
}

namespace GeneralStore.Inventory.Verification
{
    public static class QualityFeatureChecks
    {
        static void Check(bool ok,string note){if(!ok)throw new System.Exception("Shared quality check: "+note);}
        public static int Run()
        {
            var item=new ItemDefinition{id="one",type="test",basePrice=100,width=1,height=1,iconKey="base-one",qualities=new[]{
                new QualityDefinition{quality="common",name="Common",priceMultiplier=1,iconKey="old-common"},
                new QualityDefinition{quality="legendary",name="Legendary",priceMultiplier=2,iconKey="old-legendary"}}};
            var catalog=new ItemCatalog(new ItemDatabase{items=new[]{item}});
            Check(catalog.IconKey("one")=="base-one","explicit base icon");
            Check(catalog.ReferencePrice("one","legendary")==200,"quality pricing unchanged");
            item.iconKey=null;catalog=new ItemCatalog(new ItemDatabase{items=new[]{item}});
            Check(catalog.IconKey("one")=="old-common","old common sprite becomes shared base");
            item.iconKey="one-shared";foreach(var q in item.qualities)q.iconKey=null;
            catalog=new ItemCatalog(new ItemDatabase{items=new[]{item}});Check(catalog.IconKey("one")=="one-shared","no per-quality sprite keys required");
            Check(QualityVisuals.Color("none")==UnityEngine.Color.white&&QualityVisuals.Color("common")==UnityEngine.Color.white,"white");
            Check(QualityVisuals.Color("superior").g>QualityVisuals.Color("superior").r,"green");
            Check(QualityVisuals.Color("perfect").b>QualityVisuals.Color("perfect").r,"blue");
            Check(QualityVisuals.Color("masterpiece").b>QualityVisuals.Color("masterpiece").g,"purple");
            Check(QualityVisuals.Color("legendary").r==1&&QualityVisuals.Color("legendary").b<.1f,"orange");
            return 9;
        }
    }
}

namespace GeneralStore.Inventory.Verification
{
    public static class CounterMotionChecks
    {
        static int count;
        static void Check(bool value,string note){count++;if(!value)throw new Exception("Counter motion: "+note);}
        public static int Run()
        {
            count=0;var settings=new CounterTableSettings();
            var size=CounterTableMotion.VisualSize(3,3,100,1);Check(size.x==300&&size.y==300,"3x3 perspective footprint");
            size=CounterTableMotion.VisualSize(2,3,100,2);Check(size.x==200&&size.y==100,"native aspect preserved inside footprint");
            foreach(float dt in new[]{1f/30,1f/60,.1f})
            {
                var body=new CounterBodyState{x=960,y=-450,halfWidth=150,halfHeight=150,active=true,vx=200};var bodies=new List<CounterBodyState>{body};
                for(int i=0;i<600;i++){CounterTableMotion.Advance(bodies,settings,dt);Check(body.Bottom>=settings.surface-.001f,"never below table");}
                Check(body.asleep&&Math.Abs(body.Bottom-settings.surface)<.01f,"rests on table");
                Check(CounterTableMotion.Advance(bodies,settings,dt)==0,"settled bodies skip integration");
            }
            Check(CounterTableMotion.PresentationRotation(3,true,0)==1&&CounterTableMotion.PresentationRotation(2,true,0)==0,"customer tall item lays sideways");
            Check(CounterTableMotion.PresentationRotation(4,false,0)==1&&CounterTableMotion.PresentationRotation(3,true,3)==0,"recommended tall item lays sideways and rotations wrap");
            Check(Math.Abs(CounterTableMotion.SurfaceDepth(1028.5f,1080,0,8)-(985+1072)*.5f)<.01,"landing line midway between old positions");
            var lower=new CounterBodyState{x=960,y=settings.surface+50,halfWidth=50,halfHeight=50,active=true,asleep=true};
            var upper=new CounterBodyState{x=960,y=-700,halfWidth=50,halfHeight=50,active=true};var row=new List<CounterBodyState>{lower,upper};
            for(int i=0;i<300;i++)CounterTableMotion.Advance(row,settings,1f/60);
            Check(upper.asleep&&lower.asleep&&Math.Abs(upper.Bottom-settings.surface)<.01f&&Math.Abs(lower.Bottom-settings.surface)<.01f,"both goods land on table, not on each other");
            Check(upper.x==960&&lower.x==960,"overlapping horizontal positions are retained");
            upper.dragged=true;upper.y=-2000;CounterTableMotion.ClampBody(upper,settings);Check(upper.Bottom>=settings.surface,"drag clamp");
            upper.y=-500;float y=upper.y;CounterTableMotion.Advance(row,settings,1f/30);Check(upper.y==y,"held body ignores gravity");
            upper.dragged=false;upper.asleep=false;upper.x=lower.x;for(int i=0;i<300;i++)CounterTableMotion.Advance(row,settings,1f/60);
            Check(upper.x==lower.x&&Math.Abs(upper.Bottom-settings.surface)<.01f,"release over another good stays at chosen X and returns to table");
            var crowded=new List<CounterBodyState>();for(int n=0;n<5;n++)crowded.Add(new CounterBodyState{x=960,y=-500,halfWidth=250,halfHeight=50,active=true});
            for(int i=0;i<300;i++)CounterTableMotion.Advance(crowded,settings,1f/60);
            foreach(var body in crowded)Check(body.x==960&&body.halfWidth==250&&Math.Abs(body.Bottom-settings.surface)<.01,"crowded goods retain size and chosen position without extra row space");
            bool rejected=false;try{CounterTableMotion.Advance(row,settings,float.NaN);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid timestep rejected");
            return count;
        }
    }
}
#endif
