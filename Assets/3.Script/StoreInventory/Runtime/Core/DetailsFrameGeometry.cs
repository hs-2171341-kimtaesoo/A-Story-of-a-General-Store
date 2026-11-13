using System;
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
