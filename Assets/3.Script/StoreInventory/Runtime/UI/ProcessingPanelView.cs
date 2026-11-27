using UnityEngine;
using UnityEngine.UI;
using UIImage=UnityEngine.UI.Image;

namespace GeneralStore.Inventory
{
    public sealed class ProcessingPanelView : MonoBehaviour
    {
        public WorkshopJobView job;
        public Button collect,night,minus,plus;
        public Text collectLabel,nightFee;
        public GameObject nightCheck;
        public Button[] tabs,repeatButtons;
        public UIImage[] repeatIndicators;
        public Sprite radioOff,radioOn;

        public void ShowRepeat(int mode,bool started)
        {
            for(int i=0;i<repeatButtons.Length;i++)
            {
                repeatButtons[i].interactable=!started;
                repeatIndicators[i].sprite=i==mode?radioOn:radioOff;
            }
            minus.interactable=plus.interactable=mode==1&&!started;
        }

        public void ShowStatus(int count,bool canPay,bool paid,long fee)
        {
            string text="완성품 받기 "+count;
            if(collectLabel.text!=text)collectLabel.text=text;
            collect.interactable=count>0;
            collectLabel.color=count>0?new Color(.20f,.12f,.06f):new Color(1f,.86f,.54f);
            night.interactable=canPay;
            if(nightCheck.activeSelf!=paid)nightCheck.SetActive(paid);
            nightFee.text=canPay?"+"+fee.ToString("N0")+" G":"";
        }
    }
}
