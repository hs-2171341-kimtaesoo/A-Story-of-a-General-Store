using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GeneralStore.Inventory
{
    [Serializable] public sealed class ItemProposalEvent : UnityEvent<ItemProposal> { }
    // 사진의 추천 영역에 연결할 기초입니다. 손님의 적합성 판정/판매는 아직 없습니다.
    public sealed class RecommendationDropArea : MonoBehaviour, IDropHandler
    {
        public InventoryPanel panel;
        public ItemProposalEvent onProposed = new ItemProposalEvent();
        public void OnDrop(PointerEventData e)
        {
            ItemProposal proposal;
            if (panel == null || !panel.drag.TryGetProposal(out proposal)) return;
            panel.drag.Cancel(false);
            QualityDefinition quality = panel.Catalog.FindQuality(proposal.itemId, proposal.quality);
            panel.ShowMessage("추천 후보 전달: " + quality.name + " 1개 (재고 차감/판매 없음)");
            // 확정 시 instanceId가 여전히 존재하는지 다시 검증해야 합니다. 예약 기능은 아닙니다.
            onProposed.Invoke(proposal);
        }
    }
}
