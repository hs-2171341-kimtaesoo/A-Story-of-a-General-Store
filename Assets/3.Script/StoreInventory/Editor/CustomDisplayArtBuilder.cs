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
        var a=view.appearance;var template=view.grid.itemTemplate;float s=art.displayHeight/art.sourceHeight,w=art.sourceWidth*s;
        var previous=view.transform.Find("Authored Display Content");if(previous!=null)Remove(previous.gameObject);
        if(a.scroll!=null)Remove(a.scroll.gameObject);if(view.grid!=null)Remove(view.grid.gameObject);if(view.extraGrids!=null)foreach(var old in view.extraGrids)if(old!=null)Remove(old.gameObject);
        if(view.details!=null)Remove(view.details.gameObject);if(a.finishButton!=null)Remove(a.finishButton.gameObject);if(a.header!=null)Remove(a.header.gameObject);if(a.closeButton!=null)Remove(a.closeButton.gameObject);if(a.tabStrip!=null)Remove(a.tabStrip.gameObject);
        view.details=null;view.title=null;view.status=null;a.details=null;a.header=null;a.finishButton=null;a.scroll=null;a.useAuthoredLayout=true;
        Place(a.window,0,0,w,art.displayHeight);a.frame.sprite=Resources.Load<Sprite>(art.spriteKey);a.frame.color=Color.white;a.frame.raycastTarget=true;Place(a.frame.rectTransform,0,0,w,art.displayHeight);
        var contentRoot=UI.Rect("Authored Display Content",view.transform,0,0,w,art.displayHeight);var grids=new List<InventoryGridView>();
        if(!art.scrolling)foreach(var section in art.sections)grids.Add(Grid(view,contentRoot,section,s,template));
        else
        {
            var v=art.viewport;var viewport=UI.Box("Shelf Scroll Viewport",contentRoot,v.x*s,v.y*s,v.width*s,v.height*s,new Color(1,1,1,0));viewport.canvasRenderer.cullTransparentMesh=false;viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();a.scroll=scroll;scroll.viewport=viewport.rectTransform;scroll.horizontal=false;scroll.vertical=true;scroll.inertia=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=art.floors[0].art.height*s*.35f;
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
                    if(art.typeId=="drawer_cabinet"&&section.row>=6&&section.column>0){var post=UI.Box("Lower Compartment Divider",layer,(section.rect.x-r.x-3)*s,(section.rect.y-r.y)*s,6*s,section.rect.height*s,new Color(.7f,.4f,.13f));post.raycastTarget=false;}
                }
                offset+=r.height*s;
            }
            var bar=art.scrollbar;var track=UI.Box("Functional Shelf Scrollbar",contentRoot,bar.x*s,bar.y*s,bar.width*s,bar.height*s,new Color(.16f,.075f,.025f));var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.direction=Scrollbar.Direction.BottomToTop;
            var handle=UI.Box("Handle",track.transform,0,0,bar.width*s,48*s,new Color(1,.65f,.15f));handle.rectTransform.offsetMin=Vector2.zero;handle.rectTransform.offsetMax=Vector2.zero;scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;
        }
        view.grid=grids[0];a.grid=view.grid;view.extraGrids=grids.GetRange(1,grids.Count-1).ToArray();view.gameObject.SetActive(false);
    }
    [MenuItem("Store Inventory/Existing Scene Tools/Apply Latest Customer And Display UI To Selection")]
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
