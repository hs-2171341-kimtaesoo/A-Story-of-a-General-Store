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
        TextAsset json = Resources.Load<TextAsset>("StoreInventory/items");
        if (json == null) throw new System.InvalidOperationException("JSON 파일이 없습니다.");
        // 실제 프로젝트 JSON도 Unity의 실제 JsonUtility로 읽어서 검사합니다.
        var catalog = new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(json.text));
        Debug.Log("실제 JSON " + catalog.Count + "종 검증 완료 / 모델 " + InventoryModelChecks.Run() + "개 / 상점 검사 " + ShopFeatureChecks.Run() + "개 통과");
    }
}

#endif
