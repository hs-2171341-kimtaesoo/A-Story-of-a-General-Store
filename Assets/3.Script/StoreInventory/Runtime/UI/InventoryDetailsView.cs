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
        public global::UnityEngine.UI.Image iconBackground;
        public SpriteIconFit iconFit;
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
            AlignIconFrame();
            if(iconFit!=null&&icon!=null&&icon.sprite!=null)iconFit.SetSprite(icon.sprite);
        }
        public void Render(InventoryPanel panel,ItemRecord item)
        {
            AlignIconFrame();
            if(!string.IsNullOrEmpty(containerFilter))item=panel.Model.GetItem(containerFilter,panel.Selection.Get(containerFilter));
            icon.sprite=null;icon.enabled=false;nameLabel.text="물건을 선택하세요.";bodyLabel.text="";
            if(item==null)return;QualityDefinition quality=panel.Catalog.FindQuality(item.itemId,item.quality);
            var sprite=panel.FindIcon(quality.iconKey);
            if(iconFit!=null)iconFit.SetSprite(sprite);else{icon.sprite=sprite;icon.enabled=sprite!=null;icon.preserveAspect=true;}
            nameLabel.text=quality.name;
            int asking;
            string listing=InventoryModel.IsDisplay(string.IsNullOrEmpty(containerFilter)?panel.SelectedContainerId:containerFilter) && panel.Model.TryGetOfferPrice(string.IsNullOrEmpty(containerFilter)?panel.SelectedContainerId:containerFilter,item.instanceId,out asking)
                ? " / 진열가 "+asking+" G" : "";
            if(compactDisplay){bodyLabel.fontSize=16;bodyLabel.text="품질 "+QualityLabels.Title(item.quality)+" · "+panel.Model.Width(item)+"×"+panel.Model.Height(item)+listing+"\n"+quality.explanation;return;}
            bodyLabel.text="품질 "+QualityLabels.Title(item.quality)+" / "+panel.Model.Width(item)+"×"+panel.Model.Height(item)
                +" / 점유 "+panel.Catalog.Shape(item.itemId).CellCount+"칸\n매입가 "+item.purchasePrice
                +" G / 기준가 "+panel.Catalog.ReferencePrice(item.itemId,item.quality)+" G"+listing+"\n"+quality.explanation;
        }
    }
}
