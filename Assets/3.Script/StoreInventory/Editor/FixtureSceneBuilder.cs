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
    public static void BuildManagement(Transform parent,DisplayManagementUI ui,ContainerTabView unused){SideMenuSceneBuilder.BuildManagement(parent,ui);}
    public static void BuildUpgrades(Transform parent,UpgradeUI ui){SideMenuSceneBuilder.BuildUpgrades(parent,ui);}
}
#endif
