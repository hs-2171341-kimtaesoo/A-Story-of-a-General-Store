using UnityEngine;

namespace GeneralStore.Inventory
{
    // Only repeated row placement and scroll extent are dynamic. Row children stay authored.
    public static class AuthoredRowLayout
    {
        public static float Pitch(Component template,float legacyPitch,float gap,bool useTemplate)
        {
            var rect=template==null?null:template.transform as RectTransform;
            return Mathf.Max(1,useTemplate&&rect!=null?rect.rect.height+Mathf.Max(0,gap):legacyPitch);
        }
        public static void Place(RectTransform row,int index,float pitch,float width,float gap)
        {
            row.anchorMin=row.anchorMax=row.pivot=new Vector2(0,1);
            row.anchoredPosition=new Vector2(0,-index*pitch);
            row.sizeDelta=new Vector2(width,Mathf.Max(1,pitch-Mathf.Max(0,gap)));
        }
        public static Rect ReadViewport(RectTransform view,RectTransform frame)
        {
            var topLeft=frame.InverseTransformPoint(view.TransformPoint(new Vector3(view.rect.xMin,view.rect.yMax,0)));
            var bottomRight=frame.InverseTransformPoint(view.TransformPoint(new Vector3(view.rect.xMax,view.rect.yMin,0)));
            float w=Mathf.Max(1,frame.rect.width),h=Mathf.Max(1,frame.rect.height);
            return new Rect((topLeft.x-frame.rect.xMin)/w,(frame.rect.yMax-topLeft.y)/h,(bottomRight.x-topLeft.x)/w,(topLeft.y-bottomRight.y)/h);
        }
        public static void FitViewport(RectTransform view,Rect area,float width,float height)
        {
            view.anchorMin=view.anchorMax=view.pivot=new Vector2(0,1);
            view.anchoredPosition=new Vector2(area.x*width,-area.y*height);
            view.sizeDelta=new Vector2(area.width*width,area.height*height);
        }
    }
}
