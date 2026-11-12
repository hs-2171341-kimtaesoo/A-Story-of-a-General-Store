using System;
namespace GeneralStore.Inventory
{
    public struct IconFit
    {public float width,height,x,y;public IconFit(float w,float h,float x,float y){width=w;height=h;this.x=x;this.y=y;}}
    public static class IconFitGeometry
    {
        // Sprite 원본 종횡비를 보존하면서 보이는 메시 바깥 사각형의 중심을 프레임 중심에 맞춥니다.
        public static IconFit Fit(float sourceWidth,float sourceHeight,float minX,float minY,float maxX,float maxY,float frameWidth,float frameHeight,float padding)
        {
            if(sourceWidth<=0||sourceHeight<=0||maxX<=minX||maxY<=minY||frameWidth<=0||frameHeight<=0)throw new ArgumentException("아이콘 맞춤 크기 오류입니다.");
            float w=Math.Max(1,frameWidth-2*padding),h=Math.Max(1,frameHeight-2*padding);
            float scale=Math.Min(w/(maxX-minX),h/(maxY-minY));
            return new IconFit(sourceWidth*scale,sourceHeight*scale,(sourceWidth*.5f-(minX+maxX)*.5f)*scale,(sourceHeight*.5f-(minY+maxY)*.5f)*scale);
        }
    }
}
