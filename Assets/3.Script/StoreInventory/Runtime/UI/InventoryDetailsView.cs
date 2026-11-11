using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 보관함/진열장 각 창의 상세 UI에 같은 선택 정보를 표시합니다.
    public sealed class InventoryDetailsView : MonoBehaviour
    {
        public global::UnityEngine.UI.Image icon;
        public Text nameLabel,bodyLabel;
        public void Render(InventoryPanel panel,ItemRecord item)
        {
            icon.sprite=null;icon.enabled=false;nameLabel.text="물건을 선택하세요.";bodyLabel.text="";
            if(item==null)return;QualityDefinition quality=panel.Catalog.FindQuality(item.itemId,item.quality);
            icon.sprite=panel.FindIcon(quality.iconKey);icon.enabled=icon.sprite!=null;icon.preserveAspect=true;
            nameLabel.text=quality.name;
            int asking;
            string listing=InventoryModel.IsDisplay(panel.SelectedContainerId) && panel.Model.TryGetOfferPrice(panel.SelectedContainerId,item.instanceId,out asking)
                ? " / 진열가 "+asking+" G" : "";
            bodyLabel.text="품질 "+item.quality+" / "+panel.Model.Width(item)+"×"+panel.Model.Height(item)
                +" / 점유 "+panel.Catalog.Shape(item.itemId).CellCount+"칸\n매입가 "+item.purchasePrice
                +" G / 기준가 "+panel.Catalog.ReferencePrice(item.itemId,item.quality)+" G"+listing+"\n"+quality.explanation;
        }
    }
}
