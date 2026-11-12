using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class OfferRowView : MonoBehaviour
    {
        public global::UnityEngine.UI.Image icon;
        public Text nameLabel,priceLabel;
        public Button removeButton;
        string lastId,lastContainer;int lastPrice;bool lastPicked,lastAvailable,hasBinding;
        public void Bind(ShopfrontUI shop,PendingOffer offer)
        {
            ItemRecord item=shop.panel.Model.GetItem(offer.containerId,offer.instanceId);
            bool available=item!=null;
            if(hasBinding&&lastId==offer.instanceId&&lastContainer==offer.containerId&&lastPrice==offer.price&&lastPicked==offer.customerPicked&&lastAvailable==available)return;
            hasBinding=true;lastId=offer.instanceId;lastContainer=offer.containerId;lastPrice=offer.price;lastPicked=offer.customerPicked;lastAvailable=available;
            QualityDefinition quality=item==null?null:shop.panel.Catalog.FindQuality(item.itemId,item.quality);
            nameLabel.text=quality==null?"이동/소진된 물건":(offer.customerPicked?"[구매 예정] ":"[추천] ")+quality.name;
            priceLabel.text=offer.price+" G";
            icon.sprite=quality==null?null:shop.panel.FindIcon(quality.iconKey);icon.enabled=icon.sprite!=null;icon.preserveAspect=true;
            removeButton.gameObject.SetActive(!offer.customerPicked);
            removeButton.interactable=!offer.customerPicked;
            removeButton.onClick.RemoveAllListeners();removeButton.onClick.AddListener(()=>shop.RemoveOffer(offer.instanceId));
        }
    }
}
