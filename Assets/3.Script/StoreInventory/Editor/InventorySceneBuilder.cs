#if UNITY_EDITOR
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// 1920x1080 기준 UI를 실제 씬에 생성합니다. 기존 데모는 비활성화하고 새 씬에서 실행하세요.
public static class InventorySceneBuilder
{
    const float CellWidth=78,CellHeight=85.8f; // 이전 52의 정확히 1.5배. 데이터의 칸 수는 변하지 않습니다.
    static Font font;
    public static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
        rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(width,height);return rt;
    }
    static void Stretch(RectTransform rect)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=rect.offsetMax=Vector2.zero;}
    public static global::UnityEngine.UI.Image Box(string name,Transform parent,float x,float y,float w,float h,Color color)
    {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<global::UnityEngine.UI.Image>();image.color=color;return image;}
    public static Text Label(string name,Transform parent,float x,float y,float w,float h,string content,int size=22)
    {
        var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;
        text.color=new Color(.97f,.9f,.75f);text.text=content;text.raycastTarget=false;text.supportRichText=false;return text;
    }
    public static Button ButtonBox(string label,Transform parent,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
    {
        var image=Box(label,parent,x,y,w,h,new Color(.4f,.25f,.1f));var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        Label("Text",image.transform,0,0,w,h,label,20).alignment=TextAnchor.MiddleCenter;
        if(action!=null)UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,action);return button;
    }
    static Scrollbar ScrollBar(Transform parent,bool horizontal,float length)
    {
        var image=Box(horizontal?"Horizontal Scrollbar":"Vertical Scrollbar",parent,horizontal?0:length+4,horizontal?length+4:0,
            horizontal?length:18,horizontal?18:length,new Color(.14f,.12f,.09f));
        var bar=image.gameObject.AddComponent<Scrollbar>();bar.direction=horizontal?Scrollbar.Direction.LeftToRight:Scrollbar.Direction.BottomToTop;
        RectTransform area=Rect("Sliding Area",image.transform,0,0,0,0);Stretch(area);
        var handle=Box("Handle",area,0,0,0,0,new Color(.55f,.4f,.2f));Stretch(handle.rectTransform);
        bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;return bar;
    }
    static InventoryGridView GridScroll(Transform parent,string id,float x,float y,float viewW,float viewH,int columns,int rows,InventoryItemView template)
    {
        RectTransform root=Rect(id+" Scroll",parent,x,y,viewW+22,viewH+22);
        var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.horizontal=!InventoryModel.IsWarehouse(id);scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        var viewport=Box("Viewport",root,0,0,viewW,viewH,new Color(.1f,.09f,.07f));viewport.gameObject.AddComponent<RectMask2D>();
        var image=Box(id,viewport.transform,0,0,columns*CellWidth,rows*CellHeight,new Color(.3f,.24f,.15f));
        var grid=image.gameObject.AddComponent<InventoryGridView>();grid.containerId=id;grid.cellWidth=CellWidth;grid.cellHeight=CellHeight;grid.itemTemplate=template;
        grid.itemLayer=Rect("Items",grid.transform,0,0,columns*CellWidth,rows*CellHeight);Stretch(grid.itemLayer);
        scroll.viewport=viewport.rectTransform;scroll.content=grid.Rect;
        // 가로/세로 길이가 달라도 스크롤바를 정확히 배치합니다.
        if(!InventoryModel.IsWarehouse(id))
        {var horizontal=ScrollBar(root,true,viewW);horizontal.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-viewH-4);scroll.horizontalScrollbar=horizontal;}
        var vertical=ScrollBar(root,false,viewH);vertical.GetComponent<RectTransform>().anchoredPosition=new Vector2(viewW+4,0);
        scroll.verticalScrollbar=vertical;return grid;
    }
    static InventoryDetailsView Details(Transform parent,float y,float width)
    {
        var background=Box("Details",parent,20,y,width,156,new Color(.16f,.12f,.08f));
        var details=background.gameObject.AddComponent<InventoryDetailsView>();
        details.iconBackground=Box("Icon Background",background.transform,10,20,96,96,new Color(0,0,0,1));details.iconBackground.raycastTarget=false;
        details.iconBackground.gameObject.AddComponent<RectMask2D>();
        details.icon=Box("Icon",details.iconBackground.transform,6,6,84,84,Color.white);details.icon.enabled=false;details.icon.raycastTarget=false;
        details.iconFit=details.iconBackground.gameObject.AddComponent<SpriteIconFit>();details.iconFit.image=details.icon;details.iconFit.frame=details.iconBackground.rectTransform;
        details.nameLabel=Label("Name",background.transform,112,8,width-124,32,"물건을 선택하세요.",23);
        details.bodyLabel=Label("Body",background.transform,112,44,width-124,106,"",18);return details;
    }
    static OfferRowView OfferTemplate(Transform parent)
    {
        var image=Box("Offer Row Template",parent,0,0,486,96,new Color(.25f,.2f,.13f));
        var row=image.gameObject.AddComponent<OfferRowView>();
        row.icon=Box("Icon",image.transform,8,12,68,68,Color.white);row.icon.raycastTarget=false;
        row.nameLabel=Label("Name",image.transform,86,12,250,60,"",21);
        row.priceLabel=Label("Price",image.transform,338,22,112,42,"",22);
        row.removeButton=ButtonBox("×",image.transform,450,4,32,32,null);
        var element=image.gameObject.AddComponent<LayoutElement>();element.preferredHeight=96;image.gameObject.SetActive(false);return row;
    }
    static RectTransform ItemList(Transform parent,string name,float y,float height,InventoryPanel panel,bool acceptsRecommendations)
    {
        var viewport=Box(name,parent,20,y,496,height,new Color(.12f,.1f,.075f));viewport.gameObject.AddComponent<RectMask2D>();
        if(acceptsRecommendations)viewport.gameObject.AddComponent<RecommendationDropArea>().panel=panel;
        var content=Rect("Content",viewport.transform,0,0,486,height);
        var vertical=content.gameObject.AddComponent<VerticalLayoutGroup>();vertical.spacing=8;vertical.childControlHeight=true;vertical.childControlWidth=true;vertical.childForceExpandHeight=false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.content=content;scroll.viewport=viewport.rectTransform;scroll.horizontal=false;scroll.vertical=true;
        return content;
    }
    [MenuItem("Store Inventory/Create Canvas Demo")]
    public static void Create()
    {
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
        var data=new GameObject("Store Inventory Data");Undo.RegisterCreatedObjectUndo(data,"Create Store Data");var runtime=data.AddComponent<InventoryRuntime>();data.AddComponent<StorePerformanceSettings>();
        var go=new GameObject("Store Inventory Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go,"Create Store Canvas");var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var panel=go.AddComponent<InventoryPanel>();panel.inventory=runtime;panel.canvas=canvas;
        var shop=go.AddComponent<ShopfrontUI>();shop.panel=panel;panel.shop=shop;
        var supplier=go.AddComponent<SupplierUI>();supplier.panel=panel;
        var displayUI=go.AddComponent<DisplayUI>();displayUI.panel=panel;shop.displays=displayUI;
        var management=go.AddComponent<DisplayManagementUI>();management.panel=panel;management.display=displayUI;
        var upgrade=go.AddComponent<UpgradeUI>();upgrade.panel=panel;upgrade.display=displayUI;
        var simulation=data.AddComponent<ShopSimulation>();simulation.inventory=runtime;simulation.shop=shop;shop.simulation=simulation;
        var background=Box("Background",go.transform,0,0,1920,1080,new Color(.075f,.06f,.045f));Stretch(background.rectTransform);background.raycastTarget=false;
        var layout=Rect("Layout 1920x1080",go.transform,0,0,1920,1080);layout.anchorMin=layout.anchorMax=layout.pivot=new Vector2(.5f,.5f);layout.anchoredPosition=Vector2.zero;
        // 가장 뒤의 UI 바닥입니다. 창/버튼 위에 놓으면 바닥으로 통과하지 않습니다.
        var floorImage=Box("Floor Area",layout,0,0,1920,1080,new Color(1,1,1,0));
        floorImage.canvasRenderer.cullTransparentMesh=false;
        var floor=floorImage.gameObject.AddComponent<FloorDropArea>();floor.panel=panel;
        var floorCard=Box("Floor Item Template",layout,0,0,64,64,Color.white);
        var floorView=floorCard.gameObject.AddComponent<FloorItemView>();floorView.icon=floorCard;
        floorView.fallbackName=Label("Fallback Name",floorCard.transform,0,0,64,64,"",14);
        floorCard.gameObject.SetActive(false);floor.itemTemplate=floorView;
        var parcelCard=Box("Parcel Template",layout,0,0,96,96,new Color(.55f,.32f,.12f));
        var parcelView=parcelCard.gameObject.AddComponent<ParcelView>();parcelView.icon=parcelCard;
        parcelView.label=Label("Parcel Name",parcelCard.transform,0,0,96,96,"",17);parcelView.label.alignment=TextAnchor.MiddleCenter;
        parcelCard.gameObject.SetActive(false);floor.parcelTemplate=parcelView;
        var counterLine=Box("Counter Surface Guide",floorImage.transform,0,864,1920,2,new Color(1,.75f,.3f,.6f));
        counterLine.raycastTarget=false;floor.surfaceLine=counterLine.rectTransform;
        Label("Floor Hint",layout,760,916,570,46,"빈 배경에 내려놓기 / 바닥 물건을 창고로 드래그",18);
        var furnitureLayer=Rect("Scene Furniture Hotspots",layout,0,0,1920,1080);
        ButtonBox("손님 맞이",layout,456,24,160,52,shop.EnterCustomer);
        ButtonBox("저장",layout,632,24,100,52,panel.SaveInventory);ButtonBox("불러오기",layout,744,24,110,52,panel.LoadInventory);
        simulation.clockLabel=Label("Clock",layout,1530,24,360,42,"",22);simulation.showRemainingTime=false;
        ButtonBox("시간 정지/재개",layout,24,80,180,38,simulation.ToggleTime);
        ButtonBox("다음 단계 테스트",layout,216,80,190,38,simulation.AdvanceToNextPhase);
        ButtonBox("다음 새벽 배송 테스트",layout,418,80,250,38,simulation.QueueTestDelivery);
        shop.goldLabel=Label("Gold",layout,1230,24,290,42,"1,000 G",26);
        ButtonBox("거래처 열기/닫기",layout,680,80,200,38,supplier.Toggle);
        ButtonBox("진열장 관리",layout,900,80,180,38,management.Toggle);ButtonBox("업그레이드",layout,1096,80,180,38,upgrade.Toggle);
        panel.titleLabel=Label("Inventory Status",layout,1296,84,600,34,"보관함",18);panel.titleLabel.gameObject.SetActive(false);
        // 공용 카드: 루트(클릭), 아이콘 메시 하나, 대체 이름 하나뿐입니다.
        var card=Box("Item Template",layout,0,0,156,156,new Color(.35f,.23f,.12f));
        var template=card.gameObject.AddComponent<InventoryItemView>();
        var iconRect=Rect("Icon Mesh",card.transform,0,0,156,156);Stretch(iconRect);
        iconRect.gameObject.AddComponent<CanvasRenderer>();
        template.icon=iconRect.gameObject.AddComponent<InventoryIconGraphic>();template.icon.raycastTarget=false;
        template.nameLabel=Label("Fallback Name",card.transform,3,3,72,72,"",15);card.gameObject.SetActive(false);

        shop.customerPortrait=Box("Customer Portrait",layout,764,170,520,520,Color.white);shop.customerPortrait.raycastTarget=false;shop.customerPortrait.preserveAspect=true;shop.customerPortrait.enabled=false;
        var dialogue=Box("Customer Dialogue",layout,744,728,570,184,new Color(.19f,.15f,.1f));
        shop.dialogueWindow=dialogue.gameObject;
        shop.customerNameLabel=Label("Name",dialogue.transform,18,10,530,36,"손님 대기 중",24);
        shop.dialogueLabel=Label("Dialogue",dialogue.transform,18,54,530,120,"",23);

        var tabTemplate=FixtureSceneBuilder.TabTemplate(layout);
        var warehouse=Box("Warehouse Window",layout,24,146,690,882,new Color(.15f,.115f,.075f));shop.warehouseWindow=warehouse.gameObject;

        var warehouseGrid=GridScroll(warehouse.transform,"warehouse",24,164,624,450,InventoryRuntime.WarehouseColumns,runtime.warehouseRows,template);
        var warehouseUI=go.AddComponent<WarehouseUI>();warehouseUI.panel=panel;warehouseUI.grid=warehouseGrid;warehouseUI.scroll=warehouseGrid.GetComponentInParent<ScrollRect>();upgrade.warehouse=warehouseUI;
        warehouseUI.tabs=FixtureSceneBuilder.Tabs(warehouse.transform,tabTemplate,24,70,624);warehouseUI.tabs.warehouse=warehouseUI;
        warehouseUI.pageLabel=Label("Warehouse Status",warehouse.transform,24,122,624,34,"",21);
        var warehouseDetails=Details(warehouse.transform,650,650);


        var definitionFile=Resources.Load<TextAsset>("StoreInventory/display_fixtures");
        if(definitionFile==null)throw new System.InvalidOperationException("display_fixtures.json을 추가하세요.");
        var fixtures=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(definitionFile.text));
        var initialDisplaySave=new InventorySave();fixtures.AddInitialContainers(initialDisplaySave);
        var firstFixture=initialDisplaySave.containers[0];
        var display=Box("Display Window",layout,960,146,690,882,new Color(.16f,.12f,.08f));shop.displayWindow=display.gameObject;

        var displayGrid=GridScroll(display.transform,firstFixture.containerId,24,164,624,450,firstFixture.columns,firstFixture.rows,template);
        var displayView=display.gameObject.AddComponent<DisplayWindowView>();displayView.grid=displayGrid;displayUI.windowTemplate=displayView;
        displayView.title=Label("Display Selected Title",display.transform,24,122,624,34,"",20);
        displayView.status=Label("Display Status",display.transform,24,826,350,40,"",18);
        var finishDisplay=ButtonBox("닫기",display.transform,400,826,246,40,displayView.Close);
        var displayDetails=Details(display.transform,650,650);
        displayView.details=displayDetails;panel.grids=new[]{warehouseGrid};panel.detailViews=new[]{warehouseDetails};display.gameObject.SetActive(false);

        var customerWindow=Box("Customer Window",layout,1360,126,536,824,new Color(.19f,.15f,.1f));shop.customerWindow=customerWindow.gameObject;
        var checkout=Rect("Checkout Page",customerWindow.transform,0,0,536,824);shop.checkoutPage=checkout.gameObject;
        Label("Title",checkout,20,18,496,52,"가져온 물건",30);
        shop.purchaseList=ItemList(checkout,"Purchase List",90,420,panel,false);
        shop.purchaseTotalLabel=Label("Purchase Total",checkout,20,532,496,52,"",27);
        ButtonBox("판매",checkout,20,604,496,60,shop.SellPurchases);
        ButtonBox("이야기하기",checkout,20,678,496,56,shop.BeginTalk);
        ButtonBox("거래 거절",checkout,20,748,496,52,shop.LeaveCustomer);
        var recommend=Rect("Recommendation Page",customerWindow.transform,0,0,536,824);shop.recommendationPage=recommend.gameObject;
        shop.summaryLabel=Label("Purchase Summary",recommend,20,18,496,48,"",23);
        Label("Title",recommend,20,78,496,48,"추천할 물건",30);
        shop.offerList=ItemList(recommend,"Recommendation Drop Zone",140,350,panel,true);
        Label("Drop Hint",recommend,20,498,496,40,"위 영역에 물건을 드래그해서 놓으세요.",20);
        shop.totalLabel=Label("Total",recommend,20,548,496,52,"",23);
        shop.submitButton=ButtonBox("제안하고 함께 계산",recommend,20,622,496,58,shop.SubmitOffer);
        ButtonBox("가져온 물건만 판매",recommend,20,694,496,52,shop.SellPurchases);
        ButtonBox("거래 거절",recommend,20,758,496,48,shop.LeaveCustomer);
        shop.offerTemplate=OfferTemplate(layout);
        customerWindow.gameObject.SetActive(false);recommend.gameObject.SetActive(false);dialogue.gameObject.SetActive(false);
        warehouse.gameObject.SetActive(true);
        panel.messageLabel=Label("Message",layout,744,968,1152,88,"",21);

        // 창 내부 아이템 드래그와 창 이동을 분리하며 모달보다 아래에서만 앞뒤 순서를 바꿉니다.
        var floating=Rect("Floating Windows",layout,0,0,1920,1080);
        warehouse.transform.SetParent(floating,false);display.transform.SetParent(floating,false);
        // 서랍 본문과 손잡이를 분리합니다. 닫혀도 루트/손잡이는 유지됩니다.
        var body=Rect("Drawer Body",warehouse.transform,0,0,690,882);
        warehouseGrid.GetComponentInParent<ScrollRect>().transform.SetParent(body,false);
        warehouseUI.tabs.transform.SetParent(body,false);warehouseUI.pageLabel.transform.SetParent(body,false);warehouseDetails.transform.SetParent(body,false);
        var handle=Box("Drawer Handle",warehouse.transform,246,0,198,60,new Color(.5f,.32f,.12f));
        Label("Handle Hint",handle.transform,0,0,198,60,"상자 ⇅",24).alignment=TextAnchor.MiddleCenter;
        var drawer=handle.gameObject.AddComponent<WarehouseDrawer>();shop.drawer=drawer;
        drawer.panel=panel;drawer.window=warehouse.rectTransform;drawer.bounds=floating;drawer.body=body;drawer.openedPosition=new Vector2(24,-146);
        FixtureSceneBuilder.Header(display.transform,floating,panel,690,"진열장");displayUI.windowLayer=floating;
        var appearance=display.gameObject.AddComponent<DisplayWindowAppearance>();displayView.appearance=appearance;appearance.independentWindow=true;
        appearance.window=display.rectTransform;appearance.bounds=floating;
        var frame=Box("Furniture Frame Sprite",display.transform,0,0,690,882,new Color(.2f,.15f,.1f));frame.raycastTarget=false;frame.transform.SetAsFirstSibling();
        display.color=new Color(0,0,0,0);display.canvasRenderer.cullTransparentMesh=false;appearance.frame=frame;
        appearance.grid=displayGrid;appearance.scroll=DisplayWindowAppearance.FindScrollInParents(displayGrid.transform);
        appearance.header=display.GetComponentsInChildren<DraggableWindow>(true)[0].GetComponent<RectTransform>();
        displayView.title.transform.SetParent(appearance.header,false);appearance.title=displayView.title.rectTransform;appearance.status=displayView.status.rectTransform;
        appearance.finishButton=(RectTransform)finishDisplay.transform;appearance.details=(RectTransform)displayDetails.transform;
        appearance.detailsName=displayDetails.nameLabel;appearance.detailsBody=displayDetails.bodyLabel;
        appearance.profiles=new[]{
            new DisplayAppearanceProfile{typeId="shelf",initialPosition=new Vector2(1080,-100),frameColor=new Color(.3f,.2f,.1f),topPadding=100,bottomPadding=174},
            new DisplayAppearanceProfile{typeId="wall_shelf",initialPosition=new Vector2(290,-300),frameColor=new Color(.2f,.27f,.18f),topPadding=96,bottomPadding=224,closeBelowFrame=true},
            new DisplayAppearanceProfile{typeId="weapon_rack",initialPosition=new Vector2(1300,-100),frameColor=new Color(.23f,.18f,.29f),sidePadding=50,minimumWidth=440,topPadding=100,bottomPadding=174},
            new DisplayAppearanceProfile{typeId="table",initialPosition=new Vector2(740,-470),frameColor=new Color(.3f,.23f,.16f),sidePadding=46,topPadding=100,bottomPadding=174}
        };
        DrawerSceneBuilder.Hotspots(furnitureLayer,panel,displayUI);
        FixtureSceneBuilder.BuildManagement(layout,management,tabTemplate);
        FixtureSceneBuilder.BuildUpgrades(layout,upgrade);
        SupplierSceneBuilder.Build(layout,supplier);

        var modal=Box("Display Price Modal",layout,0,0,1920,1080,new Color(0,0,0,.65f));shop.priceDialog=modal.gameObject;
        var priceBox=Box("Price Dialog",modal.transform,710,390,500,280,new Color(.23f,.17f,.1f));
        Label("Title",priceBox.transform,24,20,452,56,"진열 판매가 설정",28);
        var inputImage=Box("Price Input",priceBox.transform,24,90,452,60,new Color(.1f,.085f,.06f));
        var input=inputImage.gameObject.AddComponent<InputField>();input.contentType=InputField.ContentType.IntegerNumber;input.targetGraphic=inputImage;
        input.textComponent=Label("Text",inputImage.transform,12,8,428,44,"100",26);shop.displayPriceInput=input;
        ButtonBox("진열 확정",priceBox.transform,24,196,214,52,shop.ConfirmDisplayPrice);ButtonBox("취소",priceBox.transform,262,196,214,52,shop.CancelDisplayPrice);
        modal.gameObject.SetActive(false);
        var layer=Rect("Drag Layer",go.transform,0,0,1920,1080);Stretch(layer);panel.drag=go.AddComponent<InventoryDragHandler>();panel.drag.dragLayer=layer;
        if(UnityEngine.Object.FindFirstObjectByType<EventSystem>()==null)
        {
            var es=new GameObject("EventSystem",typeof(EventSystem));Undo.RegisterCreatedObjectUndo(es,"Create EventSystem");
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
        DrawerSceneBuilder.MoveDebugButtons(layout);
        Selection.activeGameObject=go;UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("1920×1080 통합 데모 생성 완료. 한글 폰트/배경/손님 Sprite를 Inspector에서 연결하세요.");
    }
}
#endif
