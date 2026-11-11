using System;
using System.IO;
using UnityEngine;

namespace GeneralStore.Inventory
{
    // JSON 로드와 플레이 상태의 생명주기만 담당합니다. 화면이나 손님 AI를 처리하지 않습니다.
    public sealed class InventoryRuntime : MonoBehaviour
    {
        public TextAsset itemJson;
        [Range(1, 32)] public int warehouseColumns = 10;
        [Range(1, 32)] public int warehouseRows = 8;
        public bool addDemoItems = true;
        public long initialGold = 1000;
        public ItemCatalog Catalog { get; private set; }
        public InventoryModel Model { get; private set; }
        public RecommendationSession Sales { get; private set; }
        public string SavePath { get { return Path.Combine(Application.persistentDataPath, "store-shop-layout-v3.json"); } }
        void Awake() { string error; if (!TryInitialize(out error)) Debug.LogError(error, this); }
        public bool TryInitialize(out string error)
        {
            error = ""; if (Model != null) return true;
            try
            {
                TextAsset file = itemJson != null ? itemJson : Resources.Load<TextAsset>("StoreInventory/items");
                if (file == null) throw new InvalidOperationException("Resources/StoreInventory/items.json이 없습니다.");
                var catalog = new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(file.text));
                var save = new InventorySave { gold = initialGold };
                save.containers.Add(new ContainerRecord { containerId = "warehouse", columns = warehouseColumns, rows = warehouseRows });
                // 진열장 두 개입니다. 진열 등록 가격은 InventorySave.listings에 따로 저장합니다.
                save.containers.Add(new ContainerRecord { containerId = "display_1", columns = 6, rows = 4 });
                save.containers.Add(new ContainerRecord { containerId = "display_2", columns = 6, rows = 4 });
                var model = new InventoryModel(catalog, save);
                if (addDemoItems)
                {
                    Seed(model, "C001", "common", 2, 80); Seed(model, "C001", "common", 1, 100);
                    Seed(model, "C001", "superior", 1, 110); Seed(model, "E001", "none", 2, 0);
                    Seed(model, "sample_rope", "common", 1, 40); Seed(model, "sample_sword", "common", 1, 90);
                    Seed(model, "sample_bag", "common", 1, 150);
                    Seed(model, "sample_lframe", "common", 1, 50); Seed(model, "sample_cframe", "common", 1, 70);
                }
                Catalog = catalog; Model = model; Sales = new RecommendationSession(model); return true;
            }
            catch (Exception e) { error = "인벤토리 초기화 실패: " + e.Message; return false; }
        }
        void Seed(InventoryModel model, string id, string quality, int amount, int cost)
        {
            if (model.Catalog.FindQuality(id, quality) == null) return;
            string error; if (!model.TryAdd("warehouse", id, quality, amount, cost, out error)) Debug.LogWarning(error, this);
        }
        public bool Save(out string error)
        {
            error = "";
            try
            {
                if (!TryInitialize(out error)) return false;
                File.WriteAllText(SavePath, JsonUtility.ToJson(Model.Snapshot(), true)); return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
        public bool Load(out string error)
        {
            error = "";
            try
            {
                if (!TryInitialize(out error)) return false;
                if (!File.Exists(SavePath)) { error = "저장 파일이 없습니다."; return false; }
                InventorySave save = JsonUtility.FromJson<InventorySave>(File.ReadAllText(SavePath));
                // UI가 사용하는 보관함/진열장이 없는 저장 파일은 교체 전에 거절합니다.
                if (save == null || save.containers == null
                    || !save.containers.Exists(c => c != null && c.containerId == "warehouse")
                    || !save.containers.Exists(c => c != null && c.containerId == "display_1"))
                { error = "보관함/연결 공간이 없는 저장 파일입니다."; return false; }
                if (!save.containers.Exists(c => c != null && c.containerId == "display_2"))
                { error = "두 번째 진열 공간이 없는 저장 파일입니다."; return false; }
                bool restored = Model.TryRestore(save, out error);
                if (restored) Sales.Leave();
                return restored;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
    }
}
