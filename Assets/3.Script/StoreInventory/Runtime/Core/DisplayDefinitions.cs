using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    [Serializable] public sealed class DisplayDatabase {public DisplayTypeDefinition[] types;public DisplayLocationDefinition[] locations;public string[] initialTypes;}
    [Serializable] public sealed class DisplayTypeDefinition
    {
        public string id,title;public int columns,rows;public long purchasePrice;public int maximumOwned; // 0이면 무제한. 배경 가구 개수와 맞추세요.
        // 비어 있으면 모든 종류를 허용합니다. 예: 무기 거치대는 item.type=weapon만 허용.
        public string[] allowedItemTypes;
    }
    [Serializable] public sealed class DisplayLocationDefinition
    {
        public string id,title,initialTypeId;public string[] allowedTypeIds;
        // 0이면 종류의 기본 크기. 지정하면 이 자리만의 고정 내부 크기로 설치합니다.
        public int columnsOverride,rowsOverride;
        public float mapX=.5f,mapY=.5f;
    }
    [Serializable] public sealed class DisplayInstallation
    {
        public string containerId,typeId,locationId;
        public DisplayInstallation Copy(){return (DisplayInstallation)MemberwiseClone();}
    }
    public sealed class DisplayCatalog
    {
        readonly Dictionary<string,DisplayTypeDefinition> types=new Dictionary<string,DisplayTypeDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string,DisplayLocationDefinition> locations=new Dictionary<string,DisplayLocationDefinition>(StringComparer.Ordinal);
        readonly string[] initialTypes;
        readonly List<string> typeOrder=new List<string>(),locationOrder=new List<string>();
        public DisplayCatalog(DisplayDatabase database)
        {
            if(database!=null&&database.locations==null)database.locations=new DisplayLocationDefinition[0];
            if(database==null||database.types==null||database.types.Length==0||database.types.Length>32||database.locations.Length>16)throw new ArgumentException("진열 종류/위치 JSON 오류입니다.");
            foreach(var t in database.types)
            {
                if(t==null||string.IsNullOrWhiteSpace(t.id)||types.ContainsKey(t.id)||t.maximumOwned<0||t.maximumOwned>30||t.purchasePrice<0||t.columns<1||t.columns>12||t.rows<1||t.rows>10)throw new ArgumentException("진열 종류 크기/ID 오류입니다.");
                types.Add(t.id,t);typeOrder.Add(t.id);
            }
            foreach(var l in database.locations)
            {
                if(l==null||string.IsNullOrWhiteSpace(l.id)||locations.ContainsKey(l.id)||l.allowedTypeIds==null||l.allowedTypeIds.Length==0
                    ||l.columnsOverride<0||l.columnsOverride>12||l.rowsOverride<0||l.rowsOverride>10
                    ||float.IsNaN(l.mapX)||float.IsNaN(l.mapY)||l.mapX<0||l.mapX>1||l.mapY<0||l.mapY>1)throw new ArgumentException("진열 위치 오류입니다.");
                foreach(string id in l.allowedTypeIds)if(!types.ContainsKey(id))throw new ArgumentException("설치 위치의 종류 ID 오류입니다.");
                if(!string.IsNullOrEmpty(l.initialTypeId)&&Array.IndexOf(l.allowedTypeIds,l.initialTypeId)<0)throw new ArgumentException("초기 진열 종류를 해당 자리에 설치할 수 없습니다.");
                locations.Add(l.id,l);locationOrder.Add(l.id);
            }
            initialTypes=database.initialTypes;
            if(initialTypes!=null)foreach(string id in initialTypes)if(Type(id)==null)throw new ArgumentException("초기 가구 종류가 없습니다.");
        }
        public List<string> TypeIds(){return new List<string>(typeOrder);}
        public List<string> LocationIds(){return new List<string>(locationOrder);}
        public DisplayTypeDefinition Type(string id){DisplayTypeDefinition t;return id!=null&&types.TryGetValue(id,out t)?t:null;}
        public DisplayLocationDefinition Location(string id){DisplayLocationDefinition l;return id!=null&&locations.TryGetValue(id,out l)?l:null;}
        public bool Compatible(string type,string location)
        {var l=Location(location);return Type(type)!=null&&l!=null&&Array.IndexOf(l.allowedTypeIds,type)>=0;}
        public int Columns(string type,string location){var l=Location(location);return l.columnsOverride>0?l.columnsOverride:Type(type).columns;}
        public int Rows(string type,string location){var l=Location(location);return l.rowsOverride>0?l.rowsOverride:Type(type).rows;}
        public void AddInitialContainers(InventorySave save)
        {
            if(initialTypes!=null)foreach(string type in initialTypes)
            {
                string key="display_owned_"+(save.displays.Count+1);var t=Type(type);
                save.containers.Add(new ContainerRecord{containerId=key,columns=t.columns,rows=t.rows});
                save.displays.Add(new DisplayInstallation{containerId=key,typeId=type,locationId=""});
            }
            foreach(string id in locationOrder)
            {
                var l=locations[id];if(string.IsNullOrEmpty(l.initialTypeId))continue;
                string containerId="display_"+id;
                save.containers.Add(new ContainerRecord{containerId=containerId,columns=Columns(l.initialTypeId,id),rows=Rows(l.initialTypeId,id)});
                save.displays.Add(new DisplayInstallation{containerId=containerId,typeId=l.initialTypeId,locationId=id});
            }
            if(save.displays.Count==0)throw new ArgumentException("데모에는 초기 진열 공간을 하나 이상 지정하세요.");
        }
    }
    public sealed class DisplayItemGroup {public string itemId,quality;public int count;}
    public sealed class DisplaySummary
    {public string containerId,title;public int columns,rows,usedCells,itemCount,kindCount;public List<DisplayItemGroup> groups=new List<DisplayItemGroup>();}
    public enum StorageUpgradeAction {Expand,AddBox}
}
