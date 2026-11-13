#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class SideMenuSceneBuilder
{
    // 공통 우측 패널입니다. 배경/카드/아이콘은 Sprite를 연결해 교체할 수 있습니다.
    public static RectTransform Panel(Transform parent,string name,string title,out GameObject window)
    {
        var overlay=UI.Box(name,parent,0,0,1920,1080,new Color(0,0,0,.12f));window=overlay.gameObject;
        var panel=UI.Box("Panel Frame",overlay.transform,1260,112,560,860,new Color(.2f,.15f,.09f));
        // 시계 글자 변경이 정적인 목록 전체를 다시 배칭하지 않게 패널 단위로 분리합니다.
        UiPerformanceLayers.Ensure(panel.gameObject,true);
        UI.Label("Title",panel.transform,24,20,512,52,title,30).alignment=TextAnchor.MiddleCenter;
        return panel.rectTransform;
    }
    public static RectTransform List(Transform parent,string name,float x,float y,float width,float height,bool autoLayout=false)
    {
        var view=UI.Box(name,parent,x,y,width,height,new Color(.1f,.085f,.055f));view.gameObject.AddComponent<RectMask2D>();
        var content=UI.Rect("Content",view.transform,0,0,width-4,0);var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.viewport=view.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        if(autoLayout)
        {
            var group=content.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=8;group.childControlHeight=true;group.childControlWidth=true;group.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        }
        return content;
    }
    public static FurnitureCardView CardTemplate(Transform parent)
    {
        var image=UI.Box("Furniture Card Template",parent,0,0,508,98,new Color(.22f,.17f,.11f));var row=image.gameObject.AddComponent<FurnitureCardView>();row.background=image;
        row.button=image.gameObject.AddComponent<Button>();row.button.targetGraphic=image;
        row.thumbnail=UI.Box("Thumbnail",image.transform,10,10,112,78,Color.white);row.thumbnail.raycastTarget=false;
        row.title=UI.Label("Name",image.transform,136,6,358,32,"",23);row.body=UI.Label("Details",image.transform,136,42,358,26,"",18);
        row.price=UI.Label("Price",image.transform,136,72,358,24,"",18);image.gameObject.SetActive(false);return row;
    }
    static DisplayStockRowView StockTemplate(Transform parent)
    {
        var image=UI.Box("Stock Summary Template",parent,0,0,508,58,new Color(.24f,.19f,.12f));var row=image.gameObject.AddComponent<DisplayStockRowView>();
        row.icon=UI.Box("Icon",image.transform,6,6,46,46,Color.white);row.icon.raycastTarget=false;
        row.nameLabel=UI.Label("Name",image.transform,62,6,250,46,"",20);row.qualityLabel=UI.Label("Quality",image.transform,314,6,114,46,"",18);row.countLabel=UI.Label("Count",image.transform,432,6,70,46,"",20);
        image.gameObject.SetActive(false);return row;
    }
    public static void BuildManagement(Transform parent,DisplayManagementUI ui)
    {
        var p=Panel(parent,"Display Management Window","진열장 관리",out ui.window);
        ui.displayList=List(p,"Owned Displays",24,92,512,318);ui.cardTemplate=CardTemplate(parent);
        ui.selectedLabel=UI.Label("Selected",p,24,416,512,44,"",25);
        ui.stockList=List(p,"Stock Summary",24,474,512,208);ui.stockTemplate=StockTemplate(parent);
        ui.footerLabel=UI.Label("Capacity",p,24,704,512,36,"",19);
        ui.openButton=UI.ButtonBox("진열장 열기",p,24,778,308,56,ui.OpenSelected);UI.ButtonBox("닫기",p,348,778,188,56,ui.Close);ui.window.SetActive(false);
    }
    public static void BuildUpgrades(Transform parent,UpgradeUI ui)
    {
        var p=Panel(parent,"Furniture Catalogue Window","진열장 구매",out ui.window);
        ui.furniturePreview=UI.Box("Selected Furniture Sprite",p,180,82,200,126,Color.white);ui.furniturePreview.raycastTarget=false;
        ui.typeLabel=UI.Label("Selected Title",p,24,216,512,36,"",25);
        ui.displayInfo=UI.Label("Selected Info",p,24,260,512,94,"",19);
        ui.furnitureList=List(p,"Available Furniture",24,370,512,366);ui.cardTemplate=CardTemplate(parent);
        ui.addDisplayButton=UI.ButtonBox("구매하기",p,24,778,308,56,ui.AddDisplay);UI.ButtonBox("닫기",p,348,778,188,56,ui.Close);ui.window.SetActive(false);
        var s=Panel(parent,"Storage Expansion Window","상자 크기 확장",out ui.storageWindow);
        ui.storageNumbers=UI.Label("Before After",s,24,94,512,52,"",30);ui.storageNumbers.alignment=TextAnchor.MiddleCenter;
        ui.storageImage=UI.Box("Chest Illustration Sprite",s,48,164,464,280,new Color(.35f,.23f,.12f));ui.storageImage.raycastTarget=false;
        var rect=UI.Rect("Expansion Grid Preview",s,70,184,420,240);rect.gameObject.AddComponent<CanvasRenderer>();ui.storagePreview=rect.gameObject.AddComponent<StoragePreviewGraphic>();ui.storagePreview.raycastTarget=false;
        ui.storageInfo=UI.Label("Storage Quote",s,24,472,512,244,"",23);
        ui.storageActionButton=UI.ButtonBox("확장",s,24,778,308,56,ui.UpgradeStorage);ui.storageActionLabel=ui.storageActionButton.GetComponentsInChildren<Text>(true)[0];
        UI.ButtonBox("닫기",s,348,778,188,56,ui.Close);ui.storageWindow.SetActive(false);
    }
    public static RightSideMenu BuildMenu(Transform parent,InventoryPanel panel,DisplayManagementUI management,UpgradeUI upgrade,SupplierUI supplier)
    {
        var root=UI.Rect("Right Side Menu",parent,1832,190,84,700);var menu=root.gameObject.AddComponent<RightSideMenu>();menu.panel=panel;UiPerformanceLayers.Ensure(root.gameObject,true);
        menu.content=List(root,"Menu Scroll",0,0,84,700);
        var image=UI.Box("Menu Button Template",parent,0,0,82,86,new Color(.26f,.18f,.1f));var view=image.gameObject.AddComponent<RightMenuButtonView>();view.background=image;
        view.button=image.gameObject.AddComponent<Button>();view.button.targetGraphic=image;view.icon=UI.Box("Icon",image.transform,17,7,48,44,Color.white);view.icon.raycastTarget=false;
        view.label=UI.Label("Label",image.transform,3,50,76,32,"",16);view.label.alignment=TextAnchor.MiddleCenter;image.gameObject.SetActive(false);menu.template=view;
        Add(menu,"displays","진열 관리",management.window,management.Toggle);
        Add(menu,"furniture","가구 구매",upgrade.window,upgrade.OpenFurniture);
        Add(menu,"storage","상자 확장",upgrade.storageWindow,upgrade.OpenStorage);
        Add(menu,"suppliers","거래처",supplier.window,supplier.Toggle);
        return menu;
    }
    static void Add(RightSideMenu menu,string id,string title,GameObject window,UnityEngine.Events.UnityAction open)
    {var e=new ShopMenuEntry{id=id,title=title,window=window};UnityEditor.Events.UnityEventTools.AddPersistentListener(e.onOpen,open);menu.entries.Add(e);}
}
#endif
