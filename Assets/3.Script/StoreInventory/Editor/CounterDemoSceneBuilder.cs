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
        Import("warehouse_frame",1024);Import("item_tooltip",1024);Import("background",StoreTextureBudget.Background);
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
        trade.infoLabel=UI.Label("Counter Item Info",root,720,864,520,36,"",20);
        trade.tableItems.gameObject.SetActive(false);
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
        var guide=root.Find("Counter Surface Guide");trade.surfaceGuide=guide==null?UI.Box("Counter Surface Guide",root,100,1028.5f,1720,2,Color.yellow):guide.GetComponent<global::UnityEngine.UI.Image>();trade.surfaceGuide.raycastTarget=false;trade.surfaceGuide.gameObject.SetActive(false);
        trade.counterCellSize=100;trade.surfaceFromTop=1028.5f;trade.leftInset=100;trade.rightInset=100;trade.ceilingFromTop=40;trade.gravity=2200;trade.bounce=.1f;trade.horizontalDamping=6;trade.sleepSpeed=25;
        // Inventory windows and their drag ghost must remain above the physical counter bodies.
        var floating=root.parent.Find("Floating Windows");if(floating!=null)root.SetSiblingIndex(floating.GetSiblingIndex());
        if(trade.shop!=null&&trade.shop.customerInteraction!=null){trade.shop.customerInteraction.counterPose.position=new Vector2(480,-290);trade.shop.customerInteraction.counterPose.size=new Vector2(960,1440);EditorUtility.SetDirty(trade.shop.customerInteraction);}
        ConfigureCounterRow(trade);EditorUtility.SetDirty(trade);
    }
    public static void ConfigureCounterRow(CounterTradeUI trade)
    {
        if(trade==null||trade.tableItems==null)return;var root=(RectTransform)trade.tableItems.parent;
        trade.surfaceFromTop=1028.5f;trade.surfaceLowerPercent=0;trade.surfaceBottomMargin=8;trade.keepControlsAboveGoods=true;trade.controlsGap=16;
        foreach(string name in new[]{"Previous Counter Page","Next Counter Page"})
        {var old=root.Find(name);if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);}
        ConfigureVerticalTrade(trade);EditorUtility.SetDirty(trade);
    }
    public static void ConfigureVerticalTrade(CounterTradeUI trade)
    {
        if(trade==null||trade.tableItems==null)return;var root=(RectTransform)trade.tableItems.parent;
        if(trade.controls!=null)Object.DestroyImmediate(trade.controls);if(trade.priceEditor!=null)Object.DestroyImmediate(trade.priceEditor);
        var controls=UI.Box("Counter Controls",root,1266,886,540,172,new Color(.12f,.08f,.04f,.97f));trade.controls=controls.gameObject;
        var amount=UI.ButtonBox("거래 금액",controls.transform,8,8,350,48,trade.TogglePrice);trade.totalLabel=amount.GetComponentInChildren<Text>(true);
        trade.sellButton=UI.ButtonBox("판매",controls.transform,366,8,166,48,trade.Sell);
        trade.talkButton=UI.ButtonBox("이야기하기",controls.transform,8,62,524,48,trade.Talk);
        UI.ButtonBox("거절",controls.transform,8,116,524,48,trade.Reject);
        var editor=UI.Box("Counter Price Editor",root,1266,886,540,172,new Color(.2f,.13f,.07f,.98f));trade.priceEditor=editor.gameObject;
        UI.Label("Title",editor.transform,10,8,170,42,"거래 총액",22);
        var inputImage=UI.Box("Total Input",editor.transform,184,8,344,48,new Color(.06f,.04f,.02f));
        var input=inputImage.gameObject.AddComponent<InputField>();input.contentType=InputField.ContentType.IntegerNumber;input.targetGraphic=inputImage;
        input.textComponent=UI.Label("Text",inputImage.transform,10,5,324,38,"",23);trade.proposedTotalInput=input;
        UI.ButtonBox("이 금액에 판매",editor.transform,8,68,524,44,trade.ConfirmPrice);UI.ButtonBox("취소",editor.transform,8,120,524,44,trade.CancelPrice);
        trade.verticalControls=true;trade.keepControlsAboveGoods=false;trade.RefreshViewport(new Rect(0,-1080,1920,1080));
        editor.gameObject.SetActive(false);controls.gameObject.SetActive(false);
        EditorUtility.SetDirty(trade);
    }
    public static void ApplyWarehouse(InventoryPanel panel,WarehouseUI warehouse,InventoryDetailsView details=null)
    {
        var root=panel.shop.drawer.window;var sprite=Resources.Load<Sprite>("StoreInventory/CounterDemo/warehouse_frame");
        var existing=root.Find("Authored Warehouse Frame");var art=existing==null?UI.Box("Authored Warehouse Frame",root,0,0,900,900,Color.white):existing.GetComponent<global::UnityEngine.UI.Image>();
        float s=900f/1254f,h=1254*s;root.sizeDelta=new Vector2(900,h);panel.shop.drawer.body.sizeDelta=root.sizeDelta;
        Place(art.rectTransform,0,0,900,h);art.sprite=sprite;art.color=Color.white;art.raycastTarget=true;art.transform.SetAsFirstSibling();
        var rootImage=root.GetComponent<global::UnityEngine.UI.Image>();rootImage.color=Color.clear;rootImage.raycastTarget=false;
        var grid=warehouse.grid;grid.cellWidth=903*s/8;grid.cellHeight=765*s/7;grid.cellGap=0;grid.squareCells=false;grid.renderCells=false;grid.renderPartitions=false;
        Place(grid.Rect,164*s,293*s,903*s,765*s);var image=grid.GetComponent<global::UnityEngine.UI.Image>();image.color=Color.clear;image.canvasRenderer.cullTransparentMesh=false;
        Place((RectTransform)warehouse.tabs.transform,164*s,236*s,903*s,40*s);warehouse.tabs.showLabels=true;warehouse.tabs.tabHeight=34;
        if(warehouse.tabs.template.label==null)warehouse.tabs.template.label=UI.Label("Box Number",warehouse.tabs.template.transform,0,0,190,34,"상자 1",23);
        warehouse.tabs.template.label.alignment=TextAnchor.MiddleCenter;
        if(details==null){var old=root.GetComponentInChildren<InventoryDetailsView>(true);if(old!=null)details=old;}
        if(details!=null){var keep=new System.Collections.Generic.List<InventoryDetailsView>(panel.detailViews);keep.Remove(details);panel.detailViews=keep.ToArray();Object.DestroyImmediate(details.gameObject);}
        var toggle=root.Find("Warehouse Toggle");if(toggle!=null)
        {
            var tr=(RectTransform)toggle;tr.sizeDelta=new Vector2(90,40);tr.anchoredPosition=new Vector2(0,-8);
            var oldArrow=toggle.Find("Toggle Arrow");if(oldArrow==null){var arrow=UI.Rect("Toggle Arrow",toggle,33,11,24,18);arrow.gameObject.AddComponent<CanvasRenderer>();var graphic=arrow.gameObject.AddComponent<DrawerChevronGraphic>();graphic.drawer=panel.shop.drawer;graphic.raycastTarget=false;graphic.color=new Color(1,.8f,.3f,1);}
        }
    }
    public static void BuildTooltip(InventoryPanel panel)
    {
        if(panel.tooltip!=null){var updated=panel.GetComponentInChildren<DeliveryUI>(true);if(updated!=null)panel.tooltip.temporaryWindow=updated.window;return;}var ui=panel.gameObject.AddComponent<ItemHoverTooltip>();panel.tooltip=ui;ui.panel=panel;
        var frame=UI.Box("Shared Item Tooltip",panel.drag.dragLayer,0,0,460,460*1024f/1536,Color.white);frame.sprite=Resources.Load<Sprite>("StoreInventory/CounterDemo/item_tooltip");ui.window=frame.rectTransform;
        var group=frame.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;frame.raycastTarget=false;
        float s=460f/1536;
        var iconFrame=UI.Rect("Icon Frame",frame.transform,160*s,165*s,250*s,650*s);ui.icon=UI.Box("Item Icon",iconFrame,0,0,250*s,650*s,Color.white);ui.icon.raycastTarget=false;
        ui.iconFit=iconFrame.gameObject.AddComponent<SpriteIconFit>();ui.iconFit.image=ui.icon;ui.iconFit.frame=iconFrame;ui.iconFit.padding=2;
        ui.nameLabel=UI.Label("Name",frame.transform,530*s,190*s,570*s,110*s,"",27);
        ui.qualityLabel=UI.Label("Quality",frame.transform,530*s,330*s,650*s,100*s,"",21);
        ui.sizeLabel=UI.Label("Footprint",frame.transform,1140*s,198*s,215*s,95*s,"",18);ui.sizeLabel.alignment=TextAnchor.MiddleCenter;
        ui.descriptionLabel=UI.Label("Description",frame.transform,530*s,490*s,810*s,150*s,"",18);
        ui.nameLabel.resizeTextForBestFit=true;ui.nameLabel.resizeTextMinSize=14;ui.nameLabel.resizeTextMaxSize=27;
        ui.priceLabel=UI.Label("Base Price",frame.transform,530*s,700*s,820*s,110*s,"",22);ui.priceLabel.color=new Color(1,.78f,.24f);
        var storage=panel.GetComponentInChildren<DeliveryUI>(true);if(storage!=null)ui.temporaryWindow=storage.window;
        frame.gameObject.SetActive(false);EditorUtility.SetDirty(panel);
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
