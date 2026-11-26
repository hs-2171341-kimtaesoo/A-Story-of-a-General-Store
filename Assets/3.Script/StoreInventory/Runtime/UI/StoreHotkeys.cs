using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    // Shortcuts use the same coordinator as mouse menu clicks, including pending transitions.
    public sealed class StoreHotkeys:MonoBehaviour
    {
        public InventoryPanel panel;public RightSideMenu menu;public WarehouseDrawer drawer;
        public static string MenuId(KeyCode key)
        {
            switch(key)
            {
                case KeyCode.Q:return "displays";
                case KeyCode.W:return "furniture";
                case KeyCode.E:return "storage";
                case KeyCode.R:return "suppliers";
                case KeyCode.A:return "workshops";
                case KeyCode.S:return "temporary_storage";
                default:return null;
            }
        }
        public static bool IsTyping(GameObject selected)
        {
            if(selected==null||!selected.activeInHierarchy)return false;
            var input=selected.GetComponentInParent<InputField>();
            if(input!=null&&input.isFocused)return true;
            // Optional TMP support without adding a TextMeshPro package dependency.
            for(var node=selected.transform;node!=null;node=node.parent)
            {
                var tmp=node.GetComponent("TMP_InputField");if(tmp==null)continue;
                var focused=tmp.GetType().GetProperty("isFocused");
                return focused!=null&&focused.GetValue(tmp,null) is bool value&&value;
            }
            return false;
        }
        public bool HandleKey(KeyCode key)
        {
            if(panel==null||menu==null||drawer==null||panel.drag==null||panel.shop==null)return false;
            if(panel.IsWindowDragging||panel.drag.IsDragging||panel.shop.HasPendingPlacement
                ||(panel.shop.counterTrade!=null&&panel.shop.counterTrade.IsItemDragging)
                ||IsTyping(EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject))return false;
            if(key==KeyCode.Space){drawer.Toggle();return true;}
            string id=MenuId(key);if(id==null)return false;
            bool handled=menu.TryToggle(id);
            if(!handled&&id=="workshops")panel.ShowMessage("제작 의뢰는 공방 해금 후 사용할 수 있습니다.");
            return handled;
        }
        void Update()
        {
            if(!Application.isFocused||InventoryKeys.ShortcutModifierHeld())return;
            var key=InventoryKeys.PressedShortcut();if(key!=KeyCode.None)HandleKey(key);
        }
    }
}
