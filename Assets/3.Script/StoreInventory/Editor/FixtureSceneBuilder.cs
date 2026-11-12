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
    public static void BuildManagement(Transform parent,DisplayManagementUI management,ContainerTabView tabs)
    {
        var overlay=UI.Box("Display Management Window",parent,0,0,1920,1080,new Color(0,0,0,.3f));management.window=overlay.gameObject;
        UI.Label("Title",overlay.transform,24,124,900,56,"진열장 관리 · 보유 가구를 선택하세요.",28);
        UI.Label("Hint",overlay.transform,24,190,900,48,"가구 구매 메뉴에서 구입하면 바로 추가됩니다.",22);
        management.locationBoard=List(overlay.transform,"Owned Furniture",24,248,920,650);
        var info=UI.Box("Display Status Panel",overlay.transform,1000,132,896,866,new Color(.18f,.14f,.09f));
        UI.Label("Title",info.transform,24,20,848,52,"진열장 관리",32);
        management.displayList=List(info.transform,"Installed Displays",24,88,848,238);
        management.selectedLabel=UI.Label("Selected",info.transform,24,344,848,48,"",27);
        UI.Label("Columns",info.transform,24,400,848,36,"품목                                                품질               수량",21);
        management.stockList=List(info.transform,"Stock Groups",24,444,848,230);
        management.footerLabel=UI.Label("Total",info.transform,24,682,848,36,"",22);
        UI.ButtonBox("창고와 함께 열기",info.transform,24,734,848,52,management.OpenTogether);
        UI.ButtonBox("닫기",info.transform,24,798,848,44,management.Close);
        management.buttonTemplate=tabs;management.stockTemplate=StockTemplate(parent);overlay.gameObject.SetActive(false);
    }
    public static void BuildUpgrades(Transform parent,UpgradeUI upgrade)
    {
        var overlay=UI.Box("Upgrade Window",parent,0,0,1920,1080,new Color(0,0,0,.6f));upgrade.window=overlay.gameObject;
        var box=UI.Box("Upgrade Panel",overlay.transform,510,70,900,940,new Color(.16f,.12f,.075f));
        UI.Label("Title",box.transform,24,18,852,48,"가구 구매 · 상자 업그레이드",31);
        var storage=UI.Box("Storage Upgrade",box.transform,24,84,852,214,new Color(.21f,.16f,.1f));
        UI.Label("Title",storage.transform,24,12,800,40,"상자: 공간 확장 / 추가",25);
        upgrade.storageInfo=UI.Label("Info",storage.transform,24,60,800,44,"",22);
        UI.Label("Sequential Hint",storage.transform,24,118,400,70,"마지막 상자를 모두 확장한 뒤\n다음 상자를 추가합니다.",20);
        upgrade.storageActionButton=UI.ButtonBox("공간 확장",storage.transform,452,126,376,54,upgrade.UpgradeStorage);
        upgrade.storageActionLabel=upgrade.storageActionButton.GetComponentsInChildren<Text>(true)[0];
        var display=UI.Box("Display Upgrade",box.transform,24,324,852,470,new Color(.21f,.16f,.1f));
        UI.Label("Title",display.transform,24,12,800,40,"진열 공간: 추가만 가능",25);
        var catalogue=List(display.transform,"Furniture Catalogue",24,64,804,184);
        var definitions=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(Resources.Load<TextAsset>("StoreInventory/display_fixtures").text));
        int row=0;
        foreach(string id in definitions.TypeIds())
        {
            var type=definitions.Type(id);
            var button=UI.ButtonBox(type.title+"  "+type.columns+"×"+type.rows+"  "+type.purchasePrice+" G",catalogue,0,row*46,782,40,null);
            var entry=button.gameObject.AddComponent<FurnitureCatalogButton>();entry.upgrade=upgrade;entry.typeId=id;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,entry.Select);row++;
        }
        catalogue.sizeDelta=new Vector2(782,row*46);
        upgrade.typeLabel=UI.Label("Selected Type",display.transform,24,260,804,40,"",24);
        upgrade.displayInfo=UI.Label("Info",display.transform,24,308,550,146,"",21);
        upgrade.addDisplayButton=UI.ButtonBox("가구 구매",display.transform,600,376,228,60,upgrade.AddDisplay);
        UI.Label("Test Note",box.transform,24,812,852,40,"가구 가격은 JSON purchasePrice. 상자 확장은 무료 테스트입니다.",20);
        UI.ButtonBox("닫기",box.transform,630,874,246,44,upgrade.Close);overlay.gameObject.SetActive(false);
    }
}
#endif
