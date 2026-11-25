using System;

// Consolidated from DrawerGeometry.cs
namespace GeneralStore.Inventory
{
    // 아래로 내려갈수록 음수인 top-left 좌표를 사용합니다.
    public static class DrawerGeometry
    {
        public static float ClosedY(float screen,float peek){return -screen+peek;}
        public static bool ShouldClose(float y,float height,float screen,float hidden)
        {return (double)height-screen-y+.001 >= (double)height*hidden;}
        // 양옆과 위는 화면 안. 아래는 서랍을 닫기 위해 화면 밖으로 나갈 수 있습니다.
        public static WindowPoint ClampFree(float x,float y,float width,float screenWidth,float screenHeight,float peek)
        {
            if(float.IsNaN(x)||float.IsNaN(y)||float.IsInfinity(x)||float.IsInfinity(y)||width<=0||screenWidth<=0||screenHeight<=0||peek<=0)
                throw new ArgumentException("서랍 이동 범위 오류입니다.");
            return new WindowPoint(Math.Max(0,Math.Min(Math.Max(0,screenWidth-width),x)),Math.Max(ClosedY(screenHeight,Math.Min(peek,screenHeight)),Math.Min(0,y)));
        }
        public static float Step(float current,float target,float speed,float delta)
        {if(delta<=0)return current;float next=target+(current-target)*(float)Math.Exp(-speed*delta);return Math.Abs(next-target)<.25f?target:next;}
    }
    // 타이머는 탭마다 만들지 않고 드래그 처리기 하나에서 사용합니다.
    public sealed class HoverSwitchTimer
    {
        string current;float elapsed;bool fired;
        public bool Tick(string target,float delta,float delay)
        {
            if(string.IsNullOrEmpty(target)){Reset();return false;}
            if(current!=target){current=target;elapsed=0;fired=false;}
            if(fired)return false;elapsed+=Math.Max(0,delta);
            if(elapsed<Math.Max(.05f,delay))return false;fired=true;return true;
        }
        public void Reset(){current=null;elapsed=0;fired=false;}
    }
}

// Consolidated from WindowDragGeometry.cs
namespace GeneralStore.Inventory
{
    public struct WindowPoint {public float x,y;public WindowPoint(float x,float y){this.x=x;this.y=y;}}
    public static class WindowDragGeometry
    {
        // 부모 왼쪽 위 원점: x는 오른쪽+, y는 아래쪽-. UI 배율이 반영된 로컬 좌표만 받습니다.
        public static WindowPoint Clamp(float x,float y,float windowWidth,float windowHeight,float boundsWidth,float boundsHeight)
        {
            if(float.IsNaN(x)||float.IsNaN(y)||float.IsInfinity(x)||float.IsInfinity(y)||windowWidth<=0||windowHeight<=0||boundsWidth<=0||boundsHeight<=0)
                throw new ArgumentException("창 이동 범위 오류입니다.");
            return new WindowPoint(Math.Max(0,Math.Min(Math.Max(0,boundsWidth-windowWidth),x)),Math.Max(-Math.Max(0,boundsHeight-windowHeight),Math.Min(0,y)));
        }
    }
}

// Consolidated from SceneLayerGeometry.cs
namespace GeneralStore.Inventory
{
    public static class SceneLayerOrder
    {public const int Background=0,Furniture=10,Counter=20,UI=30;}
    [Serializable]public sealed class PackedHitMaskData{public int width,height;public string bits;}
    // 알파 픽셀은 제작할 때만 읽습니다. 실행 중에는 작은 비트 마스크를 조회합니다.
    public sealed class PackedHitMask
    {
        readonly byte[] bits;readonly int width,height;
        public PackedHitMask(PackedHitMaskData data)
        {
            if(data==null||data.width<1||data.height<1||data.width>512||data.height>512||string.IsNullOrEmpty(data.bits))throw new ArgumentException("카운터 입력 마스크 오류입니다.");
            width=data.width;height=data.height;bits=Convert.FromBase64String(data.bits);
            if(bits.Length!=(width*height+7)/8)throw new ArgumentException("카운터 입력 마스크 크기 오류입니다.");
        }
        public bool Contains(float x,float y)
        {
            if(float.IsNaN(x)||float.IsNaN(y)||x<0||y<0||x>=1||y>=1)return false;
            int index=(int)(y*height)*width+(int)(x*width);return (bits[index>>3]&(1<<(index&7)))!=0;
        }
    }
    public struct OutlinePlacement{public float x,y,width,height;}
    public static class FurnitureOutlineGeometry
    {
        public static OutlinePlacement Align(float sourceWidth,float sourceHeight,float frameWidth,float frameHeight,float[] sourceBounds,float[] outlineBounds,float padding)
        {
            if(sourceWidth<=0||sourceHeight<=0||frameWidth<=0||frameHeight<=0||!Valid(sourceBounds)||!Valid(outlineBounds))throw new ArgumentException("외곽선 맞춤 경계 오류입니다.");
            float scale=Math.Min(frameWidth/sourceWidth,frameHeight/sourceHeight);
            float left=(frameWidth-sourceWidth*scale)*.5f+sourceBounds[0]*sourceWidth*scale-padding;
            float top=(frameHeight-sourceHeight*scale)*.5f+sourceBounds[1]*sourceHeight*scale-padding;
            float w=((sourceBounds[2]-sourceBounds[0])*sourceWidth*scale+2*padding)/(outlineBounds[2]-outlineBounds[0]);
            float h=((sourceBounds[3]-sourceBounds[1])*sourceHeight*scale+2*padding)/(outlineBounds[3]-outlineBounds[1]);
            return new OutlinePlacement{x=left-outlineBounds[0]*w,y=top-outlineBounds[1]*h,width=w,height=h};
        }
        static bool Valid(float[] b){return b!=null&&b.Length==4&&b[0]>=0&&b[1]>=0&&b[2]<=1&&b[3]<=1&&b[0]<b[2]&&b[1]<b[3];}
    }
}

// Consolidated from CenteredMenuLayout.cs
namespace GeneralStore.Inventory
{
    public struct CenteredMenuGeometry{public float contentHeight,viewportHeight,top;public bool scrolls;}
    public static class CenteredMenuLayout
    {
        // 버튼 뒤에 불필요한 마지막 간격을 더하지 않아 짝수/홀수 개 모두 정확히 중앙에 옵니다.
        public static CenteredMenuGeometry Calculate(int count,float buttonHeight,float gap,float screenHeight,float margin,float maximumHeight)
        {
            if(count<0||buttonHeight<=0||gap<0||screenHeight<=0||margin<0||maximumHeight<=0)throw new ArgumentException("메뉴 배치 수치 오류입니다.");
            float total=count==0?0:count*buttonHeight+(count-1)*gap;
            float available=Math.Min(maximumHeight,Math.Max(1,screenHeight-2*margin));
            float view=Math.Min(total,available);
            return new CenteredMenuGeometry{contentHeight=total,viewportHeight=view,top=(screenHeight-view)*.5f,scrolls=total>view};
        }
    }
    public sealed class DragInputLease
    {
        object owner;public bool Active{get{return owner!=null;}}
        public bool IsOwner(object candidate){return owner!=null&&ReferenceEquals(owner,candidate);}
        public bool Acquire(object candidate){if(candidate==null)return false;if(owner!=null)return IsOwner(candidate);owner=candidate;return true;}
        public bool Release(object candidate){if(!IsOwner(candidate))return false;owner=null;return true;}
        public void Reset(){owner=null;}
    }
}
