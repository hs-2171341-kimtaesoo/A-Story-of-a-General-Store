using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 한 창은 하나의 containerId만 참조합니다. 재고 복사본을 만들지 않습니다.
    public sealed class DisplayWindowView:MonoBehaviour
    {
        public InventoryGridView grid;public InventoryDetailsView details;public DisplayWindowAppearance appearance;
        public Text title,status;public InventoryPanel panel;public DisplayUI owner;
        public string ContainerId{get{return grid.containerId;}}
        public void Bind(InventoryPanel p,DisplayUI manager,string id)
        {panel=p;owner=manager;grid.containerId=id;details.containerFilter=id;details.compactDisplay=true;if(panel.koreanFont!=null)foreach(var text in GetComponentsInChildren<Text>(true))text.font=panel.koreanFont;panel.RegisterWindowGrid(grid,details);Refresh();}
        public void Refresh()
        {
            var s=panel.Model.SummarizeDisplay(ContainerId);if(s==null)return;title.text=s.title;status.fontSize=16;
            status.text="전체"+(s.columns*s.rows)+"칸 · 남은"+(s.columns*s.rows-s.usedCells)+"칸"+(s.columns<=4?"\n":" · ")+"진열"+s.itemCount+"개";
            appearance.Apply(panel.Model.Installation(ContainerId).typeId,s.columns,s.rows);
        }
        public void Open(){gameObject.SetActive(true);transform.SetAsLastSibling();grid.Refresh();Refresh();}
        void OnDisable(){if(panel!=null)panel.ClearSelection(ContainerId);}
        public void Close(){panel.ClearSelection(ContainerId);panel.drag.Cancel(false);panel.shop.CancelDisplayPrice();gameObject.SetActive(false);}
    }
}
