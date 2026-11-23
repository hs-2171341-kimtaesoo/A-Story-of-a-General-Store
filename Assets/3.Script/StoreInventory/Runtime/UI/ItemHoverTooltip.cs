using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // One shared, non-interactive panel for all inventory windows.
    public sealed class ItemHoverTooltip:MonoBehaviour
    {
        public InventoryPanel panel;public RectTransform window,temporaryWindow;public global::UnityEngine.UI.Image icon;
        public SpriteIconFit iconFit;public Text nameLabel,qualityLabel,sizeLabel,descriptionLabel,priceLabel;
        [Min(0)]public float gap=12;
        InventoryItemView hovered;InventoryGridView reservationGrid;string reservationUid;RectTransform anchor;bool dirty=true,ready;readonly Vector3[] corners=new Vector3[4];
        public void Enter(InventoryItemView view){hovered=view;reservationGrid=null;reservationUid=null;dirty=true;}
        public void EnterReservation(InventoryGridView grid,string uid){hovered=null;reservationGrid=grid;reservationUid=uid;dirty=true;}
        public void ExitReservation(InventoryGridView grid,string uid){if(reservationGrid==grid&&reservationUid==uid){reservationGrid=null;reservationUid=null;dirty=true;}}
        public void Exit(InventoryItemView view){if(hovered==view){hovered=null;dirty=true;}}
        public void SelectionChanged(){dirty=true;}
        void Reload(){hovered=null;reservationGrid=null;reservationUid=null;anchor=null;dirty=true;window.gameObject.SetActive(false);}
        void Start()
        {
            if(panel==null||window==null||iconFit==null)return;string error;if(!panel.inventory.TryInitialize(out error))return;
            panel.Model.Changed+=SelectionChanged;panel.inventory.Loaded+=Reload;ready=true;window.gameObject.SetActive(false);
        }
        void Resolve()
        {
            dirty=false;string container=panel.SelectedContainerId,uid=panel.SelectedInstanceId;InventoryGridView grid=null;
            if(hovered!=null&&hovered.isActiveAndEnabled){grid=hovered.Grid;container=grid.containerId;uid=hovered.Record.instanceId;}
            if(reservationGrid!=null&&reservationGrid.gameObject.activeInHierarchy){grid=reservationGrid;container=grid.containerId;uid=reservationUid;}
            var item=panel.Model.GetItem(container,uid);if(item==null){anchor=null;return;}
            if(grid==null)foreach(var candidate in panel.grids)if(candidate!=null&&candidate.gameObject.activeInHierarchy&&candidate.containerId==container&&candidate.IsVisible(item)){grid=candidate;break;}
            if(grid==null||(InventoryModel.IsWarehouse(container)&&!panel.shop.drawer.IsOpen)){anchor=null;return;}
            var display=grid.GetComponentInParent<DisplayWindowView>();
            anchor=display!=null?(RectTransform)display.transform:InventoryModel.IsWarehouse(container)?panel.shop.drawer.window:temporaryWindow!=null?temporaryWindow:grid.Rect;
            var definition=panel.Catalog.Find(item.itemId);var quality=panel.Catalog.FindQuality(item.itemId,item.quality);
            iconFit.SetSprite(panel.FindItemIcon(item.itemId));panel.ApplyQuality(icon,item.quality);
            nameLabel.text=string.IsNullOrEmpty(definition.displayName)?quality.name:definition.displayName;
            qualityLabel.text="품질 · "+QualityLabels.Title(item.quality);qualityLabel.color=QualityVisuals.Color(item.quality);
            sizeLabel.text=definition.width+"×"+definition.height;
            descriptionLabel.text=panel.inventory.Sales.CounterPresented&&panel.Model.IsReserved(uid)?"카운터 물품의 반환 예약 칸입니다.":string.IsNullOrEmpty(quality.explanation)?definition.description:quality.explanation;
            priceLabel.text="판매가(기본)  "+panel.Catalog.ReferencePrice(item.itemId,item.quality).ToString("N0")+" G";
            leftSide=InventoryModel.IsDisplay(container);
        }
        bool leftSide;
        void LateUpdate()
        {
            if(!ready)return;
            bool busy=panel.drag.IsDragging||panel.IsWindowDragging||panel.shop.displays.IsSwitching||panel.shop.HasPendingPlacement||(panel.shop.counterTrade!=null&&panel.shop.counterTrade.IsItemDragging);
            if(busy){window.gameObject.SetActive(false);dirty=true;return;}
            if(dirty)Resolve();
            bool show=anchor!=null&&anchor.gameObject.activeInHierarchy;if(window.gameObject.activeSelf!=show)window.gameObject.SetActive(show);if(!show)return;
            var parent=(RectTransform)window.parent;float scale=Mathf.Min(1,Mathf.Min(parent.rect.width*.4f/window.rect.width,parent.rect.height*.5f/window.rect.height));
            window.localScale=new Vector3(scale,scale,1);float w=window.rect.width*scale,h=window.rect.height*scale;
            anchor.GetWorldCorners(corners);var a=parent.InverseTransformPoint(corners[1]);var b=parent.InverseTransformPoint(corners[3]);
            float x=leftSide?a.x-w-gap:b.x+gap,y=(a.y+b.y+h)*.5f;
            x=Mathf.Clamp(x,parent.rect.xMin+8,Mathf.Max(parent.rect.xMin+8,parent.rect.xMax-w-8));y=Mathf.Clamp(y,parent.rect.yMin+h+8,parent.rect.yMax-8);
            window.anchoredPosition=new Vector2(x-parent.rect.xMin,y-parent.rect.yMax);if(window.GetSiblingIndex()!=parent.childCount-1)window.SetAsLastSibling();
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory!=null&&panel.inventory.Model!=null){panel.Model.Changed-=SelectionChanged;panel.inventory.Loaded-=Reload;}}
    }
}
