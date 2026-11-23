#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;

// Editor only. Does not run resource scans or write files during Play.
[InitializeOnLoad]
public static class InventoryAssetTools
{
    const string Folder="Assets/StoreInventorySettings";
    const string CatalogPath=Folder+"/SharedInventorySprites.asset";
    static InventoryAssetTools()
    {
        AssemblyReloadEvents.beforeAssemblyReload+=()=>QualityOutlineEffect.ReleaseSharedMaterial(true);
        EditorApplication.playModeStateChanged+=state=>{if(state==global::UnityEditor.PlayModeStateChange.EnteredEditMode)QualityOutlineEffect.ReleaseSharedMaterial(true);};
    }
    public static void Connect(InventoryRuntime runtime)
    {
        if(runtime.itemJson==null)runtime.itemJson=Json("items");if(runtime.customerJson==null)runtime.customerJson=Json("customers");
        if(runtime.supplierJson==null)runtime.supplierJson=Json("suppliers");if(runtime.displayJson==null)runtime.displayJson=Json("display_fixtures");
        if(runtime.workshopJson==null)runtime.workshopJson=Json("workshops");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets","StoreInventorySettings");
        var legacy=runtime.spriteCatalog!=null?runtime.spriteCatalog:AssetDatabase.LoadAssetAtPath<InventorySpriteCatalog>(Folder+"/InventorySprites.asset");
        var catalog=AssetDatabase.LoadAssetAtPath<InventorySpriteCatalog>(CatalogPath);
        if(catalog==null){catalog=ScriptableObject.CreateInstance<InventorySpriteCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
        if(catalog.itemIcons==null)catalog.itemIcons=new List<InventorySpriteEntry>();
        if(catalog.supplierPortraits==null)catalog.supplierPortraits=new List<InventorySpriteEntry>();
        var items=JsonUtility.FromJson<ItemDatabase>(runtime.itemJson.text);
        var definitions=new ItemCatalog(items);
        var validKeys=new HashSet<string>(StringComparer.Ordinal);foreach(var item in items.items)validKeys.Add(definitions.IconKey(item.id));
        catalog.itemIcons.RemoveAll(row=>row==null||!validKeys.Contains(row.key));
        foreach(var item in items.items)
        {
            string key=definitions.IconKey(item.id);AddKey(catalog.itemIcons,key);
            var row=catalog.itemIcons.Find(e=>e!=null&&e.key==key);
            if(row.sprite==null&&legacy!=null&&legacy!=catalog)
            {
                row.sprite=legacy.FindItem(key);
                if(row.sprite==null)foreach(var quality in item.qualities){row.sprite=legacy.FindItem(quality.iconKey);if(row.sprite!=null)break;}
            }
        }
        var suppliers=JsonUtility.FromJson<SupplierDatabase>(runtime.supplierJson.text);
        foreach(var supplier in suppliers.suppliers)
        {
            AddKey(catalog.supplierPortraits,supplier.portraitKey);
            var row=catalog.supplierPortraits.Find(e=>e!=null&&e.key==supplier.portraitKey);
            if(row!=null&&row.sprite==null&&legacy!=null&&legacy!=catalog)row.sprite=legacy.FindSupplier(supplier.portraitKey);
        }
        runtime.spriteCatalog=catalog;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
    }
    [MenuItem("Store Inventory/Existing Scene Tools/Apply Quality Drawer Update To Selection")]
    public static void UpgradeQualitySelection()
    {
        if(Application.isPlaying){Debug.LogWarning("Stop Play first.");return;}
        var selected=Selection.activeGameObject;if(selected==null){Debug.LogWarning("Select Store Scene or Store Inventory Data.");return;}
        foreach(var runtime in selected.GetComponentsInChildren<InventoryRuntime>(true)){Undo.RecordObject(runtime,"Connect Shared Sprites");Connect(runtime);EditorUtility.SetDirty(runtime);}
        foreach(var drawer in selected.GetComponentsInChildren<WarehouseDrawer>(true)){Undo.RecordObject(drawer,"Drawer Proximity");drawer.startClosed=true;drawer.proximityReveal=true;}
        foreach(var display in selected.GetComponentsInChildren<DisplayUI>(true)){Undo.RecordObject(display,"Center And Speed");display.centerVertically=true;display.slideSpeed=28;}
        foreach(var menu in selected.GetComponentsInChildren<LeftDisplayMenu>(true)){Undo.RecordObject(menu,"Faster Held Hover");menu.hoverOpenSeconds=1;}
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);
        Debug.Log("Quality/drawer update applied to selection. Furniture transforms and sprite source files preserved. Save scene, then Play.");
    }
    [MenuItem("Store Inventory/Existing Scene Tools/Apply Counter Motion Update To Selection")]
    public static void UpgradeCounterMotionSelection()
    {
        if(Application.isPlaying){Debug.LogWarning("Stop Play first.");return;}
        var selected=Selection.activeGameObject;
        if(selected==null||selected.GetComponentsInChildren<CounterTradeUI>(true).Length==0){Debug.LogWarning("Hierarchy에서 Store Scene 전체를 선택하세요. Store Inventory Data만으로는 UI를 갱신할 수 없습니다.");return;}
        Undo.RegisterFullObjectHierarchyUndo(selected,"Counter Motion Update");
        foreach(var warehouse in selected.GetComponentsInChildren<WarehouseUI>(true)){warehouse.hoverSwitchSeconds=1;EditorUtility.SetDirty(warehouse);}
        foreach(var menu in selected.GetComponentsInChildren<LeftDisplayMenu>(true)){menu.hoverOpenSeconds=1;menu.keepVisibleWhileDisplayOpen=true;EditorUtility.SetDirty(menu);}
        foreach(var trade in selected.GetComponentsInChildren<CounterTradeUI>(true))CounterDemoSceneBuilder.ConfigureCounterMotion(trade);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);
        Debug.Log("카운터 연출/1초 전환/진열 목록 유지 적용. 씬을 저장하세요. 가구 위치와 재고 데이터는 유지했습니다.");
    }
    [MenuItem("Store Inventory/Existing Scene Tools/Apply Counter Placement Update To Selection")]
    public static void UpgradeCounterFlatSelection()
    {
        if(Application.isPlaying){Debug.LogWarning("Stop Play first.");return;}
        var selected=Selection.activeGameObject;
        if(selected==null||selected.GetComponentsInChildren<CounterTradeUI>(true).Length==0){Debug.LogWarning("Hierarchy에서 Store Scene 전체를 선택하세요.");return;}
        Undo.RegisterFullObjectHierarchyUndo(selected,"Counter Placement Update");
        foreach(var trade in selected.GetComponentsInChildren<CounterTradeUI>(true))CounterDemoSceneBuilder.ConfigureCounterRow(trade);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);
        Debug.Log("손님 물품90도 표시/겹쳐 놓기/클릭 물품 앞으로/중간 안착선 적용. 기존 가구와 공방 데이터는 유지했습니다. 씬을 저장하세요.");
    }
    static TextAsset Json(string name)
    {
        string path=name=="display_fixtures"?"Assets/Resources/StoreInventory/display_fixtures.json":ItemTableTools.DataFolder+"/"+name+"_table.json";
        var file=AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if(file==null)throw new InvalidOperationException(name+".json이 없습니다.");return file;
    }
    static void AddKey(List<InventorySpriteEntry> entries,string key)
    {
        if(string.IsNullOrEmpty(key)||entries.Exists(e=>e!=null&&e.key==key))return;
        entries.Add(new InventorySpriteEntry{key=key});
    }
    [MenuItem("Store Inventory/Audit/Write Project Resource Report")]
    public static void WriteReport()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogWarning("Stop Play before auditing.");return;}
        var report=new StringBuilder("Store Inventory asset audit (Editor only)\n");
        report.AppendLine("Static dependencies do not prove Resources strings or reflection unused. No assets were deleted.");
        var roots=new List<string>();
        foreach(var scene in EditorBuildSettings.scenes)if(scene.enabled)roots.Add(scene.path);
        foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets"}))roots.Add(AssetDatabase.GUIDToAssetPath(guid));
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets"}))roots.Add(AssetDatabase.GUIDToAssetPath(guid));
        var dependencies=new HashSet<string>(StringComparer.Ordinal);
        foreach(var root in roots)foreach(var path in AssetDatabase.GetDependencies(root,true))dependencies.Add(path);
        report.AppendLine("SCENE/PREFAB ROOTS: "+roots.Count+" (may include duplicates)");
        foreach(var path in AssetDatabase.GetAllAssetPaths())
        {
            if(!path.StartsWith("Assets/",StringComparison.Ordinal))continue;
            bool resource=path.IndexOf("/Resources/",StringComparison.Ordinal)>=0;
            if(path.StartsWith("Assets/3.Script/StoreInventory/",StringComparison.Ordinal)&&path.EndsWith(".cs",StringComparison.Ordinal))
                report.AppendLine("SCRIPT\t"+(path.Contains("/Editor/")?"EDITOR ONLY":dependencies.Contains(path)?"SERIALIZED REFERENCE":"NO SCENE/PREFAB DEPENDENCY (not deletion proof)")+"\t"+path);
            bool authoredData=path.StartsWith(ItemTableTools.DataFolder+"/",StringComparison.Ordinal);
            if((!resource&&!authoredData)||!File.Exists(path))continue;
            report.AppendLine((authoredData?"ITEM TABLE\t":"RESOURCE\t")+new FileInfo(path).Length+" bytes\t"+(dependencies.Contains(path)?"SERIALIZED REFERENCE":"DYNAMIC/EDITOR/UNRESOLVED")+"\t"+path);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null)report.AppendLine("  readable="+importer.isReadable+", mipmap="+importer.mipmapEnabled+", max="+importer.maxTextureSize+", compression="+importer.textureCompression);
        }
        CheckKeys(report,AssetDatabase.GetAllAssetPaths());
        string output="StoreInventory_AuditReport.txt";File.WriteAllText(output,report.ToString(),new UTF8Encoding(false));
        Debug.Log("Report written: "+Path.GetFullPath(output)+". No assets were deleted.");
    }
    static void CheckKeys(StringBuilder report,string[] paths)
    {
        // No Resources.LoadAll or texture pixel readback. The catalog may load its direct dependencies.
        var catalog=AssetDatabase.LoadAssetAtPath<InventorySpriteCatalog>(CatalogPath);
        var items=JsonUtility.FromJson<ItemDatabase>(Json("items").text);
        var definitions=new ItemCatalog(items);
        foreach(var item in items.items)
        {string key=definitions.IconKey(item.id);CheckKey(report,catalog!=null?catalog.FindItem(key):null,"ItemIcons/",key,paths);}
        var suppliers=JsonUtility.FromJson<SupplierDatabase>(Json("suppliers").text);
        foreach(var supplier in suppliers.suppliers)
            CheckKey(report,catalog!=null?catalog.FindSupplier(supplier.portraitKey):null,"SupplierPortraits/",supplier.portraitKey,paths);
    }
    static void CheckKey(StringBuilder report,Sprite direct,string folder,string key,string[] paths)
    {
        if(string.IsNullOrEmpty(key))return;
        if(direct!=null){report.AppendLine("SPRITE KEY\tDIRECT\t"+folder+key);return;}
        string target=folder+key;int matches=0;
        foreach(var path in paths)
        {
            int i=path.IndexOf("/Resources/",StringComparison.Ordinal);if(i<0||!File.Exists(path))continue;
            string resource=path.Substring(i+11);resource=resource.Substring(0,resource.Length-Path.GetExtension(resource).Length);
            if(string.Equals(target,resource,StringComparison.Ordinal))matches++;
        }
        report.AppendLine("SPRITE KEY\t"+(matches==0?"MISSING":matches>1?"DUPLICATE RESOURCE PATH":"RESOURCE PATH EXISTS (verify Sprite importer)")+"\t"+target);
    }
}
#endif
