using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    public enum ShopPhase { Dawn,Day,Night }
    [Serializable] public sealed class ShopClockState
    {
        public int day=1;
        public ShopPhase phase=ShopPhase.Day;
        public double elapsedMinutes;
        public ShopClockState Copy(){return (ShopClockState)MemberwiseClone();}
    }
    [Serializable] public sealed class ShopClockSettings
    {
        public double dawnMinutes=180,dayMinutes=540,nightMinutes=720;
        static bool Valid(double v){return !double.IsNaN(v)&&!double.IsInfinity(v)&&v>=1&&v<=1440;}
        public double Duration(ShopPhase phase){return phase==ShopPhase.Dawn?dawnMinutes:phase==ShopPhase.Day?dayMinutes:nightMinutes;}
        public void Validate()
        {
            if(!Valid(dawnMinutes)||!Valid(dayMinutes)||!Valid(nightMinutes))throw new ArgumentException("단계 길이는 1~1440 게임 분입니다.");
        }
    }
    // 시간은 재고 Revision과 별개입니다. 매 프레임 시간이 흘러도 드래그가 취소되지 않습니다.
    public sealed class ShopClock
    {
        ShopClockState state;
        public readonly ShopClockSettings Settings;
        public ShopClock(ShopClockSettings settings,ShopClockState initial)
        {settings.Validate();Settings=settings;Restore(initial);}
        public int Day {get{return state.day;}}
        public ShopPhase Phase {get{return state.phase;}}
        public double ElapsedMinutes {get{return state.elapsedMinutes;}}
        public ShopClockState Snapshot(){return state.Copy();}
        public void Validate(ShopClockState value)
        {
            if(value==null||value.day<1||value.day>1000000||(int)value.phase<0||(int)value.phase>2
                ||double.IsNaN(value.elapsedMinutes)||double.IsInfinity(value.elapsedMinutes)||value.elapsedMinutes<0
                ||value.elapsedMinutes>=Settings.Duration(value.phase))throw new ArgumentException("저장된 시간 정보 오류입니다.");
        }
        public void ValidateCurrent(){Validate(state);}
        public void Restore(ShopClockState value){Validate(value);state=value.Copy();}
        // 경계 직전까지 흐르게 하여, 호출자가 단계마다 지출/배송을 순서대로 처리할 수 있게 합니다.
        public List<ShopClockState> Advance(double minutes)
        {var output=new List<ShopClockState>();AdvanceInto(minutes,output);return output;}
        // 보통의 시간 진행은 상태 복사본/새 List를 만들지 않습니다. 전환이 생길 때만 이벤트 기록을 만듭니다.
        public void AdvanceInto(double minutes,List<ShopClockState> transitions)
        {
            if(double.IsNaN(minutes)||double.IsInfinity(minutes)||minutes<0||minutes>10080||transitions==null)throw new ArgumentException("시간 증가량 오류입니다.");
            Settings.Validate();Validate(state);int day=state.day;ShopPhase phase=state.phase;double elapsed=state.elapsedMinutes;
            transitions.Clear();
            while(minutes>0)
            {
                double remaining=Settings.Duration(phase)-elapsed;
                if(minutes<remaining){elapsed+=minutes;break;}
                minutes-=remaining;elapsed=0;
                if(phase==ShopPhase.Night){if(day>=1000000)throw new ArgumentException("날짜 범위 초과");day++;phase=ShopPhase.Dawn;}
                else phase=phase==ShopPhase.Dawn?ShopPhase.Day:ShopPhase.Night;
                transitions.Add(new ShopClockState{day=day,phase=phase,elapsedMinutes=0});
            }
            state.day=day;state.phase=phase;state.elapsedMinutes=elapsed;
        }

    }
    [Serializable] public sealed class ShopWorldSave
    {
        public string format="general-store-world";
        public int version=4;
        public InventorySave inventory;
        public ShopClockState clock;
    }
}
