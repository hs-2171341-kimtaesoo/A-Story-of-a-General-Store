#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class SideMenuSceneBuilder
{
    const string PaperKey="StoreInventory/ManagementPanel/parchment";
    const float PaperScale=560f/1006;
    static readonly Color PaperInk=new Color(.25f,.1f,.035f);
    public static void PrepareManagementArt()
    {
        string path="Assets/Resources/"+PaperKey+".png";AssetDatabase.ImportAsset(path);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new System.InvalidOperationException("진열장 관리 양피지 PNG 누락: "+path);
        if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&importer.maxTextureSize==StoreTextureBudget.DisplayArt&&!importer.isReadable&&!importer.mipmapEnabled&&importer.alphaIsTransparency&&importer.textureCompression==TextureImporterCompression.Compressed)return;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.maxTextureSize=StoreTextureBudget.DisplayArt;importer.isReadable=false;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
        importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
    }
    static Text PaperText(string name,Transform parent,float x,float y,float w,float h,string text,int size,TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        var label=UI.Label(name,parent,x*PaperScale,y*PaperScale,w*PaperScale,h*PaperScale,text,Mathf.RoundToInt(size*PaperScale));
        label.color=PaperInk;label.alignment=alignment;label.fontStyle=FontStyle.Bold;
        label.resizeTextForBestFit=true;label.resizeTextMaxSize=label.fontSize;label.resizeTextMinSize=Mathf.Max(11,Mathf.RoundToInt(label.fontSize*.7f));return label;
    }
    static RawImage PaperPart(string name,Transform parent,Texture texture,Rect source,float x,float y,float w,float h)
    {
        var rect=UI.Rect(name,parent,x*PaperScale,y*PaperScale,w*PaperScale,h*PaperScale);
        var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
        image.uvRect=new Rect(source.x/1006,1-(source.y+source.height)/1564,source.width/1006,source.height/1564);return image;
    }
    static RectTransform PaperList(Transform parent,string name,float x,float y,float w,float h,float rowHeight)
    {
        var content=List(parent,name,x*PaperScale,y*PaperScale,w*PaperScale,h*PaperScale);
        content.sizeDelta=new Vector2(w*PaperScale,0);
        var image=content.parent.GetComponent<global::UnityEngine.UI.Image>();image.color=new Color(1,1,1,.001f);image.canvasRenderer.cullTransparentMesh=false;
        var scroll=image.GetComponent<ScrollRect>();scroll.inertia=false;scroll.scrollSensitivity=rowHeight*PaperScale*.65f;
        return content;
    }
    static FurnitureCardView PaperCard(Transform parent,Texture texture)
    {
        var image=UI.Box("Parchment Display Row Template",parent,0,0,815*PaperScale,168*PaperScale,new Color(1,1,1,.001f));
        image.canvasRenderer.cullTransparentMesh=false;var row=image.gameObject.AddComponent<FurnitureCardView>();row.background=image;row.parchmentStyle=true;
        row.button=image.gameObject.AddComponent<Button>();row.button.targetGraphic=image;row.button.transition=Selectable.Transition.None;
        PaperPart("Original Row",image.transform,texture,new Rect(95,250,815,168),0,0,815,168);
        row.selectionArtwork=PaperPart("Selected Row",image.transform,texture,new Rect(95,586,815,168),0,0,815,168);row.selectionArtwork.gameObject.SetActive(false);
        row.thumbnail=UI.Box("Furniture Icon",image.transform,29*PaperScale,20*PaperScale,140*PaperScale,130*PaperScale,Color.white);row.thumbnail.raycastTarget=false;row.thumbnail.preserveAspect=true;
        row.title=PaperText("Name",image.transform,225,20,570,60,"",44);
        row.body=PaperText("Capacity",image.transform,225,86,570,52,"",34);
        row.price=PaperText("Unused Price",image.transform,0,0,1,1,"",12);row.price.gameObject.SetActive(false);
        image.gameObject.SetActive(false);return row;
    }
    static DisplayStockRowView PaperStock(Transform parent,Texture texture)
    {
        var rect=UI.Rect("Parchment Stock Row Template",parent,0,0,792*PaperScale,124*PaperScale);var row=rect.gameObject.AddComponent<DisplayStockRowView>();row.parchmentStyle=true;
        PaperPart("Original Stock Row",rect,texture,new Rect(108,906,792,124),0,0,792,124);
        row.icon=UI.Box("Item Icon",rect,16*PaperScale,14*PaperScale,120*PaperScale,94*PaperScale,Color.white);row.icon.raycastTarget=false;row.icon.preserveAspect=true;
        row.nameLabel=PaperText("Name",rect,195,15,350,78,"",42);
        row.countLabel=PaperText("Count",rect,197,85,330,28,"",25);
        var badge=UI.Rect("Quality Laurel",rect,542*PaperScale,12*PaperScale,230*PaperScale,100*PaperScale);badge.gameObject.AddComponent<CanvasRenderer>();badge.gameObject.AddComponent<LaurelBadgeGraphic>().raycastTarget=false;
        row.qualityLabel=PaperText("Quality",badge,43,10,144,80,"",40,TextAnchor.MiddleCenter);
        rect.gameObject.SetActive(false);return row;
    }
    static Button PaperButton(Transform parent,Texture texture,string title,Rect source,UnityEngine.Events.UnityAction action)
    {
        var image=PaperPart(title,parent,texture,source,source.x,source.y,source.width,source.height);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.highlightedColor=new Color(1,.94f,.8f);colors.pressedColor=new Color(.75f,.67f,.52f);colors.disabledColor=new Color(.5f,.5f,.5f,1);button.colors=colors;
        var label=PaperText("Text",image.transform,12,8,source.width-24,source.height-16,title,47,TextAnchor.MiddleCenter);label.color=new Color(.98f,.92f,.74f);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,action);return button;
    }
    // 공통 관리 패널입니다. 배경/카드/아이콘은 Sprite를 연결해 교체할 수 있습니다.
    public static RectTransform Panel(Transform parent,string name,string title,out GameObject window)
    {
        var overlay=UI.Box(name,parent,0,0,1920,1080,new Color(1,1,1,0));window=overlay.gameObject;overlay.raycastTarget=false;overlay.canvasRenderer.cullTransparentMesh=true;
        var panel=UI.Box("Panel Frame",overlay.transform,1260,112,560,860,new Color(.2f,.15f,.09f));
        // 시계 글자 변경이 정적인 목록 전체를 다시 배칭하지 않게 패널 단위로 분리합니다.
        UiPerformanceLayers.Ensure(panel.gameObject,true);
        ManagementWindowMotion.Ensure(window,panel.rectTransform);
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
        PrepareManagementArt();var texture=Resources.Load<Texture2D>(PaperKey);if(texture==null)throw new System.InvalidOperationException("관리창 양피지 Texture 누락");
        var overlay=UI.Box("Display Management Window",parent,0,0,1920,1080,Color.clear);ui.window=overlay.gameObject;overlay.raycastTarget=false;overlay.canvasRenderer.cullTransparentMesh=true;
        var p=UI.Rect("Panel Frame",overlay.transform,0,0,560,1564*PaperScale);p.gameObject.AddComponent<CanvasRenderer>();var paper=p.gameObject.AddComponent<ParchmentPanelGraphic>();paper.artwork=texture;paper.raycastTarget=true;
        UiPerformanceLayers.Ensure(p.gameObject,true);ManagementWindowMotion.Ensure(ui.window,p);
        PaperText("Title",p,105,102,796,105,"진열장 관리",72,TextAnchor.MiddleCenter);
        ui.parchmentStyle=true;ui.displayRowHeight=168*PaperScale;ui.stockRowHeight=124*PaperScale;ui.rowGap=0;
        ui.displayList=PaperList(p,"Owned Displays",95,250,815,504,168);ui.cardTemplate=PaperCard(p,texture);
        ui.emptyDisplayLabel=PaperText("No Displays",ui.displayList.parent,0,0,815,504,"소유한 진열장이 없습니다.",36,TextAnchor.MiddleCenter);ui.emptyDisplayLabel.gameObject.SetActive(false);
        UI.Box("Stock Heading Top Line",p,105*PaperScale,801*PaperScale,795*PaperScale,2*PaperScale,new Color(.55f,.29f,.09f)).raycastTarget=false;
        ui.selectedLabel=PaperText("Selected",p,110,808,785,72,"진열 품목",44,TextAnchor.MiddleCenter);
        UI.Box("Stock Heading Bottom Line",p,105*PaperScale,886*PaperScale,795*PaperScale,2*PaperScale,new Color(.55f,.29f,.09f)).raycastTarget=false;
        ui.stockList=PaperList(p,"Stock Summary",108,906,792,372,124);ui.stockTemplate=PaperStock(p,texture);
        ui.emptyStockLabel=PaperText("No Stock",ui.stockList.parent,0,0,792,372,"진열된 품목이 없습니다.",34,TextAnchor.MiddleCenter);
        ui.footerLabel=PaperText("Capacity",p,108,1291,790,32,"",25,TextAnchor.MiddleCenter);
        ui.openButton=PaperButton(p,texture,"진열장 열기",new Rect(106,1340,416,105),ui.OpenSelected);
        PaperButton(p,texture,"닫기",new Rect(563,1340,333,105),ui.Close);ui.window.SetActive(false);
    }
    public static void BuildUpgrades(Transform parent,UpgradeUI ui)
    {
        var p=Panel(parent,"Furniture Catalogue Window","진열장 구매",out ui.window);
        ui.furniturePreview=UI.Box("Selected Furniture Sprite",p,180,82,200,126,Color.white);ui.furniturePreview.raycastTarget=false;
        ui.typeLabel=UI.Label("Selected Title",p,24,216,512,36,"",25);
        ui.displayInfo=UI.Label("Selected Info",p,24,260,512,94,"",19);
        ui.furnitureList=List(p,"Available Furniture",24,370,512,366);ui.cardTemplate=CardTemplate(parent);
        ui.addDisplayButton=UI.ButtonBox("구매하기",p,24,778,308,56,ui.AddDisplay);UI.ButtonBox("닫기",p,348,778,188,56,ui.Close);ui.window.SetActive(false);
        var s=Panel(parent,"Storage Expansion Window","보관 상자 추가",out ui.storageWindow);
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
        Add(menu,"storage","상자 추가",upgrade.storageWindow,upgrade.OpenStorage);
        Add(menu,"suppliers","거래처",supplier.window,supplier.Toggle);
        return menu;
    }
    static void Add(RightSideMenu menu,string id,string title,GameObject window,UnityEngine.Events.UnityAction open)
    {var e=new ShopMenuEntry{id=id,title=title,window=window};UnityEditor.Events.UnityEventTools.AddPersistentListener(e.onOpen,open);menu.entries.Add(e);}
}
#endif
