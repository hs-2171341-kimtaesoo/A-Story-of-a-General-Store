using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class LeftDisplayButtonView:MonoBehaviour
    {
        public Button button;public global::UnityEngine.UI.Image icon,background;public Text label;
        [HideInInspector]public LeftDisplayMenu menu;[HideInInspector]public string containerId;
        public void Click(){if(menu!=null)menu.Toggle(containerId);}
    }
}
