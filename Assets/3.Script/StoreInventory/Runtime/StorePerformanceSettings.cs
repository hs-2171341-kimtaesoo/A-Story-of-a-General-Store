using Unity.Profiling;
using UnityEngine;

// StorePerformanceSettings.cs
namespace GeneralStore.Inventory
{
    // 프로젝트의 기존 품질 설정에 의존하지 않고 이 데모의 렌더링 빈도를 제한합니다.
    // 정지/컴포넌트 비활성화 시 이전 값을 복원합니다. 게임 규칙/시간 배속은 바꾸지 않습니다.
    [DisallowMultipleComponent]
    public sealed class StorePerformanceSettings : MonoBehaviour
    {
        [Tooltip("Rendering limit only. This does not change clock speed or crafting time.")]
        [Range(15,240)] public int foregroundFps=30;
        [Tooltip("Rendering limit when the application loses focus.")]
        [Range(5,60)] public int backgroundFps=10;
        public bool limitFrameRate=true;
        static StorePerformanceSettings owner;
        int previousFps,previousVsync;bool focused=true;
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            if(owner!=null&&owner!=this){Debug.LogWarning("성능 설정이 중복되어 기존 설정을 유지합니다.",this);return;}
            owner=this;previousFps=Application.targetFrameRate;previousVsync=QualitySettings.vSyncCount;Apply();
        }
        public void Apply()
        {
            if(owner!=this)return;
            QualitySettings.vSyncCount=limitFrameRate?0:previousVsync;
            Application.targetFrameRate=limitFrameRate?Mathf.Clamp(focused?foregroundFps:backgroundFps,5,240):previousFps;
        }
        void OnApplicationFocus(bool value){focused=value;Apply();}
        void OnValidate(){if(Application.isPlaying)Apply();}
        void OnDisable()
        {if(owner==this){Application.targetFrameRate=previousFps;QualitySettings.vSyncCount=previousVsync;owner=null;}}
    }
}

// StoreProfiler.cs
namespace GeneralStore.Inventory
{
    public static class StoreProfiler
    {
        public static readonly ProfilerMarker CounterRefresh=new ProfilerMarker("Store.Counter.Refresh");
        public static readonly ProfilerMarker Clock=new ProfilerMarker("Store.Clock.Tick");
        public static readonly ProfilerMarker DragRaycast=new ProfilerMarker("Store.Drag.Raycast");
        public static readonly ProfilerMarker Preview=new ProfilerMarker("Store.Drag.Preview");
    }
}
