using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // One door icon, one scroll window; only a visible row band has live item/cell views.
    public sealed class DeliveryUI:MonoBehaviour
    {
        public InventoryPanel panel;
        public InventoryGridView grid,cellReference;
        public InventoryDetailsView details;
        public Button doorButton;
        public Text badge,feeLabel;
        public RectTransform window,content;
        public ScrollRect scroll;
        [Min(0)]public float viewportMargin=50;
        public int RenderedItemCount{get{return grid==null?0:grid.LiveItemViewCount;}}
        readonly Dictionary<int,List<ItemRecord>> byRow=new Dictionary<int,List<ItemRecord>>();
        readonly HashSet<string> visibleIds=new HashSet<string>();
        ContainerRecord snapshot;
        bool ready,dirty=true;
        int maximumItemHeight=1,previousStart=-1,previousRows=-1;
        public void RefreshViewport(Rect region)
        {
            if(window==null||cellReference==null||grid==null)return;
            var parent=(RectTransform)window.parent;
            float unit=cellReference.cellWidth*Mathf.Abs(cellReference.transform.lossyScale.x)/Mathf.Max(.0001f,Mathf.Abs(parent.lossyScale.x));
            float scale=Mathf.Min(unit/grid.cellWidth,Mathf.Min(Mathf.Max(1,region.width-viewportMargin*2)/window.rect.width,Mathf.Max(1,region.height-viewportMargin*2)/window.rect.height));
            window.localScale=new Vector3(scale,scale,1);
            window.anchoredPosition=new Vector2(region.xMin+(region.width-window.rect.width*scale)*.5f,region.yMax-(region.height-window.rect.height*scale)*.5f);
        }
        void Start()
        {
            string error;
            if(panel==null||grid==null||window==null||content==null||scroll==null||doorButton==null||badge==null||feeLabel==null||details==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("DeliveryUI 참조를 확인하세요. 새 데모를 생성하세요.",this);enabled=false;return;}
            grid.externalRefresh=true;panel.RegisterWindowGrid(grid,details);
            panel.Model.Changed+=MarkDirty;panel.inventory.Loaded+=OnLoaded;scroll.onValueChanged.AddListener(OnScrolled);
            ready=true;window.gameObject.SetActive(false);Synchronize();
        }
        void MarkDirty(){dirty=true;}
        void OnLoaded(){panel.drag.Cancel(false);window.gameObject.SetActive(false);content.anchoredPosition=Vector2.zero;MarkDirty();}
        void OnScrolled(Vector2 value){if(ready&&window.gameObject.activeSelf)RenderVisible();}
        void LateUpdate(){if(ready&&dirty){dirty=false;Synchronize();}}
        public void Toggle()
        {
            if(!ready||panel.IsWindowDragging||panel.drag.IsDragging)return;
            panel.shop.CancelDisplayPrice();bool show=!window.gameObject.activeSelf;
            if(show&&panel.Model.DeliveryItemCount==0)return;
            window.gameObject.SetActive(show);if(show){window.SetAsLastSibling();Synchronize();}
        }
        public void Close(){if(ready&&!panel.drag.IsDragging)window.gameObject.SetActive(false);}
        void Synchronize()
        {
            int count=panel.Model.DeliveryItemCount;bool present=count>0;
            if(doorButton.gameObject.activeSelf!=present)doorButton.gameObject.SetActive(present);
            badge.text="택배 "+count;
            if(!present){window.gameObject.SetActive(false);snapshot=null;byRow.Clear();return;}
            if(!window.gameObject.activeSelf)return;
            snapshot=panel.Model.GetContainer(InventoryModel.DeliveryContainerId);byRow.Clear();maximumItemHeight=1;
            foreach(var item in snapshot.items)
            {
                int height=panel.Model.Height(item);maximumItemHeight=Mathf.Max(maximumItemHeight,height);
                for(int row=item.y;row<item.y+height;row++)
                {List<ItemRecord> items;if(!byRow.TryGetValue(row,out items)){items=new List<ItemRecord>();byRow.Add(row,items);}items.Add(item);}
            }
            content.sizeDelta=new Vector2(snapshot.columns*grid.cellWidth+grid.cellGap,snapshot.rows*grid.cellHeight+grid.cellGap);
            float max=Mathf.Max(0,content.rect.height-scroll.viewport.rect.height);
            content.anchoredPosition=new Vector2(0,Mathf.Clamp(content.anchoredPosition.y,0,max));
            long cells=panel.Model.DeliveryOccupiedCells;
            feeLabel.text="다음 새벽 보관료: "+cells+"칸 × "+panel.inventory.deliveryCellFee+" G";
            previousStart=previousRows=-1;RenderVisible();
        }
        void RenderVisible()
        {
            if(snapshot==null)return;
            int first=Mathf.FloorToInt(Mathf.Max(0,content.anchoredPosition.y)/grid.cellHeight);
            int start=Mathf.Clamp(first-maximumItemHeight+1,0,snapshot.rows-1);
            int rows=Mathf.Min(snapshot.rows-start,Mathf.CeilToInt(scroll.viewport.rect.height/grid.cellHeight)+maximumItemHeight*2+2);
            if(start==previousStart&&rows==previousRows)return;previousStart=start;previousRows=rows;
            var visible=new ContainerRecord{containerId=snapshot.containerId,columns=snapshot.columns,rows=snapshot.rows};visibleIds.Clear();
            for(int row=start;row<start+rows;row++)
            {
                List<ItemRecord> items;if(!byRow.TryGetValue(row,out items))continue;
                foreach(var item in items)if(visibleIds.Add(item.instanceId))visible.items.Add(item);
            }
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
