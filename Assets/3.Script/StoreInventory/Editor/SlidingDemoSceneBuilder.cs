#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class SlidingDemoSceneBuilder
{
    const string CustomerPath="Assets/Resources/temporary_customer.png";
    public static void PrepareCustomerSprite()
    {
        AssetDatabase.ImportAsset(CustomerPath);var importer=AssetImporter.GetAtPath(CustomerPath) as TextureImporter;if(importer==null)return;
        if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&importer.alphaIsTransparency&&!importer.mipmapEnabled&&importer.maxTextureSize==StoreTextureBudget.Customer&&importer.isReadable&&importer.textureCompression==TextureImporterCompression.Uncompressed)return;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=StoreTextureBudget.Customer;
        importer.isReadable=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    }
    public static void Build(RectTransform layout,RectTransform furnitureLayout,InventoryPanel panel,FurnitureArtCatalog art,SceneCounterOcclusion counter)
    {
        var shop=panel.shop;shop.drawer.openedPosition=new Vector2(1000,-146);
        var menuBounds=UI.Rect("Left Display Menu Layer",layout,0,0,1920,1080);
        var menu=menuBounds.gameObject.AddComponent<LeftDisplayMenu>();menu.panel=panel;menu.displays=shop.displays;menu.art=art;menu.bounds=menuBounds;
        menu.window=UI.Rect("Auto Hide Display Menu",menuBounds,-96,410,96,260);
        var viewport=UI.Box("Viewport",menu.window,0,0,96,260,new Color(.09f,.07f,.045f,.97f));viewport.gameObject.AddComponent<RectMask2D>();menu.viewport=viewport.rectTransform;
        menu.content=UI.Rect("Owned Displays",viewport.transform,0,0,96,260);
        menu.scroll=menu.window.gameObject.AddComponent<ScrollRect>();menu.scroll.viewport=menu.viewport;menu.scroll.content=menu.content;menu.scroll.horizontal=false;menu.scroll.vertical=true;menu.scroll.movementType=ScrollRect.MovementType.Clamped;
        var image=UI.Box("Left Display Button Template",layout,0,0,88,80,new Color(.24f,.16f,.09f));
        var view=image.gameObject.AddComponent<LeftDisplayButtonView>();view.background=image;view.button=image.gameObject.AddComponent<Button>();view.button.targetGraphic=image;
        view.icon=UI.Box("Furniture Icon",image.transform,16,5,56,44,Color.white);view.icon.preserveAspect=true;view.icon.raycastTarget=false;
        view.label=UI.Label("Name",image.transform,3,50,82,28,"진열장",12);view.label.alignment=TextAnchor.MiddleCenter;
        image.gameObject.SetActive(false);menu.template=view;
        menu.edgeIcon=UI.Box("Left Edge Menu Icon",menuBounds,8,524,22,32,Color.white);menu.edgeIcon.sprite=Resources.Load<Sprite>("StoreInventory/Furniture/furniture_01");menu.edgeIcon.preserveAspect=true;menu.edgeIcon.raycastTarget=false;
        var flow=layout.gameObject.AddComponent<StagedCustomerInteraction>();shop.customerInteraction=flow;flow.shop=shop;
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(CustomerPath);
        flow.sceneImage=UI.Box("Customer Scene Sprite",furnitureLayout,0,0,180,300,Color.white);flow.sceneImage.preserveAspect=true;
        flow.sceneImage.alphaHitTestMinimumThreshold=.1f;flow.sceneImage.raycastTarget=false;flow.sceneImage.enabled=false;
        var click=flow.sceneImage.gameObject.AddComponent<CustomerWorldClick>();click.interaction=flow;click.counter=counter;
        flow.browsingStops=new[]{
            new CustomerVisitPose{sprite=sprite,position=new Vector2(560,-300),size=new Vector2(160,260),holdSeconds=2},
            new CustomerVisitPose{sprite=sprite,position=new Vector2(1240,-340),size=new Vector2(190,310),holdSeconds=2.5f}};
        flow.counterPose=new CustomerVisitPose{sprite=sprite,position=new Vector2(830,-510),size=new Vector2(220,330)};
        menuBounds.SetAsLastSibling();shop.priceDialog.transform.SetAsLastSibling();
        var debug=layout.Find("Debug Controls") as RectTransform;
        if(debug!=null)
        {
            int n=debug.GetComponentsInChildren<Button>(true).Length;
            UI.ButtonBox("돈 +10,000 G",debug,14+(n%2)*208,14+(n/2)*84,194,68,shop.simulation.AddDebugGold);
            debug.sizeDelta=new Vector2(430,Mathf.Max(debug.sizeDelta.y,28+(n/2+1)*84));
        }
    }
}
#endif
