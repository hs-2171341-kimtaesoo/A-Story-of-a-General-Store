using System;
using System.Collections.Generic;

namespace GeneralStore.Inventory
{
    // Unity의 Vector2Int 대신 순수 C# 좌표를 사용합니다. JSON과 외부 모델 테스트에서 함께 사용합니다.
    [Serializable] public struct GridCell
    {
        public int x, y;
        public GridCell(int x, int y) { this.x = x; this.y = y; }
    }

    // Every item occupies its entire width x height rectangle. Sprite transparency does not free cells.
    public sealed class ItemFootprint
    {
        readonly int baseWidth,baseHeight;
        readonly GridCell[][] rectangles=new GridCell[2][];
        public int CellCount{get{return baseWidth*baseHeight;}}
        public ItemFootprint(int width,int height)
        {
            if(width<1||width>64||height<1||height>64)throw new ArgumentException("물품 크기는 가로/세로1~64입니다.");
            baseWidth=width;baseHeight=height;
        }
        static void CheckRotation(int rotation){if(rotation<0||rotation>3)throw new ArgumentOutOfRangeException("rotation","회전은0~3입니다.");}
        public int Width(int rotation){CheckRotation(rotation);return rotation%2==0?baseWidth:baseHeight;}
        public int Height(int rotation){CheckRotation(rotation);return rotation%2==0?baseHeight:baseWidth;}
        public GridCell[] Cells(int rotation){return (GridCell[])InternalCells(rotation).Clone();}
        internal GridCell[] InternalCells(int rotation)
        {
            CheckRotation(rotation);int index=rotation%2;
            if(rectangles[index]==null)
            {
                int width=Width(rotation),height=Height(rotation);var cells=new GridCell[CellCount];
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)cells[y*width+x]=new GridCell(x,y);
                rectangles[index]=cells;
            }
            return rectangles[index];
        }
        public bool Contains(int rotation,int x,int y){CheckRotation(rotation);return x>=0&&y>=0&&x<(rotation%2==0?baseWidth:baseHeight)&&y<(rotation%2==0?baseHeight:baseWidth);}
    }
    public enum PlacementCellState { Free, Occupied, Outside, Blocked }
    public struct PlacementCell
    {
        // 안전한 범위 검사 전에 정수 덧셈이 넘치지 않도록 미리보기 좌표는 long으로 계산합니다.
        public long x, y;
        public PlacementCellState state;
    }
    public sealed class PlacementPreview
    {
        public bool allowed;
        public string error = "";
        public List<PlacementCell> cells = new List<PlacementCell>();
    }
}
