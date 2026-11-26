#if UNITY_EDITOR
using GeneralStore.Inventory;
using GeneralStore.Inventory.Verification;
using UnityEditor;
using UnityEngine;

public static class InventoryCheckMenu
{
    [MenuItem("Store Inventory/Run Model Checks")]
    public static void Run()
    {
        ItemTableTools.RunChecks();
        Debug.Log("읽기 캐시/자동 배치/조합 키 최적화 회귀 검사 "+OptimizationFeatureChecks.Run()+"개 통과");
        Debug.Log("스크롤/8×7/하부 칸막이 검사 "+ScrollableInventoryChecks.Run()+"개 통과");
        Debug.Log("실제 JSON 소형 데모 초기화 검사 "+CompactDemoUnityChecks.Run()+"개 통과");
        Debug.Log("공방 제작/품질/일급/저장 검사 "+WorkshopFeatureChecks.Run()+"개 통과");
        Debug.Log("카운터 낙하/경계/정착 검사 "+CounterMotionChecks.Run()+"개 통과");
        Debug.Log("카운터 실물/협의 총액 검사 "+CounterCheckoutChecks.Run()+"개 통과");
        Debug.Log("층/칸막이 모델 검사 "+FurniturePartitionChecks.Run()+"개 통과");
        Debug.Log("레이어/마스크/외곽선 수학 검사 "+SceneLayerFeatureChecks.Run()+"개 통과");
        InactiveWindowReferenceChecks.Run();WindowShieldUnityChecks.Run();
        Debug.Log("입력/중앙 메뉴 검사 "+InputAndMenuChecks.Run()+"개 통과");
        Debug.Log("관리 창 순차 퇴장/마지막 선택/전환 취소 검사 "+ManagementWindowTransitionChecks.Run()+"개 통과");
        Debug.Log("양피지 관리창 비율/행 선택/스크롤/Texture 검사 "+ParchmentManagementChecks.Run()+"개 통과");
        Debug.Log("상자/가구 구매 UI 연결·실제 구매·가격 갱신·빈 목록 검사 "+PurchasePanelChecks.Run()+"개 통과");
        Debug.Log("Prefab 저장/참조 복원/수정 유지/중앙 아이콘 검사 "+PrefabLayoutChecks.Run()+"개 통과");
        Debug.Log("단축키/순차 퇴장/임시 저장고 이동/공통 속도 검사 "+WindowControlsChecks.Run()+"개 통과");
        Debug.Log("임시 저장고 순차 회수/첫 실패 중지/128칸 아트/빈 상태 검사 "+TemporaryStorageChecks.Run()+"개 통과");
        Debug.Log("고정56칸/상자 추가 검사 "+SideMenuFeatureChecks.Run()+"개 통과");
        TextAsset json = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/3.Script/StoreInventory/Editor/TestData/items.json");
        if (json == null) throw new System.InvalidOperationException("JSON 파일이 없습니다.");
        // 메인은 위 RunChecks에서 검사합니다. 아래는 Editor 전용 고정 회귀 자료입니다.
        var catalog = new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(json.text));
        var customers=UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/3.Script/StoreInventory/Editor/TestData/customers.json");
        if(customers==null)throw new System.InvalidOperationException("손님 JSON이 없습니다.");
        var customerCatalog=new CustomerCatalog(JsonUtility.FromJson<CustomerDatabase>(customers.text));
        var suppliers=UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/3.Script/StoreInventory/Editor/TestData/suppliers.json");
        if(suppliers==null)throw new System.InvalidOperationException("거래처 JSON이 없습니다.");
        var supplierCatalog=new SupplierCatalog(JsonUtility.FromJson<SupplierDatabase>(suppliers.text),catalog);
        var fixtures=Resources.Load<TextAsset>("StoreInventory/display_fixtures");
        if(fixtures==null)throw new System.InvalidOperationException("진열 정의 JSON이 없습니다.");
        var fixtureCatalog=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(fixtures.text));
        var artJson=Resources.Load<TextAsset>("StoreInventory/display_window_art");if(artJson==null)throw new System.InvalidOperationException("창 아트 JSON이 없습니다.");
        Debug.Log("전용12종 진열창 검사 "+CustomDisplayArtChecks.Run(catalog,fixtureCatalog,JsonUtility.FromJson<DisplayWindowArtDatabase>(artJson.text))+"개 통과");
        Debug.Log("실제12개 카탈로그 검사 "+FurniturePartitionChecks.CheckCatalogue(fixtureCatalog)+"개 통과");
        Debug.Log("서랍/독립 창 검사 "+DrawerFeatureChecks.Run()+"개 통과");
        Debug.Log("진열 정의 "+fixtureCatalog.TypeIds().Count+"종 / 검사 "+FixtureFeatureChecks.Run()+"개 통과");
        Debug.Log("거래처 JSON "+supplierCatalog.All().Count+"곳 / 검사 "+SupplierFeatureChecks.Run()+"개 통과");
        Debug.Log("직사각 아이콘 검사 "+CounterFeatureChecks.Run()+"개 통과");
        Debug.Log("공통 아이콘/품질 색 검사 "+QualityFeatureChecks.Run()+"개 통과");
        Debug.Log("택배/보관료 검사 "+DeliveryFeatureChecks.Run()+"개 통과");
        Debug.Log("손님 JSON "+customerCatalog.All().Count+"명 / 월드 검사 "+WorldFeatureChecks.Run()+"개 통과");
        Debug.Log("고정 회귀 JSON " + catalog.Count + "종 검증 완료 / 모델 " + InventoryModelChecks.Run() + "개 / 상점 검사 " + ShopFeatureChecks.Run() + "개 통과");
    }
}

#endif
