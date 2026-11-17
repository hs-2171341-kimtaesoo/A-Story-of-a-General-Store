using UnityEngine;
namespace GeneralStore.Inventory
{
    // 카운터 뒤에 가려진 가구만 클릭에서 제외합니다. 앞쪽 UI/바닥 아이템은 차단하지 않습니다.
    public sealed class SceneCounterOcclusion:MonoBehaviour
    {
        public TextAsset maskJson;public RectTransform imageRect;public global::UnityEngine.UI.Image counterImage;public Canvas renderCanvas;public bool blocksFurniture=true;
        PackedHitMask mask;
        void Awake(){if(maskJson!=null)mask=new PackedHitMask(JsonUtility.FromJson<PackedHitMaskData>(maskJson.text));}
        public bool Blocks(Vector2 screen,Camera camera)
        {
            if(!isActiveAndEnabled||!blocksFurniture||mask==null||imageRect==null)return false;
            if(renderCanvas!=null&&!renderCanvas.isActiveAndEnabled)return false;
            if(counterImage!=null&&(!counterImage.isActiveAndEnabled||counterImage.color.a<=.001f))return false;Vector2 local;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRect,screen,camera,out local))return false;
            var rect=imageRect.rect;if(rect.width<=0||rect.height<=0)return false;
            return mask.Contains((local.x-rect.xMin)/rect.width,(rect.yMax-local.y)/rect.height);
        }
    }
}
