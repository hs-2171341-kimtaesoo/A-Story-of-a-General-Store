using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GeneralStore.Inventory
{
    [Serializable] public sealed class ItemProposalEvent : UnityEvent<ItemProposal> { }
    // 원본 UID만 추천 목록으로 전달합니다. 수용 가격과 최종 판매는 세션에서 검증합니다.
    public sealed class RecommendationDropArea : MonoBehaviour, IDropHandler
    {
        public InventoryPanel panel;public CounterTradeUI counter;
        public ItemProposalEvent onProposed = new ItemProposalEvent();
        public void OnDrop(PointerEventData e)
        {
            ItemProposal proposal;
            if (panel == null || !panel.drag.TryGetProposal(out proposal)) return;
            panel.drag.Cancel(false);
            if (!panel.shop.Recommend(proposal.containerId, proposal.instanceId)) return;
            if(counter!=null)counter.StageDropLocation(proposal.instanceId,e.position);
            // 확정 시 instanceId가 여전히 존재하는지 다시 검증해야 합니다. 카운터 표시 중 예약은 세션이 관리합니다.
            onProposed.Invoke(proposal);
        }
    }
}
