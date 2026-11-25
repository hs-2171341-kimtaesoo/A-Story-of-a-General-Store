#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class SlidingDemoSceneBuilder
{
    const string CustomerPath="Assets/Resources/temporary_customer.png";
    public static void UpdateMenuArtwork()
    {
        if(Application.isPlaying){Debug.LogWarning("Play를 중지하고 Store Scene을 선택하세요.");return;}
        var selected=Selection.activeGameObject;if(selected==null){Debug.LogWarning("Store Scene을 선택하세요.");return;}
        var menu=selected.transform.root.GetComponentInChildren<LeftDisplayMenu>(true);
        if(menu==null){Debug.LogWarning("선택한 씬에서 진열대 목록을 찾지 못했습니다.");return;}
        DisplayMenuTextureImporter.PrepareSprites();Undo.RegisterFullObjectHierarchyUndo(menu.gameObject,"Update Display Menu Artwork");
        menu.useArtwork=true;menu.PrepareArtwork();menu.RefreshLayout();EditorUtility.SetDirty(menu);
        var management=selected.transform.root.GetComponentInChildren<RightSideMenu>(true);
        if(management!=null){Undo.RegisterFullObjectHierarchyUndo(management.gameObject,"Update Management Menu Artwork");management.useArtwork=true;management.PrepareArtwork();management.RefreshLayout();EditorUtility.SetDirty(management);}
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        Debug.Log("양쪽 목록의 새 틀과 아이콘을 연결했습니다. 진열대는 지정 순서, 임시 저장고는 마지막에 표시됩니다.");
    }
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
        DisplayMenuTextureImporter.PrepareSprites();menu.PrepareArtwork();
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
// Configure only the new menu textures. No texture pixels are changed.
public sealed class DisplayMenuTextureImporter:AssetPostprocessor
{
    const string Folder="Assets/Resources/StoreInventory/DisplayMenu/";
    const string ManagementFolder="Assets/Resources/StoreInventory/ManagementMenu/";
    void OnPreprocessTexture()
    {if(assetPath.StartsWith(Folder,System.StringComparison.Ordinal)||assetPath.StartsWith(ManagementFolder,System.StringComparison.Ordinal))Configure((TextureImporter)assetImporter);}
    static void Configure(TextureImporter importer)
    {
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;
        importer.maxTextureSize=importer.assetPath.EndsWith("/frame.png",System.StringComparison.Ordinal)?1024:256;
        importer.textureCompression=TextureImporterCompression.Compressed;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
    }
    public static void PrepareSprites()
    {
        foreach(string folder in new[]{Folder,ManagementFolder})
        {
        if(!AssetDatabase.IsValidFolder(folder.TrimEnd('/')))continue;
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{folder.TrimEnd('/')}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
            int cap=path.EndsWith("/frame.png",System.StringComparison.Ordinal)?1024:256;
            if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&importer.maxTextureSize==cap&&!importer.mipmapEnabled&&!importer.isReadable&&importer.alphaIsTransparency&&importer.textureCompression==TextureImporterCompression.Compressed)continue;
            Configure(importer);importer.SaveAndReimport();
        }
        }
    }
}
#endif
