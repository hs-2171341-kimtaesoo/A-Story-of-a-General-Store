using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    // 0~1 화면 좌표에서 계산하는 가벼운 낙하입니다. Rigidbody/Collider 오브젝트를 만들지 않습니다.
    public sealed class FloorPhysicsSettings
    {
        public float gravity=1.8f,surfaceY=.2f,radiusX=.02f,radiusY=.03f;
        public float restitution=.18f,sleepSpeed=.04f;
        static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
        public void Validate()
        {
            if(!Finite(gravity)||!Finite(surfaceY)||!Finite(radiusX)||!Finite(radiusY)||!Finite(restitution)||!Finite(sleepSpeed))
                throw new ArgumentException("낙하 수치 오류입니다.");
            if(gravity<=0||gravity>100||surfaceY<0||surfaceY>.9f||radiusX<=0||radiusX>.2f||radiusY<=0||radiusY>.2f
                ||surfaceY+2*radiusY>=1||restitution<0||restitution>.8f||sleepSpeed<=0)throw new ArgumentException("낙하 범위 오류입니다.");
        }
    }
    public struct FloorMotionState
    {
        public string id;public float x,y;
        public FloorMotionState(string id,float x,float y){this.id=id;this.x=x;this.y=y;}
    }
    public static class FloorPhysics
    {
        static float Clamp(float v,float low,float high){return Math.Max(low,Math.Min(high,v));}
        public static void Step(FloorItemRecord f,FloorPhysicsSettings s,float dt)
        {
            float bottom=s.surfaceY+s.radiusY,top=1-s.radiusY;
            f.normalizedX=Clamp(f.normalizedX,s.radiusX,1-s.radiusX);
            f.normalizedY=Math.Min(f.normalizedY,top);
            // 처음부터 선 아래에 놓인 물건은 그 높이를 유지한 채 정지합니다.
            // 위에서 낙하해 선에 닿는 처리와 구분하며 위로 위치 보정하지 않습니다.
            if(f.normalizedY<bottom){f.airborne=false;f.velocityX=f.velocityY=0;return;}
            if(!f.airborne)
            {
                if(f.normalizedY<=bottom+.00001f){f.velocityX=f.velocityY=0;return;}
                f.airborne=true; // 선을 낮췄을 때 위에 남은 물건은 자연스럽게 내려옵니다.
            }
            f.normalizedX+=f.velocityX*dt;
            if(f.normalizedX<s.radiusX||f.normalizedX>1-s.radiusX)
            {f.normalizedX=Clamp(f.normalizedX,s.radiusX,1-s.radiusX);f.velocityX=-f.velocityX*.3f;}
            float remaining=dt;
            // 프레임 끝의 침투 위치를 단순 보정하면 미세 튕김이 계속될 수 있습니다.
            // 충돌 시각을 이차식으로 구하고 남은 시간도 계산하여 에너지가 늘어나지 않게 합니다.
            for(int collision=0;collision<32&&remaining>0;collision++)
            {
                float end=f.normalizedY+f.velocityY*remaining-.5f*s.gravity*remaining*remaining;
                if(end>=bottom&&end<=top)
                {f.normalizedY=end;f.velocityY-=s.gravity*remaining;return;}
                if(end>top)
                {
                    float discriminant=Math.Max(0,f.velocityY*f.velocityY-2*s.gravity*(top-f.normalizedY));
                    float hit=Clamp((f.velocityY-(float)Math.Sqrt(discriminant))/s.gravity,0,remaining);
                    f.normalizedY=top;f.velocityY=-Math.Abs(f.velocityY-s.gravity*hit)*.2f;remaining-=hit;
                }
                else
                {
                    float hit=Clamp((f.velocityY+(float)Math.Sqrt(f.velocityY*f.velocityY+2*s.gravity*(f.normalizedY-bottom)))/s.gravity,0,remaining);
                    f.normalizedY=bottom;f.velocityY=Math.Abs(f.velocityY-s.gravity*hit)*s.restitution;f.velocityX*=.6f;remaining-=hit;
                    if(f.velocityY<=s.sleepSpeed){f.airborne=false;f.velocityX=f.velocityY=0;return;}
                }
            }
            if(remaining>0){f.normalizedY=bottom;f.airborne=false;f.velocityX=f.velocityY=0;}

        }
    }
    public sealed partial class InventoryModel
    {
        static bool FiniteVelocity(float v){return !float.IsNaN(v)&&!float.IsInfinity(v)&&Math.Abs(v)<=100;}
        // UI는 내용물 전체 복사 대신 개수/ID만 읽습니다. 대량 합배송 시 불필요한 복사 방지.
        public List<ParcelSummary> ParcelSummaries()
        {var list=new List<ParcelSummary>(state.parcels.Count);foreach(var p in state.parcels)list.Add(new ParcelSummary(p.parcelId,p.items.Count));return list;}
        public List<ParcelRecord> Parcels()
        {var result=new List<ParcelRecord>();foreach(var p in state.parcels)result.Add(p.Copy());return result;}
        public bool TryOpenParcel(string parcelId,float horizontalSpeed,float upwardSpeed,out string error)
        {
            var p=state.parcels.Find(b=>b.parcelId==parcelId);
            return TryOpenParcel(parcelId,horizontalSpeed,upwardSpeed,p==null?0:p.normalizedY,out error);
        }
        public bool TryOpenParcel(string parcelId,float horizontalSpeed,float upwardSpeed,float spawnY,out string error)
        {
            var p=state.parcels.Find(b=>b.parcelId==parcelId);
            return TryOpenParcel(parcelId,horizontalSpeed,upwardSpeed,p==null?0:p.normalizedX,spawnY,out error);
        }
        public bool TryOpenParcel(string parcelId,float horizontalSpeed,float upwardSpeed,float spawnX,float spawnY,out string error)
        {
            error="";var parcel=state.parcels.Find(p=>p.parcelId==parcelId);
            if(parcel==null||!Normalized(spawnX)||!Normalized(spawnY)||!FiniteVelocity(horizontalSpeed)||!FiniteVelocity(upwardSpeed)||horizontalSpeed<0||upwardSpeed<0)
            {error="이미 열린 상자이거나 방출 수치 오류입니다.";return false;}
            if(state.floorItems.Count+parcel.items.Count>4096){error="바닥 물건 한도입니다. 물건을 정리한 뒤 여세요.";return false;}
            var trial=state.Copy();var box=trial.parcels.Find(p=>p.parcelId==parcelId);
            for(int n=0;n<box.items.Count;n++)
            {
                float spread=box.items.Count==1?0:(n/(float)(box.items.Count-1)*2-1);
                trial.floorItems.Add(new FloorItemRecord{item=box.items[n],normalizedX=spawnX,normalizedY=spawnY,
                    velocityX=spread*horizontalSpeed,velocityY=upwardSpeed*(.9f+(n%3)*.1f),airborne=true});
            }
            trial.parcels.Remove(box);Commit(trial);return true;
        }
        readonly List<FloorItemRecord> activeFloor=new List<FloorItemRecord>();
        readonly FloorPhysicsSettings previousFloorSettings=new FloorPhysicsSettings();
        long floorCacheRevision=-1;string previousHeldFloorId;
        public int ActiveFloorBodies {get{return activeFloor.Count;}}
        public int LastFloorSteps {get;private set;}
        // 출력은 이번 호출에서 위치가 갱신된 물건만 포함합니다. 모두 정지한 다음에는 빈 목록입니다.
        // 구조/설정이 바뀐 첫 호출만 전체 위치를 보내 새 UI와 저장 복원을 맞춥니다.
        public void AdvanceFloorMotion(float seconds,FloorPhysicsSettings settings,string heldId,List<FloorMotionState> output)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0||seconds>1||output==null||settings==null)throw new ArgumentException("낙하 시간 오류입니다.");
            settings.Validate();output.Clear();LastFloorSteps=0;
            bool full=previousHeldFloorId!=heldId||floorCacheRevision!=Revision||settings.gravity!=previousFloorSettings.gravity||settings.surfaceY!=previousFloorSettings.surfaceY
                ||settings.radiusX!=previousFloorSettings.radiusX||settings.radiusY!=previousFloorSettings.radiusY
                ||settings.restitution!=previousFloorSettings.restitution||settings.sleepSpeed!=previousFloorSettings.sleepSpeed;
            if(full)
            {
                activeFloor.Clear();floorCacheRevision=Revision;previousHeldFloorId=heldId;
                previousFloorSettings.gravity=settings.gravity;previousFloorSettings.surfaceY=settings.surfaceY;
                previousFloorSettings.radiusX=settings.radiusX;previousFloorSettings.radiusY=settings.radiusY;
                previousFloorSettings.restitution=settings.restitution;previousFloorSettings.sleepSpeed=settings.sleepSpeed;
                foreach(var f in state.floorItems)
                {
                    if(f.item.instanceId!=heldId)FloorPhysics.Step(f,settings,0);
                    if(f.airborne)activeFloor.Add(f);
                }
            }
            if(activeFloor.Count>0&&seconds>0)
            {
                int steps=Math.Max(1,(int)Math.Ceiling(seconds/.016f));float dt=seconds/steps;
                for(int i=activeFloor.Count-1;i>=0;i--)
                {
                    var f=activeFloor[i];if(f.item.instanceId==heldId)continue;
                    float x=f.normalizedX,y=f.normalizedY;
                    for(int n=0;n<steps;n++){FloorPhysics.Step(f,settings,dt);LastFloorSteps++;if(!f.airborne)break;}
                    if(!full&&(f.normalizedX!=x||f.normalizedY!=y))output.Add(new FloorMotionState(f.item.instanceId,f.normalizedX,f.normalizedY));
                    if(!f.airborne)
                    {int last=activeFloor.Count-1;activeFloor[i]=activeFloor[last];activeFloor.RemoveAt(last);}
                }
            }
            if(full)foreach(var f in state.floorItems)output.Add(new FloorMotionState(f.item.instanceId,f.normalizedX,f.normalizedY));
        }

    }
}
