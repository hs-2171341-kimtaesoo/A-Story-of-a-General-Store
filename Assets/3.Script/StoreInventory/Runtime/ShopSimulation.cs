using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 시간, 자동 방문, 새벽 정산/배송의 연결부입니다. 스토리 이벤트는 UnityEvent에 연결하세요.
    public sealed class ShopSimulation : MonoBehaviour
    {
        public InventoryRuntime inventory;
        public ShopfrontUI shop;
        public bool showRemainingTime=true;
        public Text clockLabel;
        public bool timeRunning=true,autoCustomers=true,nightCustomers=true;
        [Min(0)] public float gameMinutesPerSecond=1;
        [Min(1)] public float dayArrivalMinutes=30,nightArrivalMinutes=90;
        [Tooltip("테스트 도난입니다. 기본은 꺼져 있습니다. 실제 NPC/평판 판정은 추후 교체하세요.")]
        public bool enableTheft=false;
        [Min(1)] public float theftCheckMinutes=60;
        [Range(0,1)] public float theftChance=.05f;
        [Min(0)] public int tax=10,wages=20,rent=30;
        public string testDeliveryItemId="C001",testDeliveryQuality="common";
        [Range(1,100)] public int testDeliveryAmount=2;
        [Min(0)] public int testDeliveryUnitCost=80;
        public UnityEvent onDayStarted=new UnityEvent(),onNightStarted=new UnityEvent(),onDawnStarted=new UnityEvent();
        public ICustomerVisitPolicy VisitPolicy {get;set;} = new DefaultCustomerVisitPolicy();
        readonly System.Random random=new System.Random();
        double arrivalElapsed,theftElapsed;
        [Range(.05f,.5f)] public float simulationIntervalSeconds=.1f;
        readonly System.Collections.Generic.List<ShopClockState> transitions=new System.Collections.Generic.List<ShopClockState>();
        double pendingSeconds,pendingMinutes;
        bool ready;
        int shownDay=-1,shownMinute=-1,shownRemaining=-1;ShopPhase shownPhase;
        void Start()
        {
            string error;
            if(inventory==null||shop==null||clockLabel==null||!inventory.TryInitialize(out error))
            {Debug.LogError("ShopSimulation 참조/초기화 오류입니다.",this);enabled=false;return;}
            inventory.Loaded+=ResetTimers;ready=true;ShowClock();
        }
        void ResetTimers(){arrivalElapsed=theftElapsed=0;pendingSeconds=pendingMinutes=0;ShowClock();}
        void Update()
        {
            if(!ready)return;
            if(timeRunning&&gameMinutesPerSecond>0)
            {
                pendingSeconds+=Time.deltaTime;pendingMinutes+=(double)Time.deltaTime*gameMinutesPerSecond;
                if(pendingSeconds>=Math.Max(.05,simulationIntervalSeconds))FlushClock();
            }
        }
        void FlushClock()
        {
            double minutes=pendingMinutes;pendingSeconds=pendingMinutes=0;
            if(minutes>0){using(StoreProfiler.Clock.Auto())AdvanceMinutes(Math.Min(10080,minutes));}ShowClock();
        }

        void AdvanceMinutes(double minutes)
        {
            // 경계를 하나씩 처리합니다. 정산 실패 시 경계 전 상태로 돌아가 시간을 멈춥니다.
            try{inventory.Clock.Settings.Validate();inventory.Clock.ValidateCurrent();}
            catch(ArgumentException e){timeRunning=false;shop.panel.ShowMessage(e.Message+" 단계 길이는 재생 전에 설정하세요.");return;}
            while(minutes>0)
            {
                ShopPhase previousPhase=inventory.Clock.Phase;
                double remaining=inventory.Clock.Settings.Duration(previousPhase)-inventory.Clock.ElapsedMinutes;
                double step=Math.Min(minutes,remaining);
                var before=step>=remaining?inventory.Clock.Snapshot():null;
                inventory.Clock.AdvanceInto(step,transitions);minutes-=step;
                foreach(var next in transitions)
                {
                    if(next.phase==ShopPhase.Dawn)
                    {
                        long paid;int delivered;string error;
                        if(!inventory.Model.TryBeginDawn(next.day,(long)Math.Max(0,tax)+Math.Max(0,wages)+Math.Max(0,rent),out paid,out delivered,out error))
                        {inventory.Clock.Restore(before);timeRunning=false;shop.panel.ShowMessage(error+" 시간을 멈췄습니다.");return;}
                        shop.panel.ShowMessage("새벽 정산 -"+paid+" G / 배송 "+delivered+"개가 택배 상자로 도착");inventory.Model.RefreshSuppliersForDay(next.day,out error);onDawnStarted.Invoke();
                    }
                    else if(next.phase==ShopPhase.Night)onNightStarted.Invoke();else onDayStarted.Invoke();
                    arrivalElapsed=0;
                }
                // 새 단계 경계에서 바로 새 손님이 나타나지 않도록 해당 단계의 누적 시간을 사용합니다.
                if(transitions.Count==0)TickActivities(step,previousPhase);
            }
        }
        void TickActivities(double minutes,ShopPhase phase)
        {
            bool allow=phase==ShopPhase.Day||(phase==ShopPhase.Night&&nightCustomers);
            if(autoCustomers&&allow&&!inventory.Sales.CustomerPresent)
            {
                arrivalElapsed+=minutes;double interval=Math.Max(1,phase==ShopPhase.Day?dayArrivalMinutes:nightArrivalMinutes);
                if(arrivalElapsed>=interval){arrivalElapsed=0;TryVisit(false);}
            }
            else arrivalElapsed=0; // 한 손님을 응대한 뒤부터 다음 방문 간격을 잽니다.
            if(enableTheft)
            {
                theftElapsed+=minutes;
                if(theftElapsed>=Math.Max(1,theftCheckMinutes))
                {
                    theftElapsed=0;var floor=inventory.Model.FloorItems();
                    if(floor.Count>0&&random.NextDouble()<theftChance)
                    {string error;var stolen=floor[random.Next(floor.Count)];if(inventory.Model.TryRemove("floor",stolen.item.instanceId,out error))shop.panel.ShowMessage("바닥 물건 한 개가 도난당했습니다.");}
                }
            }
        }
        public void TryVisit(bool showFailure)
        {
            if(!ready)return;if(inventory.Sales.CustomerPresent){if(showFailure)shop.panel.ShowMessage("이미 손님을 응대 중입니다.");return;}
            if(inventory.Model.DisplayListingCount==0){if(showFailure)shop.panel.ShowMessage("먼저 물건을 진열하세요.");return;}
            var profiles=inventory.Customers.All();var clock=inventory.Clock.Snapshot();
            profiles.RemoveAll(c=>!VisitPolicy.CanVisit(c,clock));
            if(profiles.Count==0){if(showFailure)shop.panel.ShowMessage("방문 가능한 손님이 없습니다.");return;}
            // 테스트는 무작위 시작점부터 각 후보를 한 번씩 검사합니다. 선호 상품이 있는 손님을 찾습니다.
            int start=random.Next(profiles.Count);
            for(int n=0;n<profiles.Count;n++)if(shop.EnterProfile(profiles[(start+n)%profiles.Count],false))return;
            if(showFailure)shop.panel.ShowMessage("선호/예산에 맞는 진열 상품이 없어 손님이 돌아갔습니다.");
        }
        public void ToggleTime(){if(ready)FlushClock();timeRunning=!timeRunning;}
        public void AdvanceToNextPhase()
        {if(ready){FlushClock();AdvanceMinutes(inventory.Clock.Settings.Duration(inventory.Clock.Phase)-inventory.Clock.ElapsedMinutes);ShowClock();}}
        public void QueueTestDelivery()
        {
            if(!ready)return;string error;
            bool success=inventory.Model.TryOrderNextDawn(inventory.Clock.Snapshot().day,testDeliveryItemId,testDeliveryQuality,testDeliveryAmount,testDeliveryUnitCost,out error);
            shop.panel.ShowMessage(success?"매입비 차감 완료. 다음 날 새벽에 택배 상자가 도착합니다.":error);
        }
        void ShowClock()
        {
            if(!ready)return;int day=inventory.Clock.Day;ShopPhase phaseNow=inventory.Clock.Phase;double elapsed=inventory.Clock.ElapsedMinutes;double duration=inventory.Clock.Settings.Duration(phaseNow);
            string phase=phaseNow==ShopPhase.Day?"낮":phaseNow==ShopPhase.Night?"밤":"새벽";
            int baseMinute=phaseNow==ShopPhase.Day?540:phaseNow==ShopPhase.Night?1080:360;
            int minute=(baseMinute+(int)elapsed)%1440;
            int remaining=(int)Math.Ceiling(duration-elapsed);
            if(shownDay==day&&shownPhase==phaseNow&&shownMinute==minute&&shownRemaining==remaining)return;
            shownDay=day;shownPhase=phaseNow;shownMinute=minute;shownRemaining=remaining;
            clockLabel.text=day+"일 / "+phase+" "+(minute/60).ToString("00")+":"+(minute%60).ToString("00")+(showRemainingTime?" / 남은 "+remaining+"분":"");
        }
        void OnDestroy(){if(ready&&inventory!=null)inventory.Loaded-=ResetTimers;}
    }
}
