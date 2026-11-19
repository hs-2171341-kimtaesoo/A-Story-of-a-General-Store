#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class CustomDisplayArtBuilder
{
    public static void PrepareSprites()
    {
        foreach(string name in new[]{"mannequin","table","weapon_rack"})
        {
            string path="Assets/Resources/StoreInventory/DisplayWindows/"+name+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("창 Sprite PNG가 없습니다: "+path);
            if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&importer.maxTextureSize==StoreTextureBudget.DisplayArt&&!importer.isReadable&&!importer.mipmapEnabled&&importer.alphaIsTransparency&&importer.textureCompression==TextureImporterCompression.Compressed)continue;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=StoreTextureBudget.DisplayArt;importer.isReadable=false;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
        }
    }
    static void Place(RectTransform rect,float x,float y,float width,float height)
    {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
    static void SourceRect(RectTransform rect,WindowArtRect box,float scale,float originX=0,float originY=0)
    {Place(rect,(box.x-originX)*scale,(box.y-originY)*scale,box.width*scale,box.height*scale);}
    static DisplayWindowArtDatabase Data()
    {var json=Resources.Load<TextAsset>("StoreInventory/display_window_art");if(json==null)throw new InvalidOperationException("display_window_art.json이 없습니다.");var data=JsonUtility.FromJson<DisplayWindowArtDatabase>(json.text);if(data==null||data.entries==null)throw new InvalidOperationException("창 아트 JSON 오류입니다.");return data;}
    public static int ApplyTemplates(DisplayUI manager,DisplayCatalog catalog)
    {
        int count=0;var data=Data();if(manager.typeTemplates==null)return count;
        foreach(var entry in manager.typeTemplates)
        {
            if(entry==null||entry.template==null||entry.template.appearance==null||entry.template.appearance.useAuthoredLayout)continue;
            foreach(var art in data.entries)if(art.typeId==entry.typeId){Apply(entry.template,art,catalog);count++;break;}
        }
        return count;
    }
    static void Apply(DisplayWindowView view,DisplayWindowArtDefinition art,DisplayCatalog catalog)
    {
        Undo.RegisterFullObjectHierarchyUndo(view.gameObject,"Apply Display Sprite Layout");
        var type=catalog.Type(art.typeId);DisplayWindowArtRules.Validate(art,type);var sprite=Resources.Load<Sprite>(art.spriteKey);if(sprite==null)throw new InvalidOperationException("창 Sprite를 먼저 임포트하세요: "+art.spriteKey);
        var appearance=view.appearance;float s=art.displayHeight/art.sourceHeight,w=art.sourceWidth*s,h=art.displayHeight;
        var itemTemplate=view.grid.itemTemplate;
        // 일반 창의 ScrollRect/Scrollbar/격자만 교체합니다. 바깥 가구의 위치는 건드리지 않습니다.
        if(appearance.scroll!=null)Undo.DestroyObjectImmediate(appearance.scroll.gameObject);
        appearance.scroll=null;appearance.useAuthoredLayout=true;var position=WindowDragGeometry.Clamp(appearance.window.anchoredPosition.x,appearance.window.anchoredPosition.y,w,h,appearance.bounds.rect.width,appearance.bounds.rect.height);Place(appearance.window,position.x,-position.y,w,h);
        appearance.frame.sprite=sprite;appearance.frame.color=Color.white;appearance.frame.preserveAspect=false;Place(appearance.frame.rectTransform,0,0,w,h);
        Place(appearance.header,0,0,w,art.sections[0].rect.y*s);var headerImage=appearance.header.GetComponent<global::UnityEngine.UI.Image>();if(headerImage!=null){headerImage.color=new Color(0,0,0,0);headerImage.canvasRenderer.cullTransparentMesh=false;}
        foreach(var label in appearance.header.GetComponentsInChildren<Text>(true))label.gameObject.SetActive(false);
        view.title.gameObject.SetActive(false);
        var grids=new InventoryGridView[art.sections.Length];
        for(int n=0;n<grids.Length;n++)
        {
            var section=art.sections[n];var image=UI.Box("Grid Section "+(n+1),view.transform,section.rect.x*s,section.rect.y*s,section.rect.width*s,section.rect.height*s,new Color(0,0,0,0));image.canvasRenderer.cullTransparentMesh=false;Undo.RegisterCreatedObjectUndo(image.gameObject,"Create Display Grid Section");
            var grid=image.gameObject.AddComponent<InventoryGridView>();grid.itemTemplate=itemTemplate;grid.squareCells=false;grid.renderCells=false;grid.renderPartitions=false;grid.cellGap=0;
            grid.startColumn=section.column;grid.startRow=section.row;grid.viewColumns=section.columns;grid.viewRows=section.rows;
            grid.cellWidth=section.rect.width*s/section.columns;grid.cellHeight=section.rect.height*s/section.rows;
            grid.itemLayer=UI.Rect("Items",image.transform,0,0,section.rect.width*s,section.rect.height*s);grids[n]=grid;
        }
        view.grid=grids[0];appearance.grid=grids[0];view.extraGrids=new InventoryGridView[grids.Length-1];for(int n=1;n<grids.Length;n++)view.extraGrids[n-1]=grids[n];
        var details=view.details;SourceRect((RectTransform)details.transform,art.details,s);details.transform.SetAsLastSibling();
        var background=details.GetComponent<global::UnityEngine.UI.Image>();if(background!=null){background.color=new Color(0,0,0,0);background.raycastTarget=false;}
        // 그림 안의 샘플 글자를 가리는 단색 받침입니다. 글자 없는 최종 프레임이 생기면 제거 가능합니다.
        var patch=UI.Box("Dynamic Text Backing",details.transform,(art.text.x-art.details.x)*s,(art.text.y-art.details.y)*s,art.text.width*s,art.text.height*s,new Color(.18f,.105f,.065f));patch.raycastTarget=false;patch.transform.SetAsFirstSibling();Undo.RegisterCreatedObjectUndo(patch.gameObject,"Create Dynamic Text Backing");
        SourceRect(details.iconBackground.rectTransform,art.icon,s,art.details.x,art.details.y);details.iconBackground.color=new Color(0,0,0,0);details.iconFit.padding=0;
        Place(details.nameLabel.rectTransform,(art.text.x-art.details.x+5)*s,(art.text.y-art.details.y+3)*s,(art.text.width-10)*s,art.text.height*.51f*s);
        Place(details.qualitySizeLabel.rectTransform,(art.text.x-art.details.x+5)*s,(art.text.y-art.details.y+art.text.height*.52f)*s,(art.text.width-10)*s,art.text.height*.46f*s);
        details.nameLabel.fontSize=Mathf.Max(12,(int)(art.text.height*.42f*s));details.qualitySizeLabel.fontSize=Mathf.Max(10,(int)(art.text.height*.34f*s));details.qualitySizeLabel.alignment=TextAnchor.MiddleLeft;
        details.bodyLabel.gameObject.SetActive(false);details.priceLabel.gameObject.SetActive(false);
        foreach(var image in details.GetComponentsInChildren<global::UnityEngine.UI.Image>(true))if(image.gameObject.name=="Gold Separator")image.gameObject.SetActive(false);
        SourceRect(appearance.finishButton,art.close,s);appearance.finishButton.SetAsLastSibling();var buttonImage=appearance.finishButton.GetComponent<global::UnityEngine.UI.Image>();buttonImage.color=new Color(0,0,0,0);buttonImage.canvasRenderer.cullTransparentMesh=false;
        foreach(var label in appearance.finishButton.GetComponentsInChildren<Text>(true))label.gameObject.SetActive(false);
    }
    [MenuItem("Store Inventory/Existing Scene Tools/Apply Three Display Sprite Layouts To Selection")]
    public static void UpgradeSelection()
    {
        if(Application.isPlaying){Debug.LogError("Play를 중지하고 Store Scene을 선택하세요.");return;}var selected=Selection.activeGameObject;if(selected==null){Debug.LogError("Store Scene 또는 Store UI Canvas를 선택하세요.");return;}
        PrepareSprites();var file=Resources.Load<TextAsset>("StoreInventory/display_fixtures");var catalog=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(file.text));int count=0;
        foreach(var manager in selected.GetComponentsInChildren<DisplayUI>(true))count+=ApplyTemplates(manager,catalog);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);Debug.Log("전용 Sprite 창 "+count+"개 적용. 이미 편집형 창인 템플릿은 보존했습니다. 씬 복사본에서 적용하고 저장하세요.");
    }
}
#endif
