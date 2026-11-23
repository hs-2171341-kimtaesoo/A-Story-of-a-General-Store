using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;

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
