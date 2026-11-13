using System;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class WarehouseUI : MonoBehaviour
    {
        public InventoryPanel panel;public InventoryGridView grid;public ScrollRect scroll;
        [Min(.05f)] public float hoverSwitchSeconds=2;
        public Text pageLabel;public ContainerTabBar tabs;
        public event Action SelectionChanged;
        public string SelectedId {get{return grid.containerId;}}
        bool ready,dirty;int index;
        void Start()
        {
            string error;if(panel==null||grid==null||scroll==null||pageLabel==null||tabs==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("WarehouseUI 참조를 확인하세요.",this);enabled=false;return;}
            panel.Model.Changed+=MarkDirty;panel.inventory.Loaded+=ResetPage;ready=true;dirty=true;
        }
        void MarkDirty(){dirty=true;}
        void ResetPage(){index=0;grid.containerId="warehouse";foreach(var detail in panel.detailViews)if(InventoryModel.IsWarehouse(detail.containerFilter))detail.containerFilter="warehouse";panel.UpdateDetails();dirty=true;if(SelectionChanged!=null)SelectionChanged();}
        public void Previous(){Switch(-1);}public void Next(){Switch(1);}
        void Switch(int delta)
        {if(!ready)return;var ids=panel.Model.WarehouseIds();Select(ids[(index+delta+ids.Count)%ids.Count]);}
        public void Select(string id){SelectInternal(id,false);}
        public void SelectWhileDragging(string id){SelectInternal(id,true);}
        void SelectInternal(string id,bool preserveDrag)
        {
            if(!ready)return;var ids=panel.Model.WarehouseIds();int next=ids.IndexOf(id);if(next<0)return;
            if(grid.containerId==id)return;
            if(preserveDrag)panel.drag.InvalidatePreview();else panel.drag.Cancel(false);
            index=next;grid.containerId=id;
            foreach(var detail in panel.detailViews)if(InventoryModel.IsWarehouse(detail.containerFilter))detail.containerFilter=id;
            panel.UpdateDetails();panel.shop.CancelDisplayPrice();scroll.verticalNormalizedPosition=1;
            if(grid.gameObject.activeInHierarchy)grid.Refresh();dirty=true;if(SelectionChanged!=null)SelectionChanged();
        }
        string Caption(string id){return id=="warehouse"?"상자 1":"상자 "+id.Substring("warehouse_".Length);}
        void LateUpdate()
        {
            if(!ready||!dirty)return;dirty=false;var ids=panel.Model.WarehouseIds();if(index>=ids.Count)index=0;grid.containerId=ids[index];
            var c=panel.Model.GetContainer(grid.containerId);pageLabel.text=Caption(grid.containerId)+" · "+(c.rows*8)+" / 80칸 · 확장은 업그레이드 메뉴";
            tabs.Sync(ids,grid.containerId,Caption,Select);
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;panel.inventory.Loaded-=ResetPage;}}
    }
}
