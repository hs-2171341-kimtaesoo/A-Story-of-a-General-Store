#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using GeneralStore.Inventory;
// 실제 CanvasGroup 차단/복원은 Unity Editor의 Run Model Checks에서 검사합니다.
public static class WindowShieldUnityChecks
{
    public static void Run()
    {
        var root=new GameObject("Window Shield Check",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        try
        {
            var bounds=(RectTransform)root.transform;bounds.sizeDelta=new Vector2(1920,1080);
            var moving=new GameObject("Moving",typeof(RectTransform));moving.transform.SetParent(bounds,false);
            var other=new GameObject("Behind",typeof(RectTransform));other.transform.SetParent(bounds,false);other.AddComponent<DisplayWindowView>();
            var group=other.AddComponent<CanvasGroup>();group.blocksRaycasts=true;
            var shield=root.AddComponent<WindowDragInputShield>();
            if(!shield.Begin(shield,(RectTransform)moving.transform,bounds)||group.blocksRaycasts)throw new System.Exception("뒤쪽 창 입력 차단 실패");
            shield.End(shield);if(!group.blocksRaycasts)throw new System.Exception("입력 복원 실패");
            group.blocksRaycasts=false;shield.Begin(shield,(RectTransform)moving.transform,bounds);shield.End(shield);
            if(group.blocksRaycasts)throw new System.Exception("기존 입력 비활성 상태 보존 실패");
            Debug.Log("창 드래그 입력 차단/원래 상태 복원 검사 통과");
        }
        finally{Object.DestroyImmediate(root);}
    }
}
#endif
