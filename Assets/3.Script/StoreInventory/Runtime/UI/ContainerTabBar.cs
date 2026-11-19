using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class ContainerTabSkin
    {public string containerId;public Sprite normalSprite,selectedSprite;public bool hideLabel;}
    // 탭만 생성/재사용하며 실제 격자는 한 개입니다. ScrollRect로 많은 탭을 넘깁니다.
    public sealed class ContainerTabBar : MonoBehaviour
    {
        public WarehouseUI warehouse;
        public RectTransform content;public ContainerTabView template;
        public float tabWidth=190,tabHeight=42,gap=8;
        public Sprite normalSprite,selectedSprite;
        public bool useWhiteWhenSprite=true;
        public bool showLabels=false; // 번호가 포함된 Sprite를 기본으로 사용합니다.
        public ContainerTabSkin[] skins;
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
                if(tab.label!=null)tab.label.text=title(id);bool active=id==selected;
                // Pressed는 누르는 순간일 뿐, 선택 유지 상태는 이 코드가 직접 바꿉니다.
                var normal=normalSprite;var chosen=selectedSprite;bool hideLabel=false;
                if(skins!=null)foreach(var skin in skins)if(skin!=null&&skin.containerId==id)
                {if(skin.normalSprite!=null)normal=skin.normalSprite;if(skin.selectedSprite!=null)chosen=skin.selectedSprite;hideLabel=skin.hideLabel;break;}
                var sprite=active?(chosen!=null?chosen:normal):normal;if(tab.label!=null)tab.label.gameObject.SetActive(showLabels&&!hideLabel);
                tab.background.sprite=sprite;tab.background.color=sprite!=null&&useWhiteWhenSprite?Color.white:active?selectedColor:normalColor;
                var rect=(RectTransform)tab.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
                rect.anchoredPosition=new Vector2(n*(tabWidth+gap),0);rect.sizeDelta=new Vector2(tabWidth,tabHeight);
            }
            removed.Clear();foreach(var pair in views)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
            foreach(string id in removed){views[id].gameObject.SetActive(false);Destroy(views[id].gameObject);views.Remove(id);}
            content.sizeDelta=new Vector2(Mathf.Max(0,ids.Count*(tabWidth+gap)-gap),tabHeight);
        }
    }
}
