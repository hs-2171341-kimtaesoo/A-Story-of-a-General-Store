using System;
using GeneralStore.Inventory;
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
            var save=new InventorySave{gold=1000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=8});
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
            Check(m.StorageAction("warehouse")==StorageUpgradeAction.Expand,"64칸 버튼은 공간 확장");
            string selected;
            Check(m.TryUpgradeStorage("warehouse",8,out selected,out error)&&selected=="warehouse"&&m.GetContainer(selected).rows==9,"첫 버튼 클릭은72칸");
            Check(m.TryUpgradeStorage("warehouse",8,out selected,out error)&&m.StorageAction(selected)==StorageUpgradeAction.AddBox,"80칸에서 같은 버튼이 상자 추가로 전환");
            Check(m.TryUpgradeStorage("warehouse",8,out selected,out error)&&selected=="warehouse_2"&&m.StorageAction(selected)==StorageUpgradeAction.Expand,"새 상자 선택 후 다시 공간 확장 표시");
            Check(!m.TryCreateWarehouse(8,out selected,out error),"이전80칸 상자로 연속 추가 우회 불가");
            Check(m.TryUpgradeStorage("warehouse",8,out selected,out error)&&selected=="warehouse_2"&&m.GetContainer(selected).rows==9,"이전 상자를 열어도 두번째 상자부터 확장");
            Check(m.GetContainer("warehouse").rows==10&&m.GetContainer("warehouse_2").rows==9,"다른 상자의 공간을 수정하지 않음");
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
            var catalog=new DisplayCatalog(db);var start=new InventorySave{gold=600};start.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=8});catalog.AddInitialContainers(start);
            var bought=new InventoryModel(InventoryModelChecks.Fixture(),start,null,catalog);
            Check(bought.TryBuyDisplay("buy",out id,out error)&&bought.Gold==350&&bought.DisplayIds().Count==2,"구매 즉시 공간 추가와 금액 차감");
            Check(bought.TryBuyDisplay("buy",out id,out error)&&bought.Gold==100&&bought.DisplayIds().Count==3,"같은 종류 여러개 구매 가능");
            long rev=bought.Revision;Check(!bought.TryBuyDisplay("buy",out id,out error)&&bought.Revision==rev&&bought.Gold==100,"잔액 부족은 금액/공간 모두 그대로");
            var round=new InventoryModel(InventoryModelChecks.Fixture(),bought.Snapshot(),null,catalog);
            Check(round.DisplayIds().Count==3&&round.Gold==100,"위치 없는 여러 가구 저장 복원");
            Check(m.TryUpgradeStorage("warehouse",8,out selected,out error)&&selected=="warehouse_2"&&m.GetContainer(selected).rows==10,"두번째 상자80칸 완료");
            Check(m.TryUpgradeStorage("warehouse",8,out selected,out error)&&selected=="warehouse_3","두번째80칸 후에만 세번째 추가");
            return count;
        }
    }
}
