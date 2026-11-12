using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    // 상자는 내용물 목록을 소유할 뿐 낱개 아이템 UI를 미리 생성하지 않습니다.
    public sealed class ParcelView : MonoBehaviour,IPointerClickHandler
    {
        public global::UnityEngine.UI.Image icon;
        public Text label;
        FloorDropArea area;string parcelId;
        public void Bind(FloorDropArea owner,ParcelSummary parcel)
        {
            area=owner;parcelId=parcel.id;icon.sprite=owner.parcelSprite;
            icon.color=icon.sprite==null?new Color(.55f,.32f,.12f):Color.white;icon.preserveAspect=true;
            label.text="택배 "+parcel.count+"개\n클릭하여 열기";
        }
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)area.OpenParcel(parcelId);}
    }
}
