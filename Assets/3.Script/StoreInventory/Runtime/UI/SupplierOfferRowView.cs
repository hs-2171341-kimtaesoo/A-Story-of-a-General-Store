using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class SupplierOfferRowView:MonoBehaviour
    {
        public global::UnityEngine.UI.Image icon;public Text nameLabel,priceLabel,stockLabel,quantityLabel,subtotalLabel;public Button minusButton,plusButton;
        SupplierUI owner;string id;
        public void Bind(SupplierUI ui,SupplierOfferDefinition offer,SupplierStock stock,int quantity,bool canTrade)
        {
            if(owner!=ui||id!=offer.offerId)
            {
                owner=ui;id=offer.offerId;minusButton.onClick.RemoveAllListeners();minusButton.onClick.AddListener(()=>owner.ChangeQuantity(id,-1));
                plusButton.onClick.RemoveAllListeners();plusButton.onClick.AddListener(()=>owner.ChangeQuantity(id,1));
            }
            var q=ui.panel.Catalog.FindQuality(offer.itemId,offer.quality);icon.sprite=ui.panel.FindIcon(q.iconKey);icon.enabled=icon.sprite!=null;icon.preserveAspect=true;
            nameLabel.text=q.name;priceLabel.text="단가 "+stock.unitPrice+" G";stockLabel.text="재고 "+stock.remaining+"개";quantityLabel.text=quantity.ToString();
            if(subtotalLabel!=null)subtotalLabel.text=((long)quantity*stock.unitPrice)+" G";
            minusButton.interactable=canTrade&&quantity>0;plusButton.interactable=canTrade&&quantity<stock.remaining;
        }
    }
}
