using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // Door click receives everything; the separately opened sixteen-column overflow window is virtualized.
    public sealed class DeliveryUI:MonoBehaviour
    {
        public InventoryPanel panel;
        public InventoryGridView grid,cellReference;
        public InventoryDetailsView details;
        public Button doorButton;
        public Text badge,feeLabel;
        public RectTransform window,content;
        public ScrollRect scroll;
        [Min(0)]public float viewportMargin=50,doorDropDistance=90;
        [Min(.05f)]public float doorDropSeconds=.42f;
        Vector2 doorHome;float doorAnimation;int previousDoorCount,lastFee=-1;
        public int RenderedItemCount{get{return grid==null?0:grid.LiveItemViewCount;}}
        readonly List<ItemRecord> orderedItems=new List<ItemRecord>();
        readonly ContainerRecord snapshot=new ContainerRecord(),visible=new ContainerRecord();
        long snapshotVersion=-1,shownDeliveryVersion=-1;int shownFee=-1;
        static int CompareRows(ItemRecord a,ItemRecord b){return a.y.CompareTo(b.y);}
        bool ready,dirty=true;
        ManagementWindowMotion motion;
        int maximumItemHeight=1,previousStart=-1,previousRows=-1;
        public void RefreshViewport(Rect region)
        {
            if(window==null||cellReference==null||grid==null)return;
            var parent=(RectTransform)window.parent;
            var workshop=panel.GetComponentInChildren<WorkshopUI>(true);if(workshop!=null&&workshop.window!=null&&workshop.window.activeSelf){region.xMin+=104;region.width=Mathf.Max(400,region.width-956);}
            float unit=cellReference.cellWidth*Mathf.Abs(cellReference.transform.lossyScale.x)/Mathf.Max(.0001f,Mathf.Abs(parent.lossyScale.x));
            float scale=Mathf.Min(unit/grid.cellWidth,Mathf.Min(Mathf.Max(1,region.width-viewportMargin*2)/window.rect.width,Mathf.Max(1,region.height-viewportMargin*2)/window.rect.height));
            window.localScale=new Vector3(scale,scale,1);
            var target=new Vector2(region.xMin+(region.width-window.rect.width*scale)*.5f,region.yMax-(region.height-window.rect.height*scale)*.5f);
            if(motion==null)motion=ManagementWindowMotion.Ensure(window.gameObject,window);motion.SetTarget(target);
        }
        void Start()
        {
            string error;
            if(panel==null||grid==null||window==null||content==null||scroll==null||doorButton==null||badge==null||feeLabel==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("DeliveryUI 참조를 확인하세요. 새 데모를 생성하세요.",this);enabled=false;return;}
            grid.externalRefresh=true;panel.RegisterWindowGrid(grid,details);
            panel.Model.Changed+=MarkDirty;panel.inventory.Loaded+=OnLoaded;scroll.onValueChanged.AddListener(OnScrolled);
            ready=true;doorHome=((RectTransform)doorButton.transform).anchoredPosition;window.gameObject.SetActive(false);Synchronize();
        }
        void MarkDirty(){dirty=true;}
        void OnLoaded(){panel.drag.Cancel(false);window.gameObject.SetActive(false);content.anchoredPosition=Vector2.zero;snapshotVersion=-1;MarkDirty();}
        void OnScrolled(Vector2 value){if(ready&&window.gameObject.activeSelf)RenderVisible();}
        void LateUpdate()
        {
            if(!ready)return;if(lastFee!=panel.inventory.storageCellFee){lastFee=panel.inventory.storageCellFee;dirty=true;}
            if(dirty){dirty=false;Synchronize();}
            if(doorAnimation>0){doorAnimation=Mathf.Max(0,doorAnimation-Time.unscaledDeltaTime);float t=doorAnimation/Mathf.Max(.05f,doorDropSeconds);((RectTransform)doorButton.transform).anchoredPosition=doorHome+Vector2.up*(doorDropDistance*(2*t-t*t));}
        }
        public void Toggle()
        {
            if(!ready||panel.IsWindowDragging||panel.drag.IsDragging||panel.shop.HasPendingPlacement||(panel.shop.counterTrade!=null&&panel.shop.counterTrade.IsItemDragging))return;
            int stored,overflow;string error;bool ok=panel.Model.TryReceiveDoorDelivery(out stored,out overflow,out error);
            panel.ShowMessage(ok?"택배 수령: 상자 "+stored+"개 / 임시 저장고 "+overflow+"개":error);
        }
        public void OpenStorage()
        {if(!ready||panel.IsWindowDragging||panel.drag.IsDragging)return;window.gameObject.SetActive(true);window.SetAsLastSibling();var viewport=panel.GetComponentInChildren<ShopViewportLayout>(true);if(viewport!=null)viewport.Apply();Synchronize();}
        public void Close(){if(ready&&!panel.drag.IsDragging)window.gameObject.SetActive(false);}
        void Synchronize()
        {
            int count=panel.Model.DeliveryItemCount;bool present=count>0;
            if(doorButton.gameObject.activeSelf!=present)doorButton.gameObject.SetActive(present);
            if(badge.text.Length==0||count!=previousDoorCount)badge.text="택배 "+count;if(count>previousDoorCount)doorAnimation=doorDropSeconds;previousDoorCount=count;
            if(!window.gameObject.activeSelf)return;
            long version=panel.Model.ContainerVersion(InventoryModel.TemporaryStorageId),deliveryVersion=panel.Model.ContainerVersion(InventoryModel.DeliveryContainerId);
            bool changed=version!=snapshotVersion;
            if(changed)
            {
                panel.Model.CopyContainerInto(InventoryModel.TemporaryStorageId,snapshot);snapshotVersion=version;orderedItems.Clear();maximumItemHeight=1;
                foreach(var item in snapshot.items){maximumItemHeight=Mathf.Max(maximumItemHeight,panel.Model.Height(item));orderedItems.Add(item);}
                orderedItems.Sort(CompareRows);
            }
            content.sizeDelta=new Vector2(snapshot.columns*grid.cellWidth+grid.cellGap,snapshot.rows*grid.cellHeight+grid.cellGap);
            float max=Mathf.Max(0,content.rect.height-scroll.viewport.rect.height);
            content.anchoredPosition=new Vector2(0,Mathf.Clamp(content.anchoredPosition.y,0,max));
            if(changed||shownDeliveryVersion!=deliveryVersion||shownFee!=panel.inventory.storageCellFee)
            {
                long cells=panel.Model.TemporaryOccupiedCells;
                feeLabel.text="임시 저장 "+cells+"칸 + 미수령 택배 "+panel.Model.DeliveryOccupiedCells+"칸 · 다음 새벽 칸당 "+panel.inventory.storageCellFee+" G";
                shownDeliveryVersion=deliveryVersion;shownFee=panel.inventory.storageCellFee;
            }
            if(changed)previousStart=previousRows=-1;RenderVisible();
        }
        int LowerBound(int row)
        {int lo=0,hi=orderedItems.Count;while(lo<hi){int mid=lo+(hi-lo)/2;if(orderedItems[mid].y<row)lo=mid+1;else hi=mid;}return lo;}

        void RenderVisible()
        {
            if(snapshotVersion<0)return;
            int first=Mathf.FloorToInt(Mathf.Max(0,content.anchoredPosition.y)/grid.cellHeight);
            int start=Mathf.Clamp(first-maximumItemHeight+1,0,snapshot.rows-1);
            int rows=Mathf.Min(snapshot.rows-start,Mathf.CeilToInt(scroll.viewport.rect.height/grid.cellHeight)+maximumItemHeight*2+2);
            if(start==previousStart&&rows==previousRows)return;previousStart=start;previousRows=rows;
            visible.containerId=snapshot.containerId;visible.columns=snapshot.columns;visible.rows=snapshot.rows;visible.items.Clear();
            // Index by item origin, not every occupied row. Tall items do not multiply bookkeeping memory.
            for(int n=LowerBound(start);n<orderedItems.Count;n++)
            {var item=orderedItems[n];if(item.y>=start+rows)break;visible.items.Add(item);}
            grid.startRow=start;grid.viewRows=rows;grid.Rect.anchoredPosition=new Vector2(0,-start*grid.cellHeight);grid.Refresh(visible);
        }
        void OnDestroy()
        {
            if(!ready)return;
            if(scroll!=null)scroll.onValueChanged.RemoveListener(OnScrolled);
            if(panel!=null&&panel.inventory!=null&&panel.inventory.Model!=null)
            {panel.Model.Changed-=MarkDirty;panel.inventory.Loaded-=OnLoaded;panel.UnregisterWindowGrid(grid,details);}
        }
    }
}
