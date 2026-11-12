using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 지도 선택 버튼/현황 행은 사건이 생겼을 때만 갱신합니다. 매 프레임 재고를 집계하지 않습니다.
    public sealed class DisplayManagementUI : MonoBehaviour
    {
        public InventoryPanel panel;public DisplayUI display;public GameObject window;
        public RectTransform locationBoard,displayList,stockList;
        public ContainerTabView buttonTemplate;public DisplayStockRowView stockTemplate;
        public Text selectedLabel,footerLabel;
        readonly Dictionary<string,ContainerTabView> map=new Dictionary<string,ContainerTabView>(),rows=new Dictionary<string,ContainerTabView>();
        readonly Dictionary<string,DisplayStockRowView> stockRows=new Dictionary<string,DisplayStockRowView>();
        readonly HashSet<string> seen=new HashSet<string>();readonly List<string> removed=new List<string>();
        bool ready,dirty;
        void Start()
        {
            string error;if(panel==null||display==null||window==null||locationBoard==null||displayList==null||stockList==null
                ||buttonTemplate==null||stockTemplate==null||selectedLabel==null||footerLabel==null||!panel.inventory.TryInitialize(out error))
            {Debug.LogError("DisplayManagementUI 참조를 확인하세요.",this);enabled=false;return;}
            panel.Model.Changed+=MarkDirty;display.SelectionChanged+=MarkDirty;ready=true;window.SetActive(false);
        }
        void MarkDirty(){dirty=true;}
        public void Toggle(){if(!ready)return;panel.drag.Cancel(false);panel.shop.CancelDisplayPrice();window.SetActive(!window.activeSelf);dirty=true;}
        public void Close(){window.SetActive(false);}
        public void OpenTogether(){window.SetActive(false);panel.shop.OpenTogether();}
        void Select(string id){display.Select(id);dirty=true;}
        ContainerTabView Button(Dictionary<string,ContainerTabView> cache,string id,RectTransform parent)
        {
            ContainerTabView view;if(cache.TryGetValue(id,out view))return view;
            view=Instantiate(buttonTemplate,parent);view.gameObject.SetActive(true);view.button.onClick.AddListener(()=>Select(id));cache.Add(id,view);return view;
        }
        void Place(ContainerTabView view,float x,float y,float w,float h)
        {var r=(RectTransform)view.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        void RemoveButtons(Dictionary<string,ContainerTabView> cache)
        {
            removed.Clear();foreach(var pair in cache)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(string id in removed){cache[id].gameObject.SetActive(false);Destroy(cache[id].gameObject);cache.Remove(id);}
        }
        void Refresh()
        {
            var ids=panel.Model.DisplayIds();int index=0;seen.Clear();
            foreach(string id in ids)
            {
                seen.Add(id);var s=panel.Model.SummarizeDisplay(id);
                var button=Button(map,id,locationBoard);Place(button,(index%3)*280,(index/3)*86,268,76);
                button.label.text=s.title+"\n"+s.columns+"×"+s.rows;
                var row=Button(rows,id,displayList);Place(row,0,index*78,displayList.rect.width,70);
                row.label.text=s.title+"\n남은 "+(s.columns*s.rows-s.usedCells)+"칸 / 진열 "+s.itemCount+"개";
                var color=id==display.SelectedId?new Color(.65f,.4f,.1f):new Color(.22f,.18f,.12f);button.background.color=color;row.background.color=color;index++;
            }
            RemoveButtons(map);RemoveButtons(rows);
            displayList.sizeDelta=new Vector2(displayList.sizeDelta.x,index*78);
            locationBoard.sizeDelta=new Vector2(locationBoard.sizeDelta.x,((index+2)/3)*86);
            var selected=panel.Model.SummarizeDisplay(display.SelectedId);if(selected==null)return;
            selectedLabel.text=selected.title+" 현황";footerLabel.text=selected.kindCount+"종 · 총 "+selected.itemCount+"개 · 사용 "+selected.usedCells+" / "+(selected.columns*selected.rows)+"칸";
            seen.Clear();index=0;
            foreach(var group in selected.groups)
            {
                string key=group.itemId+"/"+group.quality;seen.Add(key);DisplayStockRowView row;
                if(!stockRows.TryGetValue(key,out row)){row=Instantiate(stockTemplate,stockList);row.gameObject.SetActive(true);stockRows.Add(key,row);}
                row.Bind(panel,group);var rect=(RectTransform)row.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(0,-index*64);rect.sizeDelta=new Vector2(stockList.rect.width,60);index++;
            }
            removed.Clear();foreach(var pair in stockRows)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(string key in removed){stockRows[key].gameObject.SetActive(false);Destroy(stockRows[key].gameObject);stockRows.Remove(key);}
            stockList.sizeDelta=new Vector2(stockList.sizeDelta.x,index*64);
        }
        void LateUpdate(){if(ready&&dirty&&window.activeSelf){dirty=false;Refresh();}}
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;display.SelectionChanged-=MarkDirty;}}
    }
}
