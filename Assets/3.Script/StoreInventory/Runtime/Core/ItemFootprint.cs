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

    // '바깥 사각형 크기'와 '실제 점유 칸'은 서로 다릅니다.
    // 예: 3x3 ㄱ자는 바깥 사각형이 9칸이어도 점유 칸은 5개일 수 있습니다.
    public sealed class ItemFootprint
    {
        readonly int baseWidth, baseHeight;
        readonly GridCell[][] directions = new GridCell[4][];
        public int CellCount { get { return directions[0].Length; } }
        public ItemFootprint(int width, int height, GridCell[] occupiedCells)
        {
            if (width < 1 || width > 64 || height < 1 || height > 64)
                throw new ArgumentException("모양 크기는 1~64여야 합니다.");
            baseWidth = width; baseHeight = height;
            var cells = new List<GridCell>();
            if (occupiedCells == null || occupiedCells.Length == 0)
            {
                // 기존 직사각형 JSON과 호환됩니다. 별도 점유 목록이 없으면 전체를 채웁니다.
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) cells.Add(new GridCell(x, y));
            }
            else cells.AddRange(occupiedCells);
            var keys = new HashSet<int>();
            int minX = width, minY = height, maxX = -1, maxY = -1;
            foreach (GridCell cell in cells)
            {
                if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height || !keys.Add(cell.y * width + cell.x))
                    throw new ArgumentException("점유 좌표가 중복되었거나 크기 범위를 벗어났습니다.");
                minX = Math.Min(minX, cell.x); minY = Math.Min(minY, cell.y);
                maxX = Math.Max(maxX, cell.x); maxY = Math.Max(maxY, cell.y);
            }
            // 바깥쪽에 빈 여백이 있으면 기준점/회전이 혼란스러우므로 잘못된 정의로 취급합니다.
            // (0,0) 자체가 점유될 필요는 없지만 최소 x/y는 각각 0이어야 합니다.
            if (minX != 0 || minY != 0 || maxX != width - 1 || maxY != height - 1)
                throw new ArgumentException("width/height는 점유 모양의 실제 바깥 크기에 맞춰 주세요.");
            directions[0] = cells.ToArray();
            for (int rotation = 1; rotation < 4; rotation++)
            {
                int previousHeight = Height(rotation - 1);
                GridCell[] previous = directions[rotation - 1];
                directions[rotation] = new GridCell[previous.Length];
                for (int i = 0; i < previous.Length; i++)
                    // 화면 좌표는 아래쪽이 +y입니다. 시계 방향 90도 회전 공식입니다.
                    directions[rotation][i] = new GridCell(previousHeight - 1 - previous[i].y, previous[i].x);
            }
        }
        static void CheckRotation(int rotation)
        { if (rotation < 0 || rotation > 3) throw new ArgumentOutOfRangeException("rotation", "회전은 0~3입니다."); }
        public int Width(int rotation) { CheckRotation(rotation); return rotation % 2 == 0 ? baseWidth : baseHeight; }
        public int Height(int rotation) { CheckRotation(rotation); return rotation % 2 == 0 ? baseHeight : baseWidth; }
        public GridCell[] Cells(int rotation)
        { CheckRotation(rotation); return (GridCell[])directions[rotation].Clone(); }
        internal GridCell[] InternalCells(int rotation) { CheckRotation(rotation); return directions[rotation]; }
        public bool Contains(int rotation, int x, int y)
        {
            CheckRotation(rotation);
            foreach (GridCell cell in directions[rotation]) if (cell.x == x && cell.y == y) return true;
            return false;
        }
    }
    public enum PlacementCellState { Free, Occupied, Outside }
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
