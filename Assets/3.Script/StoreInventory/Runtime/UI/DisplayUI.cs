using System;
using System.Collections.Generic;
using UnityEngine;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class DisplayWindowTemplateEntry{public string typeId;public DisplayWindowView template;}
    // 창은 캐시하되 화면에는 한 개만 표시합니다. 전환은 기존 창 퇴장 → 새 창 등장 순서입니다.
    public sealed class DisplayUI:MonoBehaviour
    {
        public InventoryPanel panel;public DisplayWindowView windowTemplate;public DisplayWindowTemplateEntry[] typeTemplates;public RectTransform windowLayer;
        public Vector2 openedPosition=new Vector2(112,-110),maximumSize=new Vector2(760,900);
        [Min(1)]public float slideSpeed=12;[Min(0)]public float hiddenMargin=30;
        public event Action SelectionChanged;
        readonly Dictionary<string,DisplayWindowView> windows=new Dictionary<string,DisplayWindowView>();
        readonly List<string> removed=new List<string>();string selectedId,currentId,requestedId;bool ready,dirty;
        public string SelectedId{get{return selectedId;}}
        public bool AnyOpen{get{return currentId!=null||requestedId!=null;}}
        public bool IsSwitching{get{return currentId!=requestedId;}}
        bool Blocked{get{return panel.IsWindowDragging||panel.shop.HasPendingPlacement;}}
        void Start()
        {
            string error;if(panel==null||windowTemplate==null||windowLayer==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("슬라이드 진열장 참조를 확인하세요.",this);enabled=false;return;}
            var ids=panel.Model.DisplayIds();selectedId=ids.Count>0?ids[0]:null;
            panel.Model.Changed+=MarkDirty;panel.inventory.Loaded+=ResetWindows;windowTemplate.gameObject.SetActive(false);ready=true;
        }
        void MarkDirty(){dirty=true;}
        void Changed(){if(SelectionChanged!=null)SelectionChanged();}
        void ResetWindows()
        {
            panel.drag.Cancel(false);foreach(var v in windows.Values){v.UnregisterGrids();v.gameObject.SetActive(false);Destroy(v.gameObject);}
            windows.Clear();currentId=requestedId=null;var ids=panel.Model.DisplayIds();selectedId=ids.Count>0?ids[0]:null;dirty=true;Changed();
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
            if(panel.shop.customerInteraction!=null&&!panel.shop.customerInteraction.PrepareDisplayWork())return;
            selectedId=id;requestedId=id;Changed();if(currentId==null)StartRequested();
        }
        public void Close(string id)
        {
            if(!ready||Blocked||panel.drag.IsDragging)return;
            if(requestedId==id||currentId==id){requestedId=null;Changed();}
        }
        public void CloseSelected(){Close(selectedId);}
        public void CloseAll(){if(!ready||Blocked||panel.drag.IsDragging)return;requestedId=null;Changed();}
        float HiddenX(DisplayWindowView v){var rt=(RectTransform)v.transform;return -rt.rect.width*rt.localScale.x-hiddenMargin;}
        void StartRequested()
        {
            if(requestedId==null||panel.Model.Installation(requestedId)==null){requestedId=null;return;}
            DisplayWindowView v;
            if(!windows.TryGetValue(requestedId,out v))
            {
                var source=windowTemplate;string type=panel.Model.Installation(requestedId).typeId;
                if(typeTemplates!=null)foreach(var entry in typeTemplates)if(entry!=null&&entry.typeId==type&&entry.template!=null){source=entry.template;break;}
                v=Instantiate(source,windowLayer);v.gameObject.name="Sliding Display "+requestedId;v.Bind(panel,this,requestedId);windows.Add(requestedId,v);
                foreach(var d in v.GetComponentsInChildren<DraggableWindow>(true))d.enabled=false;
                foreach(var d in v.GetComponentsInChildren<WindowBackgroundDrag>(true))d.enabled=false;
                if(v.GetComponent<CanvasGroup>()==null)v.gameObject.AddComponent<CanvasGroup>();
            }
            currentId=requestedId;v.Open();var rt=(RectTransform)v.transform;
            float width=Mathf.Max(1,rt.rect.width),height=Mathf.Max(1,rt.rect.height);
            float scale=Mathf.Min(1,Mathf.Min(maximumSize.x/width,maximumSize.y/height));rt.localScale=new Vector3(scale,scale,1);
            rt.anchoredPosition=new Vector2(HiddenX(v),openedPosition.y);v.GetComponent<CanvasGroup>().blocksRaycasts=false;
        }
        void Update()
        {
            if(!ready||currentId==null)return;DisplayWindowView v;
            if(!windows.TryGetValue(currentId,out v))return;
            bool opening=currentId==requestedId;var rt=(RectTransform)v.transform;float target=opening?openedPosition.x:HiddenX(v);
            float x=DrawerGeometry.Step(rt.anchoredPosition.x,target,slideSpeed,Time.unscaledDeltaTime);
            rt.anchoredPosition=new Vector2(x,openedPosition.y);v.GetComponent<CanvasGroup>().blocksRaycasts=opening&&x==target;
            if(!opening&&x==target)
            {
                panel.ClearSelection(currentId);v.gameObject.SetActive(false);currentId=null;StartRequested();
            }
        }
        void LateUpdate()
        {
            if(!ready||!dirty)return;dirty=false;removed.Clear();
            foreach(var pair in windows){if(panel.Model.Installation(pair.Key)==null){removed.Add(pair.Key);continue;}if(pair.Value.gameObject.activeInHierarchy)pair.Value.Refresh();}
            foreach(string id in removed){var v=windows[id];v.UnregisterGrids();v.gameObject.SetActive(false);Destroy(v.gameObject);windows.Remove(id);if(currentId==id)currentId=null;if(requestedId==id)requestedId=null;}
            if(currentId==null)StartRequested();
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;panel.inventory.Loaded-=ResetWindows;}}
    }
}
