#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class SideMenuSceneBuilder
{
    const float PaperScale=560f/1005;
    [System.Serializable]sealed class ManagementArtPart
    {public string id,spriteKey;public float sourceWidth,sourceHeight;public int maximumTextureSize;}
    [System.Serializable]sealed class ManagementArtData{public ManagementArtPart[] entries;}
    static readonly Color PaperInk=new Color(.25f,.1f,.035f);
    static Dictionary<string,ManagementArtPart> ManagementArt()
    {
        var file=Resources.Load<TextAsset>("StoreInventory/management_panel_art");if(file==null)throw new System.InvalidOperationException("management_panel_art.json 누락");
        var data=JsonUtility.FromJson<ManagementArtData>(file.text);
        if(data==null||data.entries==null||data.entries.Length!=6)throw new System.ArgumentException("관리창 Sprite 6종이 필요합니다.");
        var result=new Dictionary<string,ManagementArtPart>();
        foreach(var part in data.entries)
        {
            if(part==null||string.IsNullOrEmpty(part.id)||string.IsNullOrEmpty(part.spriteKey)||result.ContainsKey(part.id)||!(part.sourceWidth>0&&part.sourceWidth<=8192&&part.sourceHeight>0&&part.sourceHeight<=8192)||part.maximumTextureSize<32||part.maximumTextureSize>2048||(part.maximumTextureSize&(part.maximumTextureSize-1))!=0)throw new System.ArgumentException("관리창 Sprite 정의 오류");
            result.Add(part.id,part);
        }
        foreach(string id in new[]{"frame","normal","selected","open","close","well"})if(!result.ContainsKey(id))throw new System.ArgumentException("관리창 Sprite 누락: "+id);
        return result;
    }
    public static void ValidateManagementArt()
    {
        foreach(var part in ManagementArt().Values)
            if(AssetImporter.GetAtPath("Assets/Resources/"+part.spriteKey+".png")==null)throw new System.InvalidOperationException("관리창 PNG 누락: "+part.spriteKey);
    }
    public static void PrepareManagementArt()
    {
        foreach(var part in ManagementArt().Values)PreparePanelTexture(part.spriteKey,part.maximumTextureSize);
    }
    public static void PreparePanelTexture(string spriteKey,int sizeLimit)
    {
        string path="Assets/Resources/"+spriteKey+".png";AssetDatabase.ImportAsset(path);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new System.InvalidOperationException("양피지 패널 PNG 누락: "+path);
        int maximum=StoreTextureBudget.LowMemory?sizeLimit:Mathf.Min(2048,sizeLimit*2);
        if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&importer.maxTextureSize==maximum&&!importer.isReadable&&!importer.mipmapEnabled&&importer.alphaIsTransparency&&importer.sRGBTexture&&importer.npotScale==TextureImporterNPOTScale.None&&importer.wrapMode==TextureWrapMode.Clamp&&importer.filterMode==FilterMode.Bilinear&&importer.textureCompression==TextureImporterCompression.Uncompressed)return;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.maxTextureSize=maximum;importer.isReadable=false;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.sRGBTexture=true;
        importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    }
    static Text PaperText(string name,Transform parent,float x,float y,float w,float h,string text,int size,TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        var label=UI.Label(name,parent,x*PaperScale,y*PaperScale,w*PaperScale,h*PaperScale,text,Mathf.RoundToInt(size*PaperScale));
        label.color=PaperInk;label.alignment=alignment;label.fontStyle=FontStyle.Bold;
        label.resizeTextForBestFit=true;label.resizeTextMaxSize=label.fontSize;label.resizeTextMinSize=Mathf.Max(11,Mathf.RoundToInt(label.fontSize*.7f));return label;
    }
    static RawImage PaperPart(string name,Transform parent,ManagementArtPart part,float x,float y,float w,float h)
    {
        float width=Mathf.Min(w,h*part.sourceWidth/part.sourceHeight),height=width*part.sourceHeight/part.sourceWidth;
        var rect=UI.Rect(name,parent,(x+(w-width)*.5f)*PaperScale,(y+(h-height)*.5f)*PaperScale,width*PaperScale,height*PaperScale);
        var image=rect.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>(part.spriteKey);image.raycastTarget=false;image.color=Color.white;
        if(image.texture==null)throw new System.InvalidOperationException("관리창 Texture 누락: "+part.spriteKey);
        image.uvRect=new Rect(0,0,1,1);return image;
    }
    static RectTransform PaperList(Transform parent,string name,float x,float y,float w,float h,float rowHeight)
    {
        var content=List(parent,name,x*PaperScale,y*PaperScale,w*PaperScale,h*PaperScale);
        content.sizeDelta=new Vector2(w*PaperScale,0);
        var image=content.parent.GetComponent<global::UnityEngine.UI.Image>();image.color=new Color(1,1,1,.001f);image.canvasRenderer.cullTransparentMesh=false;
        var scroll=image.GetComponent<ScrollRect>();scroll.inertia=false;scroll.scrollSensitivity=rowHeight*PaperScale*.65f;
        return content;
    }
    static FurnitureCardView PaperCard(Transform parent,Dictionary<string,ManagementArtPart> parts)
    {
        var image=UI.Box("Parchment Display Row Template",parent,0,0,815*PaperScale,168*PaperScale,Color.clear);
        image.canvasRenderer.cullTransparentMesh=false;var row=image.gameObject.AddComponent<FurnitureCardView>();row.background=image;row.parchmentStyle=true;
        row.button=image.gameObject.AddComponent<Button>();row.button.targetGraphic=image;row.button.transition=Selectable.Transition.None;
        row.normalArtwork=PaperPart("Original Row",image.transform,parts["normal"],0,0,815,168);
        row.selectionArtwork=PaperPart("Selected Row",image.transform,parts["selected"],0,0,815,168);row.selectionArtwork.gameObject.SetActive(false);
        row.thumbnail=UI.Box("Furniture Icon",image.transform,34*PaperScale,30*PaperScale,128*PaperScale,106*PaperScale,Color.white);row.thumbnail.raycastTarget=false;row.thumbnail.preserveAspect=true;
        row.title=PaperText("Name",image.transform,225,20,570,60,"",44);
        row.body=PaperText("Capacity",image.transform,225,86,570,52,"",34);
        row.price=PaperText("Unused Price",image.transform,0,0,1,1,"",12);row.price.gameObject.SetActive(false);
        image.gameObject.SetActive(false);return row;
    }
    static DisplayStockRowView PaperStock(Transform parent,Dictionary<string,ManagementArtPart> parts)
    {
        var rect=UI.Rect("Parchment Stock Row Template",parent,0,0,792*PaperScale,124*PaperScale);var row=rect.gameObject.AddComponent<DisplayStockRowView>();row.parchmentStyle=true;
        PaperPart("Thumbnail Well",rect,parts["well"],8,8,136,108);
        UI.Box("Row Separator",rect,0,123*PaperScale,792*PaperScale,PaperScale,new Color(.55f,.29f,.09f)).raycastTarget=false;
        row.icon=UI.Box("Item Icon",rect,24*PaperScale,14*PaperScale,104*PaperScale,94*PaperScale,Color.white);row.icon.raycastTarget=false;row.icon.preserveAspect=true;
        row.nameLabel=PaperText("Name",rect,195,15,350,78,"",42);
        row.countLabel=PaperText("Count",rect,197,85,330,28,"",25);
        var badge=UI.Rect("Quality Laurel",rect,542*PaperScale,12*PaperScale,230*PaperScale,100*PaperScale);badge.gameObject.AddComponent<CanvasRenderer>();badge.gameObject.AddComponent<LaurelBadgeGraphic>().raycastTarget=false;
        row.qualityLabel=PaperText("Quality",badge,43,10,144,80,"",40,TextAnchor.MiddleCenter);
        rect.gameObject.SetActive(false);return row;
    }
    static Button PaperButton(Transform parent,ManagementArtPart part,string title,Rect slot,UnityEngine.Events.UnityAction action)
    {
        var hit=UI.Box(title,parent,slot.x*PaperScale,slot.y*PaperScale,slot.width*PaperScale,slot.height*PaperScale,Color.clear);hit.canvasRenderer.cullTransparentMesh=false;
        var image=PaperPart("Button Artwork",hit.transform,part,0,0,slot.width,slot.height);
        var button=hit.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(.82f,.82f,.82f);colors.disabledColor=new Color(.5f,.5f,.5f,1);button.colors=colors;
        var label=PaperText("Text",hit.transform,12,8,slot.width-24,slot.height-16,title,47,TextAnchor.MiddleCenter);label.color=new Color(.98f,.92f,.74f);
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
        PrepareManagementArt();var parts=ManagementArt();var frameArt=parts["frame"];var texture=Resources.Load<Texture2D>(frameArt.spriteKey);if(texture==null)throw new System.InvalidOperationException("관리창 양피지 Texture 누락");
        var overlay=UI.Box("Display Management Window",parent,0,0,1920,1080,Color.clear);ui.window=overlay.gameObject;overlay.raycastTarget=false;overlay.canvasRenderer.cullTransparentMesh=true;
        var p=UI.Rect("Panel Frame",overlay.transform,0,0,560,560*frameArt.sourceHeight/frameArt.sourceWidth);p.gameObject.AddComponent<CanvasRenderer>();var paper=p.gameObject.AddComponent<ParchmentPanelGraphic>();paper.artwork=texture;paper.color=Color.white;paper.raycastTarget=true;
        UiPerformanceLayers.Ensure(p.gameObject,true);ManagementWindowMotion.Ensure(ui.window,p);
        PaperText("Title",p,105,102,796,105,"진열장 관리",72,TextAnchor.MiddleCenter);
        UI.Box("Title Separator",p,105*PaperScale,223*PaperScale,795*PaperScale,2*PaperScale,new Color(.55f,.29f,.09f)).raycastTarget=false;
        ui.parchmentStyle=true;ui.displayRowHeight=168*PaperScale;ui.stockRowHeight=124*PaperScale;ui.rowGap=0;
        ui.displayList=PaperList(p,"Owned Displays",95,250,815,504,168);ui.cardTemplate=PaperCard(p,parts);
        ui.emptyDisplayLabel=PaperText("No Displays",ui.displayList.parent,0,0,815,504,"소유한 진열장이 없습니다.",36,TextAnchor.MiddleCenter);ui.emptyDisplayLabel.gameObject.SetActive(false);
        UI.Box("Stock Heading Top Line",p,105*PaperScale,801*PaperScale,795*PaperScale,2*PaperScale,new Color(.55f,.29f,.09f)).raycastTarget=false;
        ui.selectedLabel=PaperText("Selected",p,110,808,785,72,"진열 품목",44,TextAnchor.MiddleCenter);
        UI.Box("Stock Heading Bottom Line",p,105*PaperScale,886*PaperScale,795*PaperScale,2*PaperScale,new Color(.55f,.29f,.09f)).raycastTarget=false;
        ui.stockList=PaperList(p,"Stock Summary",108,906,792,372,124);ui.stockTemplate=PaperStock(p,parts);
        ui.emptyStockLabel=PaperText("No Stock",ui.stockList.parent,0,0,792,372,"진열된 품목이 없습니다.",34,TextAnchor.MiddleCenter);
        ui.footerLabel=PaperText("Capacity",p,108,1291,790,32,"",25,TextAnchor.MiddleCenter);
        ui.openButton=PaperButton(p,parts["open"],"진열장 열기",new Rect(106,1340,416,105),ui.OpenSelected);
        PaperButton(p,parts["close"],"닫기",new Rect(563,1340,333,105),ui.Close);ui.window.SetActive(false);
    }
    public static void BuildUpgrades(Transform parent,UpgradeUI ui)
    {
        PurchaseSceneBuilder.Build(parent,ui);
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
public static class PurchaseSceneBuilder
{
    const float FurnitureScale=560f/1000,StorageScale=620f/1204;
    static readonly Color Ink=new Color(.25f,.1f,.035f),Cream=new Color(.98f,.92f,.74f);
    [System.Serializable]sealed class Part{public string id,spriteKey;public float sourceWidth,sourceHeight;public int maximumTextureSize;}
    [System.Serializable]sealed class ArtData{public Part[] entries;}
    static Dictionary<string,Part> Art()
    {
        var file=Resources.Load<TextAsset>("StoreInventory/purchase_panel_art");if(file==null)throw new System.InvalidOperationException("purchase_panel_art.json 누락");
        var data=JsonUtility.FromJson<ArtData>(file.text);if(data==null||data.entries==null||data.entries.Length!=8)throw new System.ArgumentException("구매창 Sprite 8종이 필요합니다.");
        var parts=new Dictionary<string,Part>();
        foreach(var p in data.entries)
        {
            if(p==null||string.IsNullOrEmpty(p.id)||string.IsNullOrEmpty(p.spriteKey)||parts.ContainsKey(p.id)||!(p.sourceWidth>0&&p.sourceWidth<=8192&&p.sourceHeight>0&&p.sourceHeight<=8192)||p.maximumTextureSize<32||p.maximumTextureSize>2048||(p.maximumTextureSize&(p.maximumTextureSize-1))!=0)throw new System.ArgumentException("구매창 Sprite 정의 오류");
            parts.Add(p.id,p);
        }
        foreach(string id in new[]{"frame","normal","selected","preview","price","buy","close","storage"})if(!parts.ContainsKey(id))throw new System.ArgumentException("구매창 Sprite 누락: "+id);
        return parts;
    }
    public static void ValidateArt(){foreach(var p in Art().Values)if(AssetImporter.GetAtPath("Assets/Resources/"+p.spriteKey+".png")==null)throw new System.InvalidOperationException("구매창 PNG 누락: "+p.spriteKey);}
    public static void PrepareArt(){foreach(var p in Art().Values)SideMenuSceneBuilder.PreparePanelTexture(p.spriteKey,p.maximumTextureSize);}
    static Text Label(string name,Transform parent,Rect r,string value,int size,float s,TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        var text=UI.Label(name,parent,r.x*s,r.y*s,r.width*s,r.height*s,value,Mathf.RoundToInt(size*s));text.color=Ink;text.fontStyle=FontStyle.Bold;text.alignment=alignment;
        text.resizeTextForBestFit=true;text.resizeTextMaxSize=text.fontSize;text.resizeTextMinSize=Mathf.Max(11,Mathf.RoundToInt(text.fontSize*.65f));return text;
    }
    static RawImage Artwork(string name,Transform parent,Part part,Rect slot,float s)
    {
        float w=Mathf.Min(slot.width,slot.height*part.sourceWidth/part.sourceHeight),h=w*part.sourceHeight/part.sourceWidth;
        var r=UI.Rect(name,parent,(slot.x+(slot.width-w)*.5f)*s,(slot.y+(slot.height-h)*.5f)*s,w*s,h*s);
        var image=r.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>(part.spriteKey);image.raycastTarget=false;image.color=Color.white;image.uvRect=new Rect(0,0,1,1);
        if(image.texture==null)throw new System.InvalidOperationException("구매창 Texture 누락: "+part.spriteKey);return image;
    }
    static RectTransform Window(Transform parent,string name,Part art,float width,out GameObject window)
    {
        var overlay=UI.Box(name,parent,0,0,1920,1080,Color.clear);overlay.raycastTarget=false;overlay.canvasRenderer.cullTransparentMesh=true;window=overlay.gameObject;
        var frame=UI.Box("Panel Frame",overlay.transform,0,0,width,width*art.sourceHeight/art.sourceWidth,Color.white);frame.sprite=Resources.Load<Sprite>(art.spriteKey);frame.preserveAspect=true;
        if(frame.sprite==null)throw new System.InvalidOperationException("구매창 Frame Sprite 누락: "+art.spriteKey);
        UiPerformanceLayers.Ensure(frame.gameObject,true);ManagementWindowMotion.Ensure(window,frame.rectTransform);return frame.rectTransform;
    }
    static Button ActionButton(string name,Transform parent,Rect slot,float s,UnityEngine.Events.UnityAction action,Part artwork=null)
    {
        var hit=UI.Box(name,parent,slot.x*s,slot.y*s,slot.width*s,slot.height*s,Color.clear);hit.canvasRenderer.cullTransparentMesh=false;
        var button=hit.gameObject.AddComponent<Button>();
        RawImage art=null;if(artwork!=null)art=Artwork("Button Artwork",hit.transform,artwork,new Rect(0,0,slot.width,slot.height),s);
        var label=Label("Text",hit.transform,new Rect(12,7,slot.width-24,slot.height-14),name,58,s,TextAnchor.MiddleCenter);label.color=Cream;button.targetGraphic=art!=null?(Graphic)art:label;
        var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(.82f,.82f,.82f);colors.disabledColor=new Color(.5f,.5f,.5f);button.colors=colors;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,action);return button;
    }
    static FurnitureCardView Card(Transform parent,Dictionary<string,Part> parts)
    {
        float s=FurnitureScale;var hit=UI.Box("Purchase Furniture Row Template",parent,0,0,830*s,200*s,Color.clear);hit.canvasRenderer.cullTransparentMesh=false;
        var row=hit.gameObject.AddComponent<FurnitureCardView>();row.parchmentStyle=true;row.background=hit;row.button=hit.gameObject.AddComponent<Button>();row.button.targetGraphic=hit;row.button.transition=Selectable.Transition.None;
        row.normalArtwork=Artwork("Normal Row",hit.transform,parts["normal"],new Rect(0,0,830,200),s);
        row.selectionArtwork=Artwork("Selected Row",hit.transform,parts["selected"],new Rect(0,0,830,200),s);row.selectionArtwork.gameObject.SetActive(false);
        row.thumbnail=UI.Box("Furniture Icon",hit.transform,35*s,26*s,160*s,148*s,Color.white);row.thumbnail.raycastTarget=false;row.thumbnail.preserveAspect=true;
        row.title=Label("Name",hit.transform,new Rect(250,24,560,76),"",54,s);row.body=Label("Capacity",hit.transform,new Rect(250,100,560,70),"",40,s);
        row.price=Label("Unused Price",hit.transform,new Rect(0,0,1,1),"",12,s);row.price.gameObject.SetActive(false);hit.gameObject.SetActive(false);return row;
    }
    public static void Build(Transform parent,UpgradeUI ui)
    {
        PrepareArt();var parts=Art();float s=FurnitureScale;var p=Window(parent,"Furniture Catalogue Window",parts["frame"],560,out ui.window);
        Label("Title",p,new Rect(90,92,820,116),"가구 구매",78,s,TextAnchor.MiddleCenter);
        UI.Box("Title Divider",p,100*s,215*s,800*s,2*s,new Color(.55f,.29f,.09f)).raycastTarget=false;
        ui.furnitureList=SideMenuSceneBuilder.List(p,"Available Furniture",85*s,240*s,830*s,600*s);ui.furnitureList.sizeDelta=Vector2.zero;
        ui.furnitureScroll=ui.furnitureList.parent.GetComponent<ScrollRect>();var viewport=ui.furnitureScroll.viewport;viewport.GetComponent<global::UnityEngine.UI.Image>().color=new Color(1,1,1,.001f);
        viewport.GetComponent<global::UnityEngine.UI.Image>().canvasRenderer.cullTransparentMesh=false;ui.furnitureList.sizeDelta=new Vector2(830*s,0);ui.furnitureScroll.inertia=false;ui.furnitureScroll.scrollSensitivity=130*s;
        ui.cardTemplate=Card(p,parts);ui.useFurnitureArtwork=true;ui.furnitureRowHeight=200*s;ui.furnitureRowGap=0;
        ui.furnitureEmptyLabel=Label("No Furniture",viewport,new Rect(0,0,830,600),"구매 가능한 가구가 없습니다.",36,s,TextAnchor.MiddleCenter);ui.furnitureEmptyLabel.gameObject.SetActive(false);
        ui.furniturePageLabel=Label("List Position",p,new Rect(735,840,175,34),"",32,s,TextAnchor.MiddleRight);
        UI.Box("Preview Divider",p,85*s,880*s,830*s,2*s,new Color(.55f,.29f,.09f)).raycastTarget=false;
        Artwork("Preview Well",p,parts["preview"],new Rect(95,910,400,380),s);
        ui.furniturePreview=UI.Box("Selected Furniture Sprite",p,115*s,935*s,360*s,330*s,Color.white);ui.furniturePreview.raycastTarget=false;ui.furniturePreview.preserveAspect=true;
        UI.Box("Preview Split",p,520*s,928*s,2*s,352*s,new Color(.55f,.29f,.09f)).raycastTarget=false;
        ui.typeLabel=Label("Selected Title",p,new Rect(540,920,370,82),"",50,s);
        UI.Box("Capacity Divider",p,548*s,1016*s,355*s,2*s,new Color(.55f,.29f,.09f)).raycastTarget=false;
        Label("Capacity Heading",p,new Rect(548,1040,350,44),"진열 공간",38,s);ui.displayInfo=Label("Selected Capacity",p,new Rect(548,1087,350,67),"",56,s);
        UI.Box("Price Divider",p,548*s,1164*s,355*s,2*s,new Color(.55f,.29f,.09f)).raycastTarget=false;
        Artwork("Price Well",p,parts["price"],new Rect(540,1194,370,78),s);Label("Price Heading",p,new Rect(568,1200,102,65),"가격",40,s);
        ui.furniturePriceLabel=Label("Price",p,new Rect(680,1200,205,65),"",48,s,TextAnchor.MiddleRight);ui.furnitureStatusLabel=Label("Purchase Status",p,new Rect(540,1282,370,42),"",24,s);
        ui.addDisplayButton=ActionButton("구매",p,new Rect(100,1340,390,106),s,ui.AddDisplay,parts["buy"]);ActionButton("닫기",p,new Rect(540,1340,360,106),s,ui.Close,parts["close"]);ui.window.SetActive(false);
        s=StorageScale;var storage=Window(parent,"Storage Expansion Window",parts["storage"],620,out ui.storageWindow);ui.storageImage=storage.GetComponent<global::UnityEngine.UI.Image>();ui.useStorageArtwork=true;ui.storagePreview=null;
        Label("Title",storage,new Rect(120,75,960,105),"상자 추가",104,s,TextAnchor.MiddleCenter);
        ui.storageNumbers=Label("Owned Boxes",storage,new Rect(365,228,475,80),"",50,s,TextAnchor.MiddleCenter);
        ui.storageBeforeLabel=Label("Current Box",storage,new Rect(195,500,355,60),"",58,s,TextAnchor.MiddleCenter);ui.storageAfterLabel=Label("New Box",storage,new Rect(653,500,355,60),"",58,s,TextAnchor.MiddleCenter);
        ui.storageInfo=Label("New Space",storage,new Rect(90,790,1020,56),"",48,s,TextAnchor.MiddleCenter);ui.storageStatusLabel=Label("Storage Status",storage,new Rect(90,847,1020,24),"",22,s,TextAnchor.MiddleCenter);
        ui.storageCostLabel=Label("Cost",storage,new Rect(550,882,232,70),"",70,s,TextAnchor.MiddleCenter);
        ui.storageActionButton=ActionButton("구매",storage,new Rect(144,992,454,109),s,ui.UpgradeStorage);ui.storageActionLabel=ui.storageActionButton.transform.Find("Text").GetComponent<Text>();ActionButton("닫기",storage,new Rect(643,992,420,109),s,ui.Close);ui.storageWindow.SetActive(false);
    }
}
#endif
