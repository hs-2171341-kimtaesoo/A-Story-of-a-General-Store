using System;
namespace GeneralStore.Inventory
{
    // Unity 오브젝트와 분리한 외형 계산입니다. 아이템 점유 칸은 바꾸지 않습니다.
    public struct DisplayWindowLayout
    {
        public float width,height,gridWidth,gridHeight;
        public static DisplayWindowLayout Calculate(int columns,int rows,float cellWidth,float cellHeight,float side,float top,float bottom,float minimum,float maximumWidth,float maximumHeight)
        {
            if(columns<1||rows<1||!Positive(cellWidth)||!Positive(cellHeight)||!Positive(maximumWidth)||!Positive(maximumHeight)||!Positive(minimum)||!Positive(side)||!Positive(top)||!Positive(bottom))
                throw new ArgumentException("진열창 외형 수치는 0보다 큰 유한값이어야 합니다.");
            float gw=Math.Min(columns*cellWidth,maximumWidth),gh=Math.Min(rows*cellHeight,maximumHeight);
            return new DisplayWindowLayout{gridWidth=gw,gridHeight=gh,width=Math.Max(minimum,gw+side*2+22),height=top+gh+22+bottom};
        }
        static bool Positive(float n){return n>0&&!float.IsNaN(n)&&!float.IsInfinity(n);}
    }
}
