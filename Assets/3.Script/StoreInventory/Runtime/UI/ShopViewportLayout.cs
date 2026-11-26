using UnityEngine;
using UnityEngine.Serialization;
namespace GeneralStore.Inventory
{
    // 가구의 배치를 유지하면서 화면 끝 UI를 실제 Canvas 표시 범위에 맞춥니다.
    public sealed class ShopViewportLayout:MonoBehaviour
    {
        public InventoryPanel panel;public RectTransform stage,edgeLayer,displayMenuLayer,managementMenu;
        public DeliveryUI delivery;
        public WarehouseDrawer drawer;public DisplayUI displays;public LeftDisplayMenu displayMenu;
        public RectTransform[] managementWindows,managementFrames;
        [Range(.1f,.3333333f)]public float warehouseWidthFraction=.3125f;
        [Min(0)]public float leftInset=132,bottomInset=0;
        [FormerlySerializedAs("warehouseTopFraction")][Range(.2f,.8f)]public float warehouseCenterFraction=.5f;
        [Tooltip("관리창 내부는 Prefab 배치를 유지하고 화면 밖으로 나가는 경우에만 축소합니다.")]
        public bool useAuthoredManagementPlacement;
        public Vector2 authoredReferenceSize=new Vector2(1920,1080);
        struct FramePose{public Vector2 position;public Vector3 scale;}
        FramePose[] authoredFrames;
        Vector2 previousSize;
        public void CaptureAuthoredManagementPlacement()
        {
            useAuthoredManagementPlacement=true;
            var canvas=panel==null||panel.canvas==null?null:panel.canvas.transform as RectTransform;
            authoredReferenceSize=canvas!=null&&canvas.rect.height>1?canvas.rect.size:new Vector2(1920,1080);
            ReadAuthoredFrames();
        }
        // Nonserialized poses are read from the edited prefab once, before responsive positioning.
        void ReadAuthoredFrames()
        {
            authoredFrames=new FramePose[managementFrames==null?0:managementFrames.Length];
            for(int n=0;n<authoredFrames.Length;n++)if(managementFrames[n]!=null)
                authoredFrames[n]=new FramePose{position=managementFrames[n].anchoredPosition,scale=managementFrames[n].localScale};
        }
        void Start(){Apply();}
        void Update()
        {
            if(panel==null||panel.canvas==null)return;
            var root=(RectTransform)panel.canvas.transform;var size=root.rect.size;
            if(size.x!=previousSize.x||size.y!=previousSize.y)Apply();
        }
        public void Apply()
        {
            if(panel==null||stage==null||edgeLayer==null||drawer==null||drawer.window==null)return;
            var root=(RectTransform)panel.canvas.transform;var size=root.rect.size;
            if(size.x<=1||size.y<=1)size=new Vector2(1920,1080);
            previousSize=size;float left=(stage.rect.width-size.x)*.5f,top=(size.y-stage.rect.height)*.5f,bottom=top-size.y;
            var region=new Rect(left,bottom,size.x,size.y);
            edgeLayer.anchoredPosition=new Vector2(left,top);edgeLayer.sizeDelta=size;
            float width=Mathf.Min(size.x*Mathf.Clamp(warehouseWidthFraction,.1f,.3333333f),drawer.window.rect.width);
            float scale=Mathf.Min(width/drawer.window.rect.width,Mathf.Max(1,size.y-bottomInset-40)/drawer.window.rect.height);
            float centeredHeight=2*Mathf.Min(size.y*warehouseCenterFraction-40,size.y*(1-warehouseCenterFraction)-bottomInset);
            scale=Mathf.Min(scale,Mathf.Max(1,centeredHeight)/drawer.window.rect.height);
            if(displays.cellReference!=null)
            {
                float cell=displays.cellReference.cellWidth*Mathf.Abs(displays.cellReference.transform.lossyScale.x)/Mathf.Max(.0001f,Mathf.Abs(drawer.window.lossyScale.x));
                scale=Mathf.Min(scale,displays.WarehouseCellLimit(region)/Mathf.Max(.0001f,cell));
            }
            drawer.window.localScale=new Vector3(scale,scale,1);
            var management=managementMenu==null?null:managementMenu.GetComponent<RightSideMenu>();
            float managementInset=management==null?0:management.ReservedWidth;
            float x=left+Mathf.Min(Mathf.Max(leftInset,managementInset),Mathf.Max(0,size.x-width));
            drawer.RefreshViewport(region,new Vector2(x,top-size.y*warehouseCenterFraction+drawer.window.rect.height*scale*.5f));
            displays.RefreshViewport(region);if(delivery!=null)delivery.RefreshViewport(region);
            if(management!=null)management.RefreshLayout();
            if(panel.shop.counterTrade!=null)panel.shop.counterTrade.RefreshViewport(region);
            if(displayMenuLayer!=null){displayMenuLayer.anchoredPosition=Vector2.zero;displayMenuLayer.sizeDelta=size;displayMenu.RefreshLayout();}
            if(useAuthoredManagementPlacement&&(authoredFrames==null||authoredFrames.Length!=(managementFrames==null?0:managementFrames.Length)))ReadAuthoredFrames();
            if(managementWindows!=null&&managementFrames!=null)for(int n=0;n<Mathf.Min(managementWindows.Length,managementFrames.Length);n++)
            {
                if(managementWindows[n]==null||managementFrames[n]==null)continue;
                managementWindows[n].sizeDelta=size;
                var frame=managementFrames[n];var workshop=GetComponent<WorkshopUI>();if(workshop!=null&&workshop.frame==frame)continue;
                float frameWidth=Mathf.Max(1,frame.rect.width),frameHeight=Mathf.Max(1,frame.rect.height);
                float leftEdge=Mathf.Max(100,managementInset),availableWidth=Mathf.Max(1,size.x-leftEdge-24);
                float f=Mathf.Min(1,Mathf.Min(Mathf.Max(1,size.y-40)/frameHeight,availableWidth/frameWidth));
                var frameScale=new Vector3(f,f,1);var target=new Vector2(leftEdge,-Mathf.Max(20,(size.y-frameHeight*f)*.5f));
                if(useAuthoredManagementPlacement&&authoredFrames!=null)
                {
                    var pose=authoredFrames[n];
                    float fit=Mathf.Min(1,Mathf.Min(Mathf.Max(1,size.y-40)/Mathf.Max(1,frameHeight*Mathf.Abs(pose.scale.y)),availableWidth/Mathf.Max(1,frameWidth*Mathf.Abs(pose.scale.x))));
                    frameScale=new Vector3(pose.scale.x*fit,pose.scale.y*fit,pose.scale.z);
                    float right=Mathf.Max(leftEdge,size.x-24-frameWidth*Mathf.Abs(frameScale.x));
                    float frameBottom=Mathf.Min(-20,-size.y+20+frameHeight*Mathf.Abs(frameScale.y));
                    target=new Vector2(Mathf.Clamp(pose.position.x,leftEdge,right),Mathf.Clamp(pose.position.y-(size.y-authoredReferenceSize.y)*.5f,frameBottom,-20));
                }
                frame.localScale=frameScale;
                ManagementWindowMotion.Ensure(managementWindows[n].gameObject,frame).SetTarget(target);
            }
        }
    }
}
