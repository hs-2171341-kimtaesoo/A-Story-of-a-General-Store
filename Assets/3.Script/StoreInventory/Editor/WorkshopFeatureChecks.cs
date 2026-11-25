#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace GeneralStore.Inventory.Verification
{
    public static class WorkshopFeatureChecks
    {
        static int checks;static void Check(bool value,string note){checks++;if(!value)throw new Exception("공방/임시 저장고 검사: "+note);}
        static InventoryModel Model(bool overflowSource=true,bool unlock=true)
        {
            string folder=ItemTableTools.DataFolder;
            var catalog=new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/items_table.json").text));
            var data=JsonUtility.FromJson<WorkshopDatabase>(AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/workshops_table.json").text);data.repeatUsesTemporary=overflowSource;
            var save=new InventorySave{gold=10000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=7});save.containers.Add(new ContainerRecord{containerId="display_test",columns=8,rows=5});
            var model=new InventoryModel(catalog,save,null,null,new WorkshopCatalog(data,catalog));string error;if(unlock)Check(model.TryUnlockAllWorkshopsForDemo(out error),error);return model;
        }
        static string Draft(InventoryModel model,string station,int index=0){return model.GetWorkshop(station).jobs[index].id;}
        static void Put(InventoryModel model,string station,string job,int slot,string item,string quality="common")
        {
            string error;Check(model.TryAdd("warehouse",item,quality,1,9,out error),error);var record=model.GetContainer("warehouse").items.Find(v=>v.itemId==item&&v.quality==quality);
            Check(model.TryPutWorkshopMaterial(station,job,slot,"warehouse",record.instanceId,model.Revision,out error),error);
        }
        static void Hire(InventoryModel model,string station){string error;Check(model.TryHireWorkshop(station,true,1,out error),error);}
        public static int Run()
        {
            checks=0;string error;int stored,overflow;
            var locked=Model(unlock:false);
            Check(!locked.HasUnlockedWorkshop&&!locked.IsWorkshopUnlocked("processing")&&!locked.IsWorkshopUnlocked("smith")&&!locked.IsWorkshopUnlocked("alchemy"),"해금 전 관리 메뉴/탭 숨김 조건");
            Check(locked.TryUnlockWorkshop("smith",out error),error);
            var hired=locked.GetWorkshop("smith");long wage=locked.Workshops.Find("smith").dailyWage;
            Check(locked.HasUnlockedWorkshop&&hired.unlocked&&hired.hired&&hired.wageDue==wage,"한 공방만 해금하면 즉시 고용/당일 일급 등록");
            Check(!locked.IsWorkshopUnlocked("processing")&&!locked.IsWorkshopUnlocked("alchemy"),"다른 공방은 잠금 유지");
            long before=locked.Revision;Check(locked.TryUnlockWorkshop("smith",out error)&&locked.Revision==before&&locked.WorkshopDue()==wage,"중복 해금 일급/리비전 중복 없음");
            Check(!locked.TryHireWorkshop("smith",false,1,out error)&&locked.GetWorkshop("smith").hired,"수동 고용 중지 경로 제거");
            var migration=locked.Snapshot();migration.workshops.Find(w=>w.id=="smith").hired=false;
            Check(locked.TryRestore(migration,out error)&&locked.GetWorkshop("smith").hired&&locked.WorkshopDue()==wage,"이전 미고용 상태를 자동 고용으로 복원/일급 중복 없음");
            migration=locked.Snapshot();migration.successfulCustomerSales=1;
            Check(locked.TryRestore(migration,out error)&&locked.GetWorkshop("processing").hired,"기존 판매 실적에 맞는 공방 해금/고용");
            var automatic=Model(unlock:false);Check(automatic.TryAdd("warehouse","R001","common",1,0,out error),error);
            var offer=automatic.GetContainer("warehouse").items[0];int price=automatic.Catalog.ReferencePrice("R001","common");
            Check(automatic.TryListOnDisplay(new MoveRequest{sourceContainer="warehouse",targetContainer="display_test",instanceId=offer.instanceId,x=0,y=0,expectedRevision=automatic.Revision},price,out error),error);
            var session=new RecommendationSession(automatic);
            Check(session.TryEnterShopping("해금 검사",100000,10,new[]{"R001"},1,out error),error);
            Check(session.PresentOnCounter(out error),error);long revenue;
            Check(session.TrySubmit(out revenue,out error),error);
            Check(automatic.GetWorkshop("processing").hired&&automatic.SuccessfulCustomerSales==1,"첫 손님 판매로 즉시 자동 고용");
            var m=Model();Hire(m,"processing");string job=Draft(m,"processing");
            Check(!m.CanStartWorkshopJob("processing",job),"빈 조합 제작 금지");Put(m,"processing",job,0,"R001");Check(m.MatchWorkshopRecipes("processing",job).Count==0,"불완전 조합 결과 없음");
            Put(m,"processing",job,4,"R001");var matches=m.MatchWorkshopRecipes("processing",job);Check(matches.Count==2,"슬롯 순서 무관 판재/목탄 후보2개");
            Check(m.TryChooseWorkshopRecipe("processing",job,"recipe_R013",out error),error);Put(m,"processing",job,2,"R001");Check(!m.CanStartWorkshopJob("processing",job),"재료가 더 있어도 정확한 조합만 허용");
            Check(m.TryTakeWorkshopItem("processing",job,2,false,"warehouse",out error),error);Check(m.TryChooseWorkshopRecipe("processing",job,"recipe_M001",out error),error);
            // Total five logs: staged2, remaining1 in box,2 in temporary. Display logs are excluded.
            Check(m.TryAdd("display_test","R001","common",2,1,out error),error);
            var snapshot=m.Snapshot();var temp=snapshot.containers.Find(c=>c.containerId==InventoryModel.TemporaryStorageId);temp.items.Add(new ItemRecord{instanceId="temp-a",itemId="R001",quality="common",x=0,y=0});temp.items.Add(new ItemRecord{instanceId="temp-b",itemId="R001",quality="common",x=2,y=0});
            Check(m.TryRestore(snapshot,out error),error);Check(m.TrySetProcessingRepeat(job,2,1,out error),error);Check(m.TryStartWorkshopJob("processing",job,out error),error);
            double duration=m.Workshops.Recipe("recipe_M001").Duration;m.AdvanceWorkshopsDuring(1000,duration*10,ShopPhase.Day,1);
            var done=m.GetWorkshop("processing").jobs[0];Check(done.completed==2&&done.results.Count==2&&done.exhausted&&!done.started,"가공소 소진 반복2회 종료");
            Check(m.GetContainer("display_test").items.Count==2,"진열품 자동 소비 없음");
            int leftovers=m.GetContainer("warehouse").items.FindAll(i=>i.itemId=="R001").Count+m.GetContainer(InventoryModel.TemporaryStorageId).items.FindAll(i=>i.itemId=="R001").Count;Check(leftovers==1,"부분 재료는 소모하지 않음");
            Check(m.TryCollectWorkshopResults("processing",out stored,out overflow,out error)&&stored+overflow==2&&m.WorkshopResultCount("processing")==0,"완성품 일괄 수령");long revision=m.Revision;
            Check(!m.TryCollectWorkshopResults("processing",out stored,out overflow,out error)&&m.Revision==revision,"중복 수령 방지");
            // Fixed-count crafting ignores excess stock after the requested number of cycles.
            m=Model();Hire(m,"processing");job=Draft(m,"processing");Put(m,"processing",job,0,"R001");Put(m,"processing",job,1,"R001");Check(m.TryAdd("warehouse","R001","common",5,1,out error),error);
            Check(m.TrySetProcessingRepeat(job,1,2,out error)&&m.TryStartWorkshopJob("processing",job,out error),error);m.AdvanceWorkshopsDuring(1000,duration*10,ShopPhase.Day,1);
            Check(m.GetWorkshop("processing").jobs[0].completed==2&&m.GetContainer("warehouse").items.Count==3,"횟수 지정2회만 소비");
            // Two smith rows compete for one active workstation; reordering preserves progress.
            m=Model();Hire(m,"smith");string first=Draft(m,"smith",0),second=Draft(m,"smith",1);Put(m,"smith",first,0,"M006");Put(m,"smith",second,0,"M006");
            Check(m.TryChooseWorkshopRecipe("smith",first,"recipe_M011",out error)&&m.TryChooseWorkshopRecipe("smith",second,"recipe_M012",out error),error);
            Check(m.TryStartWorkshopJob("smith",first,out error)&&m.TryStartWorkshopJob("smith",second,out error),error);m.AdvanceWorkshopsDuring(10,10,ShopPhase.Day,1);
            Check(m.GetWorkshop("smith").jobs[0].elapsed==10&&m.GetWorkshop("smith").jobs[1].elapsed==0,"맨 위 한 작업만 진행");
            Check(m.TryMoveWorkshopJob("smith",second,-1,out error),error);m.AdvanceWorkshopsDuring(5,5,ShopPhase.Day,1);
            var smith=m.GetWorkshop("smith");Check(smith.jobs[0].id==second&&smith.jobs[0].elapsed==5&&smith.jobs[1].id==first&&smith.jobs[1].elapsed==10,"대장간 순서 변경 시 두 진행 보존");
            var save=JsonUtility.FromJson<InventorySave>(JsonUtility.ToJson(m.Snapshot()));var restored=Model();Check(restored.TryRestore(save,out error)&&restored.GetWorkshop("smith").jobs[1].elapsed==10,"새 작업층 저장 복원");
            // Emergency craft shortens remaining time to25%, charges once, does not finish instantly.
            m=Model();Hire(m,"alchemy");job=Draft(m,"alchemy");Put(m,"alchemy",job,0,"M013");Put(m,"alchemy",job,1,"M014");Check(m.TryStartWorkshopJob("alchemy",job,out error),error);
            double total=m.Workshops.Recipe("recipe_P001").Duration;m.AdvanceWorkshopsDuring(10,10,ShopPhase.Day,1);double remaining=total-10;
            Check(m.TryInstantAlchemy(job,out error)&&m.GetWorkshop("alchemy").extraDue==25,error);Check(!m.TryInstantAlchemy(job,out error)&&m.GetWorkshop("alchemy").extraDue==25,"긴급 비용 중복 방지");
            Check(Math.Abs(m.WorkshopRemaining(m.GetWorkshop("alchemy").jobs[0])-remaining*.25)<1e-7&&m.WorkshopResultCount("alchemy")==0,"남은 시간만25%, 즉시 완료 아님");
            m.AdvanceWorkshopsDuring(180,180,ShopPhase.Dawn,1);Check(m.WorkshopResultCount("alchemy")==0,"긴급 제작도 새벽 휴무");m.AdvanceWorkshopsDuring(100,remaining*.25,ShopPhase.Day,1);Check(m.WorkshopResultCount("alchemy")==1,"단축 시간 후 결과1개");
            // Full storage receives products in16-column overflow, never leaves a partially collected result.
            Check(m.TryGrantDebugItems("E001",null,100,out stored,out overflow,out error),error);Check(m.TryCollectWorkshopResults("alchemy",out stored,out overflow,out error)&&stored==0&&overflow==1,"완성품 초과 임시 저장고");
            Check(m.GetContainer(InventoryModel.TemporaryStorageId).columns==16,"임시 저장고16열");
            m=Model();Hire(m,"processing");job=Draft(m,"processing");Put(m,"processing",job,0,"R001");Put(m,"processing",job,1,"R001");
            Check(m.TrySetProcessingRepeat(job,1,3,out error)&&m.TryStartWorkshopJob("processing",job,out error),error);m.AdvanceWorkshopsDuring(5,5,ShopPhase.Day,1);
            var previous=m.Snapshot();previous.version=10;previous.containers.Find(c=>c.containerId==InventoryModel.DeliveryContainerId).columns=8;
            previous.workshops.Find(w=>w.id=="processing").jobs[0].inputs.Add(new WorkshopMaterial{slot=2,item=new ItemRecord{instanceId="legacy-extra",itemId="R001",quality="common",purchasePrice=77}});
            restored=Model();Check(restored.TryRestore(previous,out error),error);
            Check(restored.GetWorkshop("processing").jobs[0].elapsed==5&&restored.GetWorkshop("processing").jobs[0].inputs.Count==2,"기존 가공 중 배치 진행 보존");
            Check(restored.GetItem(InventoryModel.TemporaryStorageId,"legacy-extra").purchasePrice==77,"기존 다음 회차 재료를 임시 저장고에 보존");
            // Door delivery is received as one command; surplus goes to16-column storage.
            m=Model();int grantStored,grantOverflow;
            Check(m.TryGrantDebugItems("E001",null,56,out grantStored,out grantOverflow,out error)&&grantStored==56,error);
            Check(m.TryOrderNextDawn(1,"P001","common",3,0,out error),error);DawnSettlement bill;
            Check(m.TryBeginDawn(2,0,10,out bill,out error)&&bill.storageCost==0&&m.DeliveryItemCount==3,"도착일 비용 없음/문 옆 수령 대기");
            var incoming=m.GetContainer(InventoryModel.DeliveryContainerId).items;var expected=new HashSet<string>();foreach(var item in incoming)expected.Add(item.instanceId);
            Check(m.TryReceiveDoorDelivery(out stored,out overflow,out error)&&stored==0&&overflow==3&&m.DeliveryItemCount==0,"택배 클릭 즉시 전체 임시 저장");
            var temporary=m.GetContainer(InventoryModel.TemporaryStorageId);Check(temporary.columns==16&&temporary.items.Count==3,"임시 저장고16열");foreach(var item in temporary.items)Check(expected.Contains(item.instanceId),"수령 UID 보존");
            Check(m.TryBeginDawn(3,0,10,out bill,out error)&&bill.occupiedCells==12&&bill.storageCost==120,"임시 저장 실제 점유칸당10");
            Check(m.TryBeginDawn(4,0,2,out bill,out error)&&bill.storageCost==24,"보관료 임의 변경");
            long storageRevision=m.Revision;Check(m.TryBeginDawn(4,0,2,out bill,out error)&&bill.alreadySettled&&m.Revision==storageRevision,"로드/재진입 중복 정산 없음");
            Check(!m.TryReceiveDoorDelivery(out stored,out overflow,out error)&&m.Revision==storageRevision,"택배 중복 클릭 없음");
            var old=m.Snapshot();old.version=10;var oldDoor=old.containers.Find(c=>c.containerId==InventoryModel.DeliveryContainerId);oldDoor.columns=8;
            oldDoor.items.Add(new ItemRecord{instanceId="old-delivery",itemId="P001",quality="superior",purchasePrice=77});
            var upgraded=Model();Check(upgraded.TryRestore(old,out error)&&upgraded.Snapshot().version==13,error);
            Check(upgraded.GetItem(InventoryModel.TemporaryStorageId,"old-delivery").purchasePrice==77&&upgraded.DeliveryItemCount==0,"이전 택배 격자는 물품 보존하여 임시 저장고로 전환");
            // Moving the lowest item within a dynamic grid must restore its row extent after removal.
            var copy=upgraded.Snapshot();var target=copy.containers.Find(c=>c.containerId==InventoryModel.TemporaryStorageId);target.rows=24;
            target.items.Add(new ItemRecord{instanceId="bottom",itemId="P001",quality="common",x=0,y=20});Check(upgraded.TryRestore(copy,out error),error);
            Check(upgraded.TryMove(new MoveRequest{sourceContainer=InventoryModel.TemporaryStorageId,targetContainer=InventoryModel.TemporaryStorageId,instanceId="bottom",x=4,y=20,expectedRevision=upgraded.Revision},out error),error);
            Check(upgraded.GetContainer(InventoryModel.TemporaryStorageId).rows>=22&&upgraded.TryRestore(upgraded.Snapshot(),out error),"임시 저장고 내부 이동/행 높이 보존");
            Debug.Log("새 슬롯 조합/후보 선택/자동 반복/일괄 수령/대장간 순서/긴급 제작 검사 완료");return checks;
        }
    }
}
#endif
