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
        [Min(0)] public int tax=10,wages=20,rent=30;
        public string testDeliveryItemId="P001",testDeliveryQuality="common";
        [Range(1,100)] public int testDeliveryAmount=2;
        [Min(0)] public int testDeliveryUnitCost=80;
        public InputField debugItemIdInput,debugItemAmountInput;
        public Text debugItemQualityLabel;
        public string debugItemId="R001",debugItemQuality="common";
        [Range(1,100)]public int debugItemAmount=1;
        string debugQualityItemId;
        public UnityEvent onDayStarted=new UnityEvent(),onNightStarted=new UnityEvent(),onDawnStarted=new UnityEvent();
        public ICustomerVisitPolicy VisitPolicy {get;set;} = new DefaultCustomerVisitPolicy();
        readonly System.Random random=new System.Random();
        double arrivalElapsed;
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
            inventory.Loaded+=ResetTimers;ready=true;ShowClock();RefreshDebugItemQuality();
        }
        void ResetTimers(){arrivalElapsed=0;pendingSeconds=pendingMinutes=0;ShowClock();}
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
            double minutes=pendingMinutes,seconds=pendingSeconds;pendingSeconds=pendingMinutes=0;
            if(minutes>0){using(StoreProfiler.Clock.Auto())AdvanceMinutes(Math.Min(10080,minutes),seconds);}ShowClock();
        }

        void AdvanceMinutes(double minutes,double realSeconds=0)
        {
            // 경계를 하나씩 처리합니다. 정산 실패 시 경계 전 상태로 돌아가 시간을 멈춥니다.
            try{inventory.Clock.Settings.Validate();inventory.Clock.ValidateCurrent();}
            catch(ArgumentException e){timeRunning=false;shop.panel.ShowMessage(e.Message+" 단계 길이는 재생 전에 설정하세요.");return;}
            double totalMinutes=minutes;
            while(minutes>0)
            {
                ShopPhase previousPhase=inventory.Clock.Phase;
                int previousDay=inventory.Clock.Day;
                double remaining=inventory.Clock.Settings.Duration(previousPhase)-inventory.Clock.ElapsedMinutes;
                double step=Math.Min(minutes,remaining);
                double workSeconds=totalMinutes>0?realSeconds*(step/totalMinutes):0;
                var before=step>=remaining?inventory.Clock.Snapshot():null;
                inventory.Clock.AdvanceInto(step,transitions);minutes-=step;
                if(transitions.Count==0)inventory.Model.AdvanceWorkshopsDuring(step,workSeconds,previousPhase,previousDay);
                foreach(var next in transitions)
                {
                    if(next.phase==ShopPhase.Dawn)
                    {
                        DawnSettlement settlement;string error;
                        if(!inventory.Model.TryBeginDawn(next.day,(long)Math.Max(0,tax)+Math.Max(0,wages)+Math.Max(0,rent),inventory.deliveryCellFee,out settlement,out error))
                        {inventory.Clock.Restore(before);timeRunning=false;shop.panel.ShowMessage(error+" 시간을 멈췄습니다.");return;}
                        inventory.Model.AdvanceWorkshopsDuring(step,workSeconds,previousPhase,previousDay);
                        shop.panel.ShowMessage("새벽 정산 -"+settlement.paid+" G · 공방 일급/추가금 "+settlement.workshopCost+" G · 보관료 "+settlement.occupiedCells+"칸 × "+inventory.deliveryCellFee+" = "+settlement.storageCost+" G · 배송 "+settlement.delivered+"개"+(settlement.addedDebt>0?" · 미납 +"+settlement.addedDebt+" G":""));inventory.Model.RefreshSuppliersForDay(next.day,out error);onDawnStarted.Invoke();
                    }
                    else {inventory.Model.AdvanceWorkshopsDuring(step,workSeconds,previousPhase,previousDay);if(next.phase==ShopPhase.Night)onNightStarted.Invoke();else onDayStarted.Invoke();}
                    arrivalElapsed=0;
                }
                // 새 단계 경계에서 바로 새 손님이 나타나지 않도록 해당 단계의 누적 시간을 사용합니다.
                if(transitions.Count==0)TickActivities(step,previousPhase);
            }
            inventory.Model.SetWorkshopTime(inventory.Clock.Phase,inventory.Clock.Day);
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
        }
        public void TryVisit(bool showFailure)
        {
            if(!ready)return;
            if(shop.panel.IsWindowDragging||shop.panel.drag.IsDragging||shop.HasPendingPlacement)return;
            if(inventory.Sales.CustomerPresent){if(showFailure)shop.panel.ShowMessage("이미 손님을 응대 중입니다.");return;}
            var profiles=inventory.Customers.All();var clock=inventory.Clock.Snapshot();
            profiles.RemoveAll(c=>inventory.Clock.HasVisited(c.id)||!VisitPolicy.CanVisit(c,clock));
            if(profiles.Count==0){if(showFailure)shop.panel.ShowMessage("방문 가능한 손님이 없습니다.");return;}
            // Visit eligibility is independent of stock; purchase selection happens after browsing.
            var guest=profiles[random.Next(profiles.Count)];
            int visit=inventory.Clock.NextVisit(guest.id);
            if(shop.EnterProfile(guest,showFailure)){
                inventory.Clock.MarkVisit(guest.id);
                inventory.Sales.ConfigureVisit(visit,random.Next(3),random.NextDouble());
            }
        }
        public void ToggleTime(){if(ready)FlushClock();timeRunning=!timeRunning;}
        public void AdvanceToNextPhase()
        {if(ready){FlushClock();AdvanceMinutes(inventory.Clock.Settings.Duration(inventory.Clock.Phase)-inventory.Clock.ElapsedMinutes);ShowClock();}}
        [Min(1)]public int debugGoldAmount=10000;
        public void AddDebugGold()
        {
            if(!ready)return;
            if(shop.panel.drag.IsDragging||shop.panel.IsWindowDragging||shop.HasPendingPlacement){shop.panel.ShowMessage("물건 이동과 가격 설정을 마친 뒤 돈을 추가하세요.");return;}
            string error;bool ok=inventory.Model.TryAddDebugGold(debugGoldAmount,out error);
            shop.panel.ShowMessage(ok?"테스트 소지금 +"+debugGoldAmount+" G":error);
        }
        public void QueueTestDelivery()
        {
            if(!ready)return;string error;
            bool success=inventory.Model.TryOrderNextDawn(inventory.Clock.Snapshot().day,testDeliveryItemId,testDeliveryQuality,testDeliveryAmount,testDeliveryUnitCost,out error);
            shop.panel.ShowMessage(success?"매입비 차감 완료. 다음 날 새벽에 택배 상자가 도착합니다.":error);
        }
        ItemDefinition DebugDefinition()
        {
            string id=(debugItemIdInput!=null?debugItemIdInput.text:debugItemId)??"";id=id.Trim();
            var definition=inventory.Catalog.Find(id);
            if(definition==null)foreach(var item in inventory.Catalog.AllDefinitions())
                if(string.Equals(item.id,id,StringComparison.OrdinalIgnoreCase)){definition=item;break;}
            return definition;
        }
        void RefreshDebugItemQuality()
        {
            if(!ready)return;var definition=DebugDefinition();
            if(definition==null){if(debugItemQualityLabel!=null)debugItemQualityLabel.text="품질: ID 확인";return;}
            if(debugQualityItemId!=definition.id||inventory.Catalog.FindQuality(definition.id,debugItemQuality)==null)
            {debugQualityItemId=definition.id;debugItemQuality=definition.qualities[0].quality;}
            if(debugItemQualityLabel!=null)debugItemQualityLabel.text=debugItemQuality=="none"?"품질 없음":"품질: "+QualityLabels.Title(debugItemQuality);
        }
        public void DebugItemIdEdited(string ignored){RefreshDebugItemQuality();}
        public void CycleDebugItemQuality()
        {
            if(!ready)return;RefreshDebugItemQuality();var definition=DebugDefinition();if(definition==null)return;
            int index=Array.FindIndex(definition.qualities,quality=>quality.quality==debugItemQuality);
            debugItemQuality=definition.qualities[(index+1)%definition.qualities.Length].quality;RefreshDebugItemQuality();
        }
        [ContextMenu("Give Debug Item By ID")]
        public void GrantDebugItem()
        {
            if(!ready)return;
            if(shop.panel.drag.IsDragging||shop.panel.IsWindowDragging||shop.HasPendingPlacement||(shop.counterTrade!=null&&shop.counterTrade.IsItemDragging))
            {shop.panel.ShowMessage("물건 이동과 가격 설정을 마친 뒤 아이템을 받으세요.");return;}
            RefreshDebugItemQuality();var definition=DebugDefinition();
            if(definition==null){shop.panel.ShowMessage("아이템 ID를 확인하세요.");return;}
            int amount=debugItemAmount;
            if(debugItemAmountInput!=null&&!int.TryParse(debugItemAmountInput.text,out amount)){shop.panel.ShowMessage("수량을 정수로 입력하세요.");return;}
            int stored,delivered;string error;
            bool granted=inventory.Model.TryGrantDebugItems(definition.id,debugItemQuality,amount,out stored,out delivered,out error);
            if(granted){debugItemId=definition.id;debugItemAmount=amount;}
            shop.panel.ShowMessage(granted?"테스트 지급: "+definition.displayName+" ×"+amount+" · 상자 "+stored+"개 / 임시 저장고 "+delivered+"개":error);
        }
        void ShowClock()
        {
            if(!ready)return;int day=inventory.Clock.Day;ShopPhase phaseNow=inventory.Clock.Phase;double elapsed=inventory.Clock.ElapsedMinutes;double duration=inventory.Clock.Settings.Duration(phaseNow);
            string phase=phaseNow==ShopPhase.Day?"낮":phaseNow==ShopPhase.Night?"밤":"새벽";
            int baseMinute=phaseNow==ShopPhase.Day?540:phaseNow==ShopPhase.Night?1080:360;
            int displayedSpan=phaseNow==ShopPhase.Day?540:phaseNow==ShopPhase.Night?720:180;
            int minute=(baseMinute+(int)(elapsed/duration*displayedSpan))%1440;
            int remaining=(int)Math.Ceiling((duration-elapsed)/duration*displayedSpan);
            if(shownDay==day&&shownPhase==phaseNow&&shownMinute==minute&&shownRemaining==remaining)return;
            shownDay=day;shownPhase=phaseNow;shownMinute=minute;shownRemaining=remaining;
            clockLabel.text=day+"일 / "+phase+" "+(minute/60).ToString("00")+":"+(minute%60).ToString("00")+(showRemainingTime?" / 남은 "+remaining+"분":"");
        }
        void OnDestroy(){if(ready&&inventory!=null)inventory.Loaded-=ResetTimers;}
    }
}
