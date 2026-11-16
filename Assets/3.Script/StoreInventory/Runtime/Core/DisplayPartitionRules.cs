using System;
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
        {ValidateBoundaries(definition.verticalDividers,columns);ValidateBoundaries(definition.horizontalDividers,rows);}
        public static bool Fits(DisplayTypeDefinition definition,GridCell[] footprint,int x,int y)
        {
            if(definition==null||footprint==null||footprint.Length==0)return false;
            // 불규칙 모양도 실제 점유 칸 전체가 한 구역에 있어야 합니다. 빈 부분으로 벽을 감싸는 배치도 막습니다.
            long left=long.MaxValue,right=long.MinValue,top=long.MaxValue,bottom=long.MinValue;
            foreach(var cell in footprint)
            {long px=(long)x+cell.x,py=(long)y+cell.y;left=Math.Min(left,px);right=Math.Max(right,px);top=Math.Min(top,py);bottom=Math.Max(bottom,py);}
            if(definition.verticalDividers!=null)foreach(int boundary in definition.verticalDividers)if(left<boundary&&right>=boundary)return false;
            if(definition.horizontalDividers!=null)foreach(int boundary in definition.horizontalDividers)if(top<boundary&&bottom>=boundary)return false;
            return true;
        }
    }
}
