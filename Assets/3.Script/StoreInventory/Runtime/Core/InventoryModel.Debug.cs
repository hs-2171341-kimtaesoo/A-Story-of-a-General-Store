using System;
namespace GeneralStore.Inventory
{
    public sealed partial class InventoryModel
    {
        // 데모 테스트 버튼 전용. 재고/진열/예약 상태는 그대로 유지합니다.
        public bool TryAddDebugGold(long amount,out string error)
        {
            error="";if(amount<=0){error="추가 금액은 1 이상이어야 합니다.";return false;}
            long next;try{next=checked(state.gold+amount);}catch(OverflowException){error="소지금 한도를 초과했습니다.";return false;}
            var trial=state.Copy();trial.gold=next;Commit(trial);return true;
        }
    }
}
