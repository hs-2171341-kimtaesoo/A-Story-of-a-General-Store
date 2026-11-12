using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class SupplierOfferRowView : MonoBehaviour
    {
        public global::UnityEngine.UI.Image icon;
        public Text nameLabel,priceLabel,stockLabel,quantityLabel;
        public Button minusButton,plusButton;
        public void Bind(SupplierUI ui,SupplierOfferDefinition offer,SupplierStock stock,int quantity,bool canTrade)
        {
            var q=ui.panel.Catalog.FindQuality(offer.itemId,offer.quality);icon.sprite=ui.panel.FindIcon(q.iconKey);
            icon.enabled=icon.sprite!=null;icon.preserveAspect=true;nameLabel.text=q.name;
            priceLabel.text=stock.unitPrice+" G";stockLabel.text=stock.remaining.ToString();quantityLabel.text=quantity.ToString();
            minusButton.onClick.RemoveAllListeners();minusButton.onClick.AddListener(()=>ui.ChangeQuantity(offer.offerId,-1));
            plusButton.onClick.RemoveAllListeners();plusButton.onClick.AddListener(()=>ui.ChangeQuantity(offer.offerId,1));
            minusButton.interactable=quantity>0;plusButton.interactable=canTrade&&quantity<stock.remaining;
        }
    }
}
