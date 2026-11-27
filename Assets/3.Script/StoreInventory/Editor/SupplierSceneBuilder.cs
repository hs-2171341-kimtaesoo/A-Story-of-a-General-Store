#if UNITY_EDITOR
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UI=InventorySceneBuilder;
using UIImage=UnityEngine.UI.Image;

public static class SupplierSceneBuilder
{
    const string Folder="StoreInventory/SupplierPanel/";
    static Sprite Art(string name)
    {
        var sprite=Resources.Load<Sprite>(Folder+name);
        if(sprite==null)throw new System.InvalidOperationException("거래처 Sprite 누락: "+name);
        return sprite;
    }
    internal static void Prepare()
    {
        foreach(var key in new[]{"list","detail","offer_row","list_row","coin","selected","close","order","plus","minus","close_pressed","order_pressed","plus_pressed","minus_pressed","calendar","basket","chevron","B001","B002","B003","B004","B005"})
        {
            int size=key=="list"||key=="detail"?1024:key=="selected"||key=="offer_row"||key=="list_row"?512:key=="coin"?128:256;
            SideMenuSceneBuilder.PreparePanelTexture(Folder+key,size);
        }
    }
    static Text Ink(string name,Transform parent,float x,float y,float w,float h,string value,int size,TextAnchor align=TextAnchor.MiddleLeft)
    {
        var t=UI.Label(name,parent,x,y,w,h,value,size);
        t.color=new Color(.22f,.105f,.045f);t.alignment=align;t.resizeTextForBestFit=true;
        t.resizeTextMinSize=13;t.resizeTextMaxSize=size;return t;
    }
    static UIImage Icon(string key,Transform parent,float x,float y,float w,float h)
    {
        var image=UI.IconSlot(key,parent,x,y,w,h);image.sprite=Art(key);return image;
    }
    static Button Button(string key,Transform parent,float x,float y,float w,float h,string label,UnityEngine.Events.UnityAction click=null)
    {
        var image=UI.Box(key,parent,x,y,w,h,Color.white);
        var button=image.gameObject.AddComponent<Button>();
        ButtonArtworkCatalog.Apply(button,image,new ButtonSpriteSet{normal=Art(key),pressed=Art(key+"_pressed")},false);
        image.preserveAspect=false;
        if(label.Length>0)UI.Label("Text",image.transform,0,0,w,h,label,24).alignment=TextAnchor.MiddleCenter;
        if(click!=null)UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,click);
        return button;
    }
    static RectTransform List(Transform parent,string name,float x,float y,float w,float h,float gap)
    {
        var content=SideMenuSceneBuilder.List(parent,name,x,y,w,h,true);
        content.parent.GetComponent<UIImage>().color=Color.clear;
        content.GetComponent<VerticalLayoutGroup>().spacing=gap;
        return content;
    }
    static SupplierListRowView SupplierTemplate(Transform parent)
    {
        var image=UI.Box("Supplier Row Template",parent,0,0,440,144,Color.clear);
        var row=image.gameObject.AddComponent<SupplierListRowView>();
        row.selectButton=image.gameObject.AddComponent<Button>();row.selectButton.targetGraphic=image;row.tradeButton=row.selectButton;
        row.portrait=UI.IconSlot("Supplier Emblem",image.transform,12,10,80,122);
        row.nameLabel=Ink("Name",image.transform,108,5,280,40,"",27);
        row.descriptionLabel=Ink("Description",image.transform,108,44,278,38,"",18);
        row.statusLabel=Ink("Status",image.transform,215,89,171,43,"",16);
        row.sampleIcons=new UIImage[2];
        for(int i=0;i<2;i++)row.sampleIcons[i]=UI.IconSlot("Sample Icon "+i,image.transform,108+i*49,85,45,48);
        Icon("chevron",image.transform,397,57,19,30);
        image.gameObject.AddComponent<LayoutElement>().preferredHeight=144;
        image.gameObject.SetActive(false);return row;
    }
    static SupplierOfferRowView OfferTemplate(Transform parent)
    {
        var frame=UI.Rect("Supplier Offer Template",parent,0,0,684,83);
        var row=frame.gameObject.AddComponent<SupplierOfferRowView>();
        row.icon=UI.IconSlot("Item Icon",frame,10,7,71,68);
        row.nameLabel=Ink("Name",frame,88,8,234,62,"",23);
        row.priceLabel=Ink("Unit Price",frame,327,4,179,38,"",20);
        row.stockLabel=Ink("Remaining Stock",frame,327,41,179,34,"",17);
        row.minusButton=Button("minus",frame,524,18,51,48,"");
        row.quantityLabel=Ink("Quantity",frame,575,18,50,48,"0",24,TextAnchor.MiddleCenter);
        row.plusButton=Button("plus",frame,625,18,51,48,"");
        frame.gameObject.AddComponent<LayoutElement>().preferredHeight=83;
        frame.gameObject.SetActive(false);return row;
    }
    public static void ApplyOrderArtwork(SupplierUI ui)
    {
        SideMenuSceneBuilder.PreparePanelTexture(Folder+"offer_row",512);
        SideMenuSceneBuilder.PreparePanelTexture(Folder+"detail",1024);
        ui.tradePage.transform.Find("Order Panel").GetComponent<UIImage>().sprite=Art("detail");
        var row=ui.offerTemplate;
        var background=row.GetComponent<UIImage>();
        if(background==null)background=row.gameObject.AddComponent<UIImage>();
        background.sprite=Art("offer_row");background.color=Color.white;background.raycastTarget=false;
        // Match the embossed controls in the row itself. Stretch anchors also follow the scroll layout.
        PlaceOnRow((RectTransform)row.minusButton.transform,949,36,92,89);
        PlaceOnRow(row.quantityLabel.rectTransform,1041,36,91,89);
        PlaceOnRow((RectTransform)row.plusButton.transform,1132,36,92,89);
    }
    static void PlaceOnRow(RectTransform child,float x,float y,float width,float height)
    {
        child.anchorMin=new Vector2(x/1255f,1-(y+height)/162f);
        child.anchorMax=new Vector2((x+width)/1255f,1-y/162f);
        child.offsetMin=child.offsetMax=Vector2.zero;
    }
    public static void Build(Transform parent,SupplierUI ui)
    {
        var panel=SideMenuSceneBuilder.Panel(parent,"Supplier Window","거래처 목록",out ui.window);
        Populate(panel,ui);
        ui.testSelectionLabel=UI.Label("Test Selection",parent,0,0,420,36,"",16);ui.testSelectionLabel.gameObject.SetActive(false);
        UI.ButtonBox("거래처 해금 테스트",parent,0,0,200,50,ui.UnlockNext);UI.ButtonBox("거래처 중단 테스트",parent,0,0,200,50,ui.BlockSelected);UI.ButtonBox("거래처 재개 테스트",parent,0,0,200,50,ui.ResumeSelected);
        ui.window.SetActive(false);
    }
    public static void Upgrade(SupplierUI ui)
    {
        if(ui.detailMotion!=null)return;
        Object.DestroyImmediate(ui.listPage);
        Object.DestroyImmediate(ui.tradePage);
        Object.DestroyImmediate(ui.supplierTemplate.gameObject);
        Object.DestroyImmediate(ui.offerTemplate.gameObject);
        Populate((RectTransform)ui.window.transform.Find("Panel Frame"),ui);
    }
    static void Populate(RectTransform frame,SupplierUI ui)
    {
        Prepare();
        var title=frame.Find("Title");
        if(title!=null)Object.DestroyImmediate(title.gameObject);
        var paper=frame.GetComponent<UIImage>();paper.sprite=Art("list");paper.color=Color.white;
        var list=UI.Rect("Supplier List Page",frame,0,0,560,860);ui.listPage=list.gameObject;
        Ink("Title",list,144,65,272,66,"거래처 목록",36,TextAnchor.MiddleCenter);
        Ink("Subtitle",list,60,124,440,52,"주문할 거래처를 선택하세요",22,TextAnchor.MiddleCenter);
        ui.supplierList=List(list,"Supplier List",58,183,448,460,12);
        Icon("calendar",list,79,667,34,34);
        Ink("Delivery",list,122,660,362,50,"주문 물품은 다음 날 새벽 도착",21);
        Button("close",list,150,733,261,63,"닫기",ui.Close);
        ui.selectedRow=Art("selected");
        var detail=UI.Rect("Supplier Detail",ui.window.transform,0,0,810,900);
        detail.anchorMin=detail.anchorMax=detail.pivot=new Vector2(1,0);detail.anchoredPosition=new Vector2(-20,20);
        ui.tradePage=detail.gameObject;
        ui.character=UI.IconSlot("Supplier Character",detail,300,0,450,370);ui.character.enabled=false;
        var sheet=UI.Box("Order Panel",detail,0,332,810,568,Color.white);sheet.sprite=Art("detail");
        var t=sheet.transform;
        ui.portrait=UI.IconSlot("Supplier Emblem",t,256,39,61,69);
        ui.titleLabel=Ink("Supplier Name",t,322,37,237,39,"",28);
        ui.greetingLabel=Ink("Greeting",t,322,75,339,31,"",17);
        ui.offerList=List(t,"Offer List",59,115,689,264,9);
        Icon("basket",t,78,402,38,34);
        Ink("Summary",t,122,395,131,48,"주문 요약",22);
        ui.totalLabel=Ink("Order Totals",t,258,391,466,53,"",19,TextAnchor.MiddleRight);
        Icon("calendar",t,77,470,35,34);
        ui.cycleLabel=Ink("Delivery",t,120,458,280,56,"",18);
        ui.buyButton=Button("order",t,494,457,248,60,"주문하기",ui.Buy);
        ui.supplierTemplate=SupplierTemplate(list);ui.offerTemplate=OfferTemplate(t);
        ApplyOrderArtwork(ui);
        ui.supplierArt=System.Array.ConvertAll(DefaultEmblems,mark=>Mark(mark.supplierId,mark.artwork));
        ui.detailMotion=ManagementWindowMotion.Ensure(detail.gameObject,detail,true);
        detail.gameObject.SetActive(false);
    }
    public static void AuthorRows(SupplierUI ui)
    {
        Prepare();
        var frame=ui.window.transform.Find("Panel Frame");
        frame.GetComponent<UIImage>().sprite=Art("list");
        ui.selectedRow=Art("selected");
        ui.tradePage.transform.Find("Order Panel").GetComponent<UIImage>().sprite=Art("detail");
        foreach(var image in ui.window.GetComponentsInChildren<UIImage>(true))
            if(image.name=="basket"||image.name=="calendar"||image.name=="chevron")image.sprite=Art(image.name);
        foreach(var button in ui.window.GetComponentsInChildren<Button>(true))
        {
            var key=button.name;
            if(key!="order"&&key!="close"&&key!="minus"&&key!="plus")continue;
            var image=button.GetComponent<UIImage>();
            ButtonArtworkCatalog.Apply(button,image,new ButtonSpriteSet{normal=Art(key),pressed=Art(key+"_pressed")},false);
            image.preserveAspect=false;
        }
        var marks=System.Array.ConvertAll(DefaultEmblems,mark=>Mark(mark.supplierId,mark.artwork));
        if(ui.supplierArt==null)ui.supplierArt=marks;
        foreach(var binding in ui.supplierArt)
            if(binding!=null&&binding.emblem==null)foreach(var mark in marks)
                if(mark.supplierId==binding.supplierId)binding.emblem=mark.emblem;
        var listSeed=ui.supplierTemplate;
        var offerSeed=ui.offerTemplate;
        ui.supplierTemplate=StoreLayoutPrefabTools.SaveTemplateAsset(listSeed,StoreLayoutPrefabTools.Folder+"/Rows/SupplierRow.prefab");
        ui.offerTemplate=StoreLayoutPrefabTools.SaveTemplateAsset(offerSeed,StoreLayoutPrefabTools.Folder+"/Rows/SupplierOfferRow.prefab");
        Object.DestroyImmediate(listSeed.gameObject);
        Object.DestroyImmediate(offerSeed.gameObject);
        var inventory=ui.panel.inventory;
        var definitions=JsonUtility.FromJson<SupplierDatabase>(inventory.supplierJson.text);
        var catalog=new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(inventory.itemJson.text));
        bool preview=true;
        foreach(var supplier in definitions.suppliers)
        {
            var listRow=PlaceInstance(ui.supplierTemplate,ui.supplierList,supplier.name);
            listRow.supplierId=supplier.id;
            listRow.nameLabel.text=supplier.name;listRow.descriptionLabel.text=supplier.description;
            listRow.portrait.sprite=ui.Emblem(supplier);listRow.portrait.enabled=listRow.portrait.sprite!=null;
            for(int i=0;i<listRow.sampleIcons.Length;i++)
            {
                var icon=listRow.sampleIcons[i];
                icon.sprite=i<supplier.offers.Length?PreviewIcon(inventory,catalog,supplier.offers[i].itemId):null;
                icon.enabled=icon.sprite!=null;
            }
            listRow.gameObject.SetActive(supplier.initiallyUnlocked);
            foreach(var offer in supplier.offers)
            {
                var offerRow=PlaceInstance(ui.offerTemplate,ui.offerList,supplier.name+" - "+offer.offerId);
                offerRow.supplierId=supplier.id;offerRow.offerId=offer.offerId;
                offerRow.icon.sprite=PreviewIcon(inventory,catalog,offer.itemId);
                offerRow.icon.enabled=offerRow.icon.sprite!=null;
                offerRow.nameLabel.text=catalog.FindQuality(offer.itemId,offer.quality).name;
                offerRow.gameObject.SetActive(preview&&supplier.initiallyUnlocked);
                PrefabUtility.RecordPrefabInstancePropertyModifications(offerRow);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(listRow);
            if(preview&&supplier.initiallyUnlocked){
                ui.portrait.sprite=listRow.portrait.sprite;ui.portrait.enabled=ui.portrait.sprite!=null;
                ui.titleLabel.text=supplier.name;ui.greetingLabel.text=supplier.greeting;preview=false;
            }
        }
        // Save nested instance overrides for labels, images, visibility and IDs on migration too.
        foreach(var content in new[]{ui.supplierList,ui.offerList})
            foreach(Transform child in content){
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                foreach(var component in child.GetComponentsInChildren<Component>(true))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
    }
    static Sprite PreviewIcon(InventoryRuntime inventory,ItemCatalog catalog,string id)
    {
        string key=catalog.IconKey(id);
        var sprite=inventory.spriteCatalog!=null?inventory.spriteCatalog.FindItem(key):null;
        return sprite!=null?sprite:Resources.Load<Sprite>("ItemIcons/"+key);
    }
    static T PlaceInstance<T>(T asset,Transform parent,string name) where T:Component
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset.gameObject,parent);
        go.name=name;return go.GetComponent<T>();
    }
    static void Place(RectTransform node,float x,float y,float w,float h)
    {
        node.anchorMin=node.anchorMax=node.pivot=new Vector2(0,1);
        node.anchoredPosition=new Vector2(x,-y);node.sizeDelta=new Vector2(w,h);
    }
    static void LabelAt(Text label,float x,float y,float w,float h,TextAnchor align)
    {
        Place(label.rectTransform,x,y,w,h);label.alignment=align;
    }
    // Center the icon and its actual text as a single unit. Layout can shrink text for large prices.
    static void CenterPair(RectTransform parent,string name,RectTransform icon,Text text,float x,float y,float w,float h,float iconSize)
    {
        var group=UI.Rect(name,parent,x,y,w,h);
        icon.SetParent(group,false);text.transform.SetParent(group,false);
        var iconLayout=icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.minWidth=iconLayout.preferredWidth=iconSize;
        iconLayout.minHeight=iconLayout.preferredHeight=iconSize;
        var layout=group.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment=TextAnchor.MiddleCenter;layout.spacing=6;
        layout.childControlWidth=layout.childControlHeight=true;
        layout.childForceExpandWidth=layout.childForceExpandHeight=false;
        // Give the text the full height of the band, so its baseline tracks the icon centre.
        var textLayout=text.gameObject.AddComponent<LayoutElement>();textLayout.preferredHeight=h;
        text.alignment=TextAnchor.MiddleCenter;text.horizontalOverflow=HorizontalWrapMode.Wrap;
    }
    public static void AlignSupplierPanel(SupplierUI ui)
    {
        SideMenuSceneBuilder.PreparePanelTexture(Folder+"list",1024);
        SideMenuSceneBuilder.PreparePanelTexture(Folder+"list_row",512);
        SideMenuSceneBuilder.PreparePanelTexture(Folder+"coin",128);
        ui.normalRow=Art("list_row");
        ui.window.transform.Find("Panel Frame").GetComponent<UIImage>().sprite=Art("list");
        // Update the two shared assets, retaining existing nested instances and their item overrides.
        string path=AssetDatabase.GetAssetPath(ui.supplierTemplate);
        using(var scope=new PrefabUtility.EditPrefabContentsScope(path))
        {
            var row=scope.prefabContentsRoot.GetComponent<SupplierListRowView>();
            row.selectButton.image.sprite=ui.normalRow;row.selectButton.image.color=Color.white;
            row.statusLabel.text="";
        }
        path=AssetDatabase.GetAssetPath(ui.offerTemplate);
        using(var scope=new PrefabUtility.EditPrefabContentsScope(path))
        {
            var row=scope.prefabContentsRoot.GetComponent<SupplierOfferRowView>();
            if(row.transform.Find("Price Group")==null){
                var coin=Icon("coin",row.transform,0,0,26,24);
                CenterPair((RectTransform)row.transform,"Price Group",(RectTransform)coin.transform.parent,row.priceLabel,294,0,133,83,25);
            }
            LabelAt(row.stockLabel,432,0,78,83,TextAnchor.MiddleCenter);
            row.priceLabel.text="0G";row.stockLabel.text="0개";
        }
        var list=(RectTransform)ui.listPage.transform;
        var delivery=list.Find("Delivery").GetComponent<Text>();
        CenterPair(list,"Delivery Group",(RectTransform)list.Find("calendar Slot"),delivery,48,656,464,52,31);
        var sheet=(RectTransform)ui.tradePage.transform.Find("Order Panel");
        CenterPair(sheet,"Summary Heading",(RectTransform)sheet.Find("basket Slot"),sheet.Find("Summary").GetComponent<Text>(),66,391,164,54,31);
        var gold=Icon("coin",sheet,0,0,26,24);
        ui.totalLabel.text="주문 합계 0G (0개)";
        CenterPair(sheet,"Order Total",(RectTransform)gold.transform.parent,ui.totalLabel,267,391,270,54,26);
        ui.balanceLabel=Ink("Balance After Order",sheet,535,391,201,54,"주문 후 소지금",18,TextAnchor.MiddleRight);
        ui.cycleLabel.text="다음 날 새벽 도착";
        CenterPair(sheet,"Delivery Group",(RectTransform)sheet.Find("calendar Slot"),ui.cycleLabel,59,456,348,60,31);
        Place((RectTransform)ui.buyButton.transform,450,456,260,60);
        var buttonText=ui.buyButton.transform.Find("Text").GetComponent<Text>();
        LabelAt(buttonText,0,0,260,60,TextAnchor.MiddleCenter);
    }
    static readonly (string supplierId,string artwork)[] DefaultEmblems={
        ("general","B001"),("alchemy","B002"),("equipment","B003"),("processed","B004"),("materials","B005")
    };
    internal static void UpgradeEmblemLayout(SupplierUI ui)
    {
        using(var scope=new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(ui.supplierTemplate)))
        {
            var row=scope.prefabContentsRoot.GetComponent<SupplierListRowView>();
            Place((RectTransform)row.portrait.transform.parent,12,5,109,134);
            row.portrait.preserveAspect=true;
            LabelAt(row.nameLabel,136,8,250,40,TextAnchor.MiddleLeft);
            LabelAt(row.descriptionLabel,136,48,250,36,TextAnchor.MiddleLeft);
            for(int i=0;i<row.sampleIcons.Length;i++)
                Place((RectTransform)row.sampleIcons[i].transform.parent,136+i*49,86,45,48);
            LabelAt(row.statusLabel,244,91,140,36,TextAnchor.MiddleLeft);
        }
        Place((RectTransform)ui.portrait.transform.parent,251,29,76,84);
        ui.portrait.preserveAspect=true;
        LabelAt(ui.titleLabel,340,35,215,39,TextAnchor.MiddleLeft);
        LabelAt(ui.greetingLabel,340,73,318,35,TextAnchor.MiddleLeft);
    }
    internal static void CenterMarkedSections(SupplierUI ui)
    {
        var list=(RectTransform)ui.listPage.transform;
        var listFrame=(RectTransform)list.parent;
        var listSize=listFrame.rect.size;
        // The delivery notice sits between the end of the list and the parchment rule.
        // The close button sits between that rule and the top of the bottom roller.
        float ruleY=listSize.y*1223f/1497f;
        var viewport=(RectTransform)ui.supplierList.parent;
        float listEnd=-viewport.anchoredPosition.y+viewport.rect.height;
        var notice=(RectTransform)list.Find("Delivery Group");
        CenterAt(notice,listSize.x*.5f,(listEnd+ruleY)*.5f);
        var close=(RectTransform)list.Find("close");
        CenterAt(close,listSize.x*.5f,(ruleY+listSize.y*1416f/1497f)*.5f);

        var sheet=(RectTransform)ui.tradePage.transform.Find("Order Panel");
        // The source coordinates mark the borders printed into the order parchment.
        float sx=sheet.rect.width/1496f,sy=sheet.rect.height/1051f;
        float summaryCenter=(106+1350)*.5f*sx;
        float summaryY=(709+803)*.5f*sy;
        CenterAt((RectTransform)sheet.Find("Summary Heading"),(106*sx+9+summaryCenter-124)*.5f,summaryY,164);
        CenterAt((RectTransform)sheet.Find("Order Total"),summaryCenter,summaryY,248);
        CenterAt(ui.balanceLabel.rectTransform,(summaryCenter+124+1350*sx-9)*.5f,summaryY,194);
        ui.balanceLabel.alignment=TextAnchor.MiddleCenter;

        float footerY=(825+932)*.5f*sy;
        CenterAt((RectTransform)sheet.Find("Delivery Group"),(106+736)*.5f*sx,footerY,(736-106)*sx-12);
        CenterAt((RectTransform)ui.buyButton.transform,(736+1350)*.5f*sx,footerY);

        var header=sheet.Find("Supplier Header") as RectTransform;
        if(header==null)
        {
            header=UI.Rect("Supplier Header",sheet,0,0,284,82);
            var layout=header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment=TextAnchor.MiddleCenter;
            layout.spacing=10;
            layout.childControlWidth=layout.childControlHeight=true;
            layout.childForceExpandWidth=layout.childForceExpandHeight=false;
            var emblem=(RectTransform)ui.portrait.transform.parent;
            emblem.SetParent(header,false);
            var emblemLayout=emblem.gameObject.AddComponent<LayoutElement>();
            emblemLayout.minWidth=emblemLayout.preferredWidth=76;
            emblemLayout.minHeight=emblemLayout.preferredHeight=82;
            ui.portrait.preserveAspect=true;

            var labels=UI.Rect("Supplier Header Labels",header,0,0,198,70);
            var column=labels.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childAlignment=TextAnchor.MiddleLeft;
            column.childControlWidth=column.childControlHeight=true;
            column.childForceExpandHeight=false;
            ui.titleLabel.transform.SetParent(labels,false);
            ui.greetingLabel.transform.SetParent(labels,false);
            ui.titleLabel.gameObject.AddComponent<LayoutElement>().preferredHeight=37;
            ui.greetingLabel.gameObject.AddComponent<LayoutElement>().preferredHeight=30;
        }
        // Center the emblem and the text block together between the decorative tips.
        CenterAt(header,(457+1001)*.5f*sx,130*sy,(1001-457)*sx-10);
    }

    static void CenterAt(RectTransform node,float x,float y,float? width=null)
    {
        float w=width??node.rect.width;
        float h=node.rect.height;
        Place(node,x-w*.5f,y-h*.5f,w,h);
    }

    static bool BindImage(UIImage image,string key,bool repair)
    {
        if(image==null)throw new System.InvalidOperationException("거래처 이미지 슬롯 누락: "+key);
        if(image.sprite!=null)return false;
        if(repair)
        {
            image.sprite=Art(key);
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        }
        return true;
    }
    static bool BindButton(Button button,string key,bool repair)
    {
        var image=button!=null?button.GetComponent<UIImage>():null;
        if(image==null)throw new System.InvalidOperationException("거래처 버튼 이미지 누락: "+key);
        var state=button.spriteState;
        if(image.sprite!=null&&button.targetGraphic==image&&button.transition==Selectable.Transition.SpriteSwap
            &&state.pressedSprite!=null&&state.pressedSprite!=image.sprite&&state.disabledSprite!=null&&state.highlightedSprite!=null&&state.selectedSprite!=null)
            return false;
        if(repair)
        {
            ButtonArtworkCatalog.Apply(button,image,new ButtonSpriteSet{normal=Art(key),pressed=Art(key+"_pressed")},false);
            image.preserveAspect=false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
        }
        return true;
    }
    static bool CheckListRowArtwork(SupplierListRowView row,bool repair)
    {
        var background=row.selectButton.image;
        bool changed=BindImage(background,"list_row",repair)
            |BindImage(row.transform.Find("chevron Slot/chevron")?.GetComponent<UIImage>(),"chevron",repair);
        if(background.color.a<=0)
        {
            changed=true;
            if(repair)
            {
                background.color=Color.white;
                PrefabUtility.RecordPrefabInstancePropertyModifications(background);
            }
        }
        return changed;
    }
    static bool CheckOfferRowArtwork(SupplierOfferRowView row,bool repair)
    {
        return BindImage(row.GetComponent<UIImage>(),"offer_row",repair)
            |BindImage(row.transform.Find("Price Group/coin Slot/coin")?.GetComponent<UIImage>(),"coin",repair)
            |BindButton(row.minusButton,"minus",repair)
            |BindButton(row.plusButton,"plus",repair);
    }
    internal static void RepairRowArtwork(SupplierUI ui)
    {
        if(CheckListRowArtwork(ui.supplierTemplate,false))
        {
            using(var scope=new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(ui.supplierTemplate)))
                CheckListRowArtwork(scope.prefabContentsRoot.GetComponent<SupplierListRowView>(),true);
        }
        if(CheckOfferRowArtwork(ui.offerTemplate,false))
        {
            using(var scope=new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(ui.offerTemplate)))
                CheckOfferRowArtwork(scope.prefabContentsRoot.GetComponent<SupplierOfferRowView>(),true);
        }
    }
    internal static bool CheckArtwork(SupplierUI ui,bool repair)
    {
        var list=ui.listPage.transform;
        var sheet=ui.tradePage.transform.Find("Order Panel");
        bool changed=BindImage(ui.window.transform.Find("Panel Frame").GetComponent<UIImage>(),"list",repair)
            |BindImage(sheet.GetComponent<UIImage>(),"detail",repair)
            |BindImage(list.Find("Delivery Group/calendar Slot/calendar")?.GetComponent<UIImage>(),"calendar",repair)
            |BindImage(sheet.Find("Delivery Group/calendar Slot/calendar")?.GetComponent<UIImage>(),"calendar",repair)
            |BindImage(sheet.Find("Summary Heading/basket Slot/basket")?.GetComponent<UIImage>(),"basket",repair)
            |BindImage(sheet.Find("Order Total/coin Slot/coin")?.GetComponent<UIImage>(),"coin",repair)
            |BindButton(list.Find("close").GetComponent<Button>(),"close",repair)
            |BindButton(ui.buyButton,"order",repair);
        if(ui.normalRow==null){changed=true;if(repair)ui.normalRow=Art("list_row");}
        if(ui.selectedRow==null){changed=true;if(repair)ui.selectedRow=Art("selected");}

        foreach(var mark in DefaultEmblems)
        {
            if(ui.FindEmblem(mark.supplierId)!=null)continue;
            changed=true;
            if(!repair)continue;
            var bindings=ui.supplierArt??new SupplierUI.SupplierArt[0];
            var binding=System.Array.Find(bindings,entry=>entry!=null&&entry.supplierId==mark.supplierId);
            if(binding!=null)binding.emblem=Art(mark.artwork);
            else
            {
                System.Array.Resize(ref bindings,bindings.Length+1);
                bindings[bindings.Length-1]=Mark(mark.supplierId,mark.artwork);
                ui.supplierArt=bindings;
            }
        }
        foreach(var row in ui.supplierList.GetComponentsInChildren<SupplierListRowView>(true))
        {
            changed|=CheckListRowArtwork(row,repair);
            var emblem=ui.FindEmblem(row.supplierId);
            if(emblem==null)continue;
            if(row.portrait.sprite==null||!row.portrait.enabled)
            {
                changed=true;
                if(repair)
                {
                    row.portrait.sprite=emblem;row.portrait.enabled=true;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(row.portrait);
                }
            }
            if(ui.portrait.sprite==null&&row.nameLabel.text==ui.titleLabel.text)
            {
                changed=true;
                if(repair){ui.portrait.sprite=emblem;ui.portrait.enabled=true;}
            }
        }
        foreach(var row in ui.offerList.GetComponentsInChildren<SupplierOfferRowView>(true))
            changed|=CheckOfferRowArtwork(row,repair);
        return changed;
    }
    static SupplierUI.SupplierArt Mark(string id,string key)
    {return new SupplierUI.SupplierArt{supplierId=id,emblem=Art(key)};}
}
#endif
