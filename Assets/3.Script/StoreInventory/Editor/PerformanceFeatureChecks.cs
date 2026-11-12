using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace GeneralStore.Inventory.Verification
{
    public static class PerformanceFeatureChecks
    {
        static int count;
        public static string Report {get;private set;}
        static void Check(bool value,string rule){count++;if(!value)throw new Exception("성능 회귀 검사 실패: "+rule);}
        static InventoryModel Model()
        {
            var s=new InventorySave{gold=1000};s.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=8});
            s.containers.Add(new ContainerRecord{containerId="display_1",columns=6,rows=4});s.containers.Add(new ContainerRecord{containerId="display_2",columns=6,rows=4});
            return new InventoryModel(InventoryModelChecks.Fixture(),s);
        }
        public static int Run()
        {
            count=0;var model=Model();string error;long paid;int delivered;
            for(int n=0;n<10;n++)model.TryOrderNextDawn(1,"C001","common",100,0,out error);
            model.TryBeginDawn(2,0,out paid,out delivered,out error);
            model.TryOpenParcel(model.Parcels()[0].parcelId,.2f,.6f,.84f,.8f,out error);
            var settings=new FloorPhysicsSettings();var output=new List<FloorMotionState>();
            model.AdvanceFloorMotion(0,settings,null,output);
            Check(model.ActiveFloorBodies==1000&&output.Count==1000,"실제 선 위1000개를 활성화하고 최초 화면 위치 전달");
            int frames=0,totalSteps=0;long revision=model.Revision;var moving=Stopwatch.StartNew();
            while(model.ActiveFloorBodies>0&&frames<1500)
            {model.AdvanceFloorMotion(.016f,settings,null,output);totalSteps+=model.LastFloorSteps;frames++;}
            moving.Stop();Check(model.ActiveFloorBodies==0&&totalSteps>1000,"실제 비행 후 전체 수면 및 실제 계산 수행");
            var snapshot=model.Snapshot();int capacity=output.Capacity;int gc=GC.CollectionCount(0);var idle=Stopwatch.StartNew();
            for(int n=0;n<100000;n++)model.AdvanceFloorMotion(.016f,settings,null,output);
            idle.Stop();int idleGc=GC.CollectionCount(0)-gc;
            Check(model.LastFloorSteps==0&&output.Count==0&&output.Capacity==capacity&&model.Revision==revision,"정지1000개는 물리/출력/재고 갱신 없음");
            Check(model.FloorItems()[0].normalizedY==snapshot.floorItems[0].normalizedY,"수면 중 위치 유지");
            model.TryAdd("warehouse","C001","common",1,80,out error);string uid=model.GetContainer("warehouse").items[0].instanceId;
            model.TryDropToFloor("warehouse",uid,0,.5f,.8f,model.Revision,out error);
            model.AdvanceFloorMotion(.016f,settings,null,output);
            Check(model.ActiveFloorBodies==1&&model.LastFloorSteps==1&&output.Count==1001,"새 낱개만 깨우고 구조 변경 최초 위치 동기화");
            model.AdvanceFloorMotion(.016f,settings,null,output);
            Check(output.Count==1&&output[0].id==uid&&model.LastFloorSteps==1,"다음 프레임에는 실제 이동한 낱개만 전달");
            float heldY=model.FloorItems()[1000].normalizedY;
            model.AdvanceFloorMotion(.1f,settings,uid,output);model.AdvanceFloorMotion(.1f,settings,uid,output);
            Check(model.LastFloorSteps==0&&output.Count==0&&model.FloorItems()[1000].normalizedY==heldY,"잡고 있는 낱개 계산 정지");
            model.AdvanceFloorMotion(.016f,settings,null,output);
            Check(model.LastFloorSteps>0&&model.FloorItems()[1000].normalizedY<heldY,"놓으면 캐시를 깨워 다시 낙하");
            model.TryRemove("floor",uid,out error);model.AdvanceFloorMotion(.016f,settings,null,output);
            Check(model.ActiveFloorBodies==0&&model.LastFloorSteps==0&&model.GetItem("floor",uid)==null,"제거된 활성 참조가 남지 않음");
            settings.surfaceY=.1f;model.AdvanceFloorMotion(.016f,settings,null,output);
            Check(model.ActiveFloorBodies==1000&&model.LastFloorSteps==1000,"선을 낮추면 기존 정지 물건을 깨움");
            var restored=Model();Check(restored.TryRestore(model.Snapshot(),out error),"운동 캐시와 무관한 저장 복원");
            restored.AdvanceFloorMotion(.016f,settings,null,output);Check(restored.ActiveFloorBodies==1000,"저장 복원 후 운동 캐시 재구성");

            var clock=new ShopClock(new ShopClockSettings{dawnMinutes=3,dayMinutes=4,nightMinutes=5},new ShopClockState{phase=ShopPhase.Dawn});
            var transitions=new List<ShopClockState>(4);var random=new Random(125);double total=0;
            for(int i=0;i<50000;i++)
            {
                double delta=random.NextDouble()*.2;total+=delta;clock.AdvanceInto(delta,transitions);
                if(i%100!=0)continue;double within=total%12;
                ShopPhase phase=within<3?ShopPhase.Dawn:within<7?ShopPhase.Day:ShopPhase.Night;
                double elapsed=within-(phase==ShopPhase.Day?3:phase==ShopPhase.Night?7:0);
                Check(clock.Day==(int)(total/12)+1&&clock.Phase==phase&&Math.Abs(clock.ElapsedMinutes-elapsed)<.0000001,"할당 없는 시계도 누적 시간/단계/날짜 보존");
            }
            clock=new ShopClock(new ShopClockSettings(),new ShopClockState());clock.AdvanceInto(.001,transitions);gc=GC.CollectionCount(0);var ticks=Stopwatch.StartNew();
            for(int i=0;i<100000;i++)clock.AdvanceInto(.001,transitions);
            ticks.Stop();int clockGc=GC.CollectionCount(0)-gc;
            Check(clock.Day==1&&clock.Phase==ShopPhase.Day&&Math.Abs(clock.ElapsedMinutes-100.001)<.000001&&transitions.Count==0,"일반 시계 진행의 실제 누적 시간 보존");
            Report="Moving 1000 bodies: "+frames+" frames, "+totalSteps+" body steps, "+moving.ElapsedMilliseconds+" ms. Idle 1000 bodies x100000 calls: "+idle.ElapsedMilliseconds+" ms, body steps=0, output=0, Gen0="+idleGc+". Clock x100000 calls: "+ticks.ElapsedMilliseconds+" ms, Gen0="+clockGc+". Local Mono only; excludes Unity rendering/UI.";
            return count;
        }
    }
}
