using System;
using System.Collections.Generic;
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
