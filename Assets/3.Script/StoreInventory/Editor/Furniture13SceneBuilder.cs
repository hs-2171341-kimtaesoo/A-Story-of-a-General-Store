#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class Furniture13SceneBuilder
{
    const string Folder="Assets/Resources/StoreInventory/Furniture/";
    // 최초 데모 생성 때만 설정합니다. 플레이 중이나 Update에서는 재임포트하지 않습니다.
    public static void PrepareSprites()
    {
        for(int n=1;n<=12;n++){Import(Folder+"furniture_"+n.ToString("00")+".png",StoreTextureBudget.Furniture);Import("Assets/Resources/StoreInventory/Scene/outline_"+n.ToString("00")+".png",StoreTextureBudget.Outline);}
        Import("Assets/Resources/StoreInventory/Scene/counter.png",StoreTextureBudget.Background);
    }
    static void Import(string path,int maximum)
    {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("가구 PNG를 먼저 Assets로 복사하세요: "+path);
        if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single
            &&!importer.mipmapEnabled&&!importer.isReadable&&importer.alphaIsTransparency&&importer.maxTextureSize==maximum
            &&importer.textureCompression==TextureImporterCompression.Compressed)return;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=maximum;
        importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
    }
    static FurnitureSceneLayout Layout(bool requireSprites=true)
    {
        var file=Resources.Load<TextAsset>("StoreInventory/furniture_layout");
        if(file==null)throw new InvalidOperationException("furniture_layout.json이 없습니다.");
        var result=JsonUtility.FromJson<FurnitureSceneLayout>(file.text);
        if(result==null||result.entries==null||result.entries.Length!=12)throw new InvalidOperationException("가구 배치 데이터12개가 필요합니다.");
        var numbers=new HashSet<int>();var ids=new HashSet<string>();
        foreach(var e in result.entries)
            if(e==null||e.number<1||e.number>12||!numbers.Add(e.number)||string.IsNullOrEmpty(e.typeId)||!ids.Add(e.typeId)
                ||string.IsNullOrEmpty(e.spriteKey)||!Finite(e.x)||!Finite(e.y)||!Finite(e.width)||!Finite(e.height)||e.width<=0||e.height<=0
                ||e.x+e.width<=0||e.y<0||e.x+e.width>1920||e.y+e.height>1080||(requireSprites&&Resources.Load<Sprite>(e.spriteKey)==null))
                throw new InvalidOperationException("가구 배치 좌표/ID/Sprite 오류입니다.");
        return result;
    }
    // Apply the reference composition once when upgrading an authored demo.
    internal static void UpgradeFloorPlacement(GameObject root)
    {
        var entries = Layout(false).entries;
        foreach (var spot in root.GetComponentsInChildren<SceneFurnitureHotspot>(true))
        {
            var entry = Array.Find(entries, candidate => candidate.typeId == spot.typeId);
            if (entry == null || spot.furnitureImage == null) continue;
            var rect = (RectTransform)spot.transform;
            var image = spot.furnitureImage.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(entry.x, -entry.y);
            rect.sizeDelta = new Vector2(entry.width, entry.height);
            rect.localScale = Vector3.one;
            image.anchorMin = image.anchorMax = image.pivot = new Vector2(0, 1);
            image.anchoredPosition = Vector2.zero;
            image.sizeDelta = rect.sizeDelta;
            image.localScale = Vector3.one;
        }
    }
    static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
    public static void Configure(DisplayWindowAppearance appearance,FurnitureArtCatalog art,DisplayCatalog catalog)
    {
        var layout=Layout();art.entries=new FurnitureArt[layout.entries.Length];appearance.profiles=new DisplayAppearanceProfile[layout.entries.Length];
        for(int n=0;n<layout.entries.Length;n++)
        {
            var e=layout.entries[n];if(catalog.Type(e.typeId)==null)throw new InvalidOperationException("배치 종류가 진열 정의에 없습니다: "+e.typeId);
            art.entries[n]=new FurnitureArt{typeId=e.typeId,thumbnail=Resources.Load<Sprite>(e.spriteKey),description=e.title+" · 층/칸막이는 JSON에서 수정할 수 있습니다."};
            appearance.profiles[n]=new DisplayAppearanceProfile{typeId=e.typeId,initialPosition=new Vector2(960+(n%3)*90,-100-(n%3)*60),
                minimumWidth=480,topPadding=100,bottomPadding=174,maximumGridWidth=624,maximumGridHeight=520,frameColor=new Color(.28f,.17f,.08f)};
        }
    }
    public static void Hotspots(Transform parent,InventoryPanel panel,DisplayUI displays,SceneCounterOcclusion counter)
    {
        var entries=Layout().entries;
        var presenter=parent.gameObject.AddComponent<FurnitureHoverPresenter>();
        presenter.image=UI.Box("Shared Furniture Outline",parent,0,0,1,1,Color.white);presenter.image.raycastTarget=false;presenter.image.enabled=false;
        presenter.thicknessEffect=presenter.image.gameObject.AddComponent<global::UnityEngine.UI.Outline>();presenter.thicknessEffect.enabled=false;
        // 뒤쪽 가구부터 만들고 앞쪽 가구가 그 위에 그려지도록 합니다.
        Array.Sort(entries,(a,b)=>a.drawOrder.CompareTo(b.drawOrder));
        foreach(var e in entries)
        {
            int number=e.number;
            var root=UI.Rect("Furniture "+number.ToString("00")+" "+e.typeId,parent,e.x,e.y,e.width,e.height);
            var hotspot=root.gameObject.AddComponent<SceneFurnitureHotspot>();hotspot.panel=panel;hotspot.displays=displays;hotspot.typeId=e.typeId;
            var sprite=Resources.Load<Sprite>(e.spriteKey);
            hotspot.furnitureImage=UI.Box("Furniture Sprite",root.transform,0,0,e.width,e.height,Color.white);
            hotspot.furnitureImage.sprite=sprite;hotspot.furnitureImage.preserveAspect=true;hotspot.furnitureImage.raycastTarget=false;
            hotspot.hoverPresenter=presenter;hotspot.counter=counter;
            hotspot.hoverSprite=Resources.Load<Sprite>(e.hoverSpriteKey);hotspot.furnitureAlphaBounds=e.furnitureAlphaBounds;hotspot.outlineAlphaBounds=e.outlineAlphaBounds;
            // 이 배치는 데모 생성 시 한 번만 합니다. Play/호버 시 Frame 값은 덮어쓰지 않습니다.
            var sp=hotspot.furnitureImage.sprite;var fit=FurnitureOutlineGeometry.Align(sp.rect.width,sp.rect.height,e.width,e.height,e.furnitureAlphaBounds,e.outlineAlphaBounds,presenter.padding);
            hotspot.outlineFrame=UI.Rect("Outline Frame",hotspot.furnitureImage.transform,fit.x,fit.y,fit.width,fit.height);

        }
    }
}
#endif
