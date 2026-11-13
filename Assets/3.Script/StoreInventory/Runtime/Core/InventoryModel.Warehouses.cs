using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    public sealed partial class InventoryModel
    {
        public static bool IsWarehouse(string id){return id=="warehouse"||(id!=null&&id.StartsWith("warehouse_",StringComparison.Ordinal));}
        public List<string> WarehouseIds()
        {var list=new List<string>();foreach(var c in state.containers)if(IsWarehouse(c.containerId))list.Add(c.containerId);return list;}
        public bool TryExpandWarehouse(string id,out string error)
        {
            error="";var c=Find(state,id);
            if(!IsWarehouse(id)||c==null){error="보관함이 없습니다.";return false;}
            if(c.rows>=10){error="80칸입니다. 추가 보관함을 만들어 주세요.";return false;}
            var trial=state.Copy();Find(trial,id).rows=Math.Min(10,c.rows+2);Commit(trial);return true;
        }
        public bool TryCreateWarehouse(int initialRows,out string id,out string error)
        {
            id="";error="";var ids=WarehouseIds();
            if(initialRows<1||initialRows>10||ids.Count<1||ids.Count>=30||state.containers.Count>=64)
            {error="보관함 크기/개수 한도 오류입니다.";return false;}
            if(Find(state,ids[ids.Count-1]).rows!=10){error="마지막 상자를80칸까지 확장한 뒤 새 보관함을 추가하세요.";return false;}
            int n=2;while(Find(state,"warehouse_"+n)!=null)n++;
            id="warehouse_"+n;var trial=state.Copy();trial.containers.Add(new ContainerRecord{containerId=id,columns=8,rows=initialRows});Commit(trial);return true;
        }
    }
}
