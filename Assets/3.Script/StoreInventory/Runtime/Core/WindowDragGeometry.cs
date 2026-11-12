using System;
namespace GeneralStore.Inventory
{
    public struct WindowPoint {public float x,y;public WindowPoint(float x,float y){this.x=x;this.y=y;}}
    public static class WindowDragGeometry
    {
        // 부모 왼쪽 위 원점: x는 오른쪽+, y는 아래쪽-. UI 배율이 반영된 로컬 좌표만 받습니다.
        public static WindowPoint Clamp(float x,float y,float windowWidth,float windowHeight,float boundsWidth,float boundsHeight)
        {
            if(float.IsNaN(x)||float.IsNaN(y)||float.IsInfinity(x)||float.IsInfinity(y)||windowWidth<=0||windowHeight<=0||boundsWidth<=0||boundsHeight<=0)
                throw new ArgumentException("창 이동 범위 오류입니다.");
            return new WindowPoint(Math.Max(0,Math.Min(Math.Max(0,boundsWidth-windowWidth),x)),Math.Max(-Math.Max(0,boundsHeight-windowHeight),Math.Min(0,y)));
        }
    }
}
