using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory.Verification
{
    public static class SupplierFeatureChecks
    {
        static int count;
        static void Check(bool yes,string rule){count++;if(!yes)throw new Exception("거래처/보관함 검사 실패: "+rule);}
        static SupplierDatabase Definitions()
        {
            return new SupplierDatabase{seed=371,suppliers=new[]{
                new SupplierDefinition{id="a",name="A",refreshDays=2,initiallyUnlocked=true,offers=new[]{
                    new SupplierOfferDefinition{offerId="potion",itemId="C001",quality="common",minStock=4,maxStock=4,priceBase=80,minPriceMultiplier=1,maxPriceMultiplier=1},
                    new SupplierOfferDefinition{offerId="rope",itemId="sample_rope",quality="common",minStock=1,maxStock=5,priceBase=40,minPriceMultiplier=.7,maxPriceMultiplier=1.3}}},
                new SupplierDefinition{id="b",name="B",refreshDays=2,initiallyUnlocked=false,offers=new[]{
                    new SupplierOfferDefinition{offerId="potion",itemId="C001",quality="superior",minStock=1,maxStock=5,priceBase=100,minPriceMultiplier=.8,maxPriceMultiplier=1.2}}}
            }};
        }
        static InventoryModel Model(long gold=1000,int rows=8)
        {
            var items=InventoryModelChecks.Fixture();var suppliers=new SupplierCatalog(Definitions(),items);var save=new InventorySave{gold=gold};
            save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=rows});save.containers.Add(new ContainerRecord{containerId="display_1",columns=6,rows=4});
            save.containers.Add(new ContainerRecord{containerId="display_2",columns=6,rows=4});var m=new InventoryModel(items,save,suppliers);
            string error;m.RefreshSuppliersForDay(1,out error);return m;
        }
        static SupplierPurchaseLine[] Cart(int amount,int price=80)
        {return new[]{new SupplierPurchaseLine{offerId="potion",quantity=amount,expectedUnitPrice=price}};}
        public static int Run()
        {
            count=0;string error;var m=Model();
            Check(m.Supplier("a").stocks[0].remaining==4,"초기 재고4개");
            Check(m.TryPurchaseFromSupplier("a",Cart(2),1,0,out error),"1일차 두 개 구매");
            Check(m.Gold==840&&m.Supplier("a").stocks[0].remaining==2,"돈/재고 동시 차감");
            var order=m.Snapshot().deliveries[0];
            Check(order.arrivalDay==2&&order.items.Count==2&&order.items[0].purchasePrice==80&&m.Parcels().Count==0,"다음 새벽 개별 매입가 배송 예약");
            long revision=m.Revision;
            Check(m.RefreshSuppliersForDay(2,out error)&&m.Revision==revision&&m.Supplier("a").stocks[0].remaining==2,"다음 날 재고/가격 유지 및 중복 Commit 없음");
            var restored=Model(0);Check(restored.TryRestore(m.Snapshot(),out error)&&restored.Supplier("a").stocks[0].remaining==2&&restored.Gold==840,"구매 재고/돈 저장 복원");
            Check(restored.RefreshSuppliersForDay(2,out error)&&restored.Supplier("a").stocks[0].remaining==2,"불러온 후 재입고 악용 방지");
            var changedDefinitions=Definitions();changedDefinitions.suppliers[0].offers[0].priceBase=200;
            changedDefinitions.suppliers[0].offers[0].minStock=changedDefinitions.suppliers[0].offers[0].maxStock=1;
            var adjusted=new InventoryModel(m.Catalog,m.Snapshot(),new SupplierCatalog(changedDefinitions,m.Catalog));
            Check(adjusted.RefreshSuppliersForDay(2,out error)&&adjusted.Supplier("a").stocks[0].unitPrice==80&&adjusted.Supplier("a").stocks[0].remaining==2,"밸런스 범위 변경은 현재 저장 주기를 다시 뽑지 않음");
            Check(adjusted.RefreshSuppliersForDay(3,out error)&&adjusted.Supplier("a").stocks[0].unitPrice==200&&adjusted.Supplier("a").stocks[0].remaining==1,"다음 주기에 수정한 가격/수량 범위 적용");
            Check(!m.TryPurchaseFromSupplier("a",Cart(3),2,0,out error)&&m.Gold==840,"재고 초과 전부 거절");
            Check(!m.TryPurchaseFromSupplier("a",Cart(1,79),2,0,out error)&&m.Supplier("a").stocks[0].remaining==2,"변조한 가격 거절");
            Check(!m.TryPurchaseFromSupplier("a",new[]{Cart(1)[0],Cart(1)[0]},2,0,out error),"중복 구매 행 거절");
            Check(m.RefreshSuppliersForDay(3,out error)&&m.Supplier("a").stocks[0].remaining==4&&m.Supplier("a").cycle==1,"3일차 잔여분에 더하지 않고 새 수량 교체");
            Check(!m.TryPurchaseFromSupplier("a",Cart(1),3,0,out error),"주기 변경된 오래된 장바구니 차단");
            Check(!m.RefreshSuppliersForDay(2,out error)&&m.Supplier("a").cycle==1,"거래처 날짜 되감기 거절");
            Check(!m.TryPurchaseFromSupplier("b",Cart(1,100),3,1,out error),"미해금 거래처 구매 차단");
            Check(m.SetSupplierAccess("b",true,false,"",out error)&&m.Supplier("b").unlocked,"거래처 해금");
            Check(m.SetSupplierAccess("a",true,true,"이벤트",out error)&&m.SupplierStates().Count==2,"중단해도 거래처 삭제하지 않음");
            Check(!m.TryPurchaseFromSupplier("a",Cart(1),3,1,out error)&&m.Gold==840,"중단 거래처 구매 차단");
            Check(m.RefreshSuppliersForDay(5,out error)&&m.Supplier("a").blocked&&m.Supplier("a").blockReason=="이벤트","재입고가 진행 상태를 초기화하지 않음");
            Check(m.SetSupplierAccess("a",true,false,"",out error)&&m.TryPurchaseFromSupplier("a",Cart(1),5,2,out error),"거래 재개 후 구매");
            long paid;int delivered;
            Check(m.TryBeginDawn(6,0,out paid,out delivered,out error)&&m.Parcels().Count==1&&delivered==3,"여러 구매를 한 합배송 상자로 도착");
            var poor=Model(10);revision=poor.Revision;
            Check(!poor.TryPurchaseFromSupplier("a",Cart(1),1,0,out error)&&poor.Gold==10&&poor.Supplier("a").stocks[0].remaining==4&&poor.Snapshot().deliveries.Count==0&&poor.Revision==revision,"돈 부족 시 재고/배송/버전 전부 유지");
            // 여러 주기에서도 아이템별 범위와 고정 시드 재현성을 확인합니다.
            var comparison=Model();bool changed=false;int previous=-1;
            for(int day=1;day<=201;day+=2)
            {
                m.RefreshSuppliersForDay(day<5?5:day,out error);comparison.RefreshSuppliersForDay(day,out error);
                var stock=comparison.Supplier("a").stocks[1];
                Check(stock.remaining>=1&&stock.remaining<=5&&stock.unitPrice>=28&&stock.unitPrice<=52,"주기별 수량/가격 범위");
                if(previous!=-1&&previous!=stock.remaining)changed=true;previous=stock.remaining;
                var defs=Definitions();var catalog=new SupplierCatalog(defs,comparison.Catalog);
                var expected=catalog.CreateStock("a",defs.suppliers[0].offers[1],(day-1)/2,comparison.Catalog);
                Check(stock.unitPrice==expected.unitPrice&&stock.remaining==expected.remaining,"고정 시드/주기로 재현되는 물품");
            }
            Check(changed,"수량이 실제로 여러 주기에 걸쳐 변동");
            var bad=comparison.Snapshot();bad.suppliers[0].stocks[0].remaining=101;
            Check(!restored.TryRestore(bad,out error),"재고 한도 초과 저장 거절");
            bad=comparison.Snapshot();bad.suppliers[0].stocks[0].unitPrice=-1;
            Check(!restored.TryRestore(bad,out error),"음수 가격 저장 거절");
            m=Model();m.TryAdd("warehouse","C001","common",1,80,out error);string uid=m.GetContainer("warehouse").items[0].instanceId;
            Check(m.TryExpandWarehouse("warehouse",out error)&&m.GetContainer("warehouse").rows==9&&m.GetContainer("warehouse").columns==8,"확장은 아래로8칸");
            string id;Check(!m.TryCreateWarehouse(8,out id,out error),"80칸 전 추가 보관함 거절");
            Check(m.TryExpandWarehouse("warehouse",out error)&&m.GetContainer("warehouse").rows*8==80,"한 보관함 최대80칸");
            Check(!m.TryExpandWarehouse("warehouse",out error)&&m.GetItem("warehouse",uid)!=null,"80칸 초과 확장 차단 및 물건 유지");
            Check(m.TryCreateWarehouse(8,out id,out error)&&id=="warehouse_2"&&m.GetContainer(id).columns==8&&m.GetContainer(id).rows==8,"별도 보관함 생성");
            Check(m.TryMove(new MoveRequest{sourceContainer="warehouse",targetContainer=id,instanceId=uid,expectedRevision=m.Revision},out error)&&m.GetItem(id,uid)!=null,"보관함 사이 낱개 이동");
            Check(restored.TryRestore(m.Snapshot(),out error)&&restored.WarehouseIds().Count==2,"다중 보관함 저장 복원");
            bad=m.Snapshot();bad.containers[0].columns=9;Check(!m.TryRestore(bad,out error),"가로8칸 고정 저장 검증");
            bad=m.Snapshot();bad.containers[0].rows=11;Check(!m.TryRestore(bad,out error),"80칸 초과 저장 검증");
            foreach(var dims in new[]{new[]{40f,200f},new[]{300f,40f},new[]{100f,100f}})
            {
                var fit=IconFitGeometry.Fit(dims[0],dims[1],0,0,dims[0],dims[1],96,96,6);
                Check(Math.Abs(fit.width/fit.height-dims[0]/dims[1])<.00001&&fit.width<=84.0001&&fit.height<=84.0001,"가로/세로/정사각 아이콘 비율/프레임 유지");
                Check(fit.x==0&&fit.y==0,"정상 아이콘 중심 유지");
            }
            var offset=IconFitGeometry.Fit(200,300,70,20,150,260,96,96,6);
            Check(Math.Abs(offset.x+(110-100)*(offset.width/200))<.00001&&Math.Abs(offset.y+(140-150)*(offset.height/300))<.00001,"메시 여백을 가진 아이콘의 보이는 중심 보정");
            return count;
        }
    }
}
