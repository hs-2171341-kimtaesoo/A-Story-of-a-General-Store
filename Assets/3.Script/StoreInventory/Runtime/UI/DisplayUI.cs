using System;
using System.Collections.Generic;
using UnityEngine;
namespace GeneralStore.Inventory
{
    // 첫 오픈에만 독립 창을 생성하고 캐시합니다. 다른 창을 닫지 않습니다.
    public sealed class DisplayUI:MonoBehaviour
    {
        public InventoryPanel panel;public DisplayWindowView windowTemplate;public RectTransform windowLayer;
        public event Action SelectionChanged;
        readonly Dictionary<string,DisplayWindowView> windows=new Dictionary<string,DisplayWindowView>();
        readonly List<string> removed=new List<string>();string selectedId;bool ready,dirty;
        public string SelectedId{get{return selectedId;}}
        void Start()
        {
            string error;if(panel==null||windowTemplate==null||windowLayer==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("독립 진열창 참조를 확인하세요.",this);enabled=false;return;}
            selectedId=panel.Model.DisplayIds()[0];panel.Model.Changed+=MarkDirty;panel.inventory.Loaded+=ResetWindows;
            windowTemplate.gameObject.SetActive(false);ready=true;
        }
        void MarkDirty(){dirty=true;}
        void ResetWindows()
        {
            panel.drag.Cancel(false);foreach(var v in windows.Values){panel.UnregisterWindowGrid(v.grid,v.details);v.gameObject.SetActive(false);Destroy(v.gameObject);}
            windows.Clear();selectedId=panel.Model.DisplayIds()[0];dirty=true;if(SelectionChanged!=null)SelectionChanged();
        }
        public void Select(string id){if(!ready||panel.Model.Installation(id)==null)return;selectedId=id;if(SelectionChanged!=null)SelectionChanged();}
        public void SelectIndex(int n){if(!ready)return;var ids=panel.Model.DisplayIds();if(n>=0&&n<ids.Count)Select(ids[n]);}
        public void OpenSelected(){Open(selectedId);}
        public void Open(string id)
        {
            if(!ready||panel.Model.Installation(id)==null)return;Select(id);DisplayWindowView v;
            if(!windows.TryGetValue(id,out v)){v=Instantiate(windowTemplate,windowLayer);v.gameObject.name="Display Window "+id;v.Bind(panel,this,id);windows.Add(id,v);}v.Open();
        }
        public void CloseSelected(){DisplayWindowView v;if(selectedId!=null&&windows.TryGetValue(selectedId,out v))v.Close();}
        void LateUpdate()
        {
            if(!ready||!dirty)return;dirty=false;removed.Clear();
            foreach(var pair in windows){if(panel.Model.Installation(pair.Key)==null){removed.Add(pair.Key);continue;}if(pair.Value.gameObject.activeInHierarchy)pair.Value.Refresh();}
            foreach(string id in removed){var v=windows[id];panel.UnregisterWindowGrid(v.grid,v.details);v.gameObject.SetActive(false);Destroy(v.gameObject);windows.Remove(id);}
        }
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;panel.inventory.Loaded-=ResetWindows;}}
    }
}
