using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // 배경 가구 클릭 영역. 구매할 때 위치를 고르는 시스템과는 별개인 씬 배치입니다.
    public sealed class SceneFurnitureHotspot:MonoBehaviour,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler
    {
        public InventoryPanel panel;public DisplayUI displays;public string typeId;[Min(0)]public int ownedIndex;
        public global::UnityEngine.UI.Image furnitureImage,highlightImage;public Text label;public bool showUnavailable;
        string boundId;bool ready,dirty,hover;
        void Start()
        {
            string error;if(panel==null||displays==null||furnitureImage==null||highlightImage==null||label==null||!panel.inventory.TryInitialize(out error)){enabled=false;return;}
            panel.Model.Changed+=MarkDirty;panel.inventory.Loaded+=MarkDirty;ready=true;dirty=true;
        }
        void MarkDirty(){dirty=true;}
        void Refresh()
        {
            boundId=panel.Model.OwnedDisplayId(typeId,ownedIndex);GetComponent<global::UnityEngine.UI.Image>().raycastTarget=boundId!=null&&!panel.inventory.Sales.CustomerPresent;
            var t=panel.Model.Displays.Type(typeId);label.text=(t==null?typeId:t.title)+(boundId==null?" · 미구매":"");
            label.gameObject.SetActive(boundId!=null||showUnavailable);furnitureImage.enabled=boundId!=null;highlightImage.enabled=boundId!=null&&hover&&!panel.inventory.Sales.CustomerPresent;
        }
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&boundId!=null&&!panel.drag.IsDragging&&!panel.IsWindowDragging&&!panel.inventory.Sales.CustomerPresent)displays.Open(boundId);}
        public void OnPointerEnter(PointerEventData e){hover=true;if(ready)Refresh();}
        public void OnPointerExit(PointerEventData e){hover=false;if(highlightImage!=null)highlightImage.enabled=false;}
        void LateUpdate(){if(ready&&dirty){dirty=false;Refresh();}}
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;panel.inventory.Loaded-=MarkDirty;}}
    }
}
