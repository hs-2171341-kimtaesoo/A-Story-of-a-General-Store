using System.Collections.Generic;
using UnityEngine;
namespace GeneralStore.Inventory
{
    public sealed class SpriteIconFit : MonoBehaviour
    {
        public global::UnityEngine.UI.Image image;
        public RectTransform frame;
        [Min(0)] public float padding=6;
        public bool useSpriteMeshBounds=true;
        struct Bounds {public float x0,y0,x1,y1;}
        // 텍스처 픽셀을 읽거나 Read/Write를 켜지 않고 Sprite 메시 경계만 한 번 캐시합니다.
        static readonly Dictionary<Sprite,Bounds> cache=new Dictionary<Sprite,Bounds>();
        public void SetSprite(Sprite sprite)
        {
            if(image==null||frame==null){Debug.LogError("SpriteIconFit의 Image/Frame을 연결하세요.",this);return;}
            image.sprite=sprite;image.enabled=sprite!=null;image.preserveAspect=true;image.raycastTarget=false;
            if(sprite==null)return;
            Bounds b;
            if(!useSpriteMeshBounds)b=new Bounds{x0=0,y0=0,x1=sprite.rect.width,y1=sprite.rect.height};
            else if(!cache.TryGetValue(sprite,out b))
            {
                b=new Bounds{x0=sprite.rect.width,y0=sprite.rect.height,x1=0,y1=0};var vertices=sprite.vertices;
                foreach(var v in vertices)
                {
                    float x=v.x*sprite.pixelsPerUnit+sprite.pivot.x,y=v.y*sprite.pixelsPerUnit+sprite.pivot.y;
                    b.x0=Mathf.Min(b.x0,x);b.y0=Mathf.Min(b.y0,y);b.x1=Mathf.Max(b.x1,x);b.y1=Mathf.Max(b.y1,y);
                }
                if(b.x1<=b.x0||b.y1<=b.y0)b=new Bounds{x0=0,y0=0,x1=sprite.rect.width,y1=sprite.rect.height};
                cache[sprite]=b;
            }
            var fit=IconFitGeometry.Fit(sprite.rect.width,sprite.rect.height,b.x0,b.y0,b.x1,b.y1,frame.rect.width,frame.rect.height,padding);
            var rect=image.rectTransform;rect.localRotation=Quaternion.Euler(0,0,0);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=new Vector2(fit.width,fit.height);rect.anchoredPosition=new Vector2(fit.x,fit.y);
        }
    }
}
