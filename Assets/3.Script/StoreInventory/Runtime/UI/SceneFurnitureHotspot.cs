using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    public sealed class SceneFurnitureHotspot:MonoBehaviour,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler,ICanvasRaycastFilter
    {
        public InventoryPanel panel;public DisplayUI displays;public string typeId;[Min(0)]public int ownedIndex;
        public global::UnityEngine.UI.Image furnitureImage;public Sprite hoverSprite;
        public float[] furnitureAlphaBounds,outlineAlphaBounds;
        public RectTransform outlineFrame;
        [Range(0,8)]public float outlineThickness; // 0은 PNG 원래 두께, 양수는 추가 두께(UI 단위).
        public FurnitureHoverPresenter hoverPresenter;public SceneCounterOcclusion counter;
        global::UnityEngine.UI.Image hitImage;string boundId;bool ready,hover;
        bool InputBlocked{get{return panel.IsWindowDragging||(panel.drag!=null&&panel.drag.IsDragging);}}
        void Start()
        {
            string error;hitImage=furnitureImage;
            if(panel==null||displays==null||furnitureImage==null||hitImage==null||!panel.inventory.TryInitialize(out error)){enabled=false;return;}
            panel.Model.Changed+=ModelChanged;panel.inventory.Loaded+=Refresh;panel.inventory.Sales.Changed+=Refresh;
            if(panel.drag!=null)panel.drag.DraggingChanged+=OnDraggingChanged;panel.WindowDragStateChanged+=OnDraggingChanged;
            ready=true;knownLayout=panel.Model.LayoutVersion;Refresh();
        }
        long knownLayout=-1;void ModelChanged(){if(knownLayout==panel.Model.LayoutVersion)return;knownLayout=panel.Model.LayoutVersion;Refresh();}
        void OnDraggingChanged(bool held){hover=false;Refresh();}
        void Refresh()
        {
            if(!ready)return;boundId=panel.Model.OwnedDisplayId(typeId,ownedIndex);
            hitImage.raycastTarget=boundId!=null&&!InputBlocked;furnitureImage.enabled=boundId!=null;
            if(hoverPresenter!=null){if(boundId!=null&&hover&&!InputBlocked)hoverPresenter.Show(this);else hoverPresenter.Hide(this);}
        }
        public bool IsRaycastLocationValid(Vector2 point,Camera camera){return counter==null||!counter.Blocks(point,camera);}
        public void OnPointerClick(PointerEventData e){if(ready&&e.button==PointerEventData.InputButton.Left&&boundId!=null&&!InputBlocked)displays.Toggle(boundId);}
        public void OnPointerEnter(PointerEventData e){if(!ready)return;hover=!InputBlocked;Refresh();}
        public void OnPointerExit(PointerEventData e){hover=false;if(hoverPresenter!=null)hoverPresenter.Hide(this);}
        [ContextMenu("Preview Outline")]public void PreviewOutline(){if(hoverPresenter!=null)hoverPresenter.Show(this);}
        [ContextMenu("Hide Outline")]public void HideOutline(){if(hoverPresenter!=null)hoverPresenter.Hide(this);}
        void OnDisable(){hover=false;if(hoverPresenter!=null)hoverPresenter.Hide(this);}
        void OnDestroy()
        {
            if(!ready||panel==null)return;
            if(panel.drag!=null)panel.drag.DraggingChanged-=OnDraggingChanged;panel.WindowDragStateChanged-=OnDraggingChanged;
            if(panel.inventory!=null&&panel.inventory.Model!=null){panel.Model.Changed-=ModelChanged;panel.inventory.Loaded-=Refresh;panel.inventory.Sales.Changed-=Refresh;}
        }
    }
}
