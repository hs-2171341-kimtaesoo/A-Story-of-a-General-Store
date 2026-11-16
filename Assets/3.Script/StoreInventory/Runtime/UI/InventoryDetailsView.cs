using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 보관함/진열장 각 창의 상세 UI에 같은 선택 정보를 표시합니다.
    public sealed class InventoryDetailsView : MonoBehaviour
    {
        [Min(1)]public float preferredIconSize=96;
        [Min(0)]public float iconFramePadding=12,iconTextGap=12;
        public string containerFilter;public bool compactDisplay;
        public global::UnityEngine.UI.Image icon;
        public Text nameLabel,bodyLabel;
        public Text qualitySizeLabel,priceLabel; // 선택 연결: Play 중 Inspector 위치를 덮어쓰지 않습니다.
        public global::UnityEngine.UI.Image iconBackground;
        public SpriteIconFit iconFit;
        // 명시적으로 호출할 때만 정렬합니다. Render/크기 변경 콜백은 위치를 쓰지 않습니다.
        // 모눈 아이템의 크기와 별개인 상세 정보용 네모칸입니다.
        public void AlignIconFrame()
        {
            if(iconBackground==null||nameLabel==null||bodyLabel==null)return;
            var area=(RectTransform)transform;if(area.rect.width<=0||area.rect.height<=0)return;
            var p=DetailsFrameGeometry.Calculate(area.rect.width,area.rect.height,preferredIconSize,iconFramePadding,iconTextGap);
            var rect=iconBackground.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.localRotation=Quaternion.Euler(0,0,0);rect.sizeDelta=new Vector2(p.side,p.side);rect.anchoredPosition=new Vector2(p.x,-p.y);
            var nameRect=nameLabel.rectTransform;nameRect.anchoredPosition=new Vector2(p.textLeft,nameRect.anchoredPosition.y);nameRect.sizeDelta=new Vector2(p.textWidth,nameRect.sizeDelta.y);
            var bodyRect=bodyLabel.rectTransform;bodyRect.anchoredPosition=new Vector2(p.textLeft,bodyRect.anchoredPosition.y);bodyRect.sizeDelta=new Vector2(p.textWidth,bodyRect.sizeDelta.y);
        }
        void OnRectTransformDimensionsChange()
        {
            if(iconFit!=null&&icon!=null&&icon.sprite!=null)iconFit.SetSprite(icon.sprite);
        }
        public void Render(InventoryPanel panel,ItemRecord item)
        {
            if(!string.IsNullOrEmpty(containerFilter))item=panel.Model.GetItem(containerFilter,panel.Selection.Get(containerFilter));
            icon.sprite=null;icon.enabled=false;nameLabel.text="물건을 선택하세요.";bodyLabel.text="";
            if(qualitySizeLabel!=null)qualitySizeLabel.text="";if(priceLabel!=null)priceLabel.text="";
            if(item==null)return;QualityDefinition quality=panel.Catalog.FindQuality(item.itemId,item.quality);
            var sprite=panel.FindIcon(quality.iconKey);
            if(iconFit!=null)iconFit.SetSprite(sprite);else{icon.sprite=sprite;icon.enabled=sprite!=null;icon.preserveAspect=true;}
            nameLabel.text=quality.name;
            int asking;
            string listing=InventoryModel.IsDisplay(string.IsNullOrEmpty(containerFilter)?panel.SelectedContainerId:containerFilter) && panel.Model.TryGetOfferPrice(string.IsNullOrEmpty(containerFilter)?panel.SelectedContainerId:containerFilter,item.instanceId,out asking)
                ? " / 진열가 "+asking+" G" : "";
            // 독립 라벨을 연결하면 참고 이미지처럼 항목을 분리합니다.
            if(qualitySizeLabel!=null&&priceLabel!=null)
            {
                qualitySizeLabel.text=QualityLabels.Title(item.quality)+" · "+panel.Model.Width(item)+"×"+panel.Model.Height(item);
                int price=panel.Catalog.ReferencePrice(item.itemId,item.quality);
                string id=string.IsNullOrEmpty(containerFilter)?panel.SelectedContainerId:containerFilter;int offer;
                if(InventoryModel.IsDisplay(id)&&panel.Model.TryGetOfferPrice(id,item.instanceId,out offer))price=offer;
                priceLabel.text="판매가 "+price+" G";bodyLabel.text=quality.explanation;return;
            }
            if(compactDisplay){bodyLabel.text="품질 "+QualityLabels.Title(item.quality)+" · "+panel.Model.Width(item)+"×"+panel.Model.Height(item)+listing+"\n"+quality.explanation;return;}
            bodyLabel.text="품질 "+QualityLabels.Title(item.quality)+" / "+panel.Model.Width(item)+"×"+panel.Model.Height(item)
                +" / 점유 "+panel.Catalog.Shape(item.itemId).CellCount+"칸\n매입가 "+item.purchasePrice
                +" G / 기준가 "+panel.Catalog.ReferencePrice(item.itemId,item.quality)+" G"+listing+"\n"+quality.explanation;
        }
    }
}
