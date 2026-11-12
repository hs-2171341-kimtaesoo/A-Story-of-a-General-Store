using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class SupplierListRowView : MonoBehaviour
    {
        public global::UnityEngine.UI.Image portrait;
        public Text nameLabel,descriptionLabel,statusLabel;
        public Button selectButton,tradeButton;
        public void Bind(SupplierUI ui,SupplierDefinition def,SupplierState state)
        {
            portrait.sprite=ui.FindPortrait(def.portraitKey);portrait.enabled=portrait.sprite!=null;portrait.preserveAspect=true;
            nameLabel.text=def.name;descriptionLabel.text=def.description;
            statusLabel.text=state.blocked?"거래 불가: "+state.blockReason:"거래 가능 · 물품 "+state.stocks.Count+"종";
            selectButton.onClick.RemoveAllListeners();selectButton.onClick.AddListener(()=>ui.SelectSupplierForTest(def.id));
            tradeButton.onClick.RemoveAllListeners();tradeButton.onClick.AddListener(()=>ui.OpenTrade(def.id));tradeButton.interactable=!state.blocked;
        }
    }
}
