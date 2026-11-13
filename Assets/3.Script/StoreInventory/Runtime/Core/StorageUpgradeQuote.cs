using System;
namespace GeneralStore.Inventory
{
    public sealed class StorageUpgradeQuote
    {
        public string targetId;public bool addsBox,allowed;public int beforeCells,afterCells,addedCells,freeBefore,freeAfter;
        public long cost,goldAfter;public string error;
    }
    public sealed partial class InventoryModel
    {
        public StorageUpgradeQuote QuoteStorageUpgrade(long expansionCost,long boxCost,int initialRows=5)
        {
            var c=Find(state,StorageUpgradeTarget());int used=0;foreach(var item in c.items)used+=Catalog.Shape(item.itemId).CellCount;
            bool add=c.rows>=10;int before=c.rows*8,after=add?initialRows*8:Math.Min(10,c.rows+2)*8;
            long cost=add?boxCost:expansionCost;string error="";
            if(expansionCost<0||boxCost<0||initialRows<1||initialRows>10)error="업그레이드 설정 오류입니다.";
            else if(add&&(WarehouseIds().Count>=30||state.containers.Count>=64))error="상자 개수 한도입니다.";
            else if(state.gold<cost)error="소지금이 부족합니다.";
            return new StorageUpgradeQuote{targetId=c.containerId,addsBox=add,beforeCells=before,afterCells=after,addedCells=add?after:after-before,
                freeBefore=before-used,freeAfter=add?after:after-used,cost=cost,goldAfter=state.gold-cost,allowed=error.Length==0,error=error};
        }
        // 비용 차감과 공간 변경을 하나의 상태 복사본에 적용해 한 번만 확정합니다.
        public bool TryPurchaseStorageUpgrade(long expansionCost,long boxCost,out string selected,out string error,int initialRows=5)
        {
            var q=QuoteStorageUpgrade(expansionCost,boxCost,initialRows);selected=q.targetId;error=q.error;if(!q.allowed)return false;
            var trial=state.Copy();trial.gold-=q.cost;
            if(q.addsBox)
            {
                int n=2;while(Find(trial,"warehouse_"+n)!=null)n++;selected="warehouse_"+n;
                trial.containers.Add(new ContainerRecord{containerId=selected,columns=8,rows=initialRows});
            }
            else Find(trial,selected).rows=q.afterCells/8;
            Commit(trial);return true;
        }
        public System.Collections.Generic.List<string> AvailableFurnitureTypes()
        {
            var result=new System.Collections.Generic.List<string>();
            if(Displays==null)return result;
            foreach(string id in Displays.TypeIds())
            {var t=Displays.Type(id);if(t.maximumOwned==0||OwnedDisplayId(id,t.maximumOwned-1)==null)result.Add(id);}
            return result;
        }
    }
}
