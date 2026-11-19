using System.Collections.Generic;
using System;

// Consolidated from InventoryModel.Warehouses.cs
namespace GeneralStore.Inventory
{
    public sealed partial class InventoryModel
    {
        public const int WarehouseColumns=8,WarehouseRows=5,WarehouseCells=40;
        public static bool IsWarehouse(string id){return id=="warehouse"||(id!=null&&id.StartsWith("warehouse_",StringComparison.Ordinal));}
        public List<string> WarehouseIds(){var result=new List<string>();foreach(var c in state.containers)if(IsWarehouse(c.containerId))result.Add(c.containerId);return result;}
        // 예전 버튼/외부 코드가 호출해도 크기가 바뀌지 않습니다.
        public bool TryExpandWarehouse(string id,out string error){error="상자 크기 확장은 제거되었습니다. 8×5 상자를 추가하세요.";return false;}
        public bool TryCreateWarehouse(int initialRows,out string id,out string error)
        {
            id="";error="";if(initialRows!=WarehouseRows){error="상자는 8×5 고정 크기입니다.";return false;}
            if(WarehouseIds().Count>=30||state.containers.Count>=64){error="상자 개수 한도입니다.";return false;}
            int number=2;while(Find(state,"warehouse_"+number)!=null)number++;
            id="warehouse_"+number;var trial=state.Copy();trial.containers.Add(new ContainerRecord{containerId=id,columns=WarehouseColumns,rows=WarehouseRows});Commit(trial);return true;
        }
    }
}

// Consolidated from StorageUpgradeQuote.cs
namespace GeneralStore.Inventory
{
    // 이름은 기존 코드 연결 호환용입니다. 견적/실행 모두 '고정 상자 추가'만 지원합니다.
    public sealed class StorageUpgradeQuote
    {public string targetId;public bool addsBox=true,allowed;public int beforeCells,afterCells=40,addedCells=40,freeBefore,freeAfter=40;public long cost,goldAfter;public string error;}
    public sealed partial class InventoryModel
    {
        public StorageUpgradeQuote QuoteWarehousePurchase(long cost)
        {
            var c=Find(state,StorageUpgradeTarget());int used=0;foreach(var item in c.items)used+=Catalog.Shape(item.itemId).CellCount;
            string error=cost<0?"비용 설정 오류입니다.":WarehouseIds().Count>=30||state.containers.Count>=64?"상자 개수 한도입니다.":state.gold<cost?"소지금이 부족합니다.":"";
            return new StorageUpgradeQuote{targetId=c.containerId,beforeCells=WarehouseCells,freeBefore=WarehouseCells-used,cost=cost,goldAfter=cost<0?state.gold:state.gold-cost,allowed=error.Length==0,error=error};
        }
        public bool TryPurchaseWarehouse(long cost,out string selected,out string error)
        {
            selected="";var q=QuoteWarehousePurchase(cost);error=q.error;if(!q.allowed)return false;
            int n=2;while(Find(state,"warehouse_"+n)!=null)n++;selected="warehouse_"+n;
            var trial=state.Copy();trial.gold-=cost;trial.containers.Add(new ContainerRecord{containerId=selected,columns=WarehouseColumns,rows=WarehouseRows});Commit(trial);return true;
        }
        // 이전 호출부와의 컴파일 호환. expansionCost는 사용하지 않으며 오직 상자를 삽니다.
        public StorageUpgradeQuote QuoteStorageUpgrade(long expansionCost,long boxCost,int initialRows=5){return QuoteWarehousePurchase(boxCost);}
        public bool TryPurchaseStorageUpgrade(long expansionCost,long boxCost,out string selected,out string error,int initialRows=5){return TryPurchaseWarehouse(boxCost,out selected,out error);}
        public System.Collections.Generic.List<string> AvailableFurnitureTypes()
        {
            var result=new System.Collections.Generic.List<string>();if(Displays==null)return result;
            foreach(string id in Displays.TypeIds()){var t=Displays.Type(id);if(t.maximumOwned==0||OwnedDisplayId(id,t.maximumOwned-1)==null)result.Add(id);}return result;
        }
    }
}
