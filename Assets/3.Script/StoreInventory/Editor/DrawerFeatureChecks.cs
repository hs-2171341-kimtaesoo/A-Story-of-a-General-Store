using System;
using GeneralStore.Inventory;
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
            Check(!DrawerGeometry.ShouldClose(-638,882,1080,.5f),"절반 이상 보이면 열림");
            Check(DrawerGeometry.ShouldClose(-639,882,1080,.5f),"정확히 절반부터 닫힘");
            Check(DrawerGeometry.ShouldClose(-900,882,1080,.5f),"더 내리면 닫힘");
            float y=-700;for(int n=0;n<100;n++){float next=DrawerGeometry.Step(y,-1020,12,1f/60);Check(next<=y&&next>=-1020,"단조롭게 내려가며 지나치지 않음");y=next;}
            Check(y==-1020,"정확히 멈춤");float partial=DrawerGeometry.Step(-700,-1020,12,1f/60);Check(partial!=-1020&&partial< -700,"첫 프레임 순간이동 없음");
            float one=DrawerGeometry.Step(-700,-146,12,.1f),two=-700;for(int n=0;n<10;n++)two=DrawerGeometry.Step(two,-146,12,.01f);Check(Math.Abs(one-two)<.001f,"프레임 간격 독립 보간");
            var timer=new HoverSwitchTimer();Check(!timer.Tick("box2",1,2),"1초 미전환");Check(timer.Tick("box2",1,2),"2초 전환");Check(!timer.Tick("box2",3,2),"반복 전환 없음");
            timer.Reset();Check(!timer.Tick("box2",1.9f,2),"새 드래그 초기화");timer.Tick(null,1,2);Check(!timer.Tick("box2",.2f,2),"탭 이탈 초기화");
            Check(!timer.Tick("box3",1.8f,2),"다른 탭 처음부터");Check(timer.Tick("box3",.3f,2),"다른 탭도 연속2초 전환");
            var definitions=new DisplayCatalog(new DisplayDatabase{types=new[]{
                new DisplayTypeDefinition{id="shelf",title="선반",columns=6,rows=5,purchasePrice=900,maximumOwned=1},
                new DisplayTypeDefinition{id="rack",title="무기대",columns=4,rows=6,purchasePrice=600,maximumOwned=1,allowedItemTypes=new[]{"weapon"}}},initialTypes=new[]{"shelf"}});
            var save=new InventorySave{gold=1500};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=8});definitions.AddInitialContainers(save);
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
            m.TryExpandWarehouse("warehouse",out error);m.TryExpandWarehouse("warehouse",out error);m.TryCreateWarehouse(8,out id,out error);m.TryAdd("warehouse","C001","common",1,80,out error);potion=m.GetContainer("warehouse").items[0];
            Check(!m.TryMove(Move(m,"warehouse",potion.instanceId,id,8),out error)&&m.GetItem("warehouse",potion.instanceId)!=null,"잘못 드롭해도 원래 상자 유지");
            Check(m.TryMove(Move(m,"warehouse",potion.instanceId,id,0),out error)&&m.GetItem(id,potion.instanceId)!=null,"다른 상자에 동일 UID 이동");
            return count;
        }
    }
}
