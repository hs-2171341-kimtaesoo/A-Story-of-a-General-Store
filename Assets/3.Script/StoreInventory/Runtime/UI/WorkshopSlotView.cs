using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class WorkshopSlotView:MonoBehaviour,IDropHandler,IPointerClickHandler
    {
        public global::UnityEngine.UI.Image icon,background;public Text label;public int slot;public bool result;
        WorkshopUI owner;string jobId;
        public void Bind(WorkshopUI ui,string job,ItemRecord item,bool locked,int count=0)
        {
            owner=ui;jobId=job;var sprite=item==null?null:ui.panel.FindItemIcon(item.itemId);icon.sprite=sprite;icon.enabled=sprite!=null;icon.raycastTarget=false;
            if(item!=null)ui.panel.ApplyQuality(icon,item.quality);
            label.text=item==null?(result?"결과":"재료 "+(slot+1)):ui.ItemName(item)+(locked?"\n작업 중":"")+(result?"\n×"+count:"\n"+QualityLabels.Title(item.quality));
            label.color=item==null?new Color(.8f,.8f,.8f):QualityVisuals.Color(item.quality);
            background.color=locked?new Color(.38f,.27f,.12f):new Color(.13f,.10f,.07f);
        }
        public void OnDrop(PointerEventData e)
        {if(owner==null||result||jobId==null)return;ItemProposal proposal;if(!owner.panel.drag.TryGetProposal(out proposal))return;owner.panel.drag.Cancel(false);owner.Put(jobId,slot,proposal);}
        public void OnPointerClick(PointerEventData e)
        {if(owner!=null&&jobId!=null&&e.button==PointerEventData.InputButton.Left)owner.Take(jobId,slot,result);}
    }
}
