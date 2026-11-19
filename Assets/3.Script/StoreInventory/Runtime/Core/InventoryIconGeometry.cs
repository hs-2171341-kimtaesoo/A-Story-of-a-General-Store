using System.Collections.Generic;
using System;

// Consolidated from IconFitGeometry.cs
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

// Consolidated from IconMeshGeometry.cs

namespace GeneralStore.Inventory
{
    public struct IconPoint { public float x, y; public IconPoint(float x,float y) { this.x=x; this.y=y; } }
    public sealed class IconQuad
    {
        public float x0,y0,x1,y1;
        public IconPoint topLeft,topRight,bottomRight,bottomLeft;
    }
    // 실제 아이콘도 하나의 메시입니다. 점유 칸마다 사각형 정점만 추가하고 GameObject는 만들지 않습니다.
    // 좌표는 칸 단위, y는 아래쪽입니다. Unity 좌표/아틀라스 UV 변환은 Graphic이 담당합니다.
    public static class IconMeshGeometry
    {
        public static List<IconQuad> Build(ItemFootprint shape, int rotation, bool hasSprite, float aspect,float cellAspect=1)
        {
            if (hasSprite && (float.IsNaN(aspect) || float.IsInfinity(aspect) || aspect <= 0)) throw new ArgumentException("아이콘 비율 오류");
            float bw=shape.Width(0),bh=shape.Height(0),rw=shape.Width(rotation),rh=shape.Height(rotation);
            if(float.IsNaN(cellAspect)||float.IsInfinity(cellAspect)||cellAspect<=0)throw new ArgumentException("격자 비율 오류");
            // 회전 후의 실제 픽셀 비율을 먼저 맞춥니다. 세로로 긴 칸에서도 그림이 늘어나지 않습니다.
            float rotatedW=rw,rotatedH=rh;
            if(hasSprite)
            {
                float rotatedAspect=(rotation%2==0?aspect:1/aspect)/cellAspect;
                if(rw/rh>rotatedAspect)rotatedW=rh*rotatedAspect;
                else rotatedH=rw/rotatedAspect;
            }
            float imageW=rotation%2==0?rotatedW:rotatedH,imageH=rotation%2==0?rotatedH:rotatedW;
            float offsetX=(bw-imageW)*.5f,offsetY=(bh-imageH)*.5f;
            float left=(rw-rotatedW)*.5f,top=(rh-rotatedH)*.5f;
            var result=new List<IconQuad>();
            foreach(GridCell cell in shape.Cells(rotation))
            {
                float x0=Math.Max(cell.x,left),y0=Math.Max(cell.y,top);
                float x1=Math.Min(cell.x+1,left+rotatedW),y1=Math.Min(cell.y+1,top+rotatedH);
                if(x1-x0<.00001f || y1-y0<.00001f) continue;
                result.Add(new IconQuad { x0=x0,y0=y0,x1=x1,y1=y1,
                    topLeft=Uv(x0,y0,rotation,bw,bh,offsetX,offsetY,imageW,imageH),
                    topRight=Uv(x1,y0,rotation,bw,bh,offsetX,offsetY,imageW,imageH),
                    bottomRight=Uv(x1,y1,rotation,bw,bh,offsetX,offsetY,imageW,imageH),
                    bottomLeft=Uv(x0,y1,rotation,bw,bh,offsetX,offsetY,imageW,imageH) });
            }
            return result;
        }
        static IconPoint Uv(float x,float y,int rotation,float bw,float bh,float ox,float oy,float iw,float ih)
        {
            float bx,by;
            if(rotation==0) { bx=x;by=y; }
            else if(rotation==1) { bx=y;by=bh-x; }
            else if(rotation==2) { bx=bw-x;by=bh-y; }
            else { bx=bw-y;by=x; }
            return new IconPoint(Clamp((bx-ox)/iw),Clamp(1-(by-oy)/ih));
        }
        static float Clamp(float value) { return Math.Max(0,Math.Min(1,value)); }
    }
}

// Consolidated from DetailsFrameGeometry.cs
namespace GeneralStore.Inventory
{
    public struct DetailsFrameLayout {public float x,y,side,textLeft,textWidth;}
    public static class DetailsFrameGeometry
    {
        // 상세창 높이가100으로 줄어도 96칸을 고정하지 않고 안쪽에 맞는 정사각형으로 배치합니다.
        public static DetailsFrameLayout Calculate(float width,float height,float preferred,float padding,float gap)
        {
            if(width<=0||height<=0||preferred<=0||padding<0||gap<0)throw new ArgumentException("상세 아이콘 배치 크기 오류입니다.");
            float side=Math.Min(preferred,Math.Max(1,Math.Min(width,height)-2*padding));
            float left=padding+side+gap;
            return new DetailsFrameLayout{x=padding,y=(height-side)*.5f,side=side,textLeft=left,textWidth=Math.Max(1,width-left-padding)};
        }
    }
}

// Consolidated from GridFrameGeometry.cs
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
