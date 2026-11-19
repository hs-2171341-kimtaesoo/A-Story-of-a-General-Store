#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class CounterDemoSceneBuilder
{
    const string Folder="Assets/Resources/StoreInventory/CounterDemo/";
    public static void PrepareSprites()
    {
        Import("warehouse_frame",1024);Import("background",StoreTextureBudget.Background);
    }
    static void Import(string name,int maximum)
    {
        string path=Folder+name+".png";AssetDatabase.ImportAsset(path);var i=AssetImporter.GetAtPath(path) as TextureImporter;if(i==null)throw new System.InvalidOperationException(path+"가 없습니다.");
        if(i.textureType==TextureImporterType.Sprite&&i.spriteImportMode==SpriteImportMode.Single&&i.alphaIsTransparency&&i.maxTextureSize==maximum&&!i.isReadable&&!i.mipmapEnabled&&i.textureCompression==TextureImporterCompression.Compressed)return;
        i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.isReadable=false;i.maxTextureSize=maximum;i.textureCompression=TextureImporterCompression.Compressed;i.SaveAndReimport();
    }
    static void Place(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    public static void Build(RectTransform layout,InventoryPanel panel,WarehouseUI warehouse,InventoryDetailsView details,SceneCounterOcclusion counter)
    {
        ApplyWarehouse(panel,warehouse,details);ApplyBackground(counter,panel);
        var shop=panel.shop;var trade=layout.gameObject.AddComponent<CounterTradeUI>();trade.panel=panel;trade.shop=shop;shop.counterTrade=trade;
        var root=UI.Rect("Counter Trade",layout,0,0,1920,1080);
        trade.tableItems=UI.Rect("Physical Goods On Counter",root,0,0,1920,1080);
        var item=UI.Box("Counter Item Template",root,0,0,76,76,Color.white);trade.itemTemplate=item.gameObject.AddComponent<CounterItemView>();trade.itemTemplate.image=item;trade.itemTemplate.fallbackLabel=UI.Label("Missing Icon Name",item.transform,4,4,68,68,"",12);trade.itemTemplate.fallbackLabel.alignment=TextAnchor.MiddleCenter;item.gameObject.SetActive(false);
        var controls=UI.Box("Counter Controls",root,300,1018,1320,54,new Color(.12f,.08f,.04f,.97f));trade.controls=controls.gameObject;
        trade.totalLabel=UI.Label("Registered Total",controls.transform,12,6,530,42,"",22);
        trade.sellButton=UI.ButtonBox("판매",controls.transform,552,6,120,42,trade.Sell);
        UI.ButtonBox("금액 변경",controls.transform,682,6,156,42,trade.TogglePrice);
        UI.ButtonBox("거절",controls.transform,848,6,120,42,trade.Reject);
        trade.talkButton=UI.ButtonBox("더 이야기",controls.transform,978,6,156,42,trade.Talk);
        UI.ButtonBox("정리하기",controls.transform,1144,6,164,42,shop.customerInteraction.Minimize);
        trade.infoLabel=UI.Label("Counter Item Info",root,420,886,800,36,"",20);
        var editor=UI.Box("Counter Price Editor",root,540,942,880,64,new Color(.2f,.13f,.07f,.98f));trade.priceEditor=editor.gameObject;
        UI.Label("Title",editor.transform,10,12,180,40,"제안할 총액",22);
        var inputImage=UI.Box("Total Input",editor.transform,196,10,280,44,new Color(.06f,.04f,.02f));
        var input=inputImage.gameObject.AddComponent<InputField>();input.contentType=InputField.ContentType.IntegerNumber;input.targetGraphic=inputImage;
        input.textComponent=UI.Label("Text",inputImage.transform,10,5,260,34,"",23);trade.proposedTotalInput=input;
        UI.ButtonBox("이 금액에 판매",editor.transform,490,10,250,44,trade.ConfirmPrice);UI.ButtonBox("취소",editor.transform,752,10,112,44,trade.CancelPrice);
        editor.gameObject.SetActive(false);controls.gameObject.SetActive(false);trade.tableItems.gameObject.SetActive(false);
        UiPerformanceLayers.Ensure(root.gameObject,true);ConfigureCounterMotion(trade);shop.priceDialog.transform.SetAsLastSibling();
        // 큰 UI 대신 매장 손님 Sprite를 테이블 뒤에 계속 표시합니다.
        shop.customerInteraction.counterPose.position=new Vector2(480,-290);shop.customerInteraction.counterPose.size=new Vector2(960,1440);
        var debug=layout.Find("Debug Controls") as RectTransform;
        if(debug!=null)
        {
            var probe=panel.inventory.gameObject.AddComponent<StorePerformanceProbe>();probe.panel=panel;probe.counter=trade;
            int n=debug.GetComponentsInChildren<Button>(true).Length;UI.ButtonBox("성능 현황 출력",debug,14+(n%2)*208,14+(n/2)*84,194,68,probe.Report);
            debug.sizeDelta=new Vector2(430,Mathf.Max(debug.sizeDelta.y,28+(n/2+1)*84));
        }
    }
    // Also used by the selected-scene upgrade. Furniture transforms/sprites are never rebuilt.
    public static void ConfigureCounterMotion(CounterTradeUI trade)
    {
        if(trade==null||trade.tableItems==null)return;
        var root=(RectTransform)trade.tableItems.parent;Place(trade.tableItems,0,0,1920,1080);
        var old=trade.tableItems.GetComponent<RecommendationDropArea>();if(old!=null)Object.DestroyImmediate(old);
        var oldImage=trade.tableItems.GetComponent<global::UnityEngine.UI.Image>();if(oldImage!=null){oldImage.raycastTarget=false;oldImage.enabled=false;}
        var drop=root.Find("Counter Stock Drop Area");
        var surface=drop==null?UI.Box("Counter Stock Drop Area",root,720,640,600,345,new Color(1,1,1,0)):drop.GetComponent<global::UnityEngine.UI.Image>();
        Place(surface.rectTransform,720,640,600,345);surface.canvasRenderer.cullTransparentMesh=false;surface.raycastTarget=false;surface.transform.SetAsFirstSibling();trade.dropSurface=surface;
        var area=surface.GetComponent<RecommendationDropArea>();if(area==null)area=surface.gameObject.AddComponent<RecommendationDropArea>();area.panel=trade.panel;area.counter=trade;
        var guide=root.Find("Counter Surface Guide");trade.surfaceGuide=guide==null?UI.Box("Counter Surface Guide",root,100,985,1720,2,Color.yellow):guide.GetComponent<global::UnityEngine.UI.Image>();trade.surfaceGuide.raycastTarget=false;trade.surfaceGuide.gameObject.SetActive(false);
        trade.counterCellSize=100;trade.surfaceFromTop=985;trade.leftInset=100;trade.rightInset=100;trade.ceilingFromTop=40;trade.gravity=2200;trade.bounce=.1f;trade.horizontalDamping=6;trade.sleepSpeed=25;
        // Inventory windows and their drag ghost must remain above the physical counter bodies.
        var floating=root.parent.Find("Floating Windows");if(floating!=null)root.SetSiblingIndex(floating.GetSiblingIndex());
        if(trade.shop!=null&&trade.shop.customerInteraction!=null){trade.shop.customerInteraction.counterPose.position=new Vector2(480,-290);trade.shop.customerInteraction.counterPose.size=new Vector2(960,1440);EditorUtility.SetDirty(trade.shop.customerInteraction);}
        ConfigureCounterRow(trade);EditorUtility.SetDirty(trade);
    }
    public static void ConfigureCounterRow(CounterTradeUI trade)
    {
        if(trade==null||trade.tableItems==null)return;var root=(RectTransform)trade.tableItems.parent;
        trade.surfaceLowerPercent=10;trade.surfaceBottomMargin=8;trade.itemGap=12;trade.keepControlsAboveGoods=true;trade.controlsGap=16;
        var previous=root.Find("Previous Counter Page");
        if(previous==null){trade.previousCounterPage=UI.ButtonBox("<",root,24,940,52,44,trade.PreviousCounterPage);trade.previousCounterPage.gameObject.name="Previous Counter Page";}
        else trade.previousCounterPage=previous.GetComponent<Button>();
        var next=root.Find("Next Counter Page");
        if(next==null){trade.nextCounterPage=UI.ButtonBox(">",root,1844,940,52,44,trade.NextCounterPage);trade.nextCounterPage.gameObject.name="Next Counter Page";}
        else trade.nextCounterPage=next.GetComponent<Button>();
        trade.previousCounterPage.gameObject.SetActive(false);trade.nextCounterPage.gameObject.SetActive(false);
        EditorUtility.SetDirty(trade);
    }
    static void ApplyWarehouse(InventoryPanel panel,WarehouseUI warehouse,InventoryDetailsView details)
    {
        var root=panel.shop.drawer.window;var rootImage=root.GetComponent<global::UnityEngine.UI.Image>();rootImage.color=new Color(0,0,0,0);rootImage.canvasRenderer.cullTransparentMesh=false;
        var art=UI.Box("Authored Warehouse Frame",root,0,0,900,900,Color.white);art.sprite=Resources.Load<Sprite>("StoreInventory/CounterDemo/warehouse_frame");art.raycastTarget=false;art.transform.SetAsFirstSibling();
        // 그림 내부에 정사각형 8×5를 맞추고 선은 중복해서 그리지 않습니다.
        float sx=900f/826,sy=900f/818,cell=367*sy/5;
        var grid=warehouse.grid;grid.cellWidth=grid.cellHeight=cell;grid.cellGap=0;grid.squareCells=true;grid.renderCells=false;grid.renderPartitions=false;
        Place(grid.Rect,106*sx+(599*sx-cell*8)*.5f,190*sy,cell*8,cell*5);
        var image=grid.GetComponent<global::UnityEngine.UI.Image>();image.color=new Color(1,1,1,0);image.canvasRenderer.cullTransparentMesh=false;
        Place((RectTransform)warehouse.tabs.transform,106*sx,132*sy,599*sx,44);warehouse.tabs.showLabels=true;warehouse.tabs.tabHeight=38;
        if(warehouse.tabs.template.label==null)warehouse.tabs.template.label=UI.Label("Box Number",warehouse.tabs.template.transform,0,0,190,38,"상자 1",21);
        warehouse.tabs.template.label.alignment=TextAnchor.MiddleCenter;
        Place((RectTransform)details.transform,106*sx,580*sy,600*sx,130*sy);
        var detailBg=details.GetComponent<global::UnityEngine.UI.Image>();detailBg.color=new Color(1,1,1,0);detailBg.raycastTarget=false;
        foreach(var child in details.GetComponentsInChildren<global::UnityEngine.UI.Image>(true))if(child.gameObject.name=="Gold Separator")Object.DestroyImmediate(child.gameObject);
        details.iconBackground.color=new Color(1,1,1,0);Place(details.iconBackground.rectTransform,12*sx,19*sy,96*sx,100*sy);Place(details.icon.rectTransform,6,6,96*sx-12,100*sy-12);
        Place(details.nameLabel.rectTransform,119*sx,18*sy,315*sx,36);Place(details.bodyLabel.rectTransform,119*sx,56*sy,300*sx,68);
        Place(details.qualitySizeLabel.rectTransform,453*sx,21*sy,130*sx,32);details.qualitySizeLabel.alignment=TextAnchor.MiddleCenter;
        Place(details.priceLabel.rectTransform,424*sx,75*sy,165*sx,35);details.priceLabel.alignment=TextAnchor.MiddleRight;
        var toggle=root.Find("Warehouse Toggle");if(toggle!=null)
        {
            var tr=(RectTransform)toggle;tr.sizeDelta=new Vector2(90,40);tr.anchoredPosition=new Vector2(0,-8);
            var arrow=UI.Rect("Toggle Arrow",toggle,33,11,24,18);arrow.gameObject.AddComponent<CanvasRenderer>();var graphic=arrow.gameObject.AddComponent<DrawerChevronGraphic>();graphic.drawer=panel.shop.drawer;graphic.raycastTarget=false;graphic.color=new Color(1,.8f,.3f,1);
        }
    }
    static void ApplyBackground(SceneCounterOcclusion counter,InventoryPanel panel)
    {
        var backgroundSprite=Resources.Load<Sprite>("StoreInventory/CounterDemo/background");
        var sceneRoot=panel.transform.parent;var background=sceneRoot.Find("Store Background Canvas/Background Layout/Background");
        if(background!=null){var image=background.GetComponent<global::UnityEngine.UI.Image>();image.sprite=backgroundSprite;image.color=Color.white;image.preserveAspect=false;}
        // 별도로 제공한 투명 테이블 원본을 그대로 앞에 올립니다. 배경 Crop 재그리기는 하지 않습니다.
        counter.counterImage.sprite=Resources.Load<Sprite>("StoreInventory/Scene/counter");counter.counterImage.color=Color.white;counter.counterImage.preserveAspect=false;
        Place(counter.imageRect,0,0,1920,1080);counter.maskJson=Resources.Load<TextAsset>("StoreInventory/Scene/counter_mask");
    }
}
#endif
