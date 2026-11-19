#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using GeneralStore.Inventory;
namespace GeneralStore.Inventory.Verification
{
    public static class WorkshopFeatureChecks
    {
        static int count;
        static void Check(bool value,string note){count++;if(!value)throw new Exception("공방 검사 실패: "+note);}
        static InventoryModel Model()
        {
            var data=JsonUtility.FromJson<ItemDatabase>(Resources.Load<TextAsset>("StoreInventory/items").text);data=WorkshopCatalog.MergeDemoItems(data,JsonUtility.FromJson<ItemDatabase>(Resources.Load<TextAsset>("StoreInventory/workshop_demo_items").text));
            var catalog=new ItemCatalog(data);var workshops=new WorkshopCatalog(JsonUtility.FromJson<WorkshopDatabase>(Resources.Load<TextAsset>("StoreInventory/workshops").text),catalog);
            var save=new InventorySave{gold=10000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=5});save.containers.Add(new ContainerRecord{containerId="display_preview",columns=8,rows=5});return new InventoryModel(catalog,save,null,null,workshops);
        }
        static void Prepare(InventoryModel m,string station){string error;Check(m.TryUnlockAllWorkshopsForDemo(out error)&&m.TryHireWorkshop(station,true,1,out error),"시험 공방 고용");}
        static string Job(InventoryModel m,string station,string recipe,int cycles=1){string id,error;Check(m.TryCreateWorkshopJob(station,recipe,cycles,out id,out error),"작업 생성");return id;}
        static void Feed(InventoryModel m,string station,string job,string item,int amount,int firstSlot=0,string quality="common")
        {
            string error;Check(m.TryAdd("warehouse",item,quality,amount,10,out error),"시험 재료 입고");
            for(int n=0;n<amount;n++){var record=m.GetContainer("warehouse").items.Find(x=>x.itemId==item);Check(m.TryPutWorkshopMaterial(station,job,firstSlot+n,"warehouse",record.instanceId,m.Revision,out error),"재료 UID 이동");}
        }
        public static int Run()
        {
            count=0;string error,id,unused;var m=Model();
            Check(!m.TryHireWorkshop("processing",true,1,out error),"해금 전 고용 차단");
            Check(m.TryAdd("warehouse","C001","common",1,10,out error),"해금 시험 물품");var sale=m.GetContainer("warehouse").items[0];
            Check(m.TryListOnDisplay(new MoveRequest{sourceContainer="warehouse",targetContainer="display_preview",instanceId=sale.instanceId,expectedRevision=m.Revision},80,out error),"손님 시험 진열");
            var session=new RecommendationSession(m);long revenue;
            Check(session.TryEnterShopping(new CustomerDefinition{id="craft",displayName="단골",preferredItemIds=new[]{"C001"},wallet=1000,spendingFraction=1,maxPriceRatio=2,maxPickCount=1},out error)&&session.PresentOnCounter(out error)&&session.TrySubmit(out revenue,out error),"손님 거래 성공");
            Check(m.SuccessfulCustomerSales==1&&m.GetWorkshop("processing").unlocked&&!m.GetWorkshop("alchemy").unlocked,"거래 성공으로만 순차 해금");
            m=Model();Prepare(m,"processing");id=Job(m,"processing","oak_plank",5);Feed(m,"processing",id,"demo_oak_log",5);
            Check(!m.TryCreateWorkshopJob("processing","oak_plank",1,out unused,out error),"가공소 자리1개 제한");
            Check(m.TryStartWorkshopJob("processing",id,out error),"순회 제작 시작");long rev=m.Revision;m.AdvanceWorkshops(1);Check(m.Revision==rev,"진행도만으로 재고 드래그 취소 안 함");m.AdvanceWorkshops(199);
            var j=m.GetWorkshop("processing").jobs[0];Check(j.completed==2&&j.results.Count==2&&j.inputs.Count==1&&j.elapsed==0,"5원목으로2판재 후 재료 대기");
            Feed(m,"processing",id,"demo_oak_log",1);m.AdvanceWorkshops(100);Check(m.GetWorkshop("processing").jobs[0].completed==3,"보충 후 자동 재개");Feed(m,"processing",id,"demo_oak_log",4);m.AdvanceWorkshops(100);
            j=m.GetWorkshop("processing").jobs[0];Check(j.completed==5&&j.results.Count==5&&j.inputs.Count==0,"원목10개 소비 판재5개 보존");
            var saved=JsonUtility.FromJson<InventorySave>(JsonUtility.ToJson(m.Snapshot()));var restored=Model();Check(restored.TryRestore(saved,out error)&&restored.GetWorkshop("processing").jobs[0].results[0].instanceId==j.results[0].instanceId,"진행/결과 UID 저장 복원");
            for(int n=0;n<5;n++)Check(m.TryTakeWorkshopItem("processing",id,0,true,"warehouse",out error),"결과 회수");Check(m.GetWorkshop("processing").jobs.Count==0,"모든 결과 수령 시 자리 반환");
            m=Model();Prepare(m,"alchemy");id=Job(m,"alchemy","potion");Feed(m,"alchemy",id,"demo_herb",2);
            long gold=m.Gold;Check(m.TryInstantAlchemy(id,out error)&&m.Gold==gold&&m.GetWorkshop("alchemy").extraDue==25,"즉시 조제 추가금만 적립");Check(!m.TryInstantAlchemy(id,out error)&&m.GetWorkshop("alchemy").extraDue==25,"즉시 조제 중복 차단");
            Check(m.TryHireWorkshop("alchemy",false,1,out error)&&m.TryHireWorkshop("alchemy",true,1,out error)&&m.GetWorkshop("alchemy").wageDue==60,"고용 토글로 일급 중복/회피 불가");
            DawnSettlement bill;Check(m.TryBeginDawn(2,0,0,out bill,out error)&&bill.workshopCost==85&&m.Gold==gold-85&&m.GetWorkshop("alchemy").wageDue==60,"일급+즉시료 새벽 정산과 다음날 일급");gold=m.Gold;Check(m.TryBeginDawn(2,0,0,out bill,out error)&&m.Gold==gold,"같은 새벽 중복 정산 방지");
            m=Model();Prepare(m,"smith");string a=Job(m,"smith","sword"),b=Job(m,"smith","sword");Feed(m,"smith",a,"demo_iron_bar",2);Feed(m,"smith",a,"demo_oak_plank",1,2);Feed(m,"smith",b,"demo_iron_bar",2);Feed(m,"smith",b,"demo_oak_plank",1,2);
            Check(m.TryStartWorkshopJob("smith",a,out error)&&m.TryStartWorkshopJob("smith",b,out error),"대기 작업 예약");m.AdvanceWorkshops(10);Check(m.GetWorkshop("smith").jobs[0].elapsed==10&&m.GetWorkshop("smith").jobs[1].elapsed==0,"맨 위 게이지만 진행");
            Check(m.TryMoveWorkshopJob("smith",b,-1,out error),"우선순위 이동");m.AdvanceWorkshops(10);Check(m.GetWorkshop("smith").jobs[0].elapsed==10&&m.GetWorkshop("smith").jobs[1].elapsed==10,"작업 교체 시 진행 보존");
            Check(!m.TryTakeWorkshopItem("smith",b,0,false,"warehouse",out error),"진행 중 재료 부분 반출 차단");
            var copy=m.Snapshot();var duplicate=copy.workshops.Find(w=>w.id=="smith").jobs[0].inputs[0].item;copy.containers[0].items.Add(duplicate.Copy());Check(!m.TryRestore(copy,out error),"공방/상자 UID 중복 저장 거절");
            var quality=m.Workshops;var output=m.Catalog.Find("C001");var low=quality.Probabilities(output,new[]{new ItemRecord{quality="common"}});var high=quality.Probabilities(output,new[]{new ItemRecord{quality="legendary"}});double lowTail=0,highTail=0,sum=0;
            for(int i=0;i<high.Length;i++){sum+=high[i];if(quality.Quality(output.qualities[i].quality).rank>=3){lowTail+=low[i];highTail+=high[i];}}Check(Math.Abs(sum-1)<1e-9&&highTail>lowTail,"고품질 재료가 상위 품질 확률 상승");
            m=Model();Prepare(m,"processing");id=Job(m,"processing","oak_plank");Feed(m,"processing",id,"demo_oak_log",2);Check(m.TryAdd("warehouse","demo_herb","common",40,1,out error),"회수 공간 채우기");rev=m.Revision;
            Check(!m.TryCancelWorkshopJob("processing",id,"warehouse",out error)&&m.Revision==rev&&m.GetWorkshop("processing").jobs[0].inputs.Count==2,"상자 부족 취소 전체 원자성");
            return count;
        }
    }
}
#endif
