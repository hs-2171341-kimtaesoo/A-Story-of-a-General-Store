using System;
using UnityEngine;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    [Serializable]
    public sealed class ButtonSpriteSet
    {
        public string id;
        public Sprite normal,pressed,active;
    }

    // Shared editable asset. Window selection comes from the menu, not EventSystem focus.
    public sealed class ButtonArtworkCatalog:ScriptableObject
    {
        public ButtonSpriteSet[] management=new ButtonSpriteSet[0],furniture=new ButtonSpriteSet[0];
        public ButtonSpriteSet close;
        static ButtonSpriteSet Find(ButtonSpriteSet[] entries,string id)
        {
            if(entries!=null)foreach(var entry in entries)if(entry!=null&&entry.id==id)return entry;
            return null;
        }
        public ButtonSpriteSet Management(string id){return Find(management,id);}
        public ButtonSpriteSet Furniture(string id){return Find(furniture,id);}
        public static void Apply(Button button,global::UnityEngine.UI.Image target,ButtonSpriteSet sprites,bool active)
        {
            if(button==null||target==null||sprites==null||sprites.normal==null)return;
            var idle=active&&sprites.active!=null?sprites.active:sprites.normal;
            target.sprite=idle;target.color=Color.white;target.preserveAspect=true;
            button.targetGraphic=target;button.transition=Selectable.Transition.SpriteSwap;
            // Focus/hover must not mark a closed window as open. Unity handles press/release,
            // release outside, keyboard submit and disabled input using the supplied sprites.
            button.spriteState=new SpriteState
            {
                highlightedSprite=idle,selectedSprite=idle,
                pressedSprite=sprites.pressed!=null?sprites.pressed:idle,disabledSprite=idle
            };
        }
    }
}
