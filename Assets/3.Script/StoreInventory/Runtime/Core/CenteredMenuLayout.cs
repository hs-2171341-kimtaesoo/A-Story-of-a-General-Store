using System;
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
