using UnityEngine;
namespace GeneralStore.Inventory
{
    public static class SidePanelInput
    {
        // 관리/구매/거래처 패널의 전체 화면 장식 배경만 통과시킵니다.
        // 자식 Panel Frame과 버튼의 Raycast는 바꾸지 않고, 가격 확인 모달에도 적용하지 않습니다.
        public static void AllowOutside(GameObject window)
        {if(window==null)return;var overlay=window.GetComponent<global::UnityEngine.UI.Image>();if(overlay!=null)overlay.raycastTarget=false;}
    }
}
