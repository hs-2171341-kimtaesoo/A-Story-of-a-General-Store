#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class CustomDisplayArtBuilder
{
    public static void PrepareSprites()
    {
        var keys=new HashSet<string>();foreach(var art in Data().entries){keys.Add(art.spriteKey);if(art.floors!=null)foreach(var floor in art.floors)keys.Add(floor.spriteKey);}
        foreach(string key in keys)
        {
            string path="Assets/Resources/"+key+".png";AssetDatabase.ImportAsset(path);var i=AssetImporter.GetAtPath(path) as TextureImporter;if(i==null)throw new InvalidOperationException("진열 Sprite 누락: "+path);
            if(i.textureType==TextureImporterType.Sprite&&i.spriteImportMode==SpriteImportMode.Single&&i.maxTextureSize==StoreTextureBudget.DisplayArt&&!i.isReadable&&!i.mipmapEnabled&&i.alphaIsTransparency&&i.textureCompression==TextureImporterCompression.Compressed)continue;
            i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.maxTextureSize=StoreTextureBudget.DisplayArt;i.isReadable=false;i.mipmapEnabled=false;i.alphaIsTransparency=true;i.textureCompression=TextureImporterCompression.Compressed;i.SaveAndReimport();
        }
    }
    static void Place(RectTransform rect,float x,float y,float width,float height)
    {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
    static DisplayWindowArtDatabase Data()
    {var json=Resources.Load<TextAsset>("StoreInventory/display_window_art");if(json==null)throw new InvalidOperationException("display_window_art.json 누락");return JsonUtility.FromJson<DisplayWindowArtDatabase>(json.text);}
    public static void ValidateDefinitions()
    {
        var file=Resources.Load<TextAsset>("StoreInventory/display_fixtures");if(file==null)throw new InvalidOperationException("display_fixtures.json 누락");
        var catalog=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(file.text));var data=Data();
        if(data==null||data.entries==null||data.entries.Length!=12)throw new InvalidOperationException("진열창 정의 12종이 필요합니다.");
        var ids=new HashSet<string>();
        foreach(var art in data.entries)
        {
            if(art==null||string.IsNullOrEmpty(art.typeId)||!ids.Add(art.typeId))throw new ArgumentException("진열창 ID 누락/중복");
            try{DisplayWindowArtRules.Validate(art,catalog.Type(art.typeId));}
            catch(ArgumentException error){throw new ArgumentException("진열창 "+art.typeId+": "+error.Message,error);}
        }
    }
    public static int ApplyTemplates(DisplayUI manager,DisplayCatalog catalog,bool replaceExisting=false)
    {
        int count=0;var data=Data();if(manager.typeTemplates==null)return count;
        foreach(var entry in manager.typeTemplates)
        {
            if(entry==null||entry.template==null||entry.template.appearance==null||(entry.template.appearance.useAuthoredLayout&&!replaceExisting))continue;
            entry.template.panel=manager.panel;foreach(var art in data.entries)if(art.typeId==entry.typeId){Apply(entry.template,art,catalog);count++;break;}
        }
        return count;
    }
    static InventoryGridView Grid(DisplayWindowView view,Transform parent,WindowArtSection section,float s,InventoryItemView template,float originX=0,float originY=0)
    {
        var r=section.rect;var image=UI.Box("Grid "+section.column+":"+section.row,parent,(r.x-originX)*s,(r.y-originY)*s,r.width*s,r.height*s,Color.clear);image.canvasRenderer.cullTransparentMesh=true;image.raycastTarget=false;
        var grid=image.gameObject.AddComponent<InventoryGridView>();grid.itemTemplate=template;grid.squareCells=false;grid.renderCells=false;grid.renderPartitions=false;grid.cellGap=0;
        var input=image.gameObject.AddComponent<DragOnlyRaycastSurface>();input.panel=view.panel;input.image=image;
        grid.startColumn=section.column;grid.startRow=section.row;grid.viewColumns=section.columns;grid.viewRows=section.rows;grid.cellWidth=r.width*s/section.columns;grid.cellHeight=r.height*s/section.rows;
        grid.itemLayer=UI.Rect("Items",image.transform,0,0,r.width*s,r.height*s);return grid;
    }
    static void Remove(GameObject go){if(go!=null)Undo.DestroyObjectImmediate(go);}
    static void Apply(DisplayWindowView view,DisplayWindowArtDefinition art,DisplayCatalog catalog)
    {
        Undo.RegisterFullObjectHierarchyUndo(view.gameObject,"Apply Clean Display Layout");DisplayWindowArtRules.Validate(art,catalog.Type(art.typeId));
        var a=view.appearance;var template=view.grid.itemTemplate;float s=art.displayHeight/art.LayoutHeight,w=art.sourceWidth*s;
        var previous=view.transform.Find("Authored Display Content");if(previous!=null)Remove(previous.gameObject);
        if(a.scroll!=null)Remove(a.scroll.gameObject);if(view.grid!=null)Remove(view.grid.gameObject);if(view.extraGrids!=null)foreach(var old in view.extraGrids)if(old!=null)Remove(old.gameObject);
        if(view.details!=null)Remove(view.details.gameObject);if(a.finishButton!=null)Remove(a.finishButton.gameObject);if(a.header!=null)Remove(a.header.gameObject);if(a.closeButton!=null)Remove(a.closeButton.gameObject);if(a.tabStrip!=null)Remove(a.tabStrip.gameObject);
        view.details=null;view.title=null;view.status=null;a.details=null;a.header=null;a.finishButton=null;a.scroll=null;a.useAuthoredLayout=true;
        Place(a.window,0,0,w,art.displayHeight);a.frame.sprite=Resources.Load<Sprite>(art.spriteKey);a.frame.color=Color.white;a.frame.raycastTarget=true;Place(a.frame.rectTransform,0,0,w,art.displayHeight);
        var contentRoot=UI.Rect("Authored Display Content",view.transform,0,0,w,art.displayHeight);var grids=new List<InventoryGridView>();
        if(art.frameSlices!=null&&art.frameSlices.Length>0)
        {
            a.frame.color=Color.clear;a.frame.canvasRenderer.cullTransparentMesh=false;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/"+art.spriteKey+".png");
            foreach(var slice in art.frameSlices)
            {
                var r=slice.rect;var layer=UI.Rect("Original Frame Slice",contentRoot,r.x*s,slice.y*s,r.width*s,r.height*s);
                var image=layer.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
                image.uvRect=new Rect(r.x/art.sourceWidth,1-(r.y+r.height)/art.sourceHeight,r.width/art.sourceWidth,r.height/art.sourceHeight);
            }
        }
        if(!art.scrolling)foreach(var section in art.sections)grids.Add(Grid(view,contentRoot,section,s,template));
        else
        {
            var v=art.viewport;var viewport=UI.Box("Shelf Scroll Viewport",contentRoot,v.x*s,v.y*s,v.width*s,v.height*s,new Color(1,1,1,0));viewport.canvasRenderer.cullTransparentMesh=false;viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();a.scroll=scroll;scroll.viewport=viewport.rectTransform;scroll.horizontal=false;scroll.vertical=true;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=art.floors[0].art.height*s*.35f;
            // Drag the empty cells or wooden gaps. No scrollbar control covers the artwork.
            scroll.horizontalScrollbar=null;scroll.verticalScrollbar=null;
            float height=0;foreach(var floor in art.floors)height+=floor.art.height*s;
            var content=UI.Rect("All Logical Floors",viewport.transform,0,0,v.width*s,height);scroll.content=content;float offset=0;
            foreach(var floor in art.floors)
            {
                var r=floor.art;var layer=UI.Rect("Floor "+floor.sections[0].row,content,0,offset,r.width*s,r.height*s);
                var background=layer.gameObject.AddComponent<RawImage>();background.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/"+floor.spriteKey+".png");background.raycastTarget=false;
                background.uvRect=new Rect(r.x/floor.sourceWidth,1-(r.y+r.height)/floor.sourceHeight,r.width/floor.sourceWidth,r.height/floor.sourceHeight);
                foreach(var section in floor.sections)
                {
                    grids.Add(Grid(view,layer,section,s,template,r.x,r.y));
                }
                offset+=r.height*s;
            }
        }
        view.grid=grids[0];a.grid=view.grid;view.extraGrids=grids.GetRange(1,grids.Count-1).ToArray();view.gameObject.SetActive(false);
    }
    public static void UpgradeDragScrollSelection()
    {
        if(Application.isPlaying){Debug.LogWarning("Play를 중지하고 Store Scene을 선택하세요.");return;}
        var selected=Selection.activeGameObject;if(selected==null){Debug.LogWarning("Store Scene을 선택하세요.");return;}
        var root=selected.transform.root;var managers=root.GetComponentsInChildren<DisplayUI>(true);
        if(managers.Length==0){Debug.LogWarning("선택한 루트에 DisplayUI가 없습니다.");return;}
        var data=Data();var catalogs=new Dictionary<DisplayUI,DisplayCatalog>();int count=0;
        // Validate every target first, using the scene's own fixture definitions.
        foreach(var manager in managers)
        {
            var runtime=manager.panel!=null?manager.panel.inventory:null;
            var file=runtime!=null&&runtime.displayJson!=null?runtime.displayJson:Resources.Load<TextAsset>("StoreInventory/display_fixtures");
            if(file==null)throw new InvalidOperationException("display_fixtures.json 누락");
            var catalog=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(file.text));catalogs.Add(manager,catalog);
            if(manager.typeTemplates==null)continue;
            foreach(var entry in manager.typeTemplates)
            {
                if(entry?.template==null)continue;
                foreach(var art in data.entries)if(art.scrolling&&art.typeId==entry.typeId)
                {
                    if(entry.template.appearance==null||entry.template.grid==null||entry.template.grid.itemTemplate==null)throw new InvalidOperationException("진열 템플릿 참조 누락: "+entry.typeId);
                    DisplayWindowArtRules.Validate(art,catalog.Type(art.typeId));count++;break;
                }
            }
        }
        if(count==0){Debug.LogWarning("업데이트할 스크롤 진열 템플릿이 없습니다.");return;}
        PrepareSprites();Selection.activeGameObject=root.gameObject;Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"Update Drag Scroll Displays");
        foreach(var manager in managers)
        {
            if(manager.typeTemplates==null)continue;
            foreach(var entry in manager.typeTemplates)
            {
                if(entry?.template==null)continue;
                foreach(var art in data.entries)if(art.scrolling&&art.typeId==entry.typeId)
                {entry.template.panel=manager.panel;Apply(entry.template,art,catalogs[manager]);break;}
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Debug.Log("스크롤 진열 템플릿 "+count+"개 업데이트: 빈 칸/선반 여백 드래그, 서랍형 하부 2×2 적용. 씬을 저장하세요.");
    }
    public static void UpgradeSelection()
    {
        if(Application.isPlaying){Debug.LogWarning("Play를 중지하고 Store Scene을 선택하세요.");return;}var selected=Selection.activeGameObject;if(selected==null){Debug.LogWarning("Store Scene을 선택하세요.");return;}
        PrepareSprites();CounterDemoSceneBuilder.PrepareSprites();var file=Resources.Load<TextAsset>("StoreInventory/display_fixtures");var catalog=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(file.text));var root=selected.transform.root;Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"Update Inventory UI");
        foreach(var runtime in root.GetComponentsInChildren<InventoryRuntime>(true)){runtime.displayJson=file;EditorUtility.SetDirty(runtime);}
        foreach(var manager in root.GetComponentsInChildren<DisplayUI>(true))ApplyTemplates(manager,catalog,true);
        var panel=root.GetComponentInChildren<InventoryPanel>(true);if(panel!=null)
        {
            CounterDemoSceneBuilder.ApplyWarehouse(panel,panel.GetComponent<WarehouseUI>());CompactDemoSceneBuilder.RebuildDelivery(panel);
            var keep=new List<InventoryDetailsView>();foreach(var detail in panel.detailViews)if(detail!=null)keep.Add(detail);panel.detailViews=keep.ToArray();CounterDemoSceneBuilder.BuildTooltip(panel);
            foreach(var layout in root.GetComponentsInChildren<ShopViewportLayout>(true))layout.Apply();
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);Debug.Log("8×7 상자/12종 진열창/스크롤/공통 툴팁 적용. 매장 가구 배치는 보존했습니다. 씬을 저장하세요.");
    }
}
#endif
