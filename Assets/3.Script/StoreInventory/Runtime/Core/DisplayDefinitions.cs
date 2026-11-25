using System.Collections.Generic;
using System;

// DisplayDefinitions.cs
namespace GeneralStore.Inventory
{
    [Serializable] public sealed class DisplayDatabase {public DisplayTypeDefinition[] types;public DisplayLocationDefinition[] locations;public string[] initialTypes;}
    [Serializable] public sealed class DisplayTypeDefinition
    {
        public string id,title;public int columns,rows;public long purchasePrice;public int maximumOwned; // 0이면 무제한. 배경 가구 개수와 맞추세요.
        // 비어 있으면 모든 종류를 허용합니다. 예: 무기 거치대는 item.type=weapon만 허용.
        public string[] allowedItemTypes;
        // 0부터 세는 칸 경계. 예:2는 1행과2행 사이(사람 기준2번째와3번째). 전체 폭/높이의 벽입니다.
        public int[] horizontalDividers,verticalDividers;
        public int[] lowerVerticalDividers;public int lowerPartitionStartRow;
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
                if(t==null||string.IsNullOrWhiteSpace(t.id)||types.ContainsKey(t.id)||t.maximumOwned<0||t.maximumOwned>30||t.purchasePrice<0||t.columns<1||t.columns>12||t.rows<1||t.rows>32)throw new ArgumentException("진열 종류 크기/ID 오류입니다.");
                DisplayPartitionRules.Validate(t);
                types.Add(t.id,t);typeOrder.Add(t.id);
            }
            foreach(var l in database.locations)
            {
                if(l==null||string.IsNullOrWhiteSpace(l.id)||locations.ContainsKey(l.id)||l.allowedTypeIds==null||l.allowedTypeIds.Length==0
                    ||l.columnsOverride<0||l.columnsOverride>12||l.rowsOverride<0||l.rowsOverride>32
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
        public void AddInitialContainers(InventorySave save,bool allTypesForTest=false,string[] initialTypeOverride=null)
        {
            var initial=initialTypeOverride??(allTypesForTest?typeOrder.ToArray():initialTypes);
            if(initial!=null)foreach(string type in initial)
            {
                string key="display_owned_"+(save.displays.Count+1);var t=Type(type);if(t==null)throw new ArgumentException("초기 진열 종류가 없습니다: "+type);
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

// DisplayPartitionRules.cs
namespace GeneralStore.Inventory
{
    // 칸은 그대로 두고 칸 '사이'의 경계만 막습니다. 벽 자체가 재고 칸을 소비하지 않습니다.
    // 현재는 선반 전체를 가로/세로로 나누는 벽입니다. 위치/칸수 변경은 JSON에서 합니다.
    public static class DisplayPartitionRules
    {
        static void ValidateBoundaries(int[] values,int limit)
        {
            if(values==null)return;
            if(values.Length>=limit)throw new ArgumentException("칸막이 개수가 너무 많습니다.");
            for(int n=0;n<values.Length;n++)
            {
                if(values[n]<=0||values[n]>=limit)throw new ArgumentException("칸막이는 격자 안쪽 경계에만 지정하세요.");
                for(int k=0;k<n;k++)if(values[k]==values[n])throw new ArgumentException("중복 칸막이입니다.");
            }
        }
        public static void Validate(DisplayTypeDefinition definition){if(definition==null)throw new ArgumentNullException("definition");ValidateSize(definition,definition.columns,definition.rows);}
        public static void ValidateSize(DisplayTypeDefinition definition,int columns,int rows)
        {
            ValidateBoundaries(definition.verticalDividers,columns);ValidateBoundaries(definition.horizontalDividers,rows);ValidateBoundaries(definition.lowerVerticalDividers,columns);
            if(definition.lowerVerticalDividers!=null&&definition.lowerVerticalDividers.Length>0&&(definition.lowerPartitionStartRow<1||definition.lowerPartitionStartRow>=rows))throw new ArgumentException("하부 칸막이 시작 행 오류입니다.");
        }
        public static bool Fits(DisplayTypeDefinition definition,GridCell[] footprint,int x,int y)
        {
            if(definition==null||footprint==null||footprint.Length==0)return false;
            // 물품의 직사각형 전체가 한 구역 안에 있어야 합니다.
            long left=long.MaxValue,right=long.MinValue,top=long.MaxValue,bottom=long.MinValue;
            foreach(var cell in footprint)
            {long px=(long)x+cell.x,py=(long)y+cell.y;left=Math.Min(left,px);right=Math.Max(right,px);top=Math.Min(top,py);bottom=Math.Max(bottom,py);}
            if(definition.verticalDividers!=null)foreach(int boundary in definition.verticalDividers)if(left<boundary&&right>=boundary)return false;
            if(definition.horizontalDividers!=null)foreach(int boundary in definition.horizontalDividers)if(top<boundary&&bottom>=boundary)return false;
            if(bottom>=definition.lowerPartitionStartRow&&definition.lowerVerticalDividers!=null)foreach(int boundary in definition.lowerVerticalDividers)if(left<boundary&&right>=boundary)return false;
            return true;
        }
    }
}

// DisplayWindowArtDefinitions.cs
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class DisplayWindowArtDatabase{public DisplayWindowArtDefinition[] entries;}
    [Serializable]public sealed class WindowArtRect{public float x,y,width,height;}
    [Serializable]public sealed class WindowArtSection{public int column,row,columns,rows;public WindowArtRect rect;}
    [Serializable]public sealed class WindowArtFloor
    {public string spriteKey;public float sourceWidth,sourceHeight;public WindowArtRect art;public WindowArtSection[] sections;}
    [Serializable]public sealed class WindowArtFrameSlice
    {public WindowArtRect rect;public float y;}
    [Serializable]public sealed class DisplayWindowArtDefinition
    {
        public string typeId,spriteKey;public float sourceWidth,sourceHeight,displayHeight;
        public WindowArtSection[] sections;public WindowArtRect details,icon,text,close;
        public bool scrolling;public WindowArtRect viewport;public WindowArtFloor[] floors;
        public float layoutHeight;public WindowArtFrameSlice[] frameSlices;
        public float LayoutHeight{get{return layoutHeight>0?layoutHeight:sourceHeight;}}
    }
    public static class DisplayWindowArtRules
    {
        static bool Positive(float n){return n>0&&!float.IsNaN(n)&&!float.IsInfinity(n);}
        static void Rect(WindowArtRect r,float w,float h)
        {if(r==null||!Positive(r.width)||!Positive(r.height)||float.IsNaN(r.x)||float.IsNaN(r.y)||r.x<0||r.y<0||r.x+r.width>w||r.y+r.height>h)throw new ArgumentException("창 Sprite 영역 좌표 오류입니다.");}
        public static IEnumerable<WindowArtSection> Sections(DisplayWindowArtDefinition art)
        {
            if(art.scrolling){if(art.floors!=null)foreach(var floor in art.floors)if(floor?.sections!=null)foreach(var section in floor.sections)yield return section;}
            else if(art.sections!=null)foreach(var section in art.sections)yield return section;
        }
        static void ValidateSections(WindowArtSection[] sections,DisplayTypeDefinition type,float width,float height,bool[] covered,WindowArtRect crop=null)
        {
            if(sections==null||sections.Length<1||sections.Length>8)throw new ArgumentException("진열 부분 격자 누락");
            foreach(var section in sections)
            {
                if(section==null||section.column<0||section.row<0||section.columns<1||section.rows<1||section.column+section.columns>type.columns||section.row+section.rows>type.rows)throw new ArgumentException("부분 격자 범위 오류입니다.");
                Rect(section.rect,width,height);
                if(crop!=null&&(section.rect.x<crop.x||section.rect.y<crop.y||section.rect.x+section.rect.width>crop.x+crop.width||section.rect.y+section.rect.height>crop.y+crop.height))throw new ArgumentException("층 이미지 밖의 격자");
                for(int y=section.row;y<section.row+section.rows;y++)for(int x=section.column;x<section.column+section.columns;x++){int k=y*type.columns+x;if(covered[k])throw new ArgumentException("부분 격자 중복");covered[k]=true;}
                if(!DisplayPartitionRules.Fits(type,new[]{new GridCell(0,0),new GridCell(section.columns-1,section.rows-1)},section.column,section.row))throw new ArgumentException("부분 격자가 모델 칸막이를 가로지릅니다.");
            }
            for(int a=0;a<sections.Length;a++)for(int b=a+1;b<sections.Length;b++)
            {var x=sections[a].rect;var y=sections[b].rect;if(x.x<y.x+y.width&&x.x+x.width>y.x&&x.y<y.y+y.height&&x.y+x.height>y.y)throw new ArgumentException("화면 격자 영역 중복");}
        }
        public static void Validate(DisplayWindowArtDefinition art,DisplayTypeDefinition type)
        {
            if(art==null||type==null||art.typeId!=type.id||string.IsNullOrEmpty(art.spriteKey)||!Positive(art.sourceWidth)||!Positive(art.sourceHeight)||!Positive(art.displayHeight))throw new ArgumentException("창 Sprite 정의 오류입니다.");
            var covered=new bool[type.columns*type.rows];
            if(!Positive(art.LayoutHeight))throw new ArgumentException("창 배치 높이 오류");
            if(art.frameSlices!=null&&art.frameSlices.Length>0)
            {
                float end=0;
                foreach(var slice in art.frameSlices)
                {
                    if(slice==null||slice.y!=end)throw new ArgumentException("프레임 조각 누락/중복");
                    Rect(slice.rect,art.sourceWidth,art.sourceHeight);end+=slice.rect.height;
                }
                if(Math.Abs(end-art.LayoutHeight)>.01f)throw new ArgumentException("프레임 조각 높이 오류");
            }
            if(art.scrolling)
            {
                Rect(art.viewport,art.sourceWidth,art.LayoutHeight);
                if(art.floors==null||art.floors.Length<1||art.floors.Length>16)throw new ArgumentException("스크롤 층수 오류");
                float contentHeight=0;
                foreach(var floor in art.floors)
                {
                    if(floor==null||string.IsNullOrEmpty(floor.spriteKey)||!Positive(floor.sourceWidth)||!Positive(floor.sourceHeight))throw new ArgumentException("층 Sprite 누락");
                    Rect(floor.art,floor.sourceWidth,floor.sourceHeight);ValidateSections(floor.sections,type,floor.sourceWidth,floor.sourceHeight,covered,floor.art);contentHeight+=floor.art.height;
                }
                if(contentHeight<=art.viewport.height)throw new ArgumentException("스크롤할 추가 층이 없습니다.");
            }
            else ValidateSections(art.sections,type,art.sourceWidth,art.sourceHeight,covered);
            foreach(bool present in covered)if(!present)throw new ArgumentException("화면에서 빠진 진열 칸이 있습니다.");
        }
    }
}
