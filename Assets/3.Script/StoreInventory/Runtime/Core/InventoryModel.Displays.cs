using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    public sealed partial class InventoryModel
    {
        public readonly DisplayCatalog Displays;
        public List<string> DisplayIds()
        {var list=new List<string>();foreach(var c in state.containers)if(IsDisplay(c.containerId))list.Add(c.containerId);return list;}
        public DisplayInstallation Installation(string id)
        {var d=state.displays.Find(v=>v.containerId==id);return d==null?null:d.Copy();}
        public string OwnedDisplayId(string typeId,int ownedIndex)
        {if(ownedIndex<0)return null;int n=0;foreach(var d in state.displays)if(d.typeId==typeId&&n++==ownedIndex)return d.containerId;return null;}
        public bool LocationOccupied(string location){return state.displays.Exists(d=>d.locationId==location);}
        bool AcceptDisplay(InventorySave save,string containerId,string itemId,out string error)
        {
            error="";if(!IsDisplay(containerId))return true;
            var d=save.displays.Find(v=>v.containerId==containerId);if(d==null)return true; // 일반 모델 테스트의 메타데이터 없는 격자 호환.
            var t=Displays==null?null:Displays.Type(d.typeId);var item=Catalog.Find(itemId);
            if(t==null||item==null){error="진열 종류 정보가 없습니다.";return false;}
            if(t.allowedItemTypes!=null&&t.allowedItemTypes.Length>0&&Array.IndexOf(t.allowedItemTypes,item.type)<0)
            {error=t.title+"에 놓을 수 없는 물품 종류입니다.";return false;}return true;
        }
        // 저장의 설치 메타데이터와 고정 크기를 대조합니다.
        // 정의를 바꿨다고 이미 물건이 든 진열장을 조용히 줄이거나 넓히지 않습니다.
        void ValidateDisplays(InventorySave save)
        {
            if(save.displays==null||save.displays.Count>30)throw new ArgumentException("진열 설치 저장 오류입니다.");
            var ids=new HashSet<string>();var locations=new HashSet<string>();var counts=new Dictionary<string,int>();
            foreach(var d in save.displays)
            {
                if(d==null||Displays==null||!ids.Add(d.containerId)||Displays.Type(d.typeId)==null||(!string.IsNullOrEmpty(d.locationId)&&(!locations.Add(d.locationId)||!Displays.Compatible(d.typeId,d.locationId))))throw new ArgumentException("중복/없는 진열 설치입니다.");
                int owned;counts.TryGetValue(d.typeId,out owned);counts[d.typeId]=++owned;
                if(Displays.Type(d.typeId).maximumOwned>0&&owned>Displays.Type(d.typeId).maximumOwned)throw new ArgumentException("배경의 최대 가구 개수를 넘었습니다.");
                var c=Find(save,d.containerId);
                if(c==null||!IsDisplay(c.containerId)||c.columns!=(string.IsNullOrEmpty(d.locationId)?Displays.Type(d.typeId).columns:Displays.Columns(d.typeId,d.locationId))||c.rows!=(string.IsNullOrEmpty(d.locationId)?Displays.Type(d.typeId).rows:Displays.Rows(d.typeId,d.locationId)))
                    throw new ArgumentException("설치 종류/위치의 고정 크기와 다릅니다.");
                foreach(var i in c.items){string error;if(!AcceptDisplay(save,c.containerId,i.itemId,out error))throw new ArgumentException(error);}
            }
            if(Displays!=null)foreach(var c in save.containers)if(IsDisplay(c.containerId)&&!ids.Contains(c.containerId))throw new ArgumentException("설치 기록이 없는 진열 공간입니다.");
        }
        public bool CanInstallDisplay(string typeId,string locationId,out string error)
        {
            error="";
            if(Displays==null||!Displays.Compatible(typeId,locationId)){error="그 종류를 설치할 수 없는 자리입니다.";return false;}
            if(LocationOccupied(locationId)){error="이미 사용 중인 설치 자리입니다.";return false;}
            if(state.displays.Count>=30||state.containers.Count>=64){error="전체 보관 공간 한도입니다.";return false;}return true;
        }
        public bool TryInstallDisplay(string typeId,string locationId,out string id,out string error)
        {
            id="";if(!CanInstallDisplay(typeId,locationId,out error))return false;
            id="display_"+locationId;
            if(Find(state,id)!=null){error="이미 사용 중인 공간 ID입니다.";id="";return false;}
            var trial=state.Copy();trial.containers.Add(new ContainerRecord{containerId=id,columns=Displays.Columns(typeId,locationId),rows=Displays.Rows(typeId,locationId)});
            trial.displays.Add(new DisplayInstallation{containerId=id,typeId=typeId,locationId=locationId});Commit(trial);return true;
        }
        public string DisplayTitle(string id)
        {
            var d=Installation(id);if(d==null||Displays==null)return id;
            return Displays.Type(d.typeId).title+" "+(DisplayIds().IndexOf(id)+1);
        }
        // 여기서만 품목/품질별로 묶습니다. 실제 items와 판매/예약은 계속 낱개 UID입니다.
        public DisplaySummary SummarizeDisplay(string id)
        {
            var c=Find(state,id);if(c==null||!IsDisplay(id))return null;
            var result=new DisplaySummary{containerId=id,title=DisplayTitle(id),columns=c.columns,rows=c.rows,itemCount=c.items.Count};
            var kinds=new HashSet<string>();var groups=new Dictionary<string,DisplayItemGroup>();
            foreach(var item in c.items)
            {
                result.usedCells+=Catalog.Shape(item.itemId).CellCount;kinds.Add(item.itemId);string key=item.itemId+"/"+item.quality;
                DisplayItemGroup group;if(!groups.TryGetValue(key,out group)){group=new DisplayItemGroup{itemId=item.itemId,quality=item.quality};groups.Add(key,group);result.groups.Add(group);}group.count++;
            }
            result.kindCount=kinds.Count;return result;
        }
        // 위치를 고르지 않습니다. 잔액 검사/차감/새 공간 추가가 한 번의 Commit으로 끝납니다.
        public bool CanBuyDisplay(string typeId,out string error)
        {
            error="";var t=Displays==null?null:Displays.Type(typeId);
            if(t==null){error="없는 가구 종류입니다.";return false;}
            if(t.maximumOwned>0&&OwnedDisplayId(typeId,t.maximumOwned-1)!=null){error="이 가구는 이미 구매했습니다.";return false;}
            if(state.displays.Count>=30||state.containers.Count>=64){error="진열장 개수 한도입니다.";return false;}
            if(state.gold<t.purchasePrice){error="소지금이 부족합니다.";return false;}return true;
        }
        public bool TryBuyDisplay(string typeId,out string id,out string error)
        {
            id="";if(!CanBuyDisplay(typeId,out error))return false;
            int number=1;while(Find(state,"display_owned_"+number)!=null)number++;
            id="display_owned_"+number;var t=Displays.Type(typeId);var trial=state.Copy();
            trial.gold-=t.purchasePrice;
            trial.containers.Add(new ContainerRecord{containerId=id,columns=t.columns,rows=t.rows});
            trial.displays.Add(new DisplayInstallation{containerId=id,typeId=typeId,locationId=""});Commit(trial);return true;
        }
        // 현재 열어 둔 상자와 무관하게 가장 최근 상자만 확장 대상입니다.
        public string StorageUpgradeTarget(){var ids=WarehouseIds();return ids[ids.Count-1];}
        public StorageUpgradeAction StorageAction(string ignoredId)
        {return Find(state,StorageUpgradeTarget()).rows<10?StorageUpgradeAction.Expand:StorageUpgradeAction.AddBox;}
        public bool TryUpgradeStorage(string ignoredId,int newRows,out string selectedId,out string error)
        {
            selectedId=StorageUpgradeTarget();error="";
            if(Find(state,selectedId).rows<10)return TryExpandWarehouse(selectedId,out error);
            return TryCreateWarehouse(newRows,out selectedId,out error);
        }
    }
}
