using System;
namespace GeneralStore.Inventory
{
    public static class SceneLayerOrder
    {public const int Background=0,Furniture=10,Counter=20,UI=30;}
    [Serializable]public sealed class PackedHitMaskData{public int width,height;public string bits;}
    // 알파 픽셀은 제작할 때만 읽습니다. 실행 중에는 작은 비트 마스크를 조회합니다.
    public sealed class PackedHitMask
    {
        readonly byte[] bits;readonly int width,height;
        public PackedHitMask(PackedHitMaskData data)
        {
            if(data==null||data.width<1||data.height<1||data.width>512||data.height>512||string.IsNullOrEmpty(data.bits))throw new ArgumentException("카운터 입력 마스크 오류입니다.");
            width=data.width;height=data.height;bits=Convert.FromBase64String(data.bits);
            if(bits.Length!=(width*height+7)/8)throw new ArgumentException("카운터 입력 마스크 크기 오류입니다.");
        }
        public bool Contains(float x,float y)
        {
            if(float.IsNaN(x)||float.IsNaN(y)||x<0||y<0||x>=1||y>=1)return false;
            int index=(int)(y*height)*width+(int)(x*width);return (bits[index>>3]&(1<<(index&7)))!=0;
        }
    }
    public struct OutlinePlacement{public float x,y,width,height;}
    public static class FurnitureOutlineGeometry
    {
        public static OutlinePlacement Align(float sourceWidth,float sourceHeight,float frameWidth,float frameHeight,float[] sourceBounds,float[] outlineBounds,float padding)
        {
            if(sourceWidth<=0||sourceHeight<=0||frameWidth<=0||frameHeight<=0||!Valid(sourceBounds)||!Valid(outlineBounds))throw new ArgumentException("외곽선 맞춤 경계 오류입니다.");
            float scale=Math.Min(frameWidth/sourceWidth,frameHeight/sourceHeight);
            float left=(frameWidth-sourceWidth*scale)*.5f+sourceBounds[0]*sourceWidth*scale-padding;
            float top=(frameHeight-sourceHeight*scale)*.5f+sourceBounds[1]*sourceHeight*scale-padding;
            float w=((sourceBounds[2]-sourceBounds[0])*sourceWidth*scale+2*padding)/(outlineBounds[2]-outlineBounds[0]);
            float h=((sourceBounds[3]-sourceBounds[1])*sourceHeight*scale+2*padding)/(outlineBounds[3]-outlineBounds[1]);
            return new OutlinePlacement{x=left-outlineBounds[0]*w,y=top-outlineBounds[1]*h,width=w,height=h};
        }
        static bool Valid(float[] b){return b!=null&&b.Length==4&&b[0]>=0&&b[1]>=0&&b[2]<=1&&b[3]<=1&&b[0]<b[2]&&b[1]<b[3];}
    }
}
