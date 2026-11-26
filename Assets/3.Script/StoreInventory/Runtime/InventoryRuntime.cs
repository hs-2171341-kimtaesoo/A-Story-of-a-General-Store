using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace GeneralStore.Inventory
{
    [Serializable] public sealed class InventoryInitialItem
    {public string containerId,itemId,quality;public int amount=1;}
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
        void OnDestroy(){activeRuntimes.Remove(this);if(Application.isPlaying&&activeRuntimes.Count==0){SpriteIconFit.ResetCache();QualityOutlineEffect.ReleaseSharedMaterial(false);}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetActiveRuntimes(){activeRuntimes.Clear();}
        public TextAsset itemJson,customerJson,supplierJson,displayJson,workshopJson;
        public InventorySpriteCatalog spriteCatalog;
        [UnityEngine.Serialization.FormerlySerializedAs("deliveryCellFee")]
        [Min(0)]public int storageCellFee=10;
        public int deliveryCellFee{get{return storageCellFee;}set{storageCellFee=value;}}
        public ShopClockSettings clockSettings=new ShopClockSettings();
        public const int WarehouseColumns = 8;
        public const int WarehouseRows = InventoryModel.WarehouseRows;
        public InventoryInitialItem[] initialItems;
        public string saveFileName="store-main-items208-v1.json";
        [Tooltip("Enable only for manual JSON inspection. Compact saves use less disk space and fewer encoding buffers.")]
        public bool prettyPrintSave;
        [Range(1,8)]public int initialWarehouseCount=2;
        public string[] initialFurnitureForDemo;
        public bool startAllFurnitureOwnedForTest; // 배치 확인용. 일반 게임에서는 끕니다.
        public long initialGold = 1000;
        public ShopClock Clock { get; private set; }
        [Tooltip("비워 두면 Customer Json을 사용합니다. 채우면 이 목록이 방문 손님 명단이 됩니다.")]
        public CustomerDefinition[] customerProfiles;
        public CustomerCatalog Customers { get; private set; }
        public event Action Loaded;
        public ItemCatalog Catalog { get; private set; }
        public InventoryModel Model { get; private set; }
        public RecommendationSession Sales { get; private set; }
        public string SavePath { get { return Path.Combine(Application.persistentDataPath,string.IsNullOrEmpty(saveFileName)?"store-main-items208-v1.json":saveFileName); } }
        void Awake() { if(GetComponent<StorePerformanceSettings>()==null)gameObject.AddComponent<StorePerformanceSettings>(); string error; if (!TryInitialize(out error)) Debug.LogError(error, this); }
        public bool TryInitialize(out string error)
        {
            error = ""; if (Model != null) return true;
            try
            {
                if(!string.IsNullOrEmpty(saveFileName)&&(saveFileName.IndexOfAny(Path.GetInvalidFileNameChars())>=0||saveFileName.Contains("/")||saveFileName.Contains("\\")||saveFileName=="."||saveFileName==".."))throw new InvalidOperationException("저장 파일 이름에는 폴더 경로를 넣지 마세요.");
                TextAsset file = itemJson;
                if (file == null) throw new InvalidOperationException("Item Json에 아이템 데이터 파일을 연결하세요.");
                var itemData=JsonUtility.FromJson<ItemDatabase>(file.text);
                var catalog = new ItemCatalog(itemData);
                var workshopCatalog=workshopJson==null?null:new WorkshopCatalog(JsonUtility.FromJson<WorkshopDatabase>(workshopJson.text),catalog);
                var save = new InventorySave { gold = initialGold };
                if(initialWarehouseCount<1||initialWarehouseCount>8)throw new InvalidOperationException("초기 상자는1~8개로 설정하세요.");
                save.containers.Add(new ContainerRecord { containerId = "warehouse", columns = WarehouseColumns, rows = WarehouseRows });
                for(int n=2;n<=initialWarehouseCount;n++)save.containers.Add(new ContainerRecord{containerId="warehouse_"+n,columns=WarehouseColumns,rows=WarehouseRows});
                // 종류/설치 위치가 정한 고정 크기만 생성합니다. 화면의 탭 수와 독립적입니다.
                var displayFile=displayJson;
                if(displayFile==null)throw new InvalidOperationException("display_fixtures.json이 없습니다.");
                var displays=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(displayFile.text));displays.AddInitialContainers(save,startAllFurnitureOwnedForTest,initialFurnitureForDemo);
                var supplierFile=supplierJson;
                if(supplierFile==null)throw new InvalidOperationException("suppliers.json이 없습니다.");
                var supplierCatalog=new SupplierCatalog(JsonUtility.FromJson<SupplierDatabase>(supplierFile.text),catalog);
                var model = new InventoryModel(catalog, save,supplierCatalog,displays,workshopCatalog);
                if(!model.RefreshSuppliersForDay(1,out error))throw new InvalidOperationException(error);
                if(initialItems!=null)foreach(var seed in initialItems)
                {
                    if(seed==null||!model.TryAdd(seed.containerId,seed.itemId,seed.quality,seed.amount,catalog.ReferencePrice(seed.itemId,seed.quality),out error))
                        throw new InvalidOperationException("초기 아이템 배치 오류: "+error);
                }
                var customerFile=customerJson;
                if(customerFile==null)throw new InvalidOperationException("Customer Json에 손님 데이터 파일을 연결하세요.");
                var customers=new CustomerCatalog(customerProfiles!=null&&customerProfiles.Length>0?new CustomerDatabase{customers=customerProfiles}:JsonUtility.FromJson<CustomerDatabase>(customerFile.text));
                var clock=new ShopClock(clockSettings,new ShopClockState());
                Catalog = catalog; Model = model; Sales = new RecommendationSession(model);Customers=customers;Clock=clock;Model.SetWorkshopTime(clock.Phase,clock.Day);return true;
            }
            catch (Exception e) { error = "인벤토리 초기화 실패: " + e.Message; return false; }
        }
        public bool Save(out string error)
        {
            error = "";
            try
            {
                if (!TryInitialize(out error)) return false;
                AtomicSaveFile.Write(SavePath, JsonUtility.ToJson(new ShopWorldSave{inventory=Model.Snapshot(),clock=Clock.Snapshot(),clockSettings=Clock.Settings}, prettyPrintSave)); return true;
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
                if(world==null||world.format!="general-store-world"||(world.version!=4&&world.version!=5)){error="월드 저장 형식이 아닙니다.";return false;}
                var previousClock=world.version==4?new ShopClockSettings{dawnMinutes=180,dayMinutes=540,nightMinutes=720}:world.clockSettings;
                if(previousClock==null){error="저장된 단계 길이가 없습니다.";return false;}
                world.clock=Clock.RemapSavedTime(world.clock,previousClock);InventorySave save=world.inventory;
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
                if (restored) {if(save.version<10)Debug.Log("직사각형 점유 전환 완료. 공간이 바뀐 물품은 상자/택배 보관함을 확인하세요.",this);Clock.Restore(world.clock);Model.SetWorkshopTime(Clock.Phase,Clock.Day);Model.RefreshSuppliersForDay(world.clock.day,out error);Sales.Leave();if(Loaded!=null)Loaded();}
                return restored;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
    }
}
