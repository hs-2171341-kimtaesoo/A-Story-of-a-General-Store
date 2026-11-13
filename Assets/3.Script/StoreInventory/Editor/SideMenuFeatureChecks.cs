using System;
using GeneralStore.Inventory;
namespace GeneralStore.Inventory.Verification
{
    public static class SideMenuFeatureChecks
    {
        static int count;static void Check(bool ok,string rule){count++;if(!ok)throw new Exception("메뉴/선택/유료 확장 검사: "+rule);}
        static InventoryModel Model(long gold=3000)
        {
            var defs=new DisplayCatalog(new DisplayDatabase{types=new[]{new DisplayTypeDefinition{id="shelf",title="선반",columns=6,rows=5,maximumOwned=1,purchasePrice=900},new DisplayTypeDefinition{id="rack",title="무기대",columns=4,rows=6,maximumOwned=1,purchasePrice=600}},initialTypes=new[]{"shelf"}});
            var save=new InventorySave{gold=gold};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=5});defs.AddInitialContainers(save);
            return new InventoryModel(InventoryModelChecks.Fixture(),save,null,defs);
        }
        public static int Run()
        {
            count=0;var m=Model();string error,id;m.TryAdd("warehouse","C001","common",1,80,out error);var item=m.GetContainer("warehouse").items[0];
            Check(m.GetContainer("warehouse").rows*8==40,"기본40칸");
            long gold=m.Gold;
            foreach(int target in new[]{56,72,80})
            {
                long revision=m.Revision;var q=m.QuoteStorageUpgrade(400,600);
                Check(q.allowed&&!q.addsBox&&q.afterCells==target&&q.addedCells==(target==80?8:16),"16칸/마지막8칸 견적");
                Check(q.freeAfter==target-m.Catalog.Shape(item.itemId).CellCount&&q.goldAfter==gold-400,"정확한 남은 칸/예상 잔액");
                Check(m.Revision==revision,"견적 조회는 모델을 바꾸지 않음");
                Check(m.TryPurchaseStorageUpgrade(400,600,out id,out error)&&id=="warehouse"&&m.Revision==revision+1,"공간/비용 한 번의 확정");
                gold-=400;var actual=m.GetItem("warehouse",item.instanceId);
                Check(m.Gold==gold&&m.GetContainer("warehouse").rows*8==target&&actual.x==item.x&&actual.y==item.y&&actual.purchasePrice==80,"돈 차감/물건 위치와 매입가 유지");
            }
            var next=m.QuoteStorageUpgrade(400,600);Check(next.addsBox&&next.afterCells==40&&next.cost==600,"80칸 뒤 새40칸 상자 견적");
            Check(m.TryPurchaseStorageUpgrade(400,600,out id,out error)&&id=="warehouse_2"&&m.Gold==1200&&m.GetContainer(id).rows==5,"새40칸 상자와 비용600");
            Check(!m.TryCreateWarehouse(5,out id,out error),"두번째 상자를 확장하지 않고 세번째 추가 불가");
            Check(m.TryPurchaseStorageUpgrade(400,600,out id,out error)&&id=="warehouse_2"&&m.GetContainer(id).rows==7,"마지막 상자부터 확장");
            var poor=Model(399);long rev=poor.Revision;
            Check(!poor.TryPurchaseStorageUpgrade(400,600,out id,out error)&&poor.Gold==399&&poor.GetContainer("warehouse").rows==5&&poor.Revision==rev,"잔액 부족은 돈/공간 모두 그대로");
            Check(!poor.TryPurchaseStorageUpgrade(-1,600,out id,out error)&&poor.Revision==rev,"잘못된 비용 거절");
            Check(poor.TryPurchaseStorageUpgrade(0,0,out id,out error)&&poor.Gold==399&&poor.GetContainer("warehouse").rows==7,"Inspector0비용 무료 테스트 가능");
            Check(m.AvailableFurnitureTypes().Count==1&&m.AvailableFurnitureTypes()[0]=="rack","이미 보유한 종류는 구매 목록에서 제외");
            Check(m.TryBuyDisplay("rack",out id,out error)&&m.AvailableFurnitureTypes().Count==0,"구매 즉시 목록에서 제거");
            Check(!m.TryBuyDisplay("rack",out id,out error),"숨김과 별개로 모델도 중복 구매 차단");
            var restored=new InventoryModel(m.Catalog,m.Snapshot(),null,m.Displays);Check(restored.Gold==m.Gold&&restored.WarehouseIds().Count==2&&restored.AvailableFurnitureTypes().Count==0,"확장/가구 구매 저장 복원");
            var selection=new ItemSelectionState();selection.Set("warehouse","w");selection.Set("display_a","a");selection.Set("display_b","b");
            Check(selection.Get("warehouse")=="w"&&selection.Get("display_a")=="a"&&selection.Get("display_b")=="b","각 창의 독립 선택");
            selection.Clear("display_a");Check(selection.Get("display_a")==null&&selection.Get("display_b")=="b"&&selection.Get("warehouse")=="w","닫은 창만 선택 초기화");
            selection.Clear("warehouse");Check(selection.Get("warehouse")==null&&selection.Get("display_b")=="b","서랍을 넣어도 진열창 선택 유지");
            selection.ClearAll();Check(selection.Get("display_b")==null,"불러오기 시 UI선택 전체 초기화");
            selection.Set(null,"x");Check(selection.Get(null)==null,"빈 컨테이너 선택 방어");
            return count;
        }
    }
}
