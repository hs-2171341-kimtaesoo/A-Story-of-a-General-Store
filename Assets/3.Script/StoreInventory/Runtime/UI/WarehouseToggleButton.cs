using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    // 위치를 강제 배치하지 않습니다. 루트 자식으로 두어 Drawer Body가 꺼져도 사용할 수 있습니다.
    public sealed class WarehouseToggleButton:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public WarehouseDrawer drawer;public Button button;public global::UnityEngine.UI.Image image;public Text label;
        public Sprite openedSprite,closedSprite;
        bool forwarding,pressed,spriteCaptured;Sprite originalSprite;
        void OnEnable()
        {
            if(image!=null&&!spriteCaptured){originalSprite=image.sprite;spriteCaptured=true;}
            if(drawer!=null)drawer.StateChanged+=Render;
            if(button!=null)button.onClick.AddListener(Toggle);
            if(drawer!=null)Render(drawer.IsOpen);
        }
        void Start(){if(drawer!=null)Render(drawer.IsOpen);}
        void Toggle(){if(drawer!=null)drawer.Toggle();}
        void Render(bool opened)
        {
            if(image!=null)image.sprite=(opened?openedSprite:closedSprite)??originalSprite;
            if(label!=null)label.text=""; // 이전 씬 호환. 라벨 자체를 삭제해도 됩니다.
        }
        public void OnPointerDown(PointerEventData e){if(drawer!=null&&e.button==PointerEventData.InputButton.Left){pressed=true;drawer.PressHandle(e);}}
        public void OnPointerUp(PointerEventData e){if(drawer!=null&&pressed)drawer.OnPointerUp(e);pressed=false;}
        public void OnBeginDrag(PointerEventData e)
        {
            forwarding=drawer!=null&&drawer.BeginHandleDrag(e);
            // 버튼을 드래그한 뒤 놓을 때 클릭까지 발생해 다시 접히는 것을 막습니다.
            if(forwarding)e.eligibleForClick=false;
        }
        public void OnDrag(PointerEventData e){if(forwarding&&drawer!=null)drawer.OnDrag(e);}
        public void OnEndDrag(PointerEventData e){if(forwarding&&drawer!=null)drawer.OnEndDrag(e);forwarding=false;}
        void OnDisable()
        {
            if(drawer!=null){if(pressed||forwarding)drawer.ReleaseHandle();drawer.StateChanged-=Render;}
            if(button!=null)button.onClick.RemoveListener(Toggle);
            pressed=false;forwarding=false;
        }
    }
}
