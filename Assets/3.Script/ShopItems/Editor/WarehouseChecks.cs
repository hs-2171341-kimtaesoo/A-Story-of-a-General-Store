using System;
using ShopItems;
using UnityEditor;
using UnityEngine;

public static class WarehouseChecks
{
    static void Check(bool passed, string rule)
    {
        if (!passed) throw new Exception("검증 실패: " + rule);
    }
    [MenuItem("Counter Shop/Create Warehouse Demo")]
    static void CreateDemo()
    {
        var go = new GameObject("Warehouse Demo");
        Undo.RegisterCreatedObjectUndo(go, "Create Warehouse Demo");
        go.AddComponent<ItemManager>();
        go.AddComponent<WarehouseDemo>();
        Selection.activeGameObject = go;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
    }
    [MenuItem("Counter Shop/Run Warehouse Checks")]
    static void Run()
    {
        var go = new GameObject("Temporary Warehouse Checks");
        try
        {
            var items = go.AddComponent<ItemManager>();
            Check(items.LoadItems(), "실제 JSON 로드");
            var warehouse = new WarehouseInventory(items, 2);
            string error;
            Check(warehouse.TryAdd("C001", "common", 10, 80, out error), "첫 입고");
            Check(warehouse.TryAdd("C001", "common", 2, 100, out error), "동일 품질 합산");
            Check(warehouse.UsedSlots == 1 && warehouse.GetQuantity("C001", "common") == 12, "한 칸 12개");
            Check(Math.Abs(warehouse.GetSlot(0).averagePurchasePrice - 1000d / 12) < .00001, "가중 평균 매입가");
            Check(warehouse.TryAdd("C001", "superior", 3, 90, out error), "다른 품질은 별도 칸");
            Check(warehouse.UsedSlots == 2, "품질별 두 칸");
            Check(!warehouse.TryAdd("E001", "None", 1, 0, out error), "가득 찬 창고의 새 종류 입고 거절");
            Check(warehouse.TryAdd("C001", "common", 1, 100, out error), "가득 차도 기존 스택 입고 가능");
            Check(!warehouse.TryRemove("C001", "common", 14, out error), "초과 출고 거절");
            Check(warehouse.GetQuantity("C001", "common") == 13, "실패한 출고는 재고 유지");
            Check(warehouse.TryRemove("C001", "superior", 3, out error) && warehouse.UsedSlots == 1, "전량 출고 시 빈칸");
            Check(warehouse.TryAdd("E001", "None", 5, 0, out error), "빈칸 재사용");
            Check(warehouse.GetSlot(1).itemId == "E001", "기존 칸 번호 유지");
            Check(warehouse.GetVisibleSlots("food").Count == 1, "종류 필터");
            Check(warehouse.TrySetSellingPrice(0, 150), "판매가 변경");
            string saved = warehouse.ToJson();
            var restored = new WarehouseInventory(items, 5);
            Check(restored.TryLoadJson(saved, out error), "저장 복원");
            Check(restored.Capacity == 2 && restored.GetSlot(0).sellingPrice == 150, "용량과 판매가 복원");
            Check(items.GetExplanation("C001", "common") == "상처를 빠르게 회복시킨다", "설명 조회");
            WarehouseSaveData invalid = JsonUtility.FromJson<WarehouseSaveData>(saved);
            invalid.stacks[1].itemId = "C001"; invalid.stacks[1].quality = "common";
            Check(!restored.TryLoadJson(JsonUtility.ToJson(invalid), out error), "중복 스택 저장 거절");
            Check(restored.GetSlot(1).itemId == "E001", "로드 실패 시 기존 데이터 유지");
            Debug.Log("Warehouse: 모든 규칙 검증 통과");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
