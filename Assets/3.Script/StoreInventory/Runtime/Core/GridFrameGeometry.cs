using System;
namespace GeneralStore.Inventory
{
    public struct GridFrame {public float width,height,origin,tileSize;}
    public static class GridFrameGeometry
    {
        // logical cell pitch를 유지하고 타일의 안팎 여백을 같은 gap으로 맞춥니다.
        public static GridFrame Calculate(int columns,int rows,float cell,float gap)
        {
            if(columns<1||rows<1||cell<=0||gap<0||gap>=cell)throw new ArgumentException("격자 프레임 수치 오류입니다.");
            return new GridFrame{width=columns*cell+gap,height=rows*cell+gap,origin=gap*.5f,tileSize=cell-gap};
        }
    }
}
