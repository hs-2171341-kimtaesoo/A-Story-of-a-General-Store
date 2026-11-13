using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class SupplierListRowView:MonoBehaviour
    {
        public global::UnityEngine.UI.Image portrait;public global::UnityEngine.UI.Image[] sampleIcons;
        public Text nameLabel,descriptionLabel,statusLabel;public Button selectButton,tradeButton;
        SupplierUI owner;string id;
        public void Bind(SupplierUI ui,SupplierDefinition def,SupplierState state)
        {
            if(owner!=ui||id!=def.id)
            {
                owner=ui;id=def.id;selectButton.onClick.RemoveAllListeners();selectButton.onClick.AddListener(()=>{owner.SelectSupplierForTest(id);owner.OpenTrade(id);});
                if(tradeButton!=selectButton){tradeButton.onClick.RemoveAllListeners();tradeButton.onClick.AddListener(()=>owner.OpenTrade(id));}
            }
            portrait.sprite=ui.FindPortrait(def.portraitKey);portrait.enabled=portrait.sprite!=null;portrait.preserveAspect=true;
            nameLabel.text=def.name;descriptionLabel.text=def.description;statusLabel.text=state.blocked?"거래 불가: "+state.blockReason:"거래 가능 · 물품 "+state.stocks.Count+"종";
            selectButton.interactable=!state.blocked;tradeButton.interactable=!state.blocked;
            if(sampleIcons!=null)for(int n=0;n<sampleIcons.Length;n++)
            {
                var icon=sampleIcons[n];icon.enabled=false;if(n>=def.offers.Length)continue;var offer=def.offers[n];
                icon.sprite=ui.panel.FindIcon(ui.panel.Catalog.FindQuality(offer.itemId,offer.quality).iconKey);icon.enabled=icon.sprite!=null;icon.preserveAspect=true;
            }
        }
    }
}
