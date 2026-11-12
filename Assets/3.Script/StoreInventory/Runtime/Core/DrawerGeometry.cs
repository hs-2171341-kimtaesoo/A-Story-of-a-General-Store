using System;
namespace GeneralStore.Inventory
{
    // 아래로 내려갈수록 음수인 top-left 좌표를 사용합니다.
    public static class DrawerGeometry
    {
        public static float ClosedY(float screen,float peek){return -screen+peek;}
        public static bool ShouldClose(float y,float height,float screen,float hidden){return screen+y<=height*(1-hidden);}
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
