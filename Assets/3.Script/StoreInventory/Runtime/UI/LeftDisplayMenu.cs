using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class LeftDisplayMenu:MonoBehaviour
    {
        public bool keepVisibleWhileDisplayOpen=true;
        public bool dockRight=true;
        public bool useArtwork=true;
        public ButtonArtworkCatalog buttonArtwork;
        [Tooltip("아이콘 Slot과 Viewport의 Prefab 배치를 사용합니다.")]
        public bool useAuthoredLayout;
        Rect authoredViewport;bool viewportCaptured;
        [Range(.4f,1.2f)]public float artworkScale=.64f;
        [Min(0)]public float peekWidth=22;
        public InventoryPanel panel;public DisplayUI displays;public FurnitureArtCatalog art;
        public RectTransform bounds,window,viewport,content;public ScrollRect scroll;public LeftDisplayButtonView template;
        public global::UnityEngine.UI.Image menuFrame;
        [HideInInspector]public global::UnityEngine.UI.Image edgeIcon; // Hide the obsolete hint in existing scenes.
        [Min(1)]public float revealWidth=64,menuWidth=96,buttonHeight=80,maximumHeight=720,slideSpeed=14;
        [Min(0)]public float gap=10,verticalMargin=100,hideDelay=.35f;
        [Min(.1f)]public float hoverOpenSeconds=1;
        public Color normalColor=new Color(.24f,.16f,.09f),selectedColor=new Color(1,.86f,.52f);
        readonly Dictionary<string,LeftDisplayButtonView> views=new Dictionary<string,LeftDisplayButtonView>();
        readonly Dictionary<string,Sprite> icons=new Dictionary<string,Sprite>();
        readonly HashSet<string> seen=new HashSet<string>();readonly List<string> removed=new List<string>();
        struct MenuDisplay{public string id,typeId;public int rank;}
        readonly List<MenuDisplay> ordered=new List<MenuDisplay>();
        static readonly System.Comparison<MenuDisplay> comparison=Compare;
        static int Compare(MenuDisplay a,MenuDisplay b){int order=a.rank.CompareTo(b.rank);return order!=0?order:string.CompareOrdinal(a.id,b.id);}
        bool ready,dirty=true,revealed,artworkReady;float outsideTime,lastHeight=-1,baseRightInset;
        void Start()
        {
            string error;if(panel==null||displays==null||art==null||bounds==null||window==null||viewport==null||content==null||scroll==null||template==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("진열장 가장자리 메뉴 참조를 확인하세요.",this);enabled=false;return;}
            baseRightInset=displays.rightInset;PrepareArtwork();
            panel.Model.Changed+=ModelChanged;displays.SelectionChanged+=MarkDirty;panel.inventory.Loaded+=Reload;ready=true;knownLayout=panel.Model.LayoutVersion;Sync();window.anchoredPosition=new Vector2(HiddenX,window.anchoredPosition.y);
        }
        public void PrepareArtwork()
        {
            if(edgeIcon!=null)edgeIcon.gameObject.SetActive(false);
            if(menuFrame!=null)menuFrame.gameObject.SetActive(useArtwork);
            if(!useArtwork||window==null||viewport==null){artworkReady=false;return;}
            Sprite frameSprite=menuFrame==null?null:menuFrame.sprite;
            if(frameSprite==null)frameSprite=Resources.Load<Sprite>("StoreInventory/DisplayMenu/frame");if(frameSprite==null)return;
            if(menuFrame==null)
            {
                var go=new GameObject("Display Menu Frame",typeof(RectTransform),typeof(CanvasRenderer),typeof(global::UnityEngine.UI.Image));
                go.transform.SetParent(window,false);go.transform.SetAsFirstSibling();menuFrame=go.GetComponent<global::UnityEngine.UI.Image>();
            }
            if(!useAuthoredLayout)
            {
                var frameRect=menuFrame.rectTransform;frameRect.anchorMin=Vector2.zero;frameRect.anchorMax=Vector2.one;frameRect.offsetMin=frameRect.offsetMax=Vector2.zero;
                menuFrame.color=Color.white;menuFrame.preserveAspect=false;menuFrame.raycastTarget=true;
            }
            menuFrame.sprite=frameSprite;
            var background=viewport.GetComponent<global::UnityEngine.UI.Image>();if(background!=null){background.sprite=null;background.color=Color.clear;background.raycastTarget=false;background.canvasRenderer.cullTransparentMesh=true;}
            artworkReady=true;
        }
        public void PrepareAuthoredLayout()
        {PrepareArtwork();ApplyLayout(0);useAuthoredLayout=true;viewportCaptured=false;}
        float ShownX{get{return dockRight?bounds.rect.width-menuWidth:0;}}
        float HiddenX{get{float peek=artworkReady?Mathf.Min(peekWidth,menuWidth):0;return dockRight?bounds.rect.width-peek:-menuWidth+peek;}}
        public void RefreshLayout(){dirty=true;lastHeight=-1;if(window!=null&&!revealed)window.anchoredPosition=new Vector2(HiddenX,window.anchoredPosition.y);}
        void MarkDirty(){dirty=true;}
        void Reload(){knownLayout=-1;MarkDirty();}
        long knownLayout=-1;
        void ModelChanged(){if(knownLayout==panel.Model.LayoutVersion)return;knownLayout=panel.Model.LayoutVersion;MarkDirty();}
        public void Toggle(string id){displays.Toggle(id);}
        public void OpenFromHover(string id){if(panel.drag.IsDragging||(panel.shop.counterTrade!=null&&panel.shop.counterTrade.IsItemDragging))displays.OpenWhileDragging(id);}
        Sprite Icon(string typeId)
        {
            Sprite sprite;if(icons.TryGetValue(typeId,out sprite))return sprite;
            sprite=Resources.Load<Sprite>("StoreInventory/DisplayMenu/"+typeId);
            if(sprite==null){var entry=art.Find(typeId);sprite=entry==null?null:entry.thumbnail;}
            icons.Add(typeId,sprite);return sprite;
        }
        void Sync()
        {
            dirty=false;seen.Clear();ordered.Clear();
            foreach(string id in panel.Model.DisplayIds())
            {
                var install=panel.Model.Installation(id);if(install==null)continue;
                ordered.Add(new MenuDisplay{id=id,typeId=install.typeId,rank=DisplayMenuOrder.Rank(install.typeId)});
            }
            ordered.Sort(comparison);ApplyLayout(ordered.Count);int n=0;
            float unit=buttonHeight*(artworkReady?artworkScale:1),spacing=gap*(artworkReady?artworkScale:1);
            foreach(var entry in ordered)
            {
                string id=entry.id;seen.Add(id);LeftDisplayButtonView v;
                if(!views.TryGetValue(id,out v))
                {v=Instantiate(template,content);v.menu=this;v.containerId=id;v.button.onClick.AddListener(v.Click);v.gameObject.SetActive(true);views.Add(id,v);}
                var states=artworkReady&&buttonArtwork!=null?buttonArtwork.Furniture(entry.typeId):null;
                v.icon.sprite=states!=null?states.normal:Icon(entry.typeId);v.icon.enabled=v.icon.sprite!=null;v.icon.preserveAspect=true;
                bool open=displays.IsOpen(id);
                v.label.text=panel.Model.DisplayTitle(id);v.label.gameObject.SetActive(!artworkReady);
                if(panel.SharedFont!=null)v.label.font=panel.SharedFont;
                v.background.sprite=null;v.background.color=artworkReady?Color.clear:open?selectedColor:normalColor;
                if(states!=null)ButtonArtworkCatalog.Apply(v.button,v.icon,states,open);
                else
                {
                    v.icon.overrideSprite=null;
                    v.button.targetGraphic=v.background;v.button.transition=artworkReady?Selectable.Transition.None:Selectable.Transition.ColorTint;
                    v.icon.color=artworkReady&&open?new Color(1,.86f,.52f):Color.white;
                }
                var rt=(RectTransform)v.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
                rt.anchoredPosition=new Vector2(artworkReady?0:4,-n++*(unit+spacing));rt.sizeDelta=new Vector2(artworkReady?viewport.rect.width:menuWidth-8,unit);
                if(artworkReady&&!useAuthoredLayout)
                {
                    var iconRect=v.icon.rectTransform;iconRect.anchorMin=iconRect.anchorMax=iconRect.pivot=new Vector2(.5f,.5f);
                    iconRect.anchoredPosition=Vector2.zero;float side=Mathf.Min(viewport.rect.width,unit);iconRect.sizeDelta=new Vector2(side,side);
                }
                v.transform.SetAsLastSibling();
            }
            removed.Clear();foreach(var v in views)if(!seen.Contains(v.Key))removed.Add(v.Key);
            foreach(string id in removed){views[id].gameObject.SetActive(false);Destroy(views[id].gameObject);views.Remove(id);}
        }
        void ApplyLayout(int count)
        {
            if(useAuthoredLayout&&!viewportCaptured){authoredViewport=AuthoredRowLayout.ReadViewport(viewport,window);viewportCaptured=true;}
            float scale=artworkReady?artworkScale:1;
            float contentHeight=Mathf.Max(0,count*(buttonHeight+gap)*scale-gap*scale),viewHeight;
            if(artworkReady)
            {
                float height=Mathf.Max(1,Mathf.Min(maximumHeight*artworkScale,bounds.rect.height-2*verticalMargin));
                menuWidth=height*menuFrame.sprite.rect.width/menuFrame.sprite.rect.height;
                window.sizeDelta=new Vector2(menuWidth,height);window.anchoredPosition=new Vector2(window.anchoredPosition.x,-(bounds.rect.height-height)*.5f);
                viewport.anchorMin=viewport.anchorMax=viewport.pivot=new Vector2(0,1);
                if(useAuthoredLayout)AuthoredRowLayout.FitViewport(viewport,authoredViewport,menuWidth,height);
                else{viewport.anchoredPosition=new Vector2(menuWidth*.30f,-height*.06f);viewport.sizeDelta=new Vector2(menuWidth*.58f,height*.88f);}viewHeight=viewport.rect.height;
            }
            else
            {
                var layout=CenteredMenuLayout.Calculate(count,buttonHeight,gap,bounds.rect.height,verticalMargin,maximumHeight);
                window.sizeDelta=new Vector2(menuWidth,layout.viewportHeight);window.anchoredPosition=new Vector2(window.anchoredPosition.x,-layout.top);
                viewport.sizeDelta=window.sizeDelta;viewHeight=layout.viewportHeight;
            }
            content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);
            content.sizeDelta=new Vector2(viewport.rect.width,contentHeight);scroll.horizontal=false;scroll.vertical=contentHeight>viewHeight;
            scroll.StopMovement();content.anchoredPosition=new Vector2(0,contentHeight<viewHeight?-(viewHeight-contentHeight)*.5f:Mathf.Clamp(content.anchoredPosition.y,0,contentHeight-viewHeight));
            // The new frame is wider than the old text buttons. Reserve its width
            // so the open inventory window does not sit underneath the menu.
            if(ready&&dockRight&&artworkReady)
            {
                float inset=Mathf.Max(baseRightInset,menuWidth+12);
                if(displays.rightInset!=inset){displays.rightInset=inset;if(displays.useViewport)displays.RefreshViewport(displays.viewport);}
            }
            lastHeight=bounds.rect.height;
        }
        void Update()
        {
            if(!ready)return;
            if(lastHeight!=bounds.rect.height)dirty=true;
            if(dirty)Sync();Vector2 screen,local;bool inside=false;
            if(InventoryKeys.TryPointerPosition(out screen)&&RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,screen,panel.EventCamera,out local))
            {
                float x=dockRight?bounds.rect.xMax-local.x:local.x-bounds.rect.xMin,y=bounds.rect.yMax-local.y;
                float top=-window.anchoredPosition.y;
                inside=x>=0&&x<=(revealed?menuWidth+20:Mathf.Max(revealWidth,peekWidth))&&y>=top-20&&y<=top+window.rect.height+20;
            }
            if(views.Count>0&&(inside||(keepVisibleWhileDisplayOpen&&displays.AnyOpen))){revealed=true;outsideTime=0;}else{outsideTime+=Time.unscaledDeltaTime;if(outsideTime>=hideDelay)revealed=false;}
            float xTarget=revealed?ShownX:HiddenX;
            float nextX=DrawerGeometry.Step(window.anchoredPosition.x,xTarget,slideSpeed,Time.unscaledDeltaTime);
            if(window.anchoredPosition.x!=nextX)window.anchoredPosition=new Vector2(nextX,window.anchoredPosition.y);
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=ModelChanged;displays.SelectionChanged-=MarkDirty;panel.inventory.Loaded-=Reload;}}
        void OnValidate(){dirty=true;}
    }
}
