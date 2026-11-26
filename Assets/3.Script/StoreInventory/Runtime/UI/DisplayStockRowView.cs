using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class DisplayStockRowView : MonoBehaviour
    {
        public global::UnityEngine.UI.Image icon;public Text nameLabel,qualityLabel,countLabel;
        public bool parchmentStyle;
        public void Bind(InventoryPanel panel,DisplayItemGroup group)
        {
            if(panel.SharedFont!=null)nameLabel.font=qualityLabel.font=countLabel.font=panel.SharedFont;
            var q=panel.Catalog.FindQuality(group.itemId,group.quality);icon.sprite=panel.FindItemIcon(group.itemId);panel.ApplyQuality(icon,group.quality);
            icon.enabled=icon.sprite!=null;icon.preserveAspect=true;nameLabel.text=q.name;qualityLabel.text=QualityLabels.Title(group.quality);
            qualityLabel.color=parchmentStyle?PaperQualityColor(group.quality):QualityVisuals.Color(group.quality);
            countLabel.text=parchmentStyle?(group.count>1?"× "+group.count:""):group.count+"개";
        }
        static Color PaperQualityColor(string quality)
        {
            switch(quality)
            {
                case "superior":return new Color(.16f,.36f,.12f);
                case "perfect":return new Color(.12f,.28f,.51f);
                case "masterpiece":return new Color(.4f,.18f,.46f);
                case "legendary":return new Color(.62f,.25f,.04f);
                default:return new Color(.25f,.1f,.035f);
            }
        }
    }
}
