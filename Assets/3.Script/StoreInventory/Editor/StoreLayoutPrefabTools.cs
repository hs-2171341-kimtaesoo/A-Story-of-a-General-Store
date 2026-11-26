#if UNITY_EDITOR
using System;
using System.IO;
using GeneralStore.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Object=UnityEngine.Object;

// Bootstrap once in the user's Unity project so references use that project's real .meta GUIDs.
// Subsequent demos only instantiate the saved layout. They never regenerate authored UI.
public static class StoreLayoutPrefabTools
{
    public const int CurrentSchema=3;
    public const string Folder="Assets/StoreInventorySettings/Prefabs";
    public const string LayoutPath=Folder+"/StoreLayout.prefab";
    public const string DisplayRowPath=Folder+"/Rows/OwnedDisplayRow.prefab";
    public const string StockRowPath=Folder+"/Rows/DisplayStockRow.prefab";
    public const string PurchaseRowPath=Folder+"/Rows/FurniturePurchaseRow.prefab";
    public const string ItemPath=Folder+"/Rows/InventoryItem.prefab";
    public const string ManagementButtonPath=Folder+"/Rows/ManagementMenuButton.prefab";
    public const string DisplayButtonPath=Folder+"/Rows/DisplayMenuButton.prefab";

    public static void CreateLatestDemo()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        TemporaryStorageArtBuilder.ValidateArt();
        var prefab=Existing(LayoutPath);
        // All validation precedes the save/new-scene dialog. A broken prefab is never replaced.
        if(prefab!=null)ValidateLayout(prefab,true);
        else
        {
            CustomDisplayArtBuilder.ValidateDefinitions();
            SideMenuSceneBuilder.ValidateManagementArt();PurchaseSceneBuilder.ValidateArt();
        }
        if(!LatestDemoSetup.BeginNewScene())return;
        GameObject root;
        if(prefab==null)
        {
            root=InventorySceneBuilder.BuildInitialLayout();
            AttachBindings(root);SaveSharedTemplates(root);ValidateLayout(root);
            SaveNewLayout(root,LayoutPath);
            // Validate the persisted representation as well as the original hierarchy.
            prefab=AssetDatabase.LoadAssetAtPath<GameObject>(LayoutPath);ValidateLayout(prefab);
        }
        else
        {
            if(prefab.GetComponent<StoreLayoutRoot>().schemaVersion<CurrentSchema)prefab=UpgradeControls(LayoutPath);
            root=InstantiateLayout(prefab);
            Undo.RegisterCreatedObjectUndo(root,"Create Prefab Demo");
        }
        EnsureSceneInput();CompactDemoSceneBuilder.ConfigureUiOnlyCamera(root.scene.handle);
        Selection.activeGameObject=root;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("새 데모를 StoreLayout.prefab 인스턴스로 생성했습니다. UI 배치는 Prefab에서 수정하고 저장하면 다음 데모에도 유지됩니다. 씬만 수정한 내용은 Unity의 Overrides > Apply로 저장하세요. Prefab: "+LayoutPath,root);
    }

    // EventSystem is a scene service. Input System default actions can be temporary assets,
    // so keep the module outside the editable game prefab and create it once in each new scene.
    static void EnsureSceneInput()
    {
        if(Object.FindFirstObjectByType<EventSystem>()!=null)return;
        var events=new GameObject("EventSystem",typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(events,"Create Demo EventSystem");
#if ENABLE_INPUT_SYSTEM
        events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
        events.AddComponent<StandaloneInputModule>();
#endif
    }

    static GameObject Existing(string path)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(asset==null&&File.Exists(path))throw new InvalidOperationException("Prefab을 읽지 못했습니다. 기존 파일은 덮어쓰지 않습니다: "+path);
        return asset;
    }
    internal static void EnsureFolder(string folder)
    {
        if(AssetDatabase.IsValidFolder(folder))return;
        string parent=Path.GetDirectoryName(folder).Replace('\\','/');
        EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));
    }
    internal static void SaveNewLayout(GameObject root,string path)
    {
        if(File.Exists(path)||AssetDatabase.LoadMainAssetAtPath(path)!=null)
            throw new InvalidOperationException("이미 저장된 Prefab은 생성 코드로 덮어쓰지 않습니다: "+path);
        ValidateReferenceScope(root);EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));
        bool success;var asset=PrefabUtility.SaveAsPrefabAssetAndConnect(root,path,InteractionMode.AutomatedAction,out success);
        if(!success||asset==null)throw new InvalidOperationException("Prefab 저장 실패. 생성된 씬은 유지됩니다: "+path);
    }
    internal static GameObject InstantiateLayout(GameObject prefab)
    {return InstantiateLayout(prefab,UnityEngine.SceneManagement.SceneManager.GetActiveScene());}
    internal static GameObject InstantiateLayout(GameObject prefab,UnityEngine.SceneManagement.Scene scene)
    {
        var root=PrefabUtility.InstantiatePrefab(prefab,scene) as GameObject;
        if(root==null)throw new InvalidOperationException("Prefab 인스턴스 생성 실패: "+AssetDatabase.GetAssetPath(prefab));
        return root;
    }
    static void AttachBindings(GameObject root)
    {
        var layout=root.AddComponent<StoreLayoutRoot>();layout.schemaVersion=CurrentSchema;
        layout.inventory=root.GetComponentInChildren<InventoryRuntime>(true);
        layout.panel=root.GetComponentInChildren<InventoryPanel>(true);
        layout.management=root.GetComponentInChildren<DisplayManagementUI>(true);
        layout.purchases=root.GetComponentInChildren<UpgradeUI>(true);
        layout.viewport=root.GetComponentInChildren<ShopViewportLayout>(true);
        root.GetComponentInChildren<RightSideMenu>(true).PrepareAuthoredLayout();
        root.GetComponentInChildren<LeftDisplayMenu>(true).PrepareAuthoredLayout();
        layout.viewport.Apply();
        layout.viewport.CaptureAuthoredManagementPlacement();
        ConfigureControls(root);
    }
    // Keep existing authored panels. Only the legacy temporary-storage window gains the new art.
    internal static GameObject UpgradeControls(string path)
    {
        var contents=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var layout=contents.GetComponent<StoreLayoutRoot>();
            if(layout==null||layout.schemaVersion<1||layout.schemaVersion>=CurrentSchema)throw new InvalidOperationException("지원하지 않는 Prefab 갱신 버전입니다: "+path);
            var delivery=contents.GetComponentInChildren<DeliveryUI>(true);
            if(!delivery.useStorageArtwork)TemporaryStorageArtBuilder.UpgradeWindow(delivery);
            ConfigureControls(contents);layout.schemaVersion=CurrentSchema;ValidateLayout(contents);
            bool success;var asset=PrefabUtility.SaveAsPrefabAsset(contents,path,out success);
            if(!success||asset==null)throw new InvalidOperationException("단축키/임시 저장고 Prefab 갱신 실패: "+path);
            return asset;
        }
        finally{PrefabUtility.UnloadPrefabContents(contents);}
    }
    static void ConfigureControls(GameObject root)
    {
        var layout=root.GetComponent<StoreLayoutRoot>();var menu=root.GetComponentInChildren<RightSideMenu>(true);
        var delivery=root.GetComponentInChildren<DeliveryUI>(true);
        var shortcuts=layout.panel.GetComponent<StoreHotkeys>();if(shortcuts==null)shortcuts=layout.panel.gameObject.AddComponent<StoreHotkeys>();
        shortcuts.panel=layout.panel;shortcuts.menu=menu;shortcuts.drawer=layout.panel.shop.drawer;layout.hotkeys=shortcuts;
        layout.management.display.managementMenu=menu;layout.management.display.slideDuration=menu.windowSlideSeconds;
        delivery.managementMenu=menu;
        var drag=delivery.window.GetComponent<TemporaryStorageWindowDrag>();if(drag==null)drag=delivery.window.gameObject.AddComponent<TemporaryStorageWindowDrag>();
        drag.delivery=delivery;drag.panel=layout.panel;drag.window=delivery.window;drag.bounds=delivery.window.parent as RectTransform;
        delivery.windowDrag=drag;
        var storageMotion=ManagementWindowMotion.Ensure(delivery.window.gameObject,delivery.window);storageMotion.duration=menu.windowSlideSeconds;
        var workshop=root.GetComponentInChildren<WorkshopUI>(true);if(workshop!=null)ManagementWindowMotion.Ensure(workshop.window,workshop.frame,true).duration=menu.windowSlideSeconds;
        foreach(var entry in menu.entries)
        {
            entry.visible=true;
            if(entry.id=="temporary_storage"){entry.unlocked=true;entry.visible=false;}
            if(entry.window==null)continue;
            var motion=entry.window.GetComponent<ManagementWindowMotion>();if(motion!=null)motion.duration=menu.windowSlideSeconds;
        }
    }
    static void SaveSharedTemplates(GameObject root)
    {
        var layout=root.GetComponent<StoreLayoutRoot>();
        ReplaceTemplate(layout.management.cardTemplate,DisplayRowPath,row=>layout.management.cardTemplate=row);
        ReplaceTemplate(layout.management.stockTemplate,StockRowPath,row=>layout.management.stockTemplate=row);
        ReplaceTemplate(layout.purchases.cardTemplate,PurchaseRowPath,row=>layout.purchases.cardTemplate=row);
        var grids=root.GetComponentsInChildren<InventoryGridView>(true);
        var item=layout.panel.grids[0].itemTemplate;
        ReplaceTemplate(item,ItemPath,row=>{foreach(var grid in grids)if(grid.itemTemplate==item)grid.itemTemplate=row;});
        var management=root.GetComponentInChildren<RightSideMenu>(true);
        ReplaceTemplate(management.template,ManagementButtonPath,row=>management.template=row);
        var display=root.GetComponentInChildren<LeftDisplayMenu>(true);
        ReplaceTemplate(display.template,DisplayButtonPath,row=>display.template=row);
    }
    static void ReplaceTemplate<T>(T seed,string path,Action<T> assign) where T:Component
    {
        var template=SaveTemplateAsset(seed,path);assign(template);
        // Controllers instantiate the asset directly. Hidden scene copies would retain root-size
        // overrides and could prevent an edited row prefab height from reaching the next demo.
        Object.DestroyImmediate(seed.gameObject);
    }
    internal static T SaveTemplateAsset<T>(T seed,string path) where T:Component
    {
        if(seed==null)throw new InvalidOperationException("행 템플릿이 없습니다: "+path);
        var asset=Existing(path);
        if(asset==null)
        {
            ValidateReferenceScope(seed.gameObject);EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));
            bool active=seed.gameObject.activeSelf;
            try
            {
                seed.gameObject.SetActive(true);bool success;
                asset=PrefabUtility.SaveAsPrefabAsset(seed.gameObject,path,out success);
                if(!success||asset==null)throw new InvalidOperationException("행 Prefab 저장 실패: "+path);
            }
            finally{seed.gameObject.SetActive(active);}
        }
        // Existing independent row assets are reused even after an interrupted initial bootstrap.
        var template=asset.GetComponent<T>();
        if(template==null)throw new InvalidOperationException("행 Prefab의 컴포넌트를 확인하세요: "+path);
        ValidateReferenceScope(asset);return template;
    }

    internal static void ValidateLayout(GameObject root,bool allowPrevious=false)
    {
        if(root==null)throw new InvalidOperationException("StoreLayout Prefab이 없습니다.");
        var layout=root.GetComponent<StoreLayoutRoot>();
        if(layout==null||(layout.schemaVersion!=CurrentSchema&&!(allowPrevious&&layout.schemaVersion>=1&&layout.schemaVersion<CurrentSchema)))
            throw new InvalidOperationException("StoreLayout Prefab 버전을 확인하세요. 기존 배치는 보존됩니다: "+LayoutPath);
        if(layout.inventory==null||layout.panel==null||layout.management==null||layout.purchases==null||layout.viewport==null)
            throw new InvalidOperationException("StoreLayout의 데이터/UI 연결이 누락되었습니다.");
        if(root.GetComponentsInChildren<InventoryRuntime>(true).Length!=1||root.GetComponentsInChildren<InventoryPanel>(true).Length!=1)
            throw new InvalidOperationException("StoreLayout에는 재고 데이터와 UI 연결이 각각 하나 필요합니다.");
        var panel=layout.panel;var management=layout.management;var purchases=layout.purchases;
        if(panel.inventory!=layout.inventory||panel.canvas==null||panel.drag==null||panel.drag.dragLayer==null||panel.shop==null||panel.grids==null||panel.grids.Length==0||panel.detailViews==null||panel.messageLabel==null)
            throw new InvalidOperationException("StoreLayout의 InventoryPanel 참조를 확인하세요.");
        if(management.panel!=panel||management.display==null||management.art==null||management.window==null||management.displayList==null||management.stockList==null||management.cardTemplate==null||management.stockTemplate==null||management.selectedLabel==null||management.footerLabel==null||management.openButton==null)
            throw new InvalidOperationException("진열장 관리 Prefab 참조가 누락되었습니다.");
        if(purchases.panel!=panel||purchases.warehouse==null||purchases.display==null||purchases.art==null||purchases.window==null||purchases.storageWindow==null||purchases.furnitureList==null||purchases.cardTemplate==null||purchases.furniturePreview==null||purchases.typeLabel==null||purchases.displayInfo==null||purchases.storageInfo==null||purchases.storageNumbers==null||purchases.storageActionLabel==null||purchases.addDisplayButton==null||purchases.storageActionButton==null)
            throw new InvalidOperationException("구매창 Prefab 참조가 누락되었습니다.");
        if(layout.viewport.panel!=panel||layout.viewport.stage==null||layout.viewport.edgeLayer==null||layout.viewport.drawer==null||layout.viewport.displays==null||layout.viewport.managementWindows==null||layout.viewport.managementFrames==null||layout.viewport.managementWindows.Length!=layout.viewport.managementFrames.Length)
            throw new InvalidOperationException("화면 대응 배치의 Prefab 참조가 누락되었습니다.");
        var managementMenu=root.GetComponentInChildren<RightSideMenu>(true);var displayMenu=root.GetComponentInChildren<LeftDisplayMenu>(true);
        if(managementMenu==null||managementMenu.panel!=panel||managementMenu.content==null||managementMenu.template==null||displayMenu==null||displayMenu.panel!=panel||displayMenu.template==null||displayMenu.scroll==null)
            throw new InvalidOperationException("양쪽 메뉴의 Prefab 참조가 누락되었습니다.");
        if(layout.schemaVersion==CurrentSchema)
        {
            var delivery=root.GetComponentInChildren<DeliveryUI>(true);var drag=delivery==null||delivery.window==null?null:delivery.window.GetComponent<TemporaryStorageWindowDrag>();
            if(layout.hotkeys==null||layout.hotkeys.panel!=panel||layout.hotkeys.menu!=managementMenu||layout.hotkeys.drawer!=panel.shop.drawer||management.display.managementMenu!=managementMenu
                ||delivery==null||delivery.managementMenu!=managementMenu||drag==null||drag.delivery!=delivery||drag.panel!=panel||drag.window!=delivery.window||drag.bounds==null)
                throw new InvalidOperationException("단축키/임시 저장고 드래그 Prefab 연결이 누락되었습니다.");
            if(!delivery.useStorageArtwork||delivery.recoverButton==null||delivery.recoverLabel==null||delivery.recoverEnabledArtwork==null||delivery.recoverDisabledArtwork==null||delivery.usedCellsLabel==null||delivery.gridArtwork==null)
                throw new InvalidOperationException("임시 저장고 아트/회수 버튼 Prefab 연결이 누락되었습니다.");
        }
        foreach(var grid in root.GetComponentsInChildren<InventoryGridView>(true))
            if(grid.itemTemplate==null||grid.itemTemplate.icon==null||grid.itemTemplate.nameLabel==null||grid.itemLayer==null)
                throw new InvalidOperationException("격자/아이템 Prefab 연결 누락: "+grid.name);
        var displays=management.display;
        if(displays.typeTemplates==null||displays.typeTemplates.Length!=12)throw new InvalidOperationException("진열창 Prefab 템플릿 12종의 연결을 확인하세요.");
        foreach(var entry in displays.typeTemplates)if(entry==null||entry.template==null||entry.template.grid==null||entry.template.appearance==null)
            throw new InvalidOperationException("진열창 Prefab 연결이 누락되었습니다.");
        ValidateReferenceScope(root);
    }
    internal static void ValidateReferenceScope(GameObject root)
    {
        foreach(var component in root.GetComponentsInChildren<Component>(true))
        {
            if(component==null)throw new InvalidOperationException("Prefab 안에 Missing Script가 있습니다: "+root.name);
            var serialized=new SerializedObject(component);var property=serialized.GetIterator();
            while(property.Next(true))
            {
                // Native prefab bookkeeping is not a user reference. A subprefab root may have
                // a scene parent; Unity intentionally excludes that parent when saving the asset.
                if(property.propertyPath=="m_PrefabInstance"||property.propertyPath=="m_PrefabAsset"||property.propertyPath=="m_CorrespondingSourceObject"||
                    (component.transform==root.transform&&component is Transform&&property.propertyPath=="m_Father"))continue;
                if(property.propertyType!=SerializedPropertyType.ObjectReference)continue;
                var reference=property.objectReferenceValue;if(reference==null)continue;
                var target=reference as Component;var go=reference as GameObject;
                Transform transform=target!=null?target.transform:go!=null?go.transform:null;
                if(transform!=null&&(transform==root.transform||transform.IsChildOf(root.transform)))continue;
                if(EditorUtility.IsPersistent(reference))continue;
                throw new InvalidOperationException("Prefab 밖의 씬/임시 오브젝트 참조: "+component.name+"."+property.propertyPath+" → "+reference.name);
            }
        }
    }
}

[CustomEditor(typeof(StoreLayoutRoot))]
public sealed class StoreLayoutRootInspector:Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();EditorGUILayout.Space();
        EditorGUILayout.HelpBox("이 Prefab은 완성품으로 계속 사용하는 공통 배치입니다. 새 데모는 저장된 Prefab을 그대로 배치합니다. 배경/아이콘 Slot/텍스트/버튼은 Prefab Mode에서 수정하세요. Play 중 변경은 저장되지 않습니다. 씬 인스턴스 수정은 Overrides > Apply로 저장하세요.",MessageType.Info);
        var layout=(StoreLayoutRoot)target;
        using(new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if(GUILayout.Button("공통 배치 Prefab 열기"))Open(StoreLayoutPrefabTools.LayoutPath);
            if(GUILayout.Button("진열장 목록 행 Prefab 열기"))Open(StoreLayoutPrefabTools.DisplayRowPath);
            if(GUILayout.Button("진열 품목 행 Prefab 열기"))Open(StoreLayoutPrefabTools.StockRowPath);
            if(GUILayout.Button("가구 구매 행 Prefab 열기"))Open(StoreLayoutPrefabTools.PurchaseRowPath);
            if(GUILayout.Button("아이템 Prefab 열기"))Open(StoreLayoutPrefabTools.ItemPath);
            if(layout.management!=null&&GUILayout.Button("Hierarchy에서 진열장 관리창 선택"))Selection.activeGameObject=layout.management.window;
            if(layout.purchases!=null&&GUILayout.Button("Hierarchy에서 가구 구매창 선택"))Selection.activeGameObject=layout.purchases.window;
            if(layout.purchases!=null&&GUILayout.Button("Hierarchy에서 상자 추가창 선택"))Selection.activeGameObject=layout.purchases.storageWindow;
        }
    }
    static void Open(string path)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(asset!=null)AssetDatabase.OpenAsset(asset);else Debug.LogWarning("Create Latest Demo를 한 번 실행해 Prefab을 생성하세요: "+path);
    }
}
#endif
