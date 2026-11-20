using System;
using GeneralStore.Inventory;
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
            var save=new InventorySave{gold=10000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=5});catalog.AddInitialContainers(save);
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
            var save=new InventorySave{gold=100000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=5});definitions.AddInitialContainers(save);
            var model=new InventoryModel(InventoryModelChecks.Fixture(),save,null,definitions);
            foreach(var id in definitions.TypeIds())
            {
                if(model.OwnedDisplayId(id,0)==null){string added,error;Check(model.TryBuyDisplay(id,out added,out error),"12종 구매");}
                string uid=model.OwnedDisplayId(id,0);Check(uid!=null&&model.GetContainer(uid)!=null,"종류별 독립 컨테이너");
                string duplicate,e;Check(!model.TryBuyDisplay(id,out duplicate,out e),"같은 위치 가구 재구매 금지");
            }
            Check(model.DisplayIds().Count==12,"전체12개 연결");
            var allSave=new InventorySave{gold=1000};allSave.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=5});definitions.AddInitialContainers(allSave,true);
            var allModel=new InventoryModel(InventoryModelChecks.Fixture(),allSave,null,definitions);Check(allModel.DisplayIds().Count==12&&allModel.Gold==1000,"전체 소유 테스트는 구매 자금을 소비하지 않는 별도 초기 상태");
            string loadError;Check(model.TryRestore(model.Snapshot(),out loadError),"12개 저장 복원");return count;
        }
    }
}
