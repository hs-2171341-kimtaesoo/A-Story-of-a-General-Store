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
        InactiveWindowReferenceChecks.Run();WindowShieldUnityChecks.Run();
        Debug.Log("입력/중앙 메뉴 검사 "+InputAndMenuChecks.Run()+"개 통과");
        Debug.Log("메뉴/선택/유료확장 검사 "+SideMenuFeatureChecks.Run()+"개 통과");
        TextAsset json = Resources.Load<TextAsset>("StoreInventory/items");
        if (json == null) throw new System.InvalidOperationException("JSON 파일이 없습니다.");
        // 실제 프로젝트 JSON도 Unity의 실제 JsonUtility로 읽어서 검사합니다.
        var catalog = new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(json.text));
        var customers=Resources.Load<TextAsset>("StoreInventory/customers");
        if(customers==null)throw new System.InvalidOperationException("손님 JSON이 없습니다.");
        var customerCatalog=new CustomerCatalog(JsonUtility.FromJson<CustomerDatabase>(customers.text));
        var suppliers=Resources.Load<TextAsset>("StoreInventory/suppliers");
        if(suppliers==null)throw new System.InvalidOperationException("거래처 JSON이 없습니다.");
        var supplierCatalog=new SupplierCatalog(JsonUtility.FromJson<SupplierDatabase>(suppliers.text),catalog);
        var fixtures=Resources.Load<TextAsset>("StoreInventory/display_fixtures");
        if(fixtures==null)throw new System.InvalidOperationException("진열 정의 JSON이 없습니다.");
        var fixtureCatalog=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(fixtures.text));
        Debug.Log("서랍/독립 창 검사 "+DrawerFeatureChecks.Run()+"개 통과");
        Debug.Log("진열 정의 "+fixtureCatalog.TypeIds().Count+"종 / 검사 "+FixtureFeatureChecks.Run()+"개 통과");
        Debug.Log("성능 회귀 검사 "+PerformanceFeatureChecks.Run()+"개 / "+PerformanceFeatureChecks.Report);
        Debug.Log("거래처 JSON "+supplierCatalog.All().Count+"곳 / 검사 "+SupplierFeatureChecks.Run()+"개 통과");
        Debug.Log("조정 검사 "+BalancedFeatureChecks.Run()+"개 통과 / "+BalancedFeatureChecks.Benchmark);
        Debug.Log("카운터 검사 "+CounterFeatureChecks.Run()+"개 통과 / "+CounterFeatureChecks.Benchmark);
        Debug.Log("손님 JSON "+customerCatalog.All().Count+"명 / 월드 검사 "+WorldFeatureChecks.Run()+"개 통과");
        Debug.Log("실제 JSON " + catalog.Count + "종 검증 완료 / 모델 " + InventoryModelChecks.Run() + "개 / 상점 검사 " + ShopFeatureChecks.Run() + "개 통과");
    }
}

#endif
