#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
// Import limits only; source PNGs and authored sprite coordinates are unchanged.
public static class StoreTextureBudget
{
    static string Key{get{return "GeneralStore.LowMemoryTextures."+Application.dataPath;}}
    public static bool LowMemory{get{return EditorPrefs.GetBool(Key,true);}}
    public static int Furniture{get{return LowMemory?512:1024;}}
    public static int Outline{get{return LowMemory?256:512;}}
    public static int DisplayArt{get{return LowMemory?1024:2048;}}
    public static int Background{get{return LowMemory?1024:2048;}}
    public static int Customer{get{return LowMemory?512:1024;}}
    [MenuItem("Store Inventory/Memory/Use Low Memory Textures")]
    public static void UseLow(){Apply(true);}
    [MenuItem("Store Inventory/Memory/Restore Previous Texture Quality")]
    public static void UsePrevious(){Apply(false);}
    public static void PrepareIconImports()
    {
        foreach(string folder in new[]{"Assets/ItemIcons","Assets/Resources/ItemIcons","Assets/Resources/SupplierPortraits"})
        {
            if(!AssetDatabase.IsValidFolder(folder))continue;
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{folder}))
            {
                var importer=AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;if(importer==null||importer.textureType!=TextureImporterType.Sprite)continue;
                int cap=importer.spriteImportMode==SpriteImportMode.Single?Mathf.Min(512,importer.maxTextureSize):importer.maxTextureSize;
                if(!importer.isReadable&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Compressed&&importer.maxTextureSize==cap)continue;
                importer.isReadable=false;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Compressed;importer.maxTextureSize=cap;importer.SaveAndReimport();
            }
        }
    }
    static void Apply(bool low)
    {
        if(Application.isPlaying){Debug.LogWarning("Stop Play before reimporting textures.");return;}
        EditorPrefs.SetBool(Key,low);
        Furniture13SceneBuilder.PrepareSprites();CustomDisplayArtBuilder.PrepareSprites();
        CounterDemoSceneBuilder.PrepareSprites();SlidingDemoSceneBuilder.PrepareCustomerSprite();DisplayMenuTextureImporter.PrepareSprites();SideMenuSceneBuilder.PrepareManagementArt();PurchaseSceneBuilder.PrepareArt();PrepareIconImports();
        Debug.Log("Store texture profile: "+(low?"Low Memory":"Previous Quality")+". Original PNGs and scene transforms preserved. Compare in a fresh Player run; this does not clear Editor memory.");
    }
}

// Applies only cache/import settings. It does not rebuild windows or move authored transforms.
public static class StoreOptimizationTools
{
    [MenuItem("Store Inventory/Optimization/Apply Safe Settings To Selection")]
    public static void ApplySelection()
    {
        if(Application.isPlaying){Debug.LogWarning("Play를 중지하고 Store Scene을 선택하세요.");return;}
        if(Selection.activeGameObject==null){Debug.LogWarning("Hierarchy의 Store Scene을 선택하세요.");return;}
        Apply(Selection.activeGameObject.transform.root.gameObject);
    }
    public static void Apply(GameObject root)
    {
        if(Application.isPlaying||root==null)return;if(root.GetComponentInChildren<GeneralStore.Inventory.InventoryRuntime>(true)==null){Debug.LogWarning("Store Inventory Data가 포함된 씬 루트를 선택하세요.");return;}Undo.RegisterFullObjectHierarchyUndo(root,"Optimize Store Settings");
        foreach(var grid in root.GetComponentsInChildren<GeneralStore.Inventory.InventoryGridView>(true)){grid.recycleItems=true;grid.maximumPooledItems=32;EditorUtility.SetDirty(grid);}
        foreach(var display in root.GetComponentsInChildren<GeneralStore.Inventory.DisplayUI>(true)){display.maximumCachedWindows=Mathf.Min(2,Mathf.Max(1,display.maximumCachedWindows));EditorUtility.SetDirty(display);}
        foreach(var runtime in root.GetComponentsInChildren<GeneralStore.Inventory.InventoryRuntime>(true))
        {
            if(runtime.GetComponent<GeneralStore.Inventory.StorePerformanceSettings>()==null)Undo.AddComponent<GeneralStore.Inventory.StorePerformanceSettings>(runtime.gameObject);
            var catalog=runtime.spriteCatalog;if(catalog!=null&&catalog.useLazyResourceIcons)
            {
                Undo.RecordObject(catalog,"Use Lazy Resource Icons");
                foreach(var row in catalog.itemIcons)
                {
                    if(row==null||row.sprite==null||row.sprite.name!=row.key)continue;string path=AssetDatabase.GetAssetPath(row.sprite);int at=path.LastIndexOf("/Resources/",System.StringComparison.Ordinal);
                    var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                    if(at<0||importer==null||importer.spriteImportMode!=SpriteImportMode.Single||System.IO.Path.GetFileNameWithoutExtension(path)!=row.key)continue;
                    row.resourcePath=path.Substring(at+11,path.Length-at-11-System.IO.Path.GetExtension(path).Length);row.sprite=null;
                }
                EditorUtility.SetDirty(catalog);
            }
        }
        AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("선택 씬의 아이템 풀32개/진열창 캐시 최대2개/Resources 아이콘 지연 로딩을 적용했습니다. 가구 위치, UI 배치, 거래·제작 데이터와 테스트 기능은 유지했습니다. 씬을 저장하세요.");
    }
}
[CustomEditor(typeof(GeneralStore.Inventory.InventoryRuntime))]
public sealed class InventoryRuntimeInspector:Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();EditorGUILayout.Space();
        EditorGUILayout.HelpBox("배치 수정은 기존 씬에서 계속해도 됩니다. 아래 최적화는 창을 재생성하지 않습니다. 텍스처 크기/Read Write/Mipmap은 Store Inventory > Memory > Use Low Memory Textures에서 적용하세요.",MessageType.Info);
        var runtime=(GeneralStore.Inventory.InventoryRuntime)target;
        if(!Application.isPlaying)
        {
            if(GUILayout.Button("기존 씬에 최적화 설정 적용"))StoreOptimizationTools.Apply(runtime.transform.root.gameObject);
        }
        else
        {
            var probe=runtime.GetComponent<GeneralStore.Inventory.StorePerformanceProbe>();
            if(probe!=null){if(GUILayout.Button("메모리 현황 출력"))probe.Report();if(GUILayout.Button("30초 GC / 프레임 측정 시작"))probe.StartGcSample();}
        }
    }
}
#endif
