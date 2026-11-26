using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class DisplayManagementUI:MonoBehaviour
    {
        public InventoryPanel panel;public DisplayUI display;public GameObject window;public FurnitureArtCatalog art;
        public RectTransform displayList,stockList;public FurnitureCardView cardTemplate;public DisplayStockRowView stockTemplate;
        public Text selectedLabel,footerLabel;public Button openButton;
        public bool parchmentStyle;public Text emptyDisplayLabel,emptyStockLabel;
        public float displayRowHeight=106,stockRowHeight=66,rowGap=8;
        [Tooltip("행 Prefab의 높이를 사용합니다. 자식 아이콘/글자 배치는 변경하지 않습니다.")]
        public bool useTemplateRowHeight;
        string shownDisplay;
        readonly Dictionary<string,FurnitureCardView> cards=new Dictionary<string,FurnitureCardView>();
        readonly List<DisplayStockRowView> stocks=new List<DisplayStockRowView>();
        readonly HashSet<string> seen=new HashSet<string>();readonly List<string> remove=new List<string>();bool ready,dirty;
        void Start(){string error;if(panel==null||display==null||window==null||art==null||displayList==null||stockList==null||cardTemplate==null||stockTemplate==null||selectedLabel==null||footerLabel==null||openButton==null||!panel.inventory.TryInitialize(out error)){Debug.LogError("새 메뉴 버전 씬을 생성하고 DisplayManagementUI 참조를 확인하세요.",this);enabled=false;return;}SidePanelInput.AllowOutside(window);panel.Model.Changed+=MarkDirty;display.SelectionChanged+=MarkDirty;ready=true;window.SetActive(false);}
        void MarkDirty(){dirty=true;}
        public void Toggle(){if(!ready)return;panel.drag.Cancel(false);panel.shop.CancelDisplayPrice();if(window.activeSelf)Close();else window.SetActive(true);dirty=true;}
        public void Close(){ManagementWindowMotion.CloseWindow(window);}
        public void OpenTogether(){OpenSelected();}
        public void OpenSelected(){if(!ready||panel.Model.Installation(display.SelectedId)==null)return;Close();display.OpenSelected();}
        void Select(string id){display.Select(id);dirty=true;}
        static void ClampScroll(RectTransform content)
        {
            var scroll=DisplayWindowAppearance.FindScrollInParents(content);if(scroll==null||scroll.viewport==null)return;
            content.anchoredPosition=new Vector2(0,Mathf.Clamp(content.anchoredPosition.y,0,Mathf.Max(0,content.rect.height-scroll.viewport.rect.height)));
        }
        void Refresh()
        {
            float displayPitch=AuthoredRowLayout.Pitch(cardTemplate,displayRowHeight,rowGap,useTemplateRowHeight);
            float stockPitch=AuthoredRowLayout.Pitch(stockTemplate,stockRowHeight,rowGap,useTemplateRowHeight);
            var ids=panel.Model.DisplayIds();if(!ids.Contains(display.SelectedId)&&ids.Count>0)display.Select(ids[0]);
            seen.Clear();int n=0;
            foreach(string id in ids)
            {
                seen.Add(id);FurnitureCardView row;if(!cards.TryGetValue(id,out row)){row=Instantiate(cardTemplate,displayList);row.gameObject.SetActive(true);cards.Add(id,row);}
                var s=panel.Model.SummarizeDisplay(id);var a=art.Find(panel.Model.Installation(id).typeId);
                string size=s.columns+"×"+s.rows;
                row.Bind(id,Select,s.title,"남은 "+(s.columns*s.rows-s.usedCells)+"칸 · 진열 "+s.itemCount+"개"+(parchmentStyle?"  ("+size+")":""),parchmentStyle?"":size,a==null?null:a.thumbnail,id==display.SelectedId,panel.SharedFont);
                AuthoredRowLayout.Place((RectTransform)row.transform,n++,displayPitch,displayList.rect.width,rowGap);
            }
            remove.Clear();foreach(var p in cards)if(!seen.Contains(p.Key))remove.Add(p.Key);foreach(string id in remove){Destroy(cards[id].gameObject);cards.Remove(id);}
            displayList.sizeDelta=new Vector2(displayList.sizeDelta.x,n*displayPitch);ClampScroll(displayList);
            if(emptyDisplayLabel!=null)emptyDisplayLabel.gameObject.SetActive(n==0);
            var selected=panel.Model.SummarizeDisplay(display.SelectedId);openButton.interactable=selected!=null;
            if(shownDisplay!=display.SelectedId)
            {
                shownDisplay=display.SelectedId;stockList.anchoredPosition=Vector2.zero;
                var scroll=DisplayWindowAppearance.FindScrollInParents(stockList);if(scroll!=null)scroll.StopMovement();
            }
            if(selected==null)
            {
                selectedLabel.text="진열 품목";footerLabel.text="소유한 진열장이 없습니다.";
                foreach(var row in stocks)row.gameObject.SetActive(false);stockList.sizeDelta=new Vector2(stockList.sizeDelta.x,0);
                if(emptyStockLabel!=null)emptyStockLabel.gameObject.SetActive(true);return;
            }
            selectedLabel.text=selected.title+(parchmentStyle?" 진열 품목":" 현황");footerLabel.text="남은 "+(selected.columns*selected.rows-selected.usedCells)+"칸 · "+selected.kindCount+"종 / 총 "+selected.itemCount+"개";
            n=0;
            foreach(var group in selected.groups)
            {
                if(n>=stocks.Count)stocks.Add(Instantiate(stockTemplate,stockList));
                var row=stocks[n];row.gameObject.SetActive(true);row.Bind(panel,group);AuthoredRowLayout.Place((RectTransform)row.transform,n++,stockPitch,stockList.rect.width,rowGap);
            }
            for(int i=n;i<stocks.Count;i++)stocks[i].gameObject.SetActive(false);
            stockList.sizeDelta=new Vector2(stockList.sizeDelta.x,n*stockPitch);ClampScroll(stockList);
            if(emptyStockLabel!=null)emptyStockLabel.gameObject.SetActive(n==0);
        }
        void LateUpdate(){if(ready&&dirty&&window.activeSelf){dirty=false;Refresh();}}
        void OnDestroy(){if(ready&&panel!=null&&panel.inventory.Model!=null){panel.Model.Changed-=MarkDirty;display.SelectionChanged-=MarkDirty;}}
    }
}
