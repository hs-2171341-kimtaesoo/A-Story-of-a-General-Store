using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class ShopMenuEntry
    {public string id,title;public Sprite icon;public bool unlocked=true,visible=true;public GameObject window;public UnityEvent onOpen=new UnityEvent();}
    public sealed class RightSideMenu:MonoBehaviour
    {
        public InventoryPanel panel;public RectTransform content;public RightMenuButtonView template;public List<ShopMenuEntry> entries=new List<ShopMenuEntry>();
        public Color normalColor=new Color(.26f,.18f,.1f),selectedColor=new Color(.7f,.45f,.1f);
        public bool useArtwork=true;
        [Tooltip("아이콘 Slot과 Viewport의 Prefab 배치를 사용합니다.")]
        public bool useAuthoredLayout;
        Rect authoredViewport;bool viewportCaptured;
        public global::UnityEngine.UI.Image menuFrame;
        [Range(.4f,1.2f)]public float artworkScale=.64f;
        [Min(0)]public float peekWidth=26;
        [Min(1)]public float buttonWidth=82,buttonHeight=86;
        [Min(0)]public float buttonGap=8,screenVerticalMargin=100;
        [Min(1)]public float maximumViewportHeight=700;
        public bool autoHide=true,keepVisibleWhileWindowOpen=true;
        [Min(1)]public float revealWidth=56,slideSpeed=14;
        [Min(0)]public float hideDelay=.35f;
        [Tooltip("관리창과 오른쪽 진열창의 등장/퇴장 공통 시간입니다.")]
        [Min(.05f)]public float windowSlideSeconds=.3f;
        bool revealed,artworkReady;float outsideTime;
        ScrollRect scroll;int layoutCount=-1;float layoutScreenHeight,layoutButtonHeight,layoutGap,layoutMaximum,layoutMargin,layoutScale;
        readonly Dictionary<string,Sprite> icons=new Dictionary<string,Sprite>();
        readonly List<ShopMenuEntry> visible=new List<ShopMenuEntry>();
        readonly Dictionary<string,RightMenuButtonView> views=new Dictionary<string,RightMenuButtonView>();bool dirty=true;string selected;
        string pendingWindowId;bool switching;GameObject closingWindow;ManagementWindowMotion closingMotion;
        public bool IsSwitching{get{return switching;}}
        float Width{get{return ((RectTransform)transform).rect.width;}}
        public float ReservedWidth{get{return artworkReady?Width+20:0;}}
        float HiddenX{get{return artworkReady?-Width+Mathf.Min(peekWidth,Width):-buttonWidth-8;}}
        void Start()
        {
            if(panel==null||content==null||template==null||template.button==null||template.icon==null||template.background==null||template.label==null)
            {Debug.LogError("관리 메뉴 템플릿/참조를 확인하세요.",this);enabled=false;return;}
            scroll=DisplayWindowAppearance.FindScrollInParents(content);
            var ids=new HashSet<string>();foreach(var e in entries)if(e==null||string.IsNullOrWhiteSpace(e.id)||!ids.Add(e.id))
            {Debug.LogError("메뉴 ID가 비어 있거나 중복되었습니다.",this);enabled=false;return;}
            PrepareArtwork();Render();
            if(autoHide){var rect=(RectTransform)transform;rect.anchoredPosition=new Vector2(HiddenX,rect.anchoredPosition.y);}
        }
        public void PrepareArtwork()
        {
            if(scroll==null&&content!=null)scroll=DisplayWindowAppearance.FindScrollInParents(content);
            if(menuFrame!=null)menuFrame.gameObject.SetActive(useArtwork);
            if(!useArtwork||scroll==null){artworkReady=false;return;}
            Sprite sprite=menuFrame==null?null:menuFrame.sprite;if(sprite==null)sprite=Resources.Load<Sprite>("StoreInventory/ManagementMenu/frame");
            if(sprite==null)return;
            if(menuFrame==null)
            {
                var go=new GameObject("Management Menu Frame",typeof(RectTransform),typeof(CanvasRenderer),typeof(global::UnityEngine.UI.Image));
                go.transform.SetParent(transform,false);go.transform.SetAsFirstSibling();menuFrame=go.GetComponent<global::UnityEngine.UI.Image>();
            }
            if(!useAuthoredLayout)
            {
                var rect=menuFrame.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                menuFrame.color=Color.white;menuFrame.preserveAspect=false;menuFrame.raycastTarget=true;
            }
            menuFrame.sprite=sprite;
            var background=scroll.viewport.GetComponent<global::UnityEngine.UI.Image>();if(background!=null){background.sprite=null;background.color=new Color(0,0,0,.001f);}
            artworkReady=true;dirty=true;layoutCount=-1;
        }
        // Initial geometry only; no menu rows or live listeners are created for the prefab.
        public void PrepareAuthoredLayout()
        {PrepareArtwork();ApplyCenteredLayout(entries.Count);useAuthoredLayout=true;viewportCaptured=false;}
        public void RefreshLayout()
        {
            dirty=true;layoutCount=-1;
            if(!Application.isPlaying){var rect=(RectTransform)transform;rect.anchoredPosition=new Vector2(autoHide?HiddenX:0,rect.anchoredPosition.y);}
        }
        void Slide()
        {
            var rect=(RectTransform)transform;var bounds=rect.parent as RectTransform;if(bounds==null)return;
            bool open=false;if(keepVisibleWhileWindowOpen)foreach(var entry in entries)if(entry.window!=null&&entry.window.activeSelf){open=true;break;}
            Vector2 screen,point;bool inside=false;
            if(InventoryKeys.TryPointerPosition(out screen)&&RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,screen,panel.EventCamera,out point))
            {
                float x=point.x-bounds.rect.xMin,y=bounds.rect.yMax-point.y,top=-rect.anchoredPosition.y;
                inside=x>=0&&x<=(revealed?Width+28:Mathf.Max(revealWidth,peekWidth))&&y>=top-20&&y<=top+rect.rect.height+20;
            }
            if(!autoHide||inside||open){revealed=true;outsideTime=0;}else{outsideTime+=Time.unscaledDeltaTime;if(outsideTime>=hideDelay)revealed=false;}
            float xTarget=revealed?0:HiddenX;float xNext=DrawerGeometry.Step(rect.anchoredPosition.x,xTarget,slideSpeed,Time.unscaledDeltaTime);
            if(rect.anchoredPosition.x!=xNext)rect.anchoredPosition=new Vector2(xNext,rect.anchoredPosition.y);
        }
        public void SetUnlocked(string id,bool value)
        {
            foreach(var e in entries)if(e.id==id&&e.unlocked!=value)
            {
                e.unlocked=value;dirty=true;
                if(!value)
                {
                    if(pendingWindowId==id){pendingWindowId=null;selected=null;}
                    if(e.window!=null&&e.window.activeSelf&&!switching){selected=null;ContinueWindowSwitch();}
                }
            }
        }
        public void SetVisible(string id,bool value)
        {foreach(var entry in entries)if(entry.id==id&&entry.visible!=value){entry.visible=value;dirty=true;}}
        public void AddEntry(ShopMenuEntry entry){if(entry==null||string.IsNullOrWhiteSpace(entry.id))throw new ArgumentException("메뉴 ID가 필요합니다.");foreach(var e in entries)if(e.id==entry.id)throw new ArgumentException("중복 메뉴 ID");entries.Add(entry);dirty=true;}
        public void Toggle(string id)
        {TryToggle(id);}
        public bool TryToggle(string id)
        {
            if(panel.IsWindowDragging)return false;
            ShopMenuEntry chosen=null;foreach(var e in entries)if(e.id==id&&e.unlocked)chosen=e;if(chosen==null)return false;
            if(!CanOpen(chosen)&&(chosen.window==null||!chosen.window.activeSelf))return false;
            panel.drag.Cancel(false);panel.shop.CancelDisplayPrice();
            bool closing=switching?pendingWindowId==id:chosen.window!=null&&chosen.window.activeSelf;
            pendingWindowId=closing?null:id;selected=pendingWindowId;dirty=true;
            if(!switching)ContinueWindowSwitch();
            return true;
        }
        bool CanOpen(ShopMenuEntry entry)
        {return entry.id!="temporary_storage"||(panel.inventory!=null&&panel.inventory.Model!=null&&panel.Model.TemporaryItemCount>0);}
        public bool CloseWindow(GameObject window)
        {
            foreach(var entry in entries)if(entry.window==window)
            {
                pendingWindowId=selected=null;dirty=true;
                if(!switching)ContinueWindowSwitch();return true;
            }
            return false;
        }
        ManagementWindowMotion WindowMotion(GameObject window)
        {
            var motion=window.GetComponent<ManagementWindowMotion>();
            if(motion==null||motion.frame==null)
            {
                var frame=window.transform.Find("Panel Frame") as RectTransform;
                motion=ManagementWindowMotion.Ensure(window,frame!=null?frame:(RectTransform)window.transform);
            }
            motion.duration=windowSlideSeconds;return motion;
        }
        void ContinueWindowSwitch()
        {
            if(closingWindow!=null)
            {
                if(closingWindow.activeSelf&&closingMotion!=null&&!closingMotion.IsClosed)return;
                closingWindow.SetActive(false);closingWindow=null;closingMotion=null;
            }
            // Finish every outgoing window before invoking the final requested menu.
            foreach(var entry in entries)if(entry.window!=null&&entry.window.activeSelf)
            {
                closingWindow=entry.window;closingMotion=WindowMotion(closingWindow);
                switching=true;closingMotion.BeginClose();return;
            }
            switching=false;string id=pendingWindowId;pendingWindowId=null;selected=null;
            foreach(var entry in entries)if(entry.id==id&&entry.unlocked&&CanOpen(entry))
            {if(entry.window!=null)WindowMotion(entry.window);entry.onOpen.Invoke();if(entry.window==null||entry.window.activeSelf)selected=id;break;}
            dirty=true;
        }
        Sprite Icon(ShopMenuEntry entry)
        {
            Sprite sprite;if(icons.TryGetValue(entry.id,out sprite))return sprite;
            sprite=entry.icon;if(sprite==null)sprite=Resources.Load<Sprite>("StoreInventory/ManagementMenu/"+entry.id);icons.Add(entry.id,sprite);return sprite;
        }
        void LateUpdate()
        {
            if(switching)ContinueWindowSwitch();
            if(!switching&&selected!=null)foreach(var e in entries)if(e.id==selected&&e.window!=null&&!e.window.activeSelf){selected=null;dirty=true;break;}
            var parent=transform.parent as RectTransform;if(parent!=null&&parent.rect.height!=layoutScreenHeight)dirty=true;
            if(dirty)Render();Slide();
        }
        void Render()
        {
            dirty=false;visible.Clear();
            for(int pass=0;pass<2;pass++)foreach(var e in entries)if(e.unlocked&&e.visible&&IsLast(e.id)==(pass==1))visible.Add(e);
            ApplyCenteredLayout(visible.Count);int n=0;float scale=artworkReady?artworkScale:1;
            float unit=buttonHeight*scale,spacing=buttonGap*scale;
            foreach(var v in views.Values)v.gameObject.SetActive(false);
            foreach(var e in visible)
            {
                RightMenuButtonView v;if(!views.TryGetValue(e.id,out v)){v=Instantiate(template,content);string key=e.id;v.button.onClick.AddListener(()=>Toggle(key));views.Add(key,v);}
                v.gameObject.SetActive(true);v.label.text=e.title;v.icon.sprite=artworkReady?Icon(e):e.icon;v.icon.enabled=v.icon.sprite!=null;v.icon.preserveAspect=true;
                if(panel.SharedFont!=null)v.label.font=panel.SharedFont;
                v.label.gameObject.SetActive(!artworkReady||v.icon.sprite==null);
                v.background.color=artworkReady?Color.clear:e.id==selected?selectedColor:normalColor;
                v.button.transition=artworkReady?Selectable.Transition.None:Selectable.Transition.ColorTint;
                v.icon.color=artworkReady&&e.id==selected?new Color(1,.86f,.52f):Color.white;
                var r=(RectTransform)v.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(0,-n++*(unit+spacing));r.sizeDelta=new Vector2(content.rect.width,unit);
                if(artworkReady&&!useAuthoredLayout)
                {
                    var iconRect=v.icon.rectTransform;iconRect.anchorMin=iconRect.anchorMax=iconRect.pivot=new Vector2(.5f,.5f);iconRect.anchoredPosition=Vector2.zero;
                    float side=Mathf.Min(content.rect.width,unit);iconRect.sizeDelta=new Vector2(side,side);
                }
                v.transform.SetAsLastSibling();
            }
        }
        public static bool IsLast(string id){return id=="temporary_storage";}
        void ApplyCenteredLayout(int count)
        {
            var root=(RectTransform)transform;var bounds=root.parent as RectTransform;if(bounds==null||scroll==null)return;
            if(useAuthoredLayout&&!viewportCaptured){authoredViewport=AuthoredRowLayout.ReadViewport(scroll.viewport,root);viewportCaptured=true;}
            float oldWidth=root.rect.width;
            float height=bounds.rect.height;
            bool changed=layoutCount!=count||layoutScreenHeight!=height||layoutButtonHeight!=buttonHeight||layoutGap!=buttonGap||layoutMaximum!=maximumViewportHeight||layoutMargin!=screenVerticalMargin||layoutScale!=artworkScale;
            if(!changed)return;
            bool wasScrolling=scroll.vertical;float oldScroll=content.anchoredPosition.y,viewHeight,contentHeight;
            var view=scroll.viewport;view.anchorMin=view.anchorMax=view.pivot=new Vector2(0,1);
            if(artworkReady)
            {
                float frameHeight=Mathf.Max(1,Mathf.Min(maximumViewportHeight*artworkScale,height-2*screenVerticalMargin));
                float frameWidth=frameHeight*menuFrame.sprite.rect.width/menuFrame.sprite.rect.height;
                root.sizeDelta=new Vector2(frameWidth,frameHeight);root.anchoredPosition=new Vector2(root.anchoredPosition.x,-(height-frameHeight)*.5f);
                if(useAuthoredLayout)AuthoredRowLayout.FitViewport(view,authoredViewport,frameWidth,frameHeight);
                else{view.anchoredPosition=new Vector2(frameWidth*.14f,-frameHeight*.07f);view.sizeDelta=new Vector2(frameWidth*.58f,frameHeight*.86f);}
                viewHeight=view.rect.height;contentHeight=Mathf.Max(0,count*(buttonHeight+buttonGap)*artworkScale-buttonGap*artworkScale);
            }
            else
            {
                var p=CenteredMenuLayout.Calculate(count,buttonHeight,buttonGap,height,screenVerticalMargin,maximumViewportHeight);
                root.sizeDelta=new Vector2(buttonWidth,p.viewportHeight);root.anchoredPosition=new Vector2(root.anchoredPosition.x,-p.top);
                view.anchoredPosition=Vector2.zero;view.sizeDelta=root.sizeDelta;viewHeight=p.viewportHeight;contentHeight=p.contentHeight;
            }
            scroll.StopMovement();content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);content.sizeDelta=new Vector2(view.rect.width,contentHeight);
            scroll.vertical=contentHeight>viewHeight;scroll.horizontal=false;
            content.anchoredPosition=new Vector2(0,scroll.vertical?Mathf.Clamp(wasScrolling?oldScroll:0,0,contentHeight-viewHeight):-(viewHeight-contentHeight)*.5f);
            layoutCount=count;layoutScreenHeight=height;layoutButtonHeight=buttonHeight;layoutGap=buttonGap;layoutMaximum=maximumViewportHeight;layoutMargin=screenVerticalMargin;layoutScale=artworkScale;
            if(Application.isPlaying&&root.rect.width!=oldWidth&&panel!=null)
            {var layout=panel.GetComponentInChildren<ShopViewportLayout>(true);if(layout!=null)layout.Apply();}
        }
        void OnValidate(){dirty=true;layoutCount=-1;}
    }
}
