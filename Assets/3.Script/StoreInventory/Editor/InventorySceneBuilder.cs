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
    const float Cell=78; // 이전 52의 정확히 1.5배. 데이터의 칸 수는 변하지 않습니다.
    static Font font;
    static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
        rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(width,height);return rt;
    }
    static void Stretch(RectTransform rect)
    {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static global::UnityEngine.UI.Image Box(string name,Transform parent,float x,float y,float w,float h,Color color)
    {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<global::UnityEngine.UI.Image>();image.color=color;return image;}
    static Text Label(string name,Transform parent,float x,float y,float w,float h,string content,int size=22)
    {
        var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;
        text.color=new Color(.97f,.9f,.75f);text.text=content;text.raycastTarget=false;text.supportRichText=false;return text;
    }
    static Button ButtonBox(string label,Transform parent,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
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
        var scroll=root.gameObject.AddComponent<ScrollRect>();scroll.horizontal=true;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        var viewport=Box("Viewport",root,0,0,viewW,viewH,new Color(.1f,.09f,.07f));viewport.gameObject.AddComponent<RectMask2D>();
        var image=Box(id,viewport.transform,0,0,columns*Cell,rows*Cell,new Color(.3f,.24f,.15f));
        var grid=image.gameObject.AddComponent<InventoryGridView>();grid.containerId=id;grid.cellSize=Cell;grid.itemTemplate=template;
        grid.itemLayer=Rect("Items",grid.transform,0,0,columns*Cell,rows*Cell);Stretch(grid.itemLayer);
        scroll.viewport=viewport.rectTransform;scroll.content=grid.Rect;
        // 가로/세로 길이가 달라도 스크롤바를 정확히 배치합니다.
        var horizontal=ScrollBar(root,true,viewW);horizontal.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-viewH-4);
        var vertical=ScrollBar(root,false,viewH);vertical.GetComponent<RectTransform>().anchoredPosition=new Vector2(viewW+4,0);
        scroll.horizontalScrollbar=horizontal;scroll.verticalScrollbar=vertical;return grid;
    }
    static InventoryDetailsView Details(Transform parent,float y,float width)
    {
        var background=Box("Details",parent,20,y,width,156,new Color(.16f,.12f,.08f));
        var details=background.gameObject.AddComponent<InventoryDetailsView>();
        details.icon=Box("Icon",background.transform,10,20,90,90,Color.white);details.icon.enabled=false;details.icon.raycastTarget=false;
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
    [MenuItem("Store Inventory/Create Canvas Demo")]
    public static void Create()
    {
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
        var data=new GameObject("Store Inventory Data");Undo.RegisterCreatedObjectUndo(data,"Create Store Data");var runtime=data.AddComponent<InventoryRuntime>();
        var go=new GameObject("Store Inventory Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go,"Create Store Canvas");var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var panel=go.AddComponent<InventoryPanel>();panel.inventory=runtime;panel.canvas=canvas;
        var shop=go.AddComponent<ShopfrontUI>();shop.panel=panel;panel.shop=shop;
        var background=Box("Background",go.transform,0,0,1920,1080,new Color(.075f,.06f,.045f));Stretch(background.rectTransform);
        var layout=Rect("Layout 1920x1080",go.transform,0,0,1920,1080);layout.anchorMin=layout.anchorMax=layout.pivot=new Vector2(.5f,.5f);layout.anchoredPosition=Vector2.zero;
        ButtonBox("보관함 열기/닫기",layout,24,24,200,52,shop.ToggleWarehouse);
        ButtonBox("진열장 열기/닫기",layout,240,24,200,52,shop.ToggleDisplay);
        ButtonBox("손님 맞이",layout,456,24,160,52,shop.EnterCustomer);
        ButtonBox("저장",layout,984,24,140,52,panel.SaveInventory);ButtonBox("불러오기",layout,1140,24,140,52,panel.LoadInventory);
        shop.goldLabel=Label("Gold",layout,1530,26,360,52,"1,000 G",30);
        panel.titleLabel=Label("Inventory Status",layout,24,84,900,34,"보관함",21);
        // 공용 카드: 루트(클릭), 아이콘 메시 하나, 대체 이름 하나뿐입니다.
        var card=Box("Item Template",layout,0,0,156,156,new Color(.35f,.23f,.12f));
        var template=card.gameObject.AddComponent<InventoryItemView>();
        var iconRect=Rect("Icon Mesh",card.transform,0,0,156,156);Stretch(iconRect);
        iconRect.gameObject.AddComponent<CanvasRenderer>();
        template.icon=iconRect.gameObject.AddComponent<InventoryIconGraphic>();template.icon.raycastTarget=false;
        template.nameLabel=Label("Fallback Name",card.transform,3,3,72,72,"",15);card.gameObject.SetActive(false);

        var warehouse=Box("Warehouse Window",layout,24,126,690,930,new Color(.15f,.115f,.075f));shop.warehouseWindow=warehouse.gameObject;
        Label("Title",warehouse.transform,24,18,560,50,"보관함",30);ButtonBox("×",warehouse.transform,632,16,40,40,shop.CloseWarehouse);
        var warehouseGrid=GridScroll(warehouse.transform,"warehouse",24,96,624,624,runtime.warehouseColumns,runtime.warehouseRows,template);
        var warehouseDetails=Details(warehouse.transform,752,650);
        var display=Box("Display Window",layout,752,126,558,818,new Color(.16f,.12f,.08f));shop.displayWindow=display.gameObject;
        Label("Title",display.transform,24,18,450,44,"진열장",30);ButtonBox("×",display.transform,500,16,40,40,shop.CloseDisplay);
        ButtonBox("진열장 1",display.transform,24,74,240,44,shop.ShowShelf1);ButtonBox("진열장 2",display.transform,280,74,240,44,shop.ShowShelf2);
        var page1=Rect("Shelf Page 1",display.transform,0,136,550,350);var page2=Rect("Shelf Page 2",display.transform,0,136,550,350);
        shop.shelfPage1=page1.gameObject;shop.shelfPage2=page2.gameObject;
        var shelf1=GridScroll(page1,"display_1",24,0,468,312,6,4,template);var shelf2=GridScroll(page2,"display_2",24,0,468,312,6,4,template);
        var displayDetails=Details(display.transform,504,518);
        Label("Hint",display.transform,24,684,510,110,"물건을 드래그해 놓으면 판매가 입력창이 열립니다.\n보관함으로 가져오면 진열 가격 등록이 해제됩니다.",20);
        panel.grids=new[]{warehouseGrid,shelf1,shelf2};panel.detailViews=new[]{warehouseDetails,displayDetails};
        page2.gameObject.SetActive(false);display.gameObject.SetActive(false);

        shop.customerPortrait=Box("Customer Portrait",layout,764,170,520,520,Color.white);shop.customerPortrait.raycastTarget=false;shop.customerPortrait.preserveAspect=true;shop.customerPortrait.enabled=false;
        var dialogue=Box("Customer Dialogue",layout,744,728,570,184,new Color(.19f,.15f,.1f));
        shop.customerNameLabel=Label("Name",dialogue.transform,18,10,530,36,"손님 대기 중",24);
        shop.dialogueLabel=Label("Dialogue",dialogue.transform,18,54,530,120,"",23);
        var recommendation=Box("Recommendation Window",layout,1360,126,536,824,new Color(.19f,.15f,.1f));
        Label("Title",recommendation.transform,20,18,496,52,"추천할 물건",30);
        var dropArea=recommendation.gameObject.AddComponent<RecommendationDropArea>();dropArea.panel=panel;
        var offerViewport=Box("Offer Viewport",recommendation.transform,20,90,496,350,new Color(.12f,.1f,.075f));offerViewport.gameObject.AddComponent<RectMask2D>();
        var offerContent=Rect("Offer List",offerViewport.transform,0,0,486,350);shop.offerList=offerContent;
        var vertical=offerContent.gameObject.AddComponent<VerticalLayoutGroup>();vertical.spacing=8;vertical.childControlHeight=true;vertical.childControlWidth=true;vertical.childForceExpandHeight=false;
        offerContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var offerScroll=offerViewport.gameObject.AddComponent<ScrollRect>();offerScroll.content=offerContent;offerScroll.viewport=offerViewport.rectTransform;offerScroll.horizontal=false;offerScroll.vertical=true;
        shop.offerTemplate=OfferTemplate(layout);
        ButtonBox("선택한 물건 추천",recommendation.transform,20,462,496,52,shop.RecommendSelected);
        shop.totalLabel=Label("Total",recommendation.transform,20,544,496,52,"제안 합계 0 G",27);
        shop.submitButton=ButtonBox("제안하고 판매",recommendation.transform,20,622,496,62,shop.SubmitOffer);
        ButtonBox("손님 보내기",recommendation.transform,20,706,496,52,shop.LeaveCustomer);
        panel.messageLabel=Label("Message",layout,744,968,1152,88,"",21);

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
        Selection.activeGameObject=go;UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("1920×1080 통합 데모 생성 완료. 한글 폰트/배경/손님 Sprite를 Inspector에서 연결하세요.");
    }
}
#endif
