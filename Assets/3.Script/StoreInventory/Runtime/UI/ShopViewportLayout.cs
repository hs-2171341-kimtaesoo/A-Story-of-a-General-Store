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
        Vector2 previousSize;
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
            float x=left+Mathf.Min(leftInset,Mathf.Max(0,size.x-width));
            drawer.RefreshViewport(region,new Vector2(x,top-size.y*warehouseCenterFraction+drawer.window.rect.height*scale*.5f));
            displays.RefreshViewport(region);if(delivery!=null)delivery.RefreshViewport(region);
            if(managementMenu!=null){var menu=managementMenu.GetComponent<RightSideMenu>();if(menu!=null)menu.RefreshLayout();}
            if(panel.shop.counterTrade!=null)panel.shop.counterTrade.RefreshViewport(region);
            if(displayMenuLayer!=null){displayMenuLayer.anchoredPosition=Vector2.zero;displayMenuLayer.sizeDelta=size;displayMenu.RefreshLayout();}
            if(managementWindows!=null)for(int n=0;n<managementWindows.Length;n++)
            {
                if(managementWindows[n]==null||managementFrames[n]==null)continue;
                managementWindows[n].sizeDelta=size;
                var frame=managementFrames[n];var workshop=GetComponent<WorkshopUI>();if(workshop!=null&&workshop.frame==frame)continue;float f=Mathf.Min(1,Mathf.Min(Mathf.Max(1,size.y-40)/860,Mathf.Max(1,size.x-210)/560));
                frame.localScale=new Vector3(f,f,1);frame.anchoredPosition=new Vector2(100,-Mathf.Max(20,(size.y-860*f)*.5f));
            }
        }
    }
}
