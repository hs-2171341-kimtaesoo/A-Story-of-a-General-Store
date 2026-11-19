using UnityEngine;
namespace GeneralStore.Inventory
{
    // 표시 Image 하나를 공유합니다. 각 가구 Outline Frame은 에디터에서 편집한 그대로 사용합니다.
    public sealed class FurnitureHoverPresenter:MonoBehaviour
    {
        public global::UnityEngine.UI.Image image;public Color tint=new Color(1,.8f,.15f,1);[Min(0)]public float padding=1;
        public global::UnityEngine.UI.Outline thicknessEffect;
        SceneFurnitureHotspot owner;
        void Start(){owner=null;if(image!=null)image.enabled=false;} // 편집 모드의 미리보기를 플레이에 남기지 않습니다.
        public void Show(SceneFurnitureHotspot target)
        {
            if(image==null||target==null||target.hoverSprite==null||target.furnitureImage==null||target.furnitureImage.sprite==null)return;
            owner=target;var rect=image.rectTransform;
            if(target.outlineFrame!=null)
            {
                // Frame 자체의 위치/크기는 절대 쓰지 않습니다. 공유 그림만 그 안에 채웁니다.
                if(image.transform.parent!=target.outlineFrame)image.transform.SetParent(target.outlineFrame,false);
                rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            else
            {
                // 이전 씬 호환: Frame을 아직 연결하지 않은 경우에만 기존 자동 맞춤을 사용합니다.
                var frame=target.furnitureImage.rectTransform;var sprite=target.furnitureImage.sprite;
                var p=FurnitureOutlineGeometry.Align(sprite.rect.width,sprite.rect.height,frame.rect.width,frame.rect.height,target.furnitureAlphaBounds,target.outlineAlphaBounds,padding);
                if(image.transform.parent!=frame)image.transform.SetParent(frame,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(p.x,-p.y);rect.sizeDelta=new Vector2(p.width,p.height);
            }
            image.sprite=target.hoverSprite;image.preserveAspect=false;image.color=tint;image.raycastTarget=false;
            // 실시간 셰이더 대신 공유 그림 하나에만 필요한 추가 두께를 줍니다. 기본0이면 효과는 꺼집니다.
            if(thicknessEffect==null){thicknessEffect=image.GetComponent<global::UnityEngine.UI.Outline>();if(thicknessEffect==null)thicknessEffect=image.gameObject.AddComponent<global::UnityEngine.UI.Outline>();}
            float thickness=Mathf.Clamp(target.outlineThickness,0,8);thicknessEffect.enabled=thickness>0;
            if(thickness>0){thicknessEffect.effectDistance=new Vector2(thickness,thickness);thicknessEffect.effectColor=tint;thicknessEffect.useGraphicAlpha=true;}
            image.enabled=true;
        }
        public void Hide(SceneFurnitureHotspot target){if(owner!=target)return;owner=null;if(image!=null){image.enabled=false;image.transform.SetParent(transform,false);}}
        void OnDisable(){owner=null;if(image!=null)image.enabled=false;}
    }
}
