using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class ShopMenuEntry
    {public string id,title;public Sprite icon;public bool unlocked=true;public GameObject window;public UnityEvent onOpen=new UnityEvent();}
    public sealed class RightSideMenu:MonoBehaviour
    {
        public InventoryPanel panel;public RectTransform content;public RightMenuButtonView template;public List<ShopMenuEntry> entries=new List<ShopMenuEntry>();
        public Color normalColor=new Color(.26f,.18f,.1f),selectedColor=new Color(.7f,.45f,.1f);
        [Min(1)]public float buttonWidth=82,buttonHeight=86;
        [Min(0)]public float buttonGap=8,screenVerticalMargin=100;
        [Min(1)]public float maximumViewportHeight=700;
        public bool autoHide=true,keepVisibleWhileWindowOpen=true;
        [Min(1)]public float revealWidth=56,slideSpeed=14;
        [Min(0)]public float hideDelay=.35f;
        bool revealed;float outsideTime;
        ScrollRect scroll;int layoutCount=-1;float layoutScreenHeight,layoutButtonHeight,layoutGap,layoutMaximum,layoutMargin;
        readonly Dictionary<string,RightMenuButtonView> views=new Dictionary<string,RightMenuButtonView>();bool dirty=true;string selected;
        void Start()
        {
            if(panel==null||content==null||template==null||template.button==null||template.icon==null||template.background==null||template.label==null)
            {Debug.LogError("관리 메뉴 템플릿/참조를 확인하세요.",this);enabled=false;return;}
            scroll=DisplayWindowAppearance.FindScrollInParents(content);
            var ids=new HashSet<string>();foreach(var e in entries)if(e==null||string.IsNullOrWhiteSpace(e.id)||!ids.Add(e.id))
            {Debug.LogError("메뉴 ID가 비어 있거나 중복되었습니다.",this);enabled=false;return;}
            if(autoHide){var rect=(RectTransform)transform;rect.anchoredPosition=new Vector2(-buttonWidth-8,rect.anchoredPosition.y);}
        }
        public void RefreshLayout(){dirty=true;layoutCount=-1;if(!Application.isPlaying){var rect=(RectTransform)transform;rect.anchoredPosition=new Vector2(autoHide?-buttonWidth-8:8,rect.anchoredPosition.y);}}
        void Slide()
        {
            var rect=(RectTransform)transform;var bounds=rect.parent as RectTransform;if(bounds==null)return;
            bool open=false;if(keepVisibleWhileWindowOpen)foreach(var entry in entries)if(entry.window!=null&&entry.window.activeSelf){open=true;break;}
            Vector2 screen,point;bool inside=false;
            if(InventoryKeys.TryPointerPosition(out screen)&&RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds,screen,panel.EventCamera,out point))
            {float x=point.x-bounds.rect.xMin;inside=x>=0&&x<=(revealed?buttonWidth+28:revealWidth)&&point.y>=bounds.rect.yMin&&point.y<=bounds.rect.yMax;}
            if(!autoHide||inside||open){revealed=true;outsideTime=0;}else{outsideTime+=Time.unscaledDeltaTime;if(outsideTime>=hideDelay)revealed=false;}
            float xTarget=revealed?8:-buttonWidth-8;float xNext=DrawerGeometry.Step(rect.anchoredPosition.x,xTarget,slideSpeed,Time.unscaledDeltaTime);
            if(rect.anchoredPosition.x!=xNext)rect.anchoredPosition=new Vector2(xNext,rect.anchoredPosition.y);
        }
        public void SetUnlocked(string id,bool value){foreach(var e in entries)if(e.id==id){e.unlocked=value;if(!value&&e.window!=null)e.window.SetActive(false);dirty=true;}}
        public void AddEntry(ShopMenuEntry entry){if(entry==null||string.IsNullOrWhiteSpace(entry.id))throw new ArgumentException("메뉴 ID가 필요합니다.");foreach(var e in entries)if(e.id==entry.id)throw new ArgumentException("중복 메뉴 ID");entries.Add(entry);dirty=true;}
        public void Toggle(string id)
        {
            if(panel.IsWindowDragging)return;
            ShopMenuEntry chosen=null;foreach(var e in entries)if(e.id==id&&e.unlocked)chosen=e;if(chosen==null)return;
            panel.drag.Cancel(false);panel.shop.CancelDisplayPrice();bool closing=chosen.window!=null&&chosen.window.activeSelf;
            foreach(var e in entries)if(e.window!=null)e.window.SetActive(false);
            selected=closing?null:id;if(!closing)chosen.onOpen.Invoke();dirty=true;
        }
        void LateUpdate()
        {
            Slide();
            if(selected!=null)foreach(var e in entries)if(e.id==selected&&e.window!=null&&!e.window.activeSelf){selected=null;dirty=true;break;}
            var parentRect=transform.parent as RectTransform;
            if(parentRect!=null&&parentRect.rect.height!=layoutScreenHeight)dirty=true;
            if(!dirty)return;dirty=false;int n=0;
            foreach(var v in views.Values)v.gameObject.SetActive(false);
            foreach(var e in entries)if(e.unlocked)
            {
                RightMenuButtonView v;if(!views.TryGetValue(e.id,out v)){v=Instantiate(template,content);string key=e.id;v.button.onClick.AddListener(()=>Toggle(key));views.Add(key,v);}
                v.gameObject.SetActive(true);v.label.text=e.title;v.icon.sprite=e.icon;v.icon.enabled=e.icon!=null;v.icon.preserveAspect=true;
                v.background.color=e.id==selected?selectedColor:normalColor;
                var r=(RectTransform)v.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(0,-n++*(buttonHeight+buttonGap));r.sizeDelta=new Vector2(buttonWidth,buttonHeight);
            }
            ApplyCenteredLayout(n);
        }
        // 선택 색만 바뀔 때는 스크롤 위치를 건드리지 않습니다. 개수/화면/설정 변경 때만 재배치합니다.
        void ApplyCenteredLayout(int count)
        {
            var root=(RectTransform)transform;var bounds=root.parent as RectTransform;if(bounds==null||scroll==null)return;
            float height=bounds.rect.height;
            bool changed=layoutCount!=count||layoutScreenHeight!=height||layoutButtonHeight!=buttonHeight||layoutGap!=buttonGap||layoutMaximum!=maximumViewportHeight||layoutMargin!=screenVerticalMargin;
            if(!changed)return;
            bool wasScrolling=scroll.vertical;float normalized=scroll.verticalNormalizedPosition;
            var p=CenteredMenuLayout.Calculate(count,buttonHeight,buttonGap,height,screenVerticalMargin,maximumViewportHeight);
            scroll.StopMovement();
            root.sizeDelta=new Vector2(root.sizeDelta.x,p.viewportHeight);root.anchoredPosition=new Vector2(root.anchoredPosition.x,-p.top);
            var view=scroll.viewport;view.anchorMin=view.anchorMax=view.pivot=new Vector2(0,1);view.anchoredPosition=Vector2.zero;view.sizeDelta=new Vector2(root.rect.width,p.viewportHeight);
            content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);content.sizeDelta=new Vector2(buttonWidth,p.contentHeight);
            scroll.vertical=p.scrolls;scroll.horizontal=false;
            if(!p.scrolls)content.anchoredPosition=Vector2.zero;
            else scroll.verticalNormalizedPosition=wasScrolling?normalized:1;
            layoutCount=count;layoutScreenHeight=height;layoutButtonHeight=buttonHeight;layoutGap=buttonGap;layoutMaximum=maximumViewportHeight;layoutMargin=screenVerticalMargin;
        }
        void OnValidate(){dirty=true;layoutCount=-1;}

    }
}
