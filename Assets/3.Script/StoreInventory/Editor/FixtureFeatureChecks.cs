#if UNITY_EDITOR
using GeneralStore.Inventory.Verification;
using GeneralStore.Inventory;
using Random=System.Random;
using System;
using UnityEngine;

// FixtureFeatureChecks.cs
namespace GeneralStore.Inventory.Verification
{
    public static class FixtureFeatureChecks
    {
        static int count;
        static void Check(bool value,string rule){count++;if(!value)throw new Exception("진열/창/업그레이드 검사 실패: "+rule);}
        static DisplayCatalog Definitions()
        {
            return new DisplayCatalog(new DisplayDatabase{
                types=new[]{
                    new DisplayTypeDefinition{id="shelf",title="일반 선반",columns=6,rows=4},
                    new DisplayTypeDefinition{id="wall",title="벽 선반",columns=8,rows=2},
                    new DisplayTypeDefinition{id="rack",title="무기 거치대",columns=3,rows=6,allowedItemTypes=new[]{"weapon"}}},
                locations=new[]{
                    new DisplayLocationDefinition{id="1",title="좌측 벽",initialTypeId="shelf",allowedTypeIds=new[]{"shelf","wall"}},
                    new DisplayLocationDefinition{id="2",title="중앙 벽",initialTypeId="wall",allowedTypeIds=new[]{"wall"}},
                    new DisplayLocationDefinition{id="rack",title="장비 자리",allowedTypeIds=new[]{"rack"}},
                    new DisplayLocationDefinition{id="narrow",title="좁은 창가",allowedTypeIds=new[]{"shelf"},columnsOverride=4,rowsOverride=3},
                    new DisplayLocationDefinition{id="extra",title="추가 벽",allowedTypeIds=new[]{"wall"}}}});
        }
        static InventoryModel Model()
        {
            var catalog=InventoryModelChecks.Fixture();catalog.Find("C001").type="potion";catalog.Find("sample_sword").type="weapon";
            var save=new InventorySave{gold=1000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});
            var defs=Definitions();defs.AddInitialContainers(save);return new InventoryModel(catalog,save,null,defs);
        }
        static MoveRequest Move(InventoryModel m,string src,string uid,string dst,int x=0,int y=0)
        {return new MoveRequest{sourceContainer=src,targetContainer=dst,instanceId=uid,x=x,y=y,expectedRevision=m.Revision};}
        public static int Run()
        {
            count=0;string error,id;var m=Model();
            Check(m.DisplayIds().Count==2&&m.GetContainer("display_1").columns==6&&m.GetContainer("display_2").rows==2,"초기 종류별 다른 고정 크기");
            Check(m.TryInstallDisplay("rack","rack",out id,out error)&&id=="display_rack","거치대 추가");
            Check(m.GetContainer(id).columns==3&&m.GetContainer(id).rows==6,"거치대 고정18칸");
            long revision=m.Revision;
            Check(!m.TryInstallDisplay("rack","rack",out id,out error)&&m.Revision==revision,"같은 자리에 중복 설치 차단");
            Check(!m.TryInstallDisplay("rack","narrow",out id,out error),"자리와 종류 호환 검사");
            Check(!m.TryExpandWarehouse("display_rack",out error)&&m.GetContainer("display_rack").rows==6,"진열 내부 확장 API 차단");
            Check(m.TryInstallDisplay("shelf","narrow",out id,out error)&&m.GetContainer(id).columns==4&&m.GetContainer(id).rows==3,"같은 선반도 자리 지정4x3 용량");
            Check(m.TryAdd("warehouse","C001","common",2,80,out error)&&m.TryAdd("warehouse","C001","superior",1,90,out error),"품질별 낱개 준비");
            var potions=m.GetContainer("warehouse").items;string potion=potions[0].instanceId;
            var rejected=Move(m,"warehouse",potion,"display_rack");var preview=m.Preview(rejected);
            Check(!preview.allowed&&preview.cells.Count==4,"거치대에 포션은 배치 셀을 유지한 빨강 판정");
            revision=m.Revision;
            Check(!m.TryListOnDisplay(rejected,100,out error)&&m.Revision==revision&&m.GetItem("warehouse",potion)!=null,"종류 거절 시 낱개/등록/버전 유지");
            Check(!m.TryAdd("display_rack","C001","common",1,80,out error),"직접 추가로 종류 제한 우회 차단");
            for(int n=0;n<3;n++)Check(m.TryListOnDisplay(Move(m,"warehouse",potions[n].instanceId,"display_1",n*2),100,out error),"품질별 일반 선반 진열");
            var summary=m.SummarizeDisplay("display_1");
            Check(summary.itemCount==3&&summary.kindCount==1&&summary.groups.Count==2&&summary.usedCells==12,"품목/품질 요약과 실제 점유 칸");
            Check(summary.groups[0].count==2&&summary.groups[1].count==1,"요약만 묶고 낱개는3개 유지");
            Check(m.GetContainer("display_1").items.Count==3,"실물 스택으로 변환하지 않음");
            m.TryAdd("warehouse","sample_sword","common",1,80,out error);string sword=m.GetContainer("warehouse").items[0].instanceId;
            Check(m.TryListOnDisplay(Move(m,"warehouse",sword,"display_rack"),100,out error),"거치대에 무기 진열");
            Check(m.SummarizeDisplay("display_rack").usedCells==3&&m.SummarizeDisplay("display_rack").itemCount==1,"거치대 실제 점유량");
            var restored=Model();Check(restored.TryRestore(m.Snapshot(),out error)&&restored.DisplayIds().Count==4,"추가 진열/자리/물건/가격 저장 복원");
            var bad=m.Snapshot();bad.containers.Find(c=>c.containerId=="display_rack").rows=7;
            Check(!restored.TryRestore(bad,out error)&&restored.GetContainer("display_rack").rows==6,"진열 저장의 내부 크기 변조 거절");
            bad=m.Snapshot();bad.displays[1].locationId=bad.displays[0].locationId;Check(!restored.TryRestore(bad,out error),"중복 설치 자리 저장 거절");
            bad=m.Snapshot();bad.containers.Find(c=>c.containerId=="display_rack").items[0].itemId="C001";
            Check(!restored.TryRestore(bad,out error),"저장에서도 무기 전용 제한 검사");
            var customer=new RecommendationSession(m);
            Check(customer.TryEnterShopping("손님",1000,2,new[]{"sample_sword"},1,out error),"새로 추가한 거치대도 손님이 선택");
            Check(m.IsReserved(sword)&&!m.TryMove(Move(m,"display_rack",sword,"warehouse"),out error),"추가 공간 손님 선택 잠금 유지");
            Check(m.TryInstallDisplay("wall","extra",out id,out error),"방문 중 다른 공간 추가 가능");long revenue;
            Check(customer.TrySubmitPurchasesOnly(out revenue,out error)&&revenue==100&&m.SummarizeDisplay("display_rack").itemCount==0,"동적 진열 구매/돈/차감 정상");
            string selected;Check(m.StorageAction("warehouse")==StorageUpgradeAction.AddBox,"상자 추가 버튼만 제공");
            Check(m.TryCreateWarehouse(InventoryModel.WarehouseRows,out selected,out error)&&selected=="warehouse_2","직접 고정 크기 상자 추가");
            // 드래그 수학은 CanvasScaler의 배율이 적용된 로컬 좌표로 테스트합니다.
            var inside=WindowDragGeometry.Clamp(960,-146,690,882,1920,1080);
            Check(inside.x==960&&inside.y==-146,"창의 정상 위치 유지");
            var outside=WindowDragGeometry.Clamp(3000,-3000,690,882,1920,1080);
            Check(outside.x==1230&&outside.y==-198,"창이 화면 오른쪽/아래로 사라지지 않음");
            outside=WindowDragGeometry.Clamp(-100,100,690,882,1920,1080);Check(outside.x==0&&outside.y==0,"화면 왼쪽/위 경계");
            var random=new Random(83);
            for(int n=0;n<1000;n++)
            {
                float w=100+random.Next(700),h=100+random.Next(800);var point=WindowDragGeometry.Clamp(random.Next(-3000,3000),random.Next(-3000,3000),w,h,1920,1080);
                Check(point.x>=0&&point.x+w<=1920&&point.y<=0&&-point.y+h<=1080,"다양한 창 크기의 경계 유지");
                var twice=WindowDragGeometry.Clamp(point.x,point.y,w,h,1920,1080);Check(twice.x==point.x&&twice.y==point.y,"반복 보정해도 떨리지 않는 위치");
            }
            // 외형은 실제 칸 크기로 계산하되, 논리 점유 셀 수에는 영향을 주지 않습니다.
            var wide=DisplayWindowLayout.Calculate(6,5,78,85.8f,24,164,220,440,624,520);
            var tall=DisplayWindowLayout.Calculate(4,6,78,85.8f,50,164,220,440,624,520);
            var small=DisplayWindowLayout.Calculate(6,2,78,85.8f,24,164,220,440,624,520);
            Check(wide.width>tall.width&&tall.height>wide.height&&small.height<wide.height,"가구마다 창 전체 비율이 다름");
            var capped=DisplayWindowLayout.Calculate(12,10,78,85.8f,24,164,220,440,624,520);
            Check(capped.gridWidth==624&&capped.gridHeight==520,"큰 격자는 뷰포트 제한 후 스크롤");
            bool invalidLayout=false;try{DisplayWindowLayout.Calculate(1,1,float.NaN,85,24,164,220,440,624,520);}catch(ArgumentException){invalidLayout=true;}
            Check(invalidLayout,"유효하지 않은 외형 수치 거절");
            var db=new DisplayDatabase{types=new[]{new DisplayTypeDefinition{id="buy",title="구매 선반",columns=6,rows=2,purchasePrice=250}},initialTypes=new[]{"buy"}};
            var catalog=new DisplayCatalog(db);var start=new InventorySave{gold=600};start.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});catalog.AddInitialContainers(start);
            var bought=new InventoryModel(InventoryModelChecks.Fixture(),start,null,catalog);
            Check(bought.TryBuyDisplay("buy",out id,out error)&&bought.Gold==350&&bought.DisplayIds().Count==2,"구매 즉시 공간 추가와 금액 차감");
            Check(bought.TryBuyDisplay("buy",out id,out error)&&bought.Gold==100&&bought.DisplayIds().Count==3,"같은 종류 여러개 구매 가능");
            long rev=bought.Revision;Check(!bought.TryBuyDisplay("buy",out id,out error)&&bought.Revision==rev&&bought.Gold==100,"잔액 부족은 금액/공간 모두 그대로");
            var round=new InventoryModel(InventoryModelChecks.Fixture(),bought.Snapshot(),null,catalog);
            Check(round.DisplayIds().Count==3&&round.Gold==100,"위치 없는 여러 가구 저장 복원");
            return count;
        }
    }
}

// DrawerFeatureChecks.cs
namespace GeneralStore.Inventory.Verification
{
    public static class DrawerFeatureChecks
    {
        static int count;static void Check(bool value,string rule){count++;if(!value)throw new Exception("서랍/독립 진열: "+rule);}
        static MoveRequest Move(InventoryModel m,string source,string uid,string target,int x)
        {return new MoveRequest{sourceContainer=source,targetContainer=target,instanceId=uid,x=x,y=0,expectedRevision=m.Revision};}
        public static int Run()
        {
            count=0;
            Check(DrawerGeometry.ClosedY(1080,60)==-1020,"손잡이60이 남음");
            Check(!DrawerGeometry.ShouldClose(-491,882,1080,1f/3f),"1/3 미만 내려가면 열림 유지");
            Check(DrawerGeometry.ShouldClose(-492,882,1080,1f/3f),"정확히 1/3부터 닫힘");
            Check(DrawerGeometry.ShouldClose(-900,882,1080,1f/3f),"더 내리면 닫힘");
            float y=-700;for(int n=0;n<100;n++){float next=DrawerGeometry.Step(y,-1020,12,1f/60);Check(next<=y&&next>=-1020,"단조롭게 내려가며 지나치지 않음");y=next;}
            Check(y==-1020,"정확히 멈춤");float partial=DrawerGeometry.Step(-700,-1020,12,1f/60);Check(partial!=-1020&&partial< -700,"첫 프레임 순간이동 없음");
            float one=DrawerGeometry.Step(-700,-146,12,.1f),two=-700;for(int n=0;n<10;n++)two=DrawerGeometry.Step(two,-146,12,.01f);Check(Math.Abs(one-two)<.001f,"프레임 간격 독립 보간");
            var timer=new HoverSwitchTimer();Check(!timer.Tick("box2",1,2),"1초 미전환");Check(timer.Tick("box2",1,2),"2초 전환");Check(!timer.Tick("box2",3,2),"반복 전환 없음");
            timer.Reset();Check(!timer.Tick("box2",1.9f,2),"새 드래그 초기화");timer.Tick(null,1,2);Check(!timer.Tick("box2",.2f,2),"탭 이탈 초기화");
            Check(!timer.Tick("box3",1.8f,2),"다른 탭 처음부터");Check(timer.Tick("box3",.3f,2),"다른 탭도 연속2초 전환");
            var definitions=new DisplayCatalog(new DisplayDatabase{types=new[]{
                new DisplayTypeDefinition{id="shelf",title="선반",columns=6,rows=5,purchasePrice=900,maximumOwned=1},
                new DisplayTypeDefinition{id="rack",title="무기대",columns=4,rows=6,purchasePrice=600,maximumOwned=1,allowedItemTypes=new[]{"weapon"}}},initialTypes=new[]{"shelf"}});
            var save=new InventorySave{gold=1500};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});definitions.AddInitialContainers(save);
            var catalog=InventoryModelChecks.Fixture();catalog.Find("sample_sword").type="weapon";var m=new InventoryModel(catalog,save,null,definitions);string id,error;
            Check(m.OwnedDisplayId("rack",0)==null,"미구매 가구 연결 없음");Check(!m.TryBuyDisplay("shelf",out id,out error)&&m.Gold==1500,"초기 지급도 보유 수 포함");
            Check(m.TryBuyDisplay("rack",out id,out error)&&m.Gold==900,"가구 구매/차감");Check(m.OwnedDisplayId("rack",0)==id&&m.OwnedDisplayId("rack",1)==null,"구매 후 해당 핫스팟 연결");
            Check(!m.TryBuyDisplay("rack",out id,out error)&&m.Gold==900,"중복 구매 방지");
            string shelf=m.OwnedDisplayId("shelf",0),rack=m.OwnedDisplayId("rack",0);
            m.TryAdd("warehouse","C001","common",1,80,out error);var potion=m.GetContainer("warehouse").items[0];Check(m.TryListOnDisplay(Move(m,"warehouse",potion.instanceId,shelf,0),100,out error),"첫 진열장 진열");
            m.TryAdd("warehouse","sample_sword","common",1,60,out error);var sword=m.GetContainer("warehouse").items[0];Check(m.TryListOnDisplay(Move(m,"warehouse",sword.instanceId,rack,0),200,out error),"둘째 진열장 진열");
            var session=new RecommendationSession(m);Check(session.TryEnterShopping("테스트",1000,4,new[]{"C001","sample_sword"},5,out error),"창 유무 없이 모든 가구 선택");
            Check(m.IsReserved(potion.instanceId)&&m.IsReserved(sword.instanceId),"서로 다른 진열장의 동시 예약");Check(!m.TryMove(Move(m,rack,sword.instanceId,"warehouse",0),out error),"예약 이동 방지");
            long revenue;Check(session.TrySubmitPurchasesOnly(out revenue,out error)&&revenue==300&&m.Gold==1200,"합계 한 번 입금");Check(m.GetContainer(shelf).items.Count==0&&m.GetContainer(rack).items.Count==0,"각 가구에서 제거");
            var duplicate=m.Snapshot();duplicate.displays.Add(new DisplayInstallation{containerId="display_extra",typeId="shelf",locationId=""});duplicate.containers.Add(new ContainerRecord{containerId="display_extra",columns=6,rows=5});
            Check(!m.TryRestore(duplicate,out error)&&m.DisplayIds().Count==2,"최대 보유 수 초과 저장 거절");
            m.TryCreateWarehouse(InventoryModel.WarehouseRows,out id,out error);m.TryAdd("warehouse","C001","common",1,80,out error);potion=m.GetContainer("warehouse").items[0];
            Check(!m.TryMove(Move(m,"warehouse",potion.instanceId,id,8),out error)&&m.GetItem("warehouse",potion.instanceId)!=null,"잘못 드롭해도 원래 상자 유지");
            Check(m.TryMove(Move(m,"warehouse",potion.instanceId,id,0),out error)&&m.GetItem(id,potion.instanceId)!=null,"다른 상자에 동일 UID 이동");
            // 자유 이동 경계와 원래 하단 자리로 향하는 양축 보간을 검사합니다.
            var topLeft=DrawerGeometry.ClampFree(-50,50,690,1920,1080,60);
            Check(topLeft.x==0&&topLeft.y==0,"상자 위/왼쪽 경계");
            var bottomRight=DrawerGeometry.ClampFree(3000,-3000,690,1920,1080,60);
            Check(bottomRight.x==1230&&bottomRight.y==-1020,"오른쪽 경계와 하단 손잡이 유지");
            var placed=DrawerGeometry.ClampFree(780,-320,690,1920,1080,60);
            Check(placed.x==780&&placed.y==-320&&!DrawerGeometry.ShouldClose(placed.y,882,1080,.5f),"놓은 자유 위치를 변경하지 않음");
            float x=1100,down=-700;
            for(int frame=0;frame<120;frame++)
            {
                float nx=DrawerGeometry.Step(x,24,12,1f/60),ny=DrawerGeometry.Step(down,-1020,12,1f/60);
                Check(nx<=x&&nx>=24&&ny<=down&&ny>=-1020,"하단 원래 자리까지 양축 자연 복귀");x=nx;down=ny;
            }
            Check(x==24&&down==-1020,"가로/세로 모두 하단 원래 자리 도착");
            var rng=new Random(34);
            for(int n=0;n<1000;n++)
            {
                var point=DrawerGeometry.ClampFree(rng.Next(-4000,4000),rng.Next(-4000,4000),690,1920,1080,60);
                Check(point.x>=0&&point.x+690<=1920&&point.y<=0&&point.y>=-1020,"자유 이동 경계 무작위 검사");
            }
            foreach(float height in new[]{100f,136f,156f})
            {
                var box=DetailsFrameGeometry.Calculate(400,height,96,12,12);
                Check(box.x>=0&&box.y>=0&&box.x+box.side<=400&&box.y+box.side<=height,"상세 이미지 네모가 부모 안에 들어감");
                Check(Math.Abs(box.y+box.side*.5f-height*.5f)<.001f,"높이가 다른 상세창에서도 세로 중앙");
                Check(box.textLeft>=box.x+box.side+12&&box.textLeft+box.textWidth<=400,"텍스트와 네모의 겹침 방지");
            }
            var compact=DetailsFrameGeometry.Calculate(400,100,96,12,12);
            Check(compact.side==76&&compact.y==12,"100높이 창에96아이콘이 튀어나오던 문제 회귀");
            return count;
        }
    }
}

// FurniturePartitionChecks.cs
namespace GeneralStore.Inventory.Verification
{
    // Unity 메뉴에서 실행하는 실제 C# 모델 검사입니다. 화면 클릭/렌더링 검사는 별도입니다.
    public static class FurniturePartitionChecks
    {
        static int count;static void Check(bool ok,string note){count++;if(!ok)throw new Exception("칸막이 검사: "+note);}
        static DisplayTypeDefinition Definition(){return new DisplayTypeDefinition{id="partition",title="칸막이",columns=6,rows=6,maximumOwned=1,purchasePrice=100,horizontalDividers=new[]{3},verticalDividers=new[]{3}};}
        static InventoryModel Model(out string display)
        {
            var catalog=new DisplayCatalog(new DisplayDatabase{types=new[]{Definition()},initialTypes=new[]{"partition"}});
            var save=new InventorySave{gold=10000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});catalog.AddInitialContainers(save);
            var model=new InventoryModel(InventoryModelChecks.Fixture(),save,null,catalog);display=model.DisplayIds()[0];return model;
        }
        public static int Run()
        {
            count=0;var definition=Definition();DisplayPartitionRules.Validate(definition);
            var cells=new[]{new GridCell(0,0),new GridCell(1,0),new GridCell(0,1),new GridCell(1,1)};
            Check(DisplayPartitionRules.Fits(definition,cells,1,1),"벽 바로 앞까지 허용");
            Check(DisplayPartitionRules.Fits(definition,cells,3,3),"벽 바로 뒤에서 시작 허용");
            Check(!DisplayPartitionRules.Fits(definition,cells,2,0),"세로 벽 가로지르기 거절");
            Check(!DisplayPartitionRules.Fits(definition,cells,0,2),"가로 벽 가로지르기 거절");
            foreach(var id in new[]{"sample_lframe","sample_cframe"})
            {
                var shape=InventoryModelChecks.Fixture().Shape(id);
                for(int rotation=0;rotation<4;rotation++)
                {Check(DisplayPartitionRules.Fits(definition,shape.Cells(rotation),0,0),"3x3 사각형 같은 구역");Check(!DisplayPartitionRules.Fits(definition,shape.Cells(rotation),2,0),"3x3 사각형 벽 통과 거절");}
            }
            foreach(var bad in new[]{new[]{0},new[]{6},new[]{-1},new[]{2,2}})
            {
                var d=Definition();d.verticalDividers=bad;bool rejected=false;try{DisplayPartitionRules.Validate(d);}catch(ArgumentException){rejected=true;}Check(rejected,"잘못된 경계 거절");
            }
            string display,error;var model=Model(out display);Check(model.TryAdd("warehouse","C001","common",1,80,out error),"원본 아이템 준비");
            var item=model.GetContainer("warehouse").items[0];long revision=model.Revision;
            var request=new MoveRequest{sourceContainer="warehouse",targetContainer=display,instanceId=item.instanceId,x=2,y=0,expectedRevision=revision};
            var preview=model.Preview(request);Check(!preview.allowed&&preview.error.Contains("칸막이"),"미리보기의 벽 판정");
            Check(!model.TryMove(request,out error)&&model.Revision==revision&&model.GetItem("warehouse",item.instanceId)!=null,"거절 시 원본/리비전 유지");
            Check(!model.TryListOnDisplay(request,150,out error)&&model.DisplayListingCount==0,"진열 가격 등록도 벽 우회 불가");
            request.x=0;Check(model.TryListOnDisplay(request,150,out error),"같은 구역에 진열 허용");
            var saved=model.Snapshot();saved.containers.Find(c=>c.containerId==display).items[0].x=2;
            Check(!model.TryRestore(saved,out error)&&model.GetItem(display,item.instanceId).x==0,"벽을 넘는 저장 거절/기존 재고 유지");
            long revenue;Check(model.TrySell(new[]{new SaleLine{containerId=display,instanceId=item.instanceId,price=150}},model.Revision,out revenue,out error)&&revenue==150,"칸막이 진열장도 기존 판매 사용");
            Check(model.TryAdd(display,"C001","common",4,80,out error),"자동 입고는 각 구역에 배치");
            foreach(var i in model.GetContainer(display).items)Check(DisplayPartitionRules.Fits(definition,model.Cells(i),i.x,i.y),"자동 입고 벽 미통과");
            revision=model.Revision;Check(!model.TryAdd(display,"sample_bag","common",1,20,out error)&&model.Revision==revision,"총 빈칸이 남아도 구역에 맞지 않으면 원자적 거절");
            var d2=Definition();d2.horizontalDividers=null;d2.verticalDividers=null;
            Check(DisplayPartitionRules.Fits(d2,cells,2,2),"벽 없는 가구는 기존 동작");
            return count;
        }
        // 실제 13개 JSON을 Unity JsonUtility로 읽은 뒤 메뉴에서 호출합니다.
        public static int CheckCatalogue(DisplayCatalog definitions)
        {
            count=0;Check(definitions.TypeIds().Count==12,"가구12개");
            var save=new InventorySave{gold=100000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});definitions.AddInitialContainers(save);
            var model=new InventoryModel(InventoryModelChecks.Fixture(),save,null,definitions);
            foreach(var id in definitions.TypeIds())
            {
                if(model.OwnedDisplayId(id,0)==null){string added,error;Check(model.TryBuyDisplay(id,out added,out error),"12종 구매");}
                string uid=model.OwnedDisplayId(id,0);Check(uid!=null&&model.GetContainer(uid)!=null,"종류별 독립 컨테이너");
                string duplicate,e;Check(!model.TryBuyDisplay(id,out duplicate,out e),"같은 위치 가구 재구매 금지");
            }
            Check(model.DisplayIds().Count==12,"전체12개 연결");
            var allSave=new InventorySave{gold=1000};allSave.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});definitions.AddInitialContainers(allSave,true);
            var allModel=new InventoryModel(InventoryModelChecks.Fixture(),allSave,null,definitions);Check(allModel.DisplayIds().Count==12&&allModel.Gold==1000,"전체 소유 테스트는 구매 자금을 소비하지 않는 별도 초기 상태");
            string loadError;Check(model.TryRestore(model.Snapshot(),out loadError),"12개 저장 복원");return count;
        }
    }
}

// CustomDisplayArtChecks.cs
namespace GeneralStore.Inventory.Verification
{
    public static class CustomDisplayArtChecks
    {
        static int count;static void Check(bool ok,string message){count++;if(!ok)throw new Exception("전용 진열창 검사: "+message);}
        public static int Run(ItemCatalog items,DisplayCatalog catalog,DisplayWindowArtDatabase data)
        {
            count=0;Check(data!=null&&data.entries!=null&&data.entries.Length==12,"전용 창12개");
            foreach(var art in data.entries)
            {
                var type=catalog.Type(art.typeId);DisplayWindowArtRules.Validate(art,type);Check(true,"모든 논리 칸이 화면에 한 번씩 대응");
                foreach(var section in DisplayWindowArtRules.Sections(art))
                    for(int row=0;row<section.rows;row++)for(int column=0;column<section.columns;column++)
                    {
                        float px=section.rect.x+(column+.5f)*section.rect.width/section.columns,py=section.rect.y+(row+.5f)*section.rect.height/section.rows;
                        int x=(int)Math.Floor((px-section.rect.x)/(section.rect.width/section.columns))+section.column;
                        int y=(int)Math.Floor((py-section.rect.y)/(section.rect.height/section.rows))+section.row;
                        Check(x==column+section.column&&y==row+section.row,"화면 셀 중심과 전역 재고 좌표 왕복");
                    }
            }
            Check(catalog.Type("mannequin").columns==5&&catalog.Type("mannequin").rows==5,"마네킹5×5");
            Check(catalog.Type("table").columns==6&&catalog.Type("table").rows==6,"문 달린 진열장36칸");
            Check(catalog.Type("weapon_rack").columns==7&&catalog.Type("weapon_rack").rows==9,"무기 거치대63칸");
            var save=new InventorySave{gold=100000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});catalog.AddInitialContainers(save,true);
            var model=new InventoryModel(items,save,null,catalog);string error;Check(model.TryAdd("warehouse","C001","common",1,80,out error),"포션 준비");
            var uid=model.GetContainer("warehouse").items[0].instanceId;string cabinet=model.OwnedDisplayId("table",0);long before=model.Revision;
            var request=new MoveRequest{sourceContainer="warehouse",targetContainer=cabinet,instanceId=uid,x=0,y=1,expectedRevision=before};
            Check(!model.Preview(request).allowed&&!model.TryMove(request,out error)&&model.Revision==before,"상하 선반 경계 통과 거절/원본 유지");
            request.y=2;Check(model.TryMove(request,out error)&&model.GetItem(cabinet,uid).y==2,"아래쪽 격자로 이동해도 같은 컨테이너/UID");
            Check(model.TryAdd("warehouse","sample_sword","common",1,90,out error),"무기 준비");uid=model.GetContainer("warehouse").items[0].instanceId;
            request=new MoveRequest{sourceContainer="warehouse",targetContainer=model.OwnedDisplayId("weapon_rack",0),instanceId=uid,x=1,y=0,rotation=1,expectedRevision=model.Revision};
            Check(!model.TryMove(request,out error),"회전한 장검의 세로 기둥 횡단 거절");request.rotation=0;
            Check(model.TryMove(request,out error),"세로 장검을 왼쪽 구역에 진열");
            Check(model.TryRestore(model.Snapshot(),out error),"새 크기의12개 진열장 저장 복원");
            var legacy=new InventorySave{version=11,gold=100};legacy.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});
            var oldWall=new ContainerRecord{containerId="display_owned_1",columns=8,rows=2};legacy.containers.Add(oldWall);legacy.displays.Add(new DisplayInstallation{containerId=oldWall.containerId,typeId="wall_shelf",locationId=""});
            for(int n=0;n<4;n++){var item=new ItemRecord{instanceId="wall_old_"+n,itemId="C001",quality="common",x=n*2,y=0,purchasePrice=71+n};oldWall.items.Add(item);legacy.listings.Add(new DisplayListing{instanceId=item.instanceId,price=100});}
            var migrated=new InventoryModel(items,legacy,null,catalog);
            Check(migrated.GetContainer(oldWall.containerId).columns==6&&migrated.GetContainer(oldWall.containerId).items.Count==3,"기존8열 벽 선반을 새6열로 변환");
            Check(migrated.GetItem(InventoryModel.TemporaryStorageId,"wall_old_3").purchasePrice==74&&migrated.DisplayListingCount==3,"넘치는 UID/매입가 보존 및 판매 등록 해제");
            Check(oldWall.columns==8&&oldWall.items.Count==4&&migrated.Snapshot().version==13,"이전 저장은 수정하지 않고 복사본만 갱신");return count;
        }
    }
}

// ScrollableInventoryChecks.cs
public static class ScrollableInventoryChecks
{
    static int count;static void Check(bool value,string note){count++;if(!value)throw new Exception("스크롤 인벤토리: "+note);}
    public static int Run()
    {
        count=0;var catalog=InventoryModelChecks.Fixture();var displays=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(Resources.Load<TextAsset>("StoreInventory/display_fixtures").text));
        var old=new InventorySave{version=12,gold=1000};old.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=5});
        old.containers[0].items.Add(new ItemRecord{instanceId="kept",itemId="C001",quality="common",purchasePrice=77,x=6,y=3});
        string[] types={"side_shelf","cubby_cabinet","drawer_cabinet","single_stand","weapon_rack"};int[] columns={6,8,8,5,7},rows={10,6,6,8,8};
        for(int n=0;n<types.Length;n++){string id="display_owned_"+(n+1);old.displays.Add(new DisplayInstallation{containerId=id,typeId=types[n],locationId=""});old.containers.Add(new ContainerRecord{containerId=id,columns=columns[n],rows=rows[n]});}
        old.containers[1].items.Add(new ItemRecord{instanceId="side-kept",itemId="C001",quality="common",purchasePrice=81,x=4,y=8});
        old.containers[2].items.Add(new ItemRecord{instanceId="cubby-kept",itemId="E001",quality="none",purchasePrice=13,x=6,y=5});
        old.containers[3].items.Add(new ItemRecord{instanceId="drawer-kept",itemId="C001",quality="common",purchasePrice=83,x=6,y=4});
        old.listings.Add(new DisplayListing{instanceId="side-kept",price=199});old.listings.Add(new DisplayListing{instanceId="cubby-kept",price=29});old.listings.Add(new DisplayListing{instanceId="drawer-kept",price=211});
        var model=new InventoryModel(catalog,old,null,displays);
        Check(model.GetContainer("warehouse").rows==7&&old.containers[0].rows==5,"기존 상자를 복사본에서만7행 확장");
        Check(model.GetItem("warehouse","kept").purchasePrice==77&&model.GetItem("warehouse","kept").x==6,"기존 UID/비용/좌표 보존");
        Check(model.GetContainer(model.OwnedDisplayId("side_shelf",0)).columns==8,"안쪽 선반8열");
        Check(model.GetContainer(model.OwnedDisplayId("drawer_cabinet",0)).rows==12&&model.GetContainer(model.OwnedDisplayId("cubby_cabinet",0)).rows==12,"6층 총12행");
        Check(model.GetItem("display_owned_1","side-kept").x==4&&model.GetItem("display_owned_1","side-kept").y==8&&model.GetItem("display_owned_1","side-kept").purchasePrice==81,"선반 기존 물품 위치/매입가 보존");
        Check(model.GetItem("display_owned_2","cubby-kept").y==5&&model.GetItem("display_owned_3","drawer-kept").purchasePrice==83,"칸막이/서랍 확장 시 물품 보존");
        int price;Check(model.DisplayListingCount==3&&model.TryGetOfferPrice("display_owned_1","side-kept",out price)&&price==199&&old.listings[0].price==199,"판매가와 원본 저장 보존");
        var drawer=displays.Type("drawer_cabinet");var wide=catalog.Shape("sample_bag").Cells(0);
        Check(DisplayPartitionRules.Fits(drawer,wide,0,0),"상부는 넓은 물건 허용");Check(!DisplayPartitionRules.Fits(drawer,wide,0,6),"하부2×2 칸막이 통과 금지");
        Check(DisplayPartitionRules.Fits(drawer,catalog.Shape("C001").Cells(0),6,10),"하부 마지막2×2칸 배치");
        string error;Check(model.TryRestore(model.Snapshot(),out error),error);var bad=model.Snapshot();bad.containers[0].rows=5;Check(!model.TryRestore(bad,out error),"새 형식의 임의 축소는 거절");
        var data=JsonUtility.FromJson<DisplayWindowArtDatabase>(Resources.Load<TextAsset>("StoreInventory/display_window_art").text);
        Check(data.entries.Length==12,"전용 프레임12종");int scrolls=0;
        foreach(var art in data.entries)
        {
            DisplayWindowArtRules.Validate(art,displays.Type(art.typeId));if(!art.scrolling)continue;
            scrolls++;
            Check(art.floors.Length==(art.typeId=="side_shelf"||art.typeId=="shelf"?5:6),"전체 층수 유지");
            if(art.typeId!="drawer_cabinet")continue;
            float topHeight=0;
            for(int n=0;n<6;n++)
            {
                var floor=art.floors[n];Check(floor.sections.Length==(n<3?1:4),"상부 선반/하부 서랍 수");
                foreach(var section in floor.sections)Check(section.row==n*2&&section.rows==2&&section.columns==(n<3?8:2),"선반8×2/서랍2×2 매핑");
                if(n<3)topHeight+=floor.art.height;
                else Check(floor.art.y>=art.floors[2].art.y+art.floors[2].art.height,"하부에 실제 서랍 그림 사용");
            }
            Check(Math.Abs(topHeight-art.viewport.height)<.01f,"처음에는 상부3층만 표시");
            Check(art.LayoutHeight<art.sourceHeight&&art.frameSlices!=null&&art.frameSlices.Length==2,"긴 원본의 머리/받침을 작은 창에 연결");
        }
        var roundTrip=JsonUtility.FromJson<DisplayWindowArtDatabase>(JsonUtility.ToJson(data));
        foreach(var art in roundTrip.entries){DisplayWindowArtRules.Validate(art,displays.Type(art.typeId));Check(true,"Unity JSON 왕복 후 전용 창 검증");}
        Check(scrolls==4,"스크롤 진열4종");return count;
    }
}

// SideMenuFeatureChecks.cs
namespace GeneralStore.Inventory.Verification
{
    public static class SideMenuFeatureChecks
    {
        static int count;static void Check(bool value,string rule){count++;if(!value)throw new Exception("고정56칸 검사: "+rule);}
        public static int Run()
        {
            count=0;string id,error;
            var save=new InventorySave{gold=1500};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});
            var m=new InventoryModel(InventoryModelChecks.Fixture(),save);long revision=m.Revision;
            Check(m.GetContainer("warehouse").columns==8&&m.GetContainer("warehouse").rows==7,"8×7 고정 상자");
            Check(!m.TryExpandWarehouse("warehouse",out error)&&m.Revision==revision,"확장 API는 변경 없이 거절");
            var q=m.QuoteWarehousePurchase(600);Check(q.allowed&&q.addsBox&&q.afterCells==56&&q.goldAfter==900&&m.Revision==revision,"추가 전용 견적은 조회만 함");
            Check(m.TryPurchaseWarehouse(600,out id,out error)&&id=="warehouse_2"&&m.Gold==900&&m.Revision==revision+1,"비용/새 상자 한 번 확정");
            Check(m.TryPurchaseWarehouse(600,out id,out error)&&id=="warehouse_3"&&m.Gold==300,"앞 상자 확장 없이 다음 상자 추가");
            revision=m.Revision;Check(!m.TryPurchaseWarehouse(600,out id,out error)&&m.Gold==300&&m.WarehouseIds().Count==3&&m.Revision==revision,"잔액 부족은 원본 유지");
            Check(!m.TryPurchaseWarehouse(-1,out id,out error)&&m.Revision==revision,"잘못된 비용 거절");
            Check(!m.TryCreateWarehouse(6,out id,out error),"임의 크기 상자 생성 차단");
            var wrong=m.Snapshot();wrong.containers[0].columns=7;Check(!m.TryRestore(wrong,out error),"구형7열 저장을 임의 확대하지 않음");
            m.TryAdd("warehouse","C001","common",1,80,out error);var item=m.GetContainer("warehouse").items[0];
            Check(m.TryMove(new MoveRequest{sourceContainer="warehouse",targetContainer="warehouse_2",instanceId=item.instanceId,x=0,y=0,expectedRevision=m.Revision},out error)&&m.GetItem("warehouse_2",item.instanceId)!=null,"탭 간 같은 UID 이동");
            var geometry=GridFrameGeometry.Calculate(8,5,78,2);
            Check(geometry.width==626&&geometry.height==392&&geometry.tileSize==76,"정사각형 칸/프레임 크기");
            Check(2+7*78+geometry.tileSize==geometry.width-2,"오른쪽 바깥 여백도2");
            Check(2+4*78+geometry.tileSize==geometry.height-2,"아래 바깥 여백도2");
            var p=WindowDragGeometry.Clamp(-300,400,690,882,1920,1080);Check(p.x==0&&p.y==0,"좌측/위쪽 화면 안 복귀 목표");
            p=WindowDragGeometry.Clamp(2000,-1000,690,882,1920,1080);Check(p.x==1230&&p.y==-198,"진열창 우측/아래 복귀 목표");
            Check(DrawerGeometry.ClosedY(1080,60)==-1020,"이전보다108픽셀 낮은 하단 복귀 위치");
            return count;
        }
    }
}
#endif
