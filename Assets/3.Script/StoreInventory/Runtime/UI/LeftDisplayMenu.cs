using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class LeftDisplayMenu:MonoBehaviour
    {
        public InventoryPanel panel;public DisplayUI displays;public FurnitureArtCatalog art;
        public RectTransform bounds,window,viewport,content;public ScrollRect scroll;public LeftDisplayButtonView template;
        public global::UnityEngine.UI.Image edgeIcon;
        [Min(1)]public float revealWidth=64,menuWidth=96,buttonHeight=80,maximumHeight=720,slideSpeed=14;
        [Min(0)]public float gap=10,verticalMargin=100,hideDelay=.35f;
        [Min(.1f)]public float hoverOpenSeconds=2;
        public Color normalColor=new Color(.24f,.16f,.09f),selectedColor=new Color(.65f,.4f,.12f);
        readonly Dictionary<string,LeftDisplayButtonView> views=new Dictionary<string,LeftDisplayButtonView>();
        readonly HashSet<string> seen=new HashSet<string>();readonly List<string> removed=new List<string>();
        bool ready,dirty=true,revealed;float outsideTime,lastHeight=-1;
        void Start()
        {
            string error;if(panel==null||displays==null||art==null||bounds==null||window==null||viewport==null||content==null||scroll==null||template==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("왼쪽 진열 메뉴 참조를 확인하세요.",this);enabled=false;return;}
            panel.Model.Changed+=MarkDirty;displays.SelectionChanged+=MarkDirty;ready=true;Sync();window.anchoredPosition=new Vector2(-menuWidth,window.anchoredPosition.y);
        }
        void MarkDirty(){dirty=true;}
        public void Toggle(string id){displays.Toggle(id);}
        public void OpenFromHover(string id){if(panel.drag.IsDragging)displays.OpenWhileDragging(id);}
        void Sync()
        {
            dirty=false;seen.Clear();var ids=panel.Model.DisplayIds();int n=0;
            foreach(string id in ids)
            {
                seen.Add(id);LeftDisplayButtonView v;
                if(!views.TryGetValue(id,out v))
                {v=Instantiate(template,content);v.menu=this;v.containerId=id;v.button.onClick.AddListener(v.Click);v.gameObject.SetActive(true);views.Add(id,v);}
                var install=panel.Model.Installation(id);var image=art.Find(install.typeId);
                v.icon.sprite=image==null?null:image.thumbnail;v.icon.enabled=v.icon.sprite!=null;
                v.label.text=panel.Model.DisplayTitle(id);v.background.color=displays.IsOpen(id)?selectedColor:normalColor;
                var rt=(RectTransform)v.transform;rt.anchoredPosition=new Vector2(4,-n++*(buttonHeight+gap));rt.sizeDelta=new Vector2(menuWidth-8,buttonHeight);
            }
            removed.Clear();foreach(var v in views)if(!seen.Contains(v.Key))removed.Add(v.Key);
            foreach(string id in removed){Destroy(views[id].gameObject);views.Remove(id);}
            ApplyLayout(ids.Count);
        }
        void ApplyLayout(int count)
        {
            var layout=CenteredMenuLayout.Calculate(count,buttonHeight,gap,bounds.rect.height,verticalMargin,maximumHeight);
            window.sizeDelta=new Vector2(menuWidth,layout.viewportHeight);window.anchoredPosition=new Vector2(window.anchoredPosition.x,-layout.top);
            viewport.sizeDelta=window.sizeDelta;content.sizeDelta=new Vector2(menuWidth,layout.contentHeight);scroll.vertical=layout.scrolls;
            if(!layout.scrolls){scroll.StopMovement();content.anchoredPosition=Vector2.zero;}
            lastHeight=bounds.rect.height;
        }
        void Update()
        {
            if(!ready)return;
            if(lastHeight!=bounds.rect.height)dirty=true;
            if(dirty)Sync();Vector2 screen,local;bool inside=false;
            if(InventoryKeys.TryPointerPosition(out screen)&&RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,screen,panel.EventCamera,out local))
            {
                float x=local.x-bounds.rect.xMin,y=bounds.rect.yMax-local.y;
                inside=x>=0&&x<=(revealed?menuWidth+20:revealWidth)&&y>=0&&y<=bounds.rect.height;
            }
            if(panel.shop.customerInteraction!=null&&panel.shop.customerInteraction.Engaged)inside=false;
            if(inside){revealed=true;outsideTime=0;}else{outsideTime+=Time.unscaledDeltaTime;if(outsideTime>=hideDelay)revealed=false;}
            float xTarget=revealed?0:-menuWidth;
            window.anchoredPosition=new Vector2(DrawerGeometry.Step(window.anchoredPosition.x,xTarget,slideSpeed,Time.unscaledDeltaTime),window.anchoredPosition.y);
            if(edgeIcon!=null)edgeIcon.enabled=!revealed&&views.Count>0;
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;displays.SelectionChanged-=MarkDirty;}}
        void OnValidate(){dirty=true;}
    }
}
