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
            var canvas=go.GetComponent<Canvas>();if(canvas==null)canvas=go.AddComponent<Canvas>();
            canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
            // 중첩 Canvas의 렌더는 부모 순서를 따르고, Raycaster 우선순위도 최상위 UI와 맞춥니다.
            if(canvas.rootCanvas!=null&&canvas.rootCanvas!=canvas){canvas.overrideSorting=false;canvas.sortingOrder=canvas.rootCanvas.sortingOrder;}
            if(interactive&&go.GetComponent<GraphicRaycaster>()==null)go.AddComponent<GraphicRaycaster>();
        }
    }
}
