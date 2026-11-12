using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 탭만 생성/재사용하며 실제 격자는 한 개입니다. ScrollRect로 많은 탭을 넘깁니다.
    public sealed class ContainerTabBar : MonoBehaviour
    {
        public WarehouseUI warehouse;
        public RectTransform content;public ContainerTabView template;
        public float tabWidth=190,tabHeight=42,gap=8;
        public Color selectedColor=new Color(.65f,.4f,.1f),normalColor=new Color(.22f,.18f,.12f);
        readonly Dictionary<string,ContainerTabView> views=new Dictionary<string,ContainerTabView>();
        readonly HashSet<string> seen=new HashSet<string>();readonly List<string> removed=new List<string>();
        public void Sync(List<string> ids,string selected,Func<string,string> title,Action<string> select)
        {
            seen.Clear();
            for(int n=0;n<ids.Count;n++)
            {
                string id=ids[n];seen.Add(id);ContainerTabView tab;
                if(!views.TryGetValue(id,out tab))
                {
                    tab=Instantiate(template,content);tab.gameObject.SetActive(true);views.Add(id,tab);
                    tab.button.onClick.AddListener(()=>select(id));
                }
                if(warehouse!=null){var t=tab.GetComponent<WarehouseTabTarget>();if(t==null)t=tab.gameObject.AddComponent<WarehouseTabTarget>();t.warehouse=warehouse;t.containerId=id;}
                tab.label.text=title(id);tab.background.color=id==selected?selectedColor:normalColor;
                var rect=(RectTransform)tab.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(n*(tabWidth+gap),0);rect.sizeDelta=new Vector2(tabWidth,tabHeight);
            }
            removed.Clear();foreach(var pair in views)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(string id in removed){views[id].gameObject.SetActive(false);Destroy(views[id].gameObject);views.Remove(id);}
            content.sizeDelta=new Vector2(Mathf.Max(0,ids.Count*(tabWidth+gap)-gap),tabHeight);
        }
    }
}
