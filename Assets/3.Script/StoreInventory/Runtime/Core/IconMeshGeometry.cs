using System;
using System.Collections.Generic;

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
        public static List<IconQuad> Build(ItemFootprint shape, int rotation, bool hasSprite, float aspect)
        {
            if (hasSprite && (float.IsNaN(aspect) || float.IsInfinity(aspect) || aspect <= 0)) throw new ArgumentException("아이콘 비율 오류");
            float bw=shape.Width(0),bh=shape.Height(0),rw=shape.Width(rotation),rh=shape.Height(rotation);
            float imageW=bw,imageH=bh;
            if(hasSprite)
            {
                if(bw/bh>aspect) imageW=bh*aspect;
                else imageH=bw/aspect;
            }
            float offsetX=(bw-imageW)*.5f,offsetY=(bh-imageH)*.5f;
            float rotatedW=rotation%2==0?imageW:imageH,rotatedH=rotation%2==0?imageH:imageW;
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
