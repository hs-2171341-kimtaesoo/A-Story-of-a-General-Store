#if UNITY_EDITOR
using GeneralStore.Inventory;
using UnityEngine;
using UnityEngine.UI;
using UI=InventorySceneBuilder;
public static class FixtureSceneBuilder
{
    static void Stretch(RectTransform r)
    {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
    public static ContainerTabView TabTemplate(Transform parent)
    {
        var image=UI.Box("Container Tab Template",parent,0,0,190,42,new Color(.22f,.18f,.12f));
        var tab=image.gameObject.AddComponent<ContainerTabView>();tab.background=image;tab.button=image.gameObject.AddComponent<Button>();tab.button.targetGraphic=image;
        tab.label=UI.Label("Title",image.transform,0,0,190,42,"",19);Stretch(tab.label.rectTransform);tab.label.alignment=TextAnchor.MiddleCenter;
        image.gameObject.SetActive(false);return tab;
    }
    public static ContainerTabBar Tabs(Transform parent,ContainerTabView template,float x,float y,float width)
    {
        var viewport=UI.Box("Container Tabs",parent,x,y,width,46,new Color(.09f,.075f,.055f));viewport.gameObject.AddComponent<RectMask2D>();
        var content=UI.Rect("Tabs Content",viewport.transform,0,0,width,42);var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=true;scroll.vertical=false;
        var bar=viewport.gameObject.AddComponent<ContainerTabBar>();bar.content=content;bar.template=template;return bar;
    }
    public static void Header(Transform parent,RectTransform bounds,InventoryPanel panel,float width,string title)
    {
        var header=UI.Box("Window Title Drag Handle",parent,0,0,width-62,62,new Color(.22f,.16f,.09f));
        UI.Label("Title",header.transform,20,12,width-96,42,title,27);
        var drag=header.gameObject.AddComponent<DraggableWindow>();drag.window=(RectTransform)parent;drag.bounds=bounds;drag.panel=panel;
    }
    static RectTransform List(Transform parent,string title,float x,float y,float width,float height)
    {
        var image=UI.Box(title,parent,x,y,width,height,new Color(.1f,.085f,.06f));image.gameObject.AddComponent<RectMask2D>();
        var content=UI.Rect("Content",image.transform,0,0,width,0);var scroll=image.gameObject.AddComponent<ScrollRect>();
        scroll.content=content;scroll.viewport=image.rectTransform;scroll.horizontal=false;scroll.vertical=true;return content;
    }
    static DisplayStockRowView StockTemplate(Transform parent)
    {
        var image=UI.Box("Display Stock Row Template",parent,0,0,820,60,new Color(.18f,.14f,.09f));var row=image.gameObject.AddComponent<DisplayStockRowView>();
        row.icon=UI.Box("Icon",image.transform,6,6,48,48,Color.white);row.icon.raycastTarget=false;
        row.nameLabel=UI.Label("Name",image.transform,68,10,430,42,"",21);
        row.qualityLabel=UI.Label("Quality",image.transform,520,10,160,42,"",21);row.countLabel=UI.Label("Count",image.transform,710,10,100,42,"",21);
        image.gameObject.SetActive(false);return row;
    }
    public static void BuildManagement(Transform parent,DisplayManagementUI ui,ContainerTabView unused){SideMenuSceneBuilder.BuildManagement(parent,ui);}
    public static void BuildUpgrades(Transform parent,UpgradeUI ui){SideMenuSceneBuilder.BuildUpgrades(parent,ui);}
}
#endif
