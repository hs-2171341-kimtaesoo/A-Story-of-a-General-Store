#if UNITY_EDITOR
using UnityEngine;
using GeneralStore.Inventory;
public static class Fixed35SceneBuilder
{
    // 설명창은 에디터에서 최초 생성할 때만 배치합니다. Play 중에는 이 코드를 호출하지 않습니다.
    public static void Templates(DisplayUI ui,DisplayWindowView original,RectTransform parent,DisplayCatalog definitions)
    {
        var ids=definitions.TypeIds();ui.typeTemplates=new DisplayWindowTemplateEntry[ids.Count];
        for(int n=0;n<ids.Count;n++)
        {
            string id=ids[n];var view=n==0?original:Object.Instantiate(original,parent);view.gameObject.name="Display Template "+id;
            var def=definitions.Type(id);
            // 큰 진열장은 정사각형 칸을 함께 줄여 내부 스크롤 없이 전체 층을 보여 줍니다.
            view.grid.cellWidth=Mathf.Min(78,Mathf.Min(622f/def.columns,518f/def.rows));view.grid.cellHeight=view.grid.cellWidth;
            view.appearance.Apply(id,def.columns,def.rows);
            float width=view.appearance.window.rect.width,height=view.appearance.window.rect.height;
            var detail=(RectTransform)view.details.transform;detail.anchoredPosition=new Vector2(20,-height+166);detail.sizeDelta=new Vector2(width-40,100);
            view.details.nameLabel.rectTransform.anchoredPosition=new Vector2(112,-8);view.details.nameLabel.rectTransform.sizeDelta=new Vector2(Mathf.Max(80,width-360),32);
            view.details.bodyLabel.rectTransform.anchoredPosition=new Vector2(112,-44);view.details.bodyLabel.rectTransform.sizeDelta=new Vector2(Mathf.Max(80,width-360),40);
            view.details.qualitySizeLabel.rectTransform.anchoredPosition=new Vector2(width-250,-8);view.details.qualitySizeLabel.rectTransform.sizeDelta=new Vector2(190,28);
            view.details.priceLabel.rectTransform.anchoredPosition=new Vector2(width-270,-66);view.details.priceLabel.rectTransform.sizeDelta=new Vector2(210,28);
            view.details.iconBackground.rectTransform.anchoredPosition=new Vector2(10,-6);view.details.iconBackground.rectTransform.sizeDelta=new Vector2(88,88);
            view.details.bodyLabel.fontSize=16;view.gameObject.SetActive(false);ui.typeTemplates[n]=new DisplayWindowTemplateEntry{typeId=id,template=view};
        }
    }
}
#endif
