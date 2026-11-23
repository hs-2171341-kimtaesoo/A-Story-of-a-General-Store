using System;
using GeneralStore.Inventory;
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
            var selection=new ItemSelectionState();selection.Set("warehouse","a");selection.Set("display_1","b");selection.Clear("warehouse");
            Check(selection.Get("warehouse")==null&&selection.Get("display_1")=="b","창별 선택 초기화 분리");
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
