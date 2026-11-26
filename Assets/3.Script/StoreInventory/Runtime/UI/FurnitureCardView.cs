using System;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class FurnitureCardView:MonoBehaviour
    {
        public Button button;public global::UnityEngine.UI.Image thumbnail,background;public Text title,body,price;
        public bool parchmentStyle;public RawImage normalArtwork,selectionArtwork;
        string key;
        public void Bind(string id,Action<string> select,string heading,string detail,string money,Sprite sprite,bool selected,Font sharedFont=null)
        {
            if(key!=id){key=id;button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>select(key));}
            if(sharedFont!=null){title.font=body.font=price.font=sharedFont;}
            title.text=heading;body.text=detail;price.text=money;thumbnail.sprite=sprite;thumbnail.preserveAspect=true;thumbnail.enabled=sprite!=null;
            background.color=parchmentStyle?Color.clear:selected?new Color(.58f,.4f,.17f):new Color(.22f,.17f,.11f);
            if(normalArtwork!=null)normalArtwork.gameObject.SetActive(!selected);
            if(selectionArtwork!=null)selectionArtwork.gameObject.SetActive(selected);
        }
    }
}
