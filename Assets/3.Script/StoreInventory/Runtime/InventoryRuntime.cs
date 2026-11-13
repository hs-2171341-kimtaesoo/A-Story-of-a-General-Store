using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace GeneralStore.Inventory
{
    // JSON 로드와 플레이 상태의 생명주기만 담당합니다. 화면이나 손님 AI를 처리하지 않습니다.
    public sealed class InventoryRuntime : MonoBehaviour
    {
        static readonly HashSet<InventoryRuntime> activeRuntimes=new HashSet<InventoryRuntime>();
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            activeRuntimes.Add(this);
            if(activeRuntimes.Count>1)Debug.LogWarning("InventoryRuntime가 여러 개 실행 중입니다. 이전 데모의 Data/Canvas 또는 중복 로드된 씬을 확인하고 하나만 켠 뒤 재생하세요.",this);
        }
        void OnDisable(){activeRuntimes.Remove(this);}
        void OnDestroy(){activeRuntimes.Remove(this);}
        public TextAsset itemJson,customerJson,supplierJson,displayJson;
        public ShopClockSettings clockSettings=new ShopClockSettings();
        public const int WarehouseColumns = 8;
        [Range(1, 10)] public int warehouseRows = 5;
        public bool addDemoItems = true;
        public long initialGold = 1000;
        public ShopClock Clock { get; private set; }
        public CustomerCatalog Customers { get; private set; }
        public event Action Loaded;
        public ItemCatalog Catalog { get; private set; }
        public InventoryModel Model { get; private set; }
        public RecommendationSession Sales { get; private set; }
        public string SavePath { get { return Path.Combine(Application.persistentDataPath, "store-shop-menu-v10.json"); } }
        void Awake() { if(GetComponent<StorePerformanceSettings>()==null)gameObject.AddComponent<StorePerformanceSettings>(); string error; if (!TryInitialize(out error)) Debug.LogError(error, this); }
        public bool TryInitialize(out string error)
        {
            error = ""; if (Model != null) return true;
            try
            {
                TextAsset file = itemJson != null ? itemJson : Resources.Load<TextAsset>("StoreInventory/items");
                if (file == null) throw new InvalidOperationException("Resources/StoreInventory/items.json이 없습니다.");
                var catalog = new ItemCatalog(JsonUtility.FromJson<ItemDatabase>(file.text));
                var save = new InventorySave { gold = initialGold };
                save.containers.Add(new ContainerRecord { containerId = "warehouse", columns = WarehouseColumns, rows = warehouseRows });
                // 종류/설치 위치가 정한 고정 크기만 생성합니다. 화면의 탭 수와 독립적입니다.
                var displayFile=displayJson!=null?displayJson:Resources.Load<TextAsset>("StoreInventory/display_fixtures");
                if(displayFile==null)throw new InvalidOperationException("display_fixtures.json이 없습니다.");
                var displays=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(displayFile.text));displays.AddInitialContainers(save);
                var supplierFile=supplierJson!=null?supplierJson:Resources.Load<TextAsset>("StoreInventory/suppliers");
                if(supplierFile==null)throw new InvalidOperationException("suppliers.json이 없습니다.");
                var supplierCatalog=new SupplierCatalog(JsonUtility.FromJson<SupplierDatabase>(supplierFile.text),catalog);
                var model = new InventoryModel(catalog, save,supplierCatalog,displays);
                if(!model.RefreshSuppliersForDay(1,out error))throw new InvalidOperationException(error);
                if (addDemoItems)
                {
                    // 40칸 데모에는 큰 모양부터 넣어 공간 부족 경고 없이 모든 유형을 확인합니다.
                    Seed(model, "sample_lframe", "common", 1, 50); Seed(model, "sample_cframe", "common", 1, 70);
                    Seed(model, "sample_bag", "common", 1, 150);Seed(model, "sample_rope", "common", 1, 40);
                    Seed(model, "sample_sword", "common", 1, 90);Seed(model, "C001", "common", 1, 80);
                    Seed(model, "C001", "superior", 1, 110);Seed(model, "E001", "none", 1, 0);
                }
                var customerFile=customerJson!=null?customerJson:Resources.Load<TextAsset>("StoreInventory/customers");
                if(customerFile==null)throw new InvalidOperationException("Resources/StoreInventory/customers.json이 없습니다.");
                var customers=new CustomerCatalog(JsonUtility.FromJson<CustomerDatabase>(customerFile.text));
                var clock=new ShopClock(clockSettings,new ShopClockState());
                Catalog = catalog; Model = model; Sales = new RecommendationSession(model);Customers=customers;Clock=clock;return true;
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
                AtomicSaveFile.Write(SavePath, JsonUtility.ToJson(new ShopWorldSave{inventory=Model.Snapshot(),clock=Clock.Snapshot()}, true)); return true;
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
                ShopWorldSave world=JsonUtility.FromJson<ShopWorldSave>(File.ReadAllText(SavePath));
                if(world==null||world.format!="general-store-world"||world.version!=4){error="월드 저장 형식이 아닙니다.";return false;}
                Clock.Validate(world.clock);InventorySave save=world.inventory;
                if(save!=null&&save.lastSettledDay>world.clock.day){error="날짜/정산 정보가 맞지 않습니다.";return false;}
                // UI가 사용하는 보관함/진열장이 없는 저장 파일은 교체 전에 거절합니다.
                if (save == null || save.containers == null
                    || !save.containers.Exists(c => c != null && c.containerId == "warehouse")
                    || save.displays==null || save.displays.Count==0)
                { error = "보관함/연결 공간이 없는 저장 파일입니다."; return false; }
                foreach(var vendor in save.suppliers)
                    if(Model.Suppliers.Find(vendor.supplierId)==null||vendor.cycle>Model.Suppliers.Cycle(vendor.supplierId,world.clock.day))
                    {error="거래처의 저장 날짜가 잘못되었습니다.";return false;}
                bool restored = Model.TryRestore(save, out error);
                if (restored) {Clock.Restore(world.clock);Model.RefreshSuppliersForDay(world.clock.day,out error);Sales.Leave();if(Loaded!=null)Loaded();}
                return restored;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
    }
}
