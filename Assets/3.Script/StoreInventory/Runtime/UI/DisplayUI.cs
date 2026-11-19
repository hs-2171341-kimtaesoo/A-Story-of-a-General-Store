using System;
using System.Collections.Generic;
using UnityEngine;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class DisplayWindowTemplateEntry{public string typeId;public DisplayWindowView template;}
    // 창은 캐시하되 화면에는 한 개만 표시합니다. 전환은 기존 창 퇴장 → 새 창 등장 순서입니다.
    public sealed class DisplayUI:MonoBehaviour
    {
        public InventoryPanel panel;public InventoryGridView cellReference;
        public bool matchWarehouseCells=true;public DisplayWindowView windowTemplate;public DisplayWindowTemplateEntry[] typeTemplates;public RectTransform windowLayer;
        public bool centerVertically=true;public bool slideFromRight=true;[Min(0)]public float rightInset=112;
        [HideInInspector]public bool useViewport;[HideInInspector]public Rect viewport;
        public Vector2 openedPosition=new Vector2(112,-110),maximumSize=new Vector2(760,900);
        [Min(1)]public float slideSpeed=28;[Min(0)]public float hiddenMargin=30;
        public event Action SelectionChanged;
        readonly Dictionary<string,DisplayWindowView> windows=new Dictionary<string,DisplayWindowView>();
        [Range(1,12)]public int maximumCachedWindows=2;
        readonly Dictionary<string,long> lastUse=new Dictionary<string,long>();long useCounter;
        readonly List<string> removed=new List<string>();string selectedId,currentId,requestedId;bool ready,dirty,moving;
        RectTransform movingRect;CanvasGroup movingGroup;
        public string SelectedId{get{return selectedId;}}
        public int CachedWindowCount{get{return windows.Count;}}
        public bool AnyOpen{get{return currentId!=null||requestedId!=null;}}
        public bool IsSwitching{get{return moving;}}
        bool Blocked{get{return panel.IsWindowDragging||panel.shop.HasPendingPlacement;}}
        void Start()
        {
            string error;if(panel==null||windowTemplate==null||windowLayer==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("슬라이드 진열장 참조를 확인하세요.",this);enabled=false;return;}
            var ids=panel.Model.DisplayIds();selectedId=ids.Count>0?ids[0]:null;
            panel.Model.Changed+=MarkDirty;panel.inventory.Loaded+=ResetWindows;panel.WindowDragStateChanged+=OnWindowDrag;windowTemplate.gameObject.SetActive(false);ready=true;
        }
        void OnWindowDrag(bool active)
        {
            if(movingGroup==null||movingRect==null||currentId==null)return;
            DisplayWindowView view;if(!windows.TryGetValue(currentId,out view))return;
            movingGroup.blocksRaycasts=!active&&currentId==requestedId&&movingRect.anchoredPosition.x==OpenX(view);
        }
        void MarkDirty(){dirty=true;}
        void Changed(){if(SelectionChanged!=null)SelectionChanged();}
        void ResetWindows()
        {
            panel.drag.Cancel(false);foreach(var v in windows.Values){v.UnregisterGrids();v.gameObject.SetActive(false);Destroy(v.gameObject);}
            windows.Clear();lastUse.Clear();currentId=requestedId=null;var ids=panel.Model.DisplayIds();selectedId=ids.Count>0?ids[0]:null;dirty=true;Changed();
        }
        public void Select(string id){if(!ready||panel.Model.Installation(id)==null)return;selectedId=id;Changed();}
        public void SelectIndex(int n){var ids=panel.Model.DisplayIds();if(n>=0&&n<ids.Count)Select(ids[n]);}
        public void OpenSelected(){Open(selectedId);}
        public bool IsOpen(string id){return id!=null&&requestedId==id;}
        public void Toggle(string id){if(!ready||Blocked||panel.drag.IsDragging)return;if(IsOpen(id))Close(id);else Open(id);}
        public void Open(string id){Request(id,false);}
        public void OpenWhileDragging(string id){Request(id,true);}
        void Request(string id,bool allowHeld)
        {
            if(!ready||Blocked||(!allowHeld&&panel.drag.IsDragging)||panel.Model.Installation(id)==null)return;
            selectedId=id;requestedId=id;moving=true;Changed();if(currentId==null)StartRequested();
        }
        public void Close(string id)
        {
            if(!ready||Blocked||panel.drag.IsDragging)return;
            if(requestedId==id||currentId==id){requestedId=null;moving=true;Changed();}
        }
        public void CloseSelected(){Close(selectedId);}
        public void CloseAll(){if(!ready||Blocked||panel.drag.IsDragging)return;requestedId=null;moving=true;Changed();}
        float RightEdge{get{return useViewport?viewport.xMax:windowLayer.rect.width;}}
        float Top
        {
            get
            {
                float top=useViewport?viewport.yMax:0,height=useViewport?viewport.height:windowLayer.rect.height;
                if(centerVertically&&movingRect!=null)return top-(height-movingRect.rect.height*Mathf.Abs(movingRect.localScale.y))*.5f;
                return top+openedPosition.y;
            }
        }
        float OpenX(DisplayWindowView v){var rt=(RectTransform)v.transform;return slideFromRight?RightEdge-rightInset-rt.rect.width*rt.localScale.x:openedPosition.x;}
        float HiddenX(DisplayWindowView v){var rt=(RectTransform)v.transform;return slideFromRight?RightEdge+hiddenMargin:-rt.rect.width*rt.localScale.x-hiddenMargin;}
        void Fit(DisplayWindowView v)
        {
            var rt=(RectTransform)v.transform;float width=Mathf.Max(1,rt.rect.width),height=Mathf.Max(1,rt.rect.height);
            float availableWidth=useViewport?Mathf.Min(maximumSize.x,viewport.width*.45f):maximumSize.x;
            float availableHeight=useViewport?Mathf.Min(maximumSize.y,Mathf.Max(100,viewport.height+openedPosition.y-70)):maximumSize.y;
            float scale=Mathf.Min(availableWidth/width,availableHeight/height);
            if(matchWarehouseCells&&cellReference!=null)
            {
                float target=cellReference.cellWidth*Mathf.Abs(cellReference.transform.lossyScale.x)/Mathf.Max(.0001f,Mathf.Abs(windowLayer.lossyScale.x));
                float cell=LocalCellEdge(v.grid,rt);if(v.extraGrids!=null)foreach(var grid in v.extraGrids)if(grid!=null)cell=Mathf.Max(cell,LocalCellEdge(grid,rt));
                scale=Mathf.Min(scale,target/Mathf.Max(.0001f,cell));
            }
            else scale=Mathf.Min(1,scale);
            rt.localScale=new Vector3(scale,scale,1);
        }
        static float LocalCellEdge(InventoryGridView grid,RectTransform root)
        {
            return Mathf.Max(grid.cellWidth*Mathf.Abs(grid.transform.lossyScale.x)/Mathf.Max(.0001f,Mathf.Abs(root.lossyScale.x)),
                grid.cellHeight*Mathf.Abs(grid.transform.lossyScale.y)/Mathf.Max(.0001f,Mathf.Abs(root.lossyScale.y)));
        }
        public void RefreshViewport(Rect region)
        {
            viewport=region;useViewport=true;
            if(currentId!=null&&windows.ContainsKey(currentId)){Fit(windows[currentId]);moving=true;}
        }
        void StartRequested()
        {
            if(requestedId==null||panel.Model.Installation(requestedId)==null){requestedId=null;return;}
            DisplayWindowView v;
            if(!windows.TryGetValue(requestedId,out v))
            {
                var source=windowTemplate;string type=panel.Model.Installation(requestedId).typeId;
                if(typeTemplates!=null)foreach(var entry in typeTemplates)if(entry!=null&&entry.typeId==type&&entry.template!=null){source=entry.template;break;}
                v=Instantiate(source,windowLayer);v.gameObject.name="Sliding Display "+requestedId;v.Bind(panel,this,requestedId);windows.Add(requestedId,v);
                if(v.GetComponent<CanvasGroup>()==null)v.gameObject.AddComponent<CanvasGroup>();
                UiPerformanceLayers.Ensure(v.gameObject,true);
            }
            currentId=requestedId;lastUse[currentId]=++useCounter;v.Open();var rt=(RectTransform)v.transform;
            Fit(v);movingRect=rt;movingGroup=v.GetComponent<CanvasGroup>();moving=true;
            rt.anchoredPosition=new Vector2(HiddenX(v),Top);movingGroup.blocksRaycasts=false;TrimWindows();
        }
        void Update()
        {
            if(!ready||!moving||currentId==null)return;DisplayWindowView v;
            if(!windows.TryGetValue(currentId,out v))return;
            bool opening=currentId==requestedId;var rt=movingRect;float target=opening?OpenX(v):HiddenX(v);
            float x=DrawerGeometry.Step(rt.anchoredPosition.x,target,slideSpeed,Time.unscaledDeltaTime);
            if(rt.anchoredPosition.x!=x||rt.anchoredPosition.y!=Top)rt.anchoredPosition=new Vector2(x,Top);
            var group=movingGroup;bool interactive=opening&&x==target&&!panel.IsWindowDragging;if(group.blocksRaycasts!=interactive)group.blocksRaycasts=interactive;
            if(opening&&x==target)moving=false;
            if(!opening&&x==target)
            {
                panel.ClearSelection(currentId);v.gameObject.SetActive(false);currentId=null;moving=false;StartRequested();TrimWindows();
            }
        }
        void LateUpdate()
        {
            if(!ready||!dirty)return;dirty=false;removed.Clear();
            foreach(var pair in windows){if(panel.Model.Installation(pair.Key)==null){removed.Add(pair.Key);continue;}if(pair.Value.gameObject.activeInHierarchy)pair.Value.Refresh();}
            foreach(string id in removed){var v=windows[id];v.UnregisterGrids();v.gameObject.SetActive(false);Destroy(v.gameObject);windows.Remove(id);lastUse.Remove(id);if(currentId==id)currentId=null;if(requestedId==id)requestedId=null;}
            if(currentId==null)StartRequested();
        }
        void TrimWindows()
        {
            int limit=Mathf.Clamp(maximumCachedWindows,1,12);
            while(windows.Count>limit)
            {
                string oldest=null;long age=long.MaxValue;
                foreach(var entry in windows)
                {
                    if(entry.Key==currentId||entry.Key==requestedId||entry.Value.gameObject.activeSelf)continue;
                    long used;if(!lastUse.TryGetValue(entry.Key,out used))used=0;
                    if(used<age){oldest=entry.Key;age=used;}
                }
                if(oldest==null)break;
                var view=windows[oldest];view.UnregisterGrids();windows.Remove(oldest);lastUse.Remove(oldest);Destroy(view.gameObject);
            }
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;panel.inventory.Loaded-=ResetWindows;panel.WindowDragStateChanged-=OnWindowDrag;}}
    }
}
