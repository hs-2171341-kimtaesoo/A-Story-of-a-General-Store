#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using GeneralStore.Inventory;
using UI=InventorySceneBuilder;
public static class SceneLayerBuilder
{
    public static GameObject CreateCanvas(string name,Transform parent,int order,bool interactive)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(parent,false);
        var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=order;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        if(interactive)go.AddComponent<GraphicRaycaster>();return go;
    }
    public static RectTransform Layout(Transform parent,string name)
    {var r=UI.Rect(name,parent,0,0,1920,1080);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;return r;}
    public static SceneCounterOcclusion Counter(Transform parent)
    {
        var image=UI.Box("Foreground Counter Image",parent,0,0,1920,1080,Color.white);image.raycastTarget=false;
        image.sprite=Resources.Load<Sprite>("StoreInventory/Scene/counter");image.preserveAspect=false;
        var occlusion=image.gameObject.AddComponent<SceneCounterOcclusion>();occlusion.imageRect=image.rectTransform;occlusion.counterImage=image;occlusion.renderCanvas=parent.GetComponentInParent<Canvas>();occlusion.maskJson=Resources.Load<TextAsset>("StoreInventory/Scene/counter_mask");return occlusion;
    }
}
#endif
