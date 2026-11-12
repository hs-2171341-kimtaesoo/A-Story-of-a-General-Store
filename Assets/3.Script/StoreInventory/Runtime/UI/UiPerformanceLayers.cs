using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public static class UiPerformanceLayers
    {
        // 낙하/고스트의 매 프레임 이동이 정적인 배경·창 UI까지 다시 배칭하지 않게 분리합니다.
        // 정렬 순서를 강제로 덮어쓰지 않아 기존 Hierarchy 순서를 유지합니다.
        public static void Ensure(GameObject go,bool interactive)
        {
            if(go.GetComponent<Canvas>()==null)go.AddComponent<Canvas>();
            if(interactive&&go.GetComponent<GraphicRaycaster>()==null)go.AddComponent<GraphicRaycaster>();
        }
    }
}
