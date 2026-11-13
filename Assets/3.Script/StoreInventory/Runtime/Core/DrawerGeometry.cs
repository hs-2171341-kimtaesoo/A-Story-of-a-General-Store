using System;
namespace GeneralStore.Inventory
{
    // 아래로 내려갈수록 음수인 top-left 좌표를 사용합니다.
    public static class DrawerGeometry
    {
        public static float ClosedY(float screen,float peek){return -screen+peek;}
        public static bool ShouldClose(float y,float height,float screen,float hidden){return screen+y<=height*(1-hidden);}
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
