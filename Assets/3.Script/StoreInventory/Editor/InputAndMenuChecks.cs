using System;
using GeneralStore.Inventory;
namespace GeneralStore.Inventory.Verification
{
    public static class InputAndMenuChecks
    {
        static int count;static void Check(bool value,string rule){count++;if(!value)throw new Exception("입력/중앙 메뉴 검사: "+rule);}
        public static int Run()
        {
            count=0;var four=CenteredMenuLayout.Calculate(4,86,8,1080,100,700);
            Check(four.contentHeight==368&&four.top==356&&!four.scrolls,"4개는 마지막 간격을 빼고 중앙");
            float second=four.top+94+43,third=four.top+188+43;Check((second+third)*.5f==540,"4개는2번/3번 사이가 중앙");
            var five=CenteredMenuLayout.Calculate(5,86,8,1080,100,700);Check(five.top+2*94+43==540,"5개는3번이 중앙");
            var seven=CenteredMenuLayout.Calculate(7,86,8,1080,100,700);Check(seven.top+3*94+43==540&&!seven.scrolls,"7개도 가운데4번이 중앙");
            for(int n=0;n<=60;n++)
            {
                var p=CenteredMenuLayout.Calculate(n,86,8,1080,100,700);
                Check(Math.Abs(p.top+p.viewportHeight*.5f-540)<.001f,"개수와 관계없이 표시 영역 중앙");
                Check(p.top>=100&&p.top+p.viewportHeight<=980,"많아져도 위/아래 여백 유지");
                Check(p.scrolls==(p.contentHeight>p.viewportHeight)&&p.viewportHeight<=700,"넘치면 자동 스크롤");
                if(!p.scrolls&&n>0)Check(Math.Abs((p.top+43+p.top+(n-1)*94+43)*.5f-540)<.001f,"전체 버튼 묶음의 중앙");
            }
            var many=CenteredMenuLayout.Calculate(30,86,8,1080,100,700);Check(many.scrolls&&many.viewportHeight==700&&many.contentHeight>700,"30개 이상도 잘리지 않는 스크롤 데이터");
            var small=CenteredMenuLayout.Calculate(8,86,8,600,100,700);Check(small.viewportHeight==400&&small.top==100,"작은 화면은 사용 가능한 높이 우선");
            var lease=new DragInputLease();var a=new object();var b=new object();
            Check(!lease.Acquire(null)&&!lease.Active,"없는 입력 주체 차단");
            Check(lease.Acquire(a)&&lease.IsOwner(a)&&lease.Active,"창 이동 입력 소유");
            Check(!lease.Acquire(b)&&!lease.Release(b)&&lease.IsOwner(a),"다른 창이 입력을 빼앗거나 해제하지 못함");
            Check(lease.Acquire(a)&&lease.Release(a)&&!lease.Active,"같은 창 재진입/해제");
            Check(!lease.Release(a)&&lease.Acquire(b),"반복 해제는 무시하고 다음 드래그 가능");
            lease.Reset();Check(!lease.Active&&!lease.IsOwner(b),"포커스 상실/비활성화 해제");
            return count;
        }
    }
}
