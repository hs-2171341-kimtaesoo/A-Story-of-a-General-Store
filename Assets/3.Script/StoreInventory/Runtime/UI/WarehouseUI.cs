using System;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class WarehouseUI : MonoBehaviour
    {
        public InventoryPanel panel;public InventoryGridView grid;
        [Min(.05f)] public float hoverSwitchSeconds=1;
        public Text pageLabel;public ContainerTabBar tabs;
        public event Action SelectionChanged;
        public string SelectedId {get{return grid.containerId;}}
        bool ready,dirty;int index;
        void Start()
        {
            string error;if(panel==null||grid==null||tabs==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("WarehouseUI 참조를 확인하세요.",this);enabled=false;return;}
            if(pageLabel!=null)pageLabel.text=""; // 이전 씬의 선택 사항. Text 오브젝트는 삭제해도 됩니다.
            panel.Model.Changed+=ModelChanged;panel.inventory.Loaded+=ResetPage;ready=true;knownLayout=panel.Model.LayoutVersion;dirty=true;
        }
        void MarkDirty(){dirty=true;}
        long knownLayout=-1;
        void ModelChanged(){if(knownLayout==panel.Model.LayoutVersion)return;knownLayout=panel.Model.LayoutVersion;MarkDirty();}
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
            panel.UpdateDetails();panel.shop.CancelDisplayPrice();
            if(grid.gameObject.activeInHierarchy)grid.Refresh();dirty=true;if(SelectionChanged!=null)SelectionChanged();
        }
        string Caption(string id){return id=="warehouse"?"상자 1":"상자 "+id.Substring("warehouse_".Length);}
        void LateUpdate()
        {
            if(!ready||!dirty)return;dirty=false;var ids=panel.Model.WarehouseIds();if(index>=ids.Count)index=0;grid.containerId=ids[index];
            tabs.Sync(ids,grid.containerId,Caption,Select);
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=ModelChanged;panel.inventory.Loaded-=ResetPage;}}
    }
}
