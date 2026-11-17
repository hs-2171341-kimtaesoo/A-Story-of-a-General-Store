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
        public Vector2 tradingDrawerPosition=new Vector2(24,-146);
        [Min(.05f)]public float fadeSeconds=.25f;
        public bool Engaged{get;private set;}public bool AtCounter{get;private set;}
        bool visiting,restoreDrawer,restoreDisplay;string displayId;Vector2 previousDrawerPosition;int stage;float elapsed;
        bool Busy{get{return shop.panel.IsWindowDragging||shop.panel.drag.IsDragging||shop.HasPendingPlacement;}}
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
            restoreDrawer=shop.drawer.IsOpen;restoreDisplay=shop.displays.AnyOpen;displayId=shop.displays.SelectedId;previousDrawerPosition=shop.drawer.openedPosition;
            shop.displays.CloseAll();shop.drawer.Close();shop.drawer.openedPosition=tradingDrawerPosition;
            Engaged=true;sceneImage.enabled=false;sceneImage.raycastTarget=false;shop.RefreshDialogue();
        }
        public void Minimize()
        {
            if(!Engaged||Busy)return;
            Engaged=false;shop.drawer.openedPosition=previousDrawerPosition;
            if(restoreDrawer)shop.drawer.Open();else shop.drawer.Close();
            if(restoreDisplay&&shop.panel.Model.Installation(displayId)!=null)shop.displays.Open(displayId);
            sceneImage.enabled=visiting&&shop.panel.inventory.Sales.CustomerPresent&&sceneImage.sprite!=null;
            sceneImage.raycastTarget=sceneImage.enabled&&AtCounter;shop.RefreshDialogue();
        }
        public bool PrepareDisplayWork()
        {
            // 손님 거래 화면은 원래 큰 크기를 유지합니다. 대화의 정리하기로 돌아온 뒤 진열장을 엽니다.
            if(Engaged){shop.panel.ShowMessage("대화창의 정리하기를 눌러 진열 작업으로 돌아가세요.");return false;}return true;
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
            float alpha=Mathf.Clamp01(elapsed/fade);if(!last)alpha=Mathf.Min(alpha,Mathf.Clamp01((hold+fade*2-elapsed)/fade));
            sceneImage.color=new Color(1,1,1,alpha);
            if(last&&elapsed>=fade){AtCounter=true;sceneImage.color=Color.white;sceneImage.raycastTarget=!Busy;}
            else if(!last&&elapsed>=hold+fade*2){stage++;elapsed=0;ApplyPose();}
        }
    }
}
