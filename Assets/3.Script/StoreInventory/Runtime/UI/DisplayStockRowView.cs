using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class DisplayStockRowView : MonoBehaviour
    {
        public global::UnityEngine.UI.Image icon;public Text nameLabel,qualityLabel,countLabel;
        public void Bind(InventoryPanel panel,DisplayItemGroup group)
        {
            var q=panel.Catalog.FindQuality(group.itemId,group.quality);icon.sprite=panel.FindIcon(q.iconKey);
            icon.enabled=icon.sprite!=null;icon.preserveAspect=true;nameLabel.text=q.name;qualityLabel.text=QualityLabels.Title(group.quality);qualityLabel.color=group.quality=="superior"?new Color(.85f,.45f,1):Color.white;countLabel.text=group.count+"개";
        }
    }
}
