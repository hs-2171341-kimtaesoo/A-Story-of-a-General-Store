#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using GeneralStore.Inventory;
// Unity Editor의 Run Model Checks에서 실제 비활성 계층을 만들어 검사합니다.
// 로컬 대체 API 검사는 이 메서드를 실행하지 않고 컴파일만 합니다.
public static class InactiveWindowReferenceChecks
{
    public static void Run()
    {
        var root=new GameObject("Inactive Scroll Reference Check",typeof(RectTransform));root.SetActive(false);
        try
        {
            var scroll=root.AddComponent<ScrollRect>();
            var child=new GameObject("Inactive Grid",typeof(RectTransform));child.transform.SetParent(root.transform,false);
            if(DisplayWindowAppearance.FindScrollInParents(child.transform)!=scroll)
                throw new System.InvalidOperationException("비활성 진열창의 ScrollRect 검색 실패");
            if(DisplayWindowAppearance.FindScrollInParents(null)!=null)
                throw new System.InvalidOperationException("없는 부모 검색 결과 오류");
            Debug.Log("비활성 진열창 ScrollRect 참조 검사 통과");
        }
        finally{Object.DestroyImmediate(root);}
    }
}
#endif
