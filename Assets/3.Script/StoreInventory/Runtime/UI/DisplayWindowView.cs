using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 한 창은 하나의 containerId만 참조합니다. 재고 복사본을 만들지 않습니다.
    public sealed class DisplayWindowView:MonoBehaviour
    {
        public InventoryGridView grid;public InventoryGridView[] extraGrids;public InventoryDetailsView details;public DisplayWindowAppearance appearance;
        public Text title,status;public InventoryPanel panel;public DisplayUI owner;
        public string ContainerId{get{return grid.containerId;}}
        public void Bind(InventoryPanel p,DisplayUI manager,string id)
        {panel=p;owner=manager;grid.containerId=id;if(details!=null){details.containerFilter=id;details.compactDisplay=true;}if(panel.koreanFont!=null)foreach(var text in GetComponentsInChildren<Text>(true))text.font=panel.koreanFont;panel.RegisterWindowGrid(grid,details);if(extraGrids!=null)foreach(var extra in extraGrids){if(extra==null)continue;extra.containerId=id;panel.RegisterWindowGrid(extra,null);}Refresh();}
        public void Refresh()
        {
            var install=panel.Model.Installation(ContainerId);if(install==null)return;
            var type=panel.Model.Displays.Type(install.typeId);if(type==null)return;
            if(title!=null)title.text=panel.Model.DisplayTitle(ContainerId);if(status!=null)status.text="";
            // 용량/품목 묶음 정보는 관리창에서만 계산합니다. 개별 창의 갱신에는 종류 크기만 필요합니다.
            int columns=string.IsNullOrEmpty(install.locationId)?type.columns:panel.Model.Displays.Columns(install.typeId,install.locationId);
            int rows=string.IsNullOrEmpty(install.locationId)?type.rows:panel.Model.Displays.Rows(install.typeId,install.locationId);
            appearance.Apply(install.typeId,columns,rows);
        }
        public void Open(){gameObject.SetActive(true);transform.SetAsLastSibling();grid.Refresh();if(extraGrids!=null)foreach(var extra in extraGrids)if(extra!=null)extra.Refresh();Refresh();}
        public void UnregisterGrids(){panel.UnregisterWindowGrid(grid,details);if(extraGrids!=null)foreach(var extra in extraGrids)if(extra!=null)panel.UnregisterWindowGrid(extra,null);}
        public void Close(){if(owner!=null){owner.Close(ContainerId);return;}gameObject.SetActive(false);}
    }
}
