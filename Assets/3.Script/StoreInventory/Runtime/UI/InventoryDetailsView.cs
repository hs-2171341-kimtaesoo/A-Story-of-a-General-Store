using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 보관함/진열장 각 창의 상세 UI에 같은 선택 정보를 표시합니다.
    public sealed class InventoryDetailsView : MonoBehaviour
    {
        public string containerFilter;public bool compactDisplay;string localSelection;
        public global::UnityEngine.UI.Image icon;
        public Text nameLabel,bodyLabel;
        public global::UnityEngine.UI.Image iconBackground;
        public SpriteIconFit iconFit;
        public void Render(InventoryPanel panel,ItemRecord item)
        {
            if(!string.IsNullOrEmpty(containerFilter)){if(panel.SelectedContainerId==containerFilter)localSelection=panel.SelectedInstanceId;item=panel.Model.GetItem(containerFilter,localSelection);}
            icon.sprite=null;icon.enabled=false;nameLabel.text="물건을 선택하세요.";bodyLabel.text="";
            if(item==null)return;QualityDefinition quality=panel.Catalog.FindQuality(item.itemId,item.quality);
            var sprite=panel.FindIcon(quality.iconKey);
            if(iconFit!=null)iconFit.SetSprite(sprite);else{icon.sprite=sprite;icon.enabled=sprite!=null;icon.preserveAspect=true;}
            nameLabel.text=quality.name;
            int asking;
            string listing=InventoryModel.IsDisplay(string.IsNullOrEmpty(containerFilter)?panel.SelectedContainerId:containerFilter) && panel.Model.TryGetOfferPrice(string.IsNullOrEmpty(containerFilter)?panel.SelectedContainerId:containerFilter,item.instanceId,out asking)
                ? " / 진열가 "+asking+" G" : "";
            if(compactDisplay){bodyLabel.fontSize=16;bodyLabel.text="품질 "+item.quality+" · "+panel.Model.Width(item)+"×"+panel.Model.Height(item)+listing+"\n"+quality.explanation;return;}
            bodyLabel.text="품질 "+item.quality+" / "+panel.Model.Width(item)+"×"+panel.Model.Height(item)
                +" / 점유 "+panel.Catalog.Shape(item.itemId).CellCount+"칸\n매입가 "+item.purchasePrice
                +" G / 기준가 "+panel.Catalog.ReferencePrice(item.itemId,item.quality)+" G"+listing+"\n"+quality.explanation;
        }
    }
}
