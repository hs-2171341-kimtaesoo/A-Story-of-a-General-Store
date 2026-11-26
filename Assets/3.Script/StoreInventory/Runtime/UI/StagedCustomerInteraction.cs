using System;
using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    [Serializable]public sealed class CustomerVisitPose
    {
        public Sprite sprite;public Vector2 position,size=new Vector2(180,300);
        [Min(.2f)]public float holdSeconds=2;
    }
    public sealed class StagedCustomerInteraction:MonoBehaviour
    {
        public ShopfrontUI shop;public global::UnityEngine.UI.Image sceneImage;
        public CustomerVisitPose[] browsingStops;public CustomerVisitPose counterPose;
        [Min(.05f)]public float fadeSeconds=.25f;
        public bool Engaged{get;private set;}public bool AtCounter{get;private set;}
        bool visiting;int stage;float elapsed;
        bool Busy{get{return shop.panel.IsWindowDragging||shop.panel.drag.IsDragging||shop.HasPendingPlacement||(shop.counterTrade!=null&&shop.counterTrade.IsItemDragging);}}
        CustomerVisitPose Pose{get{return browsingStops!=null&&stage<browsingStops.Length?browsingStops[stage]:counterPose;}}
        public void BeginVisit(){visiting=true;Engaged=false;AtCounter=false;stage=0;elapsed=0;ApplyPose();shop.RefreshDialogue();}
        void ApplyPose()
        {
            var p=Pose;if(p==null){sceneImage.enabled=false;return;}
            sceneImage.sprite=p.sprite;sceneImage.enabled=p.sprite!=null;sceneImage.raycastTarget=false;
            sceneImage.rectTransform.anchoredPosition=p.position;sceneImage.rectTransform.sizeDelta=p.size;sceneImage.color=new Color(1,1,1,0);
        }
        public void Respond()
        {
            if(!visiting||!AtCounter||Engaged||Busy)return;
            string error;if(!shop.panel.inventory.Sales.PresentOnCounter(out error)){shop.panel.ShowMessage(error);return;}
            shop.panel.inventory.Sales.BeginConversation(out error);Engaged=true;sceneImage.enabled=sceneImage.sprite!=null;sceneImage.raycastTarget=false;shop.RefreshDialogue();
        }
        public void Minimize()
        {
            if(!Engaged||Busy)return;
            Engaged=false;
            sceneImage.enabled=visiting&&shop.panel.inventory.Sales.CustomerPresent&&sceneImage.sprite!=null;
            sceneImage.raycastTarget=sceneImage.enabled&&AtCounter;shop.RefreshDialogue();
        }
        void Update()
        {
            if(!visiting)return;
            if(!shop.panel.inventory.Sales.CustomerPresent)
            {
                if(Busy)return;if(Engaged)Minimize();visiting=false;AtCounter=false;sceneImage.enabled=false;sceneImage.raycastTarget=false;shop.RefreshDialogue();return;
            }
            if(AtCounter){sceneImage.raycastTarget=!Engaged&&!Busy;return;}
            if(!shop.simulation.timeRunning)return;
            elapsed+=Time.deltaTime;var p=Pose;float hold=p==null?2:Mathf.Max(.2f,p.holdSeconds),fade=Mathf.Max(.05f,fadeSeconds);
            bool last=browsingStops==null||stage>=browsingStops.Length;
            if(last&&shop.panel.inventory.Sales.Browsing)
            {
                if(Busy)return;string error;if(!shop.panel.inventory.Sales.FinishBrowsing(out error))
                {visiting=false;sceneImage.enabled=false;sceneImage.raycastTarget=false;shop.panel.ShowMessage(error);shop.RefreshDialogue();return;}
            }
            float alpha=Mathf.Clamp01(elapsed/fade);if(!last)alpha=Mathf.Min(alpha,Mathf.Clamp01((hold+fade*2-elapsed)/fade));
            sceneImage.color=new Color(1,1,1,alpha);
            if(last&&elapsed>=fade){AtCounter=true;sceneImage.color=Color.white;sceneImage.raycastTarget=!Busy;}
            else if(!last&&elapsed>=hold+fade*2)
            {
                bool leavingBrowse=stage+1>=browsingStops.Length;
                if(leavingBrowse&&shop.panel.inventory.Sales.Browsing)
                {
                    if(Busy)return;string error;
                    if(!shop.panel.inventory.Sales.FinishBrowsing(out error))
                    {visiting=false;sceneImage.enabled=false;sceneImage.raycastTarget=false;shop.panel.ShowMessage(error);shop.RefreshDialogue();return;}
                }
                stage++;elapsed=0;ApplyPose();
            }
        }
    }
}
