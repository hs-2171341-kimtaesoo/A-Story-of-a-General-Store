using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // Shared hover-only panel. It never captures pointer input or stores a clicked selection.
    public sealed class ItemHoverTooltip:MonoBehaviour
    {
        public InventoryPanel panel;public RectTransform window;public global::UnityEngine.UI.Image icon;
        public SpriteIconFit iconFit;public Text nameLabel,qualityLabel,sizeLabel,descriptionLabel,priceLabel;
        [Min(0)]public float gap=12;
        [Min(100)]public float preferredWidth=340;
        InventoryItemView hovered;InventoryGridView reservationGrid,activeGrid;
        string hoverUid,hoverContainer,reservationUid;
        bool dirty=true,ready;
        Canvas rootCanvas;
        readonly Vector3[] corners=new Vector3[4];
        public void Enter(InventoryItemView view)
        {
            hovered=view;hoverUid=view.Record==null?null:view.Record.instanceId;hoverContainer=view.Grid.containerId;
            reservationGrid=null;reservationUid=null;dirty=true;
        }
        public void EnterReservation(InventoryGridView grid,string uid)
        {hovered=null;hoverUid=hoverContainer=null;reservationGrid=grid;reservationUid=uid;dirty=true;}
        public void ExitReservation(InventoryGridView grid,string uid)
        {if(reservationGrid==grid&&reservationUid==uid){reservationGrid=null;reservationUid=null;dirty=true;Hide();}}
        public void Exit(InventoryItemView view){if(hovered==view){hovered=null;hoverUid=hoverContainer=null;dirty=true;Hide();}}
        public void InvalidateContent(){dirty=true;}
        void Hide(){activeGrid=null;if(window!=null&&window.gameObject.activeSelf)window.gameObject.SetActive(false);}
        void Reload(){hovered=null;hoverUid=hoverContainer=null;reservationGrid=null;reservationUid=null;dirty=true;Hide();}
        void Start()
        {
            if(panel==null||window==null||iconFit==null)return;string error;if(!panel.inventory.TryInitialize(out error))return;
            rootCanvas=panel.canvas.rootCanvas;
            var overlay=window.GetComponent<Canvas>();
            if(overlay==null)overlay=window.gameObject.AddComponent<Canvas>();
            overlay.overrideSorting=true;
            overlay.sortingLayerID=rootCanvas.sortingLayerID;
            overlay.sortingOrder=Mathf.Min(short.MaxValue,rootCanvas.sortingOrder+100);
            overlay.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
            window.anchorMin=window.anchorMax=window.pivot=new Vector2(0,1);
            var group=window.GetComponent<CanvasGroup>();if(group==null)group=window.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            panel.Model.Changed+=InvalidateContent;panel.inventory.Loaded+=Reload;ready=true;Hide();
        }
        void Resolve()
        {
            dirty=false;activeGrid=null;InventoryGridView grid=null;string uid=null;
            if(hovered!=null&&hovered.isActiveAndEnabled&&hovered.Record!=null&&hovered.Record.instanceId==hoverUid&&hovered.Grid.containerId==hoverContainer)
            {grid=hovered.Grid;uid=hoverUid;}
            else if(reservationGrid!=null&&reservationGrid.gameObject.activeInHierarchy)
            {grid=reservationGrid;uid=reservationUid;}
            if(grid==null)return;
            var item=panel.Model.GetItem(grid.containerId,uid);if(item==null||!grid.IsVisible(item))return;
            var definition=panel.Catalog.Find(item.itemId);var quality=panel.Catalog.FindQuality(item.itemId,item.quality);
            iconFit.SetSprite(panel.FindItemIcon(item.itemId));panel.ApplyQuality(icon,item.quality);
            nameLabel.text=string.IsNullOrEmpty(definition.displayName)?quality.name:definition.displayName;
            qualityLabel.text="품질 · "+QualityLabels.Title(item.quality);qualityLabel.color=QualityVisuals.Color(item.quality);
            sizeLabel.text=definition.width+"×"+definition.height;
            descriptionLabel.text=panel.inventory.Sales.CounterPresented&&panel.Model.IsReserved(uid)?"카운터 물품의 반환 예약 칸입니다.":string.IsNullOrEmpty(quality.explanation)?definition.description:quality.explanation;
            priceLabel.text="판매가(기본)  "+panel.Catalog.ReferencePrice(item.itemId,item.quality).ToString("N0")+" G";
            activeGrid=grid;
        }
        void LateUpdate()
        {
            if(!ready)return;
            if(hovered!=null&&(!hovered.isActiveAndEnabled||hovered.Record==null||hovered.Record.instanceId!=hoverUid||hovered.Grid.containerId!=hoverContainer))
            {hovered=null;dirty=true;}
            bool busy=panel.drag.IsDragging||panel.IsWindowDragging||panel.shop.displays.IsSwitching||panel.shop.HasPendingPlacement||(panel.shop.counterTrade!=null&&panel.shop.counterTrade.IsItemDragging);
            if(busy){Hide();dirty=true;return;}
            if(dirty)Resolve();
            Vector2 screen,point;var parent=(RectTransform)window.parent;
            if(activeGrid==null||!activeGrid.gameObject.activeInHierarchy||!InventoryKeys.TryPointerPosition(out screen)||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,panel.EventCamera,out point))
            {Hide();dirty=true;return;}
            // Clamp against the real canvas viewport, even when the stage stays at 1920x1080.
            ((RectTransform)rootCanvas.transform).GetWorldCorners(corners);
            var lower=parent.InverseTransformPoint(corners[0]);var upper=parent.InverseTransformPoint(corners[2]);
            float availableWidth=Mathf.Max(1,upper.x-lower.x-16),availableHeight=Mathf.Max(1,upper.y-lower.y-16);
            float scale=Mathf.Min(1,Mathf.Min(preferredWidth/window.rect.width,Mathf.Min(availableWidth/window.rect.width,availableHeight*.5f/window.rect.height)));
            window.localScale=new Vector3(scale,scale,1);float w=window.rect.width*scale,h=window.rect.height*scale;
            var position=PlaceNearPointer(Rect.MinMaxRect(lower.x,lower.y,upper.x,upper.y),point,new Vector2(w,h),gap);
            window.anchoredPosition=new Vector2(position.x-parent.rect.xMin,position.y-parent.rect.yMax);
            if(!window.gameObject.activeSelf)window.gameObject.SetActive(true);
            if(window.GetSiblingIndex()!=parent.childCount-1)window.SetAsLastSibling();
        }
        public static Vector2 PlaceNearPointer(Rect area,Vector2 pointer,Vector2 size,float gap)
        {
            float x=pointer.x+gap;if(x+size.x>area.xMax-8)x=pointer.x-gap-size.x;
            x=Mathf.Clamp(x,area.xMin+8,Mathf.Max(area.xMin+8,area.xMax-size.x-8));
            float y=Mathf.Clamp(pointer.y-gap,area.yMin+size.y+8,Mathf.Max(area.yMin+size.y+8,area.yMax-8));
            return new Vector2(x,y);
        }
        void OnDisable(){Reload();}
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory!=null&&panel.inventory.Model!=null){panel.Model.Changed-=InvalidateContent;panel.inventory.Loaded-=Reload;}}
    }
}
