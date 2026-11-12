using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
namespace GeneralStore.Inventory.Verification
{
    public static class CounterFeatureChecks
    {
        static int count;
        public static string Benchmark {get;private set;}
        static void Check(bool value,string rule){count++;if(!value)throw new Exception("카운터 검사 실패: "+rule);}
        static InventoryModel Model()
        {
            var s=new InventorySave{gold=1000};
            s.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=8});
            s.containers.Add(new ContainerRecord{containerId="display_1",columns=6,rows=4});
            s.containers.Add(new ContainerRecord{containerId="display_2",columns=6,rows=4});
            return new InventoryModel(InventoryModelChecks.Fixture(),s);
        }
        public static int Run()
        {
            count=0;var catalog=InventoryModelChecks.Fixture();float cellAspect=1/1.1f;
            foreach(string id in new[]{"C001","sample_rope","sample_lframe","sample_cframe"})
            for(int rotation=0;rotation<4;rotation++)
            foreach(float spriteAspect in new[]{.3f,.7f,1f,1.6f,3f})
            {
                var shape=catalog.Shape(id);var quads=IconMeshGeometry.Build(shape,rotation,true,spriteAspect,cellAspect);
                Check(quads.Count<=shape.CellCount,"직사각형 셀에서 메시 조각 증가 없음");
                foreach(var q in quads)
                {
                    Check(shape.Contains(rotation,(int)Math.Floor((q.x0+q.x1)/2),(int)Math.Floor((q.y0+q.y1)/2)),"직사각형 셀에서도 모양 구멍 유지");
                    foreach(var uv in new[]{q.topLeft,q.topRight,q.bottomRight,q.bottomLeft})
                        Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"직사각형 셀/회전 UV 범위");
                }
                if(id=="C001"||id=="sample_rope")
                {
                    float x0=float.MaxValue,y0=float.MaxValue,x1=float.MinValue,y1=float.MinValue;
                    foreach(var q in quads){x0=Math.Min(x0,q.x0);y0=Math.Min(y0,q.y0);x1=Math.Max(x1,q.x1);y1=Math.Max(y1,q.y1);}
                    float actual=(x1-x0)*78/((y1-y0)*85.8f),expected=rotation%2==0?spriteAspect:1/spriteAspect;
                    Check(Math.Abs(actual-expected)<.0001f,"그림의 실제 픽셀 종횡비 보존");
                }
            }
            foreach(float dt in new[]{.004f,.016f,.033f,.1f})
            foreach(float bounce in new[]{0f,.18f,.6f})
            {
                var settings=new FloorPhysicsSettings{restitution=bounce};settings.Validate();
                var f=new FloorItemRecord{normalizedX=.5f,normalizedY=.8f,velocityX=1.3f,velocityY=.6f,airborne=true};
                for(int n=0;n<(int)(12/dt);n++)
                {
                    FloorPhysics.Step(f,settings,dt);
                    Check(f.normalizedY-settings.radiusY>=settings.surfaceY-.00001f,"어느 프레임에서도 카운터 아래로 관통하지 않음");
                    Check(f.normalizedX>=settings.radiusX&&f.normalizedX<=1-settings.radiusX,"수평 경계 관통 방지");
                    Check(!float.IsNaN(f.normalizedY)&&f.normalizedY<=1-settings.radiusY,"낙하 숫자/상단 경계 정상");
                }
                Check(!f.airborne&&f.velocityX==0&&f.velocityY==0,"낙하 후 수면 상태로 정지 dt="+dt+" bounce="+bounce+" y="+f.normalizedY+" vy="+f.velocityY+" airborne="+f.airborne);
            }
            string error;var model=Model();
            Check(model.TryOrderNextDawn(1,"C001","common",3,80,out error),"택배 주문");
            var ordered=model.Snapshot().deliveries[0];long paid;int delivered;
            Check(model.TryBeginDawn(2,0,out paid,out delivered,out error)&&delivered==3,"다음 새벽 상자 도착");
            Check(model.Parcels().Count==1&&model.FloorItems().Count==0,"개봉 전 물건 UI/바닥 실물 없음");
            var restored=Model();Check(restored.TryRestore(model.Snapshot(),out error)&&restored.Parcels()[0].items.Count==3,"미개봉 상자 저장 복원");
            var leak=restored.Parcels();leak[0].items.Clear();
            Check(restored.Parcels()[0].items.Count==3,"상자 조회 복사본 보호");
            string boxId=model.Parcels()[0].parcelId;
            Check(!model.TryOpenParcel(boxId,float.NaN,.5f,out error)&&model.Parcels().Count==1,"잘못된 방출 값이면 상자 유지");
            Check(model.TryOpenParcel(boxId,.2f,.6f,.3f,out error)&&model.Parcels().Count==0&&model.FloorItems().Count==3,"개봉 시 한 번만 낱개 전환");
            Check(!model.TryOpenParcel(boxId,.2f,.6f,out error)&&model.FloorItems().Count==3,"더블 클릭으로 내용물 복제 차단");
            var ids=new HashSet<string>();foreach(var f in model.FloorItems())ids.Add(f.item.instanceId);
            foreach(var i in ordered.items)Check(ids.Contains(i.instanceId),"주문 때의 낱개 UID 유지");
            var settings2=new FloorPhysicsSettings();var output=new List<FloorMotionState>();
            var floors=model.FloorItems();string held=floors[0].item.instanceId;
            long revision=model.Revision;
            model.AdvanceFloorMotion(.1f,settings2,held,output);
            Check(model.Revision==revision,"낙하가 재고 버전/드래그를 무효화하지 않음");
            floors=model.FloorItems();Check(floors[0].normalizedY==.3f&&floors[0].velocityY==.54f,"잡고 있는 물건은 낙하 일시 정지");
            Check(floors[1].normalizedY>.3f,"상자 내용물이 먼저 위로 방출됨");
            Check(restored.TryRestore(model.Snapshot(),out error)&&restored.FloorItems()[1].velocityY==floors[1].velocityY,"비행 중 위치/속도 저장 복원");
            settings2.gravity=float.NaN;bool threw=false;
            try{model.AdvanceFloorMotion(.1f,settings2,null,output);}catch(ArgumentException){threw=true;}
            Check(threw&&model.Revision==revision,"비정상 낙하 수치 거절");
            var bad=model.Snapshot();bad.floorItems[0].velocityY=float.PositiveInfinity;
            Check(!restored.TryRestore(bad,out error),"비정상 낙하 저장 거절");
            bad=model.Snapshot();bad.parcels.Add(new ParcelRecord{parcelId="dup",normalizedX=.5f,normalizedY=.3f,items=new List<ItemRecord>{bad.floorItems[0].item.Copy()}});
            Check(!restored.TryRestore(bad,out error),"상자와 바닥 사이 UID 중복 거절");
            // 낙하 루프는 동일한 출력 버퍼를 재사용합니다. 시간은 로컬 Mono 수치이지 Unity FPS가 아닙니다.
            model=Model();model.TryOrderNextDawn(1,"C001","common",100,0,out error);model.TryBeginDawn(2,0,out paid,out delivered,out error);
            model.TryOpenParcel(model.Parcels()[0].parcelId,.2f,.6f,out error);settings2=new FloorPhysicsSettings();
            model.AdvanceFloorMotion(.016f,settings2,null,output);int capacity=output.Capacity;revision=model.Revision;
            int collections=GC.CollectionCount(0);var watch=Stopwatch.StartNew();
            for(int frame=0;frame<6000;frame++)model.AdvanceFloorMotion(.016f,settings2,null,output);
            watch.Stop();
            Check(output.Count==0&&output.Capacity==capacity&&model.Revision==revision,"100개 6000프레임 출력 버퍼 크기/재고 버전 유지");
            Benchmark="100 items x 6000 simulation frames: "+watch.ElapsedMilliseconds+" ms; Gen0 collections during loop: "+(GC.CollectionCount(0)-collections)+" (local Mono, no Unity rendering).";
            // 파일 저장도 실제 파일 I/O로 확인합니다. 실패 시 원본을 유지하고 임시 파일을 정리합니다.
            string dir=Path.Combine(Path.GetTempPath(),"store-counter-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try
            {
                string file=Path.Combine(dir,"save.json");AtomicSaveFile.Write(file,"첫 번째");AtomicSaveFile.Write(file,"두 번째");AtomicSaveFile.Write(file,"세 번째");
                Check(File.ReadAllText(file)=="세 번째"&&File.ReadAllText(file+".bak")=="두 번째","저장 교체와 직전 백업 유지");
                File.Delete(file+".bak");Directory.CreateDirectory(file+".bak");threw=false;
                try{AtomicSaveFile.Write(file,"실패해야 하는 저장");}catch(IOException){threw=true;}catch(UnauthorizedAccessException){threw=true;}
                Check(threw&&File.ReadAllText(file)=="세 번째"&&Directory.GetFiles(dir,"*.tmp").Length==0,"저장 실패 시 원본 보존/임시 파일 정리 threw="+threw+" text="+File.ReadAllText(file)+" temps="+Directory.GetFiles(dir,"*.tmp").Length);
            }
            finally{Directory.Delete(dir,true);}
            return count;
        }
    }
}
