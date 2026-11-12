using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace GeneralStore.Inventory.Verification
{
    public static class BalancedFeatureChecks
    {
        static int count;
        public static string Benchmark {get;private set;}
        static void Check(bool yes,string rule){count++;if(!yes)throw new Exception("조정 검사 실패: "+rule);}
        static InventoryModel Model()
        {
            var s=new InventorySave{gold=1000};s.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=8});
            s.containers.Add(new ContainerRecord{containerId="display_1",columns=6,rows=4});
            s.containers.Add(new ContainerRecord{containerId="display_2",columns=6,rows=4});return new InventoryModel(InventoryModelChecks.Fixture(),s);
        }
        static ItemRecord Item(){return new ItemRecord{instanceId=Guid.NewGuid().ToString("N"),itemId="C001",quality="common",purchasePrice=80};}
        public static int Run()
        {
            count=0;var settings=new FloorPhysicsSettings{surfaceY=.1f};
            Check(new FloorPhysicsSettings().surfaceY==.2f,"기본 낙하선은 아래20퍼센트");
            foreach(float y in new[]{0f,.03f,.08f,.12f})
            {
                var f=new FloorItemRecord{normalizedX=.5f,normalizedY=y,velocityX=.2f,velocityY=1,airborne=true};
                for(int n=0;n<600;n++)FloorPhysics.Step(f,settings,.016f);
                Check(f.normalizedY==y&&!f.airborne&&f.velocityX==0&&f.velocityY==0,"선 아래 물건은 위로 보정하지 않고 정지");
            }
            var above=new FloorItemRecord{normalizedX=.5f,normalizedY=.28f,airborne=false};
            FloorPhysics.Step(above,settings,.016f);
            Check(above.normalizedY<.28f&&above.normalizedY>.13f&&above.airborne,"선을 낮추면 위 물건은 순간 이동 없이 낙하");
            for(int n=0;n<600;n++)FloorPhysics.Step(above,settings,.016f);
            Check(Math.Abs(above.normalizedY-.13f)<.00001f&&!above.airborne,"위 물건은 새 낙하선에서 정지");
            settings.surfaceY=.25f;FloorPhysics.Step(above,settings,.016f);
            Check(Math.Abs(above.normalizedY-.13f)<.00001f,"선을 다시 올려도 아래 물건을 밀어 올리지 않음");
            settings=new FloorPhysicsSettings();
            var m=Model();string error;long paid;int delivered;
            Check(m.TryOrderNextDawn(1,"C001","common",3,80,out error),"첫 주문");
            Check(m.TryOrderNextDawn(1,"C001","superior",2,110,out error),"다른 품질 추가 주문");
            var expected=new HashSet<string>();foreach(var d in m.Snapshot().deliveries)foreach(var i in d.items)expected.Add(i.instanceId);
            Check(m.TryOrderNextDawn(2,"E001","none",4,0,out error),"다른 도착일 주문");
            Check(m.TryBeginDawn(2,0,out paid,out delivered,out error)&&delivered==5,"같은 새벽 도착 주문만 처리");
            Check(m.Parcels().Count==1&&m.ParcelSummaries()[0].count==5&&m.Snapshot().deliveries.Count==1,"여러 주문 합배송 및 미래 주문 유지");
            string id=m.Parcels()[0].parcelId;
            Check(m.Parcels()[0].normalizedX>.5f,"상자 위치 오른쪽");
            foreach(var i in m.Parcels()[0].items)Check(expected.Contains(i.instanceId),"합배송 낱개 UID 보존");
            Check(m.TryBeginDawn(3,0,out paid,out delivered,out error)&&delivered==4,"다음 날짜 배송 도착");
            Check(m.Parcels().Count==1&&m.Parcels()[0].parcelId==id&&m.ParcelSummaries()[0].count==9,"미개봉 상자에 후속 배송 누적");
            var restored=Model();Check(restored.TryRestore(m.Snapshot(),out error)&&restored.ParcelSummaries()[0].count==9,"합배송 저장 복원");
            Check(m.TryOpenParcel(id,.2f,.6f,.84f,.15f,out error)&&m.FloorItems().Count==9,"합배송 한 번에 개봉");
            Check(!m.TryOpenParcel(id,.2f,.6f,out error)&&m.FloorItems().Count==9,"합배송 이중 개봉 차단");
            Check(m.Gold==540,"합배송해도 주문 비용 추가 차감 없음");
            m.TryOrderNextDawn(3,"C001","common",1,0,out error);m.TryBeginDawn(4,0,out paid,out delivered,out error);
            Check(m.Parcels().Count==1&&m.ParcelSummaries()[0].count==1&&m.FloorItems().Count==9,"개봉 후 새 배송은 새 상자");
            // 기존 v5의 여러 상자는 읽을 때 데이터 유실 없이 한 개로 합칩니다.
            var old=Model().Snapshot();
            for(int box=0;box<3;box++)
            {
                var p=new ParcelRecord{parcelId="old_"+box,normalizedX=.2f,normalizedY=.3f};p.items.Add(Item());p.items.Add(Item());old.parcels.Add(p);
            }
            Check(restored.TryRestore(old,out error)&&restored.Parcels().Count==1&&restored.ParcelSummaries()[0].count==6,"기존 다중 상자 불러오기 자동 합산");
            Check(restored.Parcels()[0].normalizedX==.84f,"이전 왼쪽 상자도 오른쪽 기본 위치로 변경");
            m=Model();for(int n=0;n<10;n++)Check(m.TryOrderNextDawn(1,"C001","common",100,0,out error),"대량 합배송 주문");
            Check(m.TryBeginDawn(2,0,out paid,out delivered,out error)&&delivered==1000&&m.Parcels().Count==1,"열 주문 천 개도 상자 하나");
            Check(restored.TryRestore(m.Snapshot(),out error)&&restored.ParcelSummaries()[0].count==1000,"100개 초과 합배송 저장 지원");
            id=m.ParcelSummaries()[0].id;
            Check(m.TryOpenParcel(id,.2f,.6f,.84f,.15f,out error)&&m.FloorItems().Count==1000,"천 개 동시 개봉 시 낱개 개수 보존");
            var output=new List<FloorMotionState>();m.AdvanceFloorMotion(.016f,settings,null,output);int capacity=output.Capacity;
            int collections=GC.CollectionCount(0);long revision=m.Revision;var watch=Stopwatch.StartNew();
            for(int frame=0;frame<3000;frame++)m.AdvanceFloorMotion(.016f,settings,null,output);
            watch.Stop();
            Check(output.Count==0&&output.Capacity==capacity&&m.Revision==revision,"대량 개봉 후 출력 버퍼/재고 버전 유지");
            Benchmark="1000 items x 3000 simulation frames: "+watch.ElapsedMilliseconds+" ms; Gen0 collections: "+(GC.CollectionCount(0)-collections)+" (local Mono only, no Unity UI).";
            // 한도 초과는 내용물/돈/예약을 그대로 둔 채 전체 처리를 거절합니다.
            var full=Model().Snapshot();var parcel=new ParcelRecord{parcelId="full",normalizedX=.84f,normalizedY=.15f};
            for(int n=0;n<4096;n++)parcel.items.Add(Item());full.parcels.Add(parcel);
            Check(restored.TryRestore(full,out error),"합배송 한도까지 저장 허용");
            restored.TryOrderNextDawn(1,"C001","common",1,0,out error);revision=restored.Revision;
            Check(!restored.TryBeginDawn(2,60,out paid,out delivered,out error)&&restored.Gold==1000&&restored.Revision==revision&&restored.Snapshot().deliveries.Count==1,"합배송 한도 초과 시 정산/주문 보존");
            return count;
        }
    }
}
