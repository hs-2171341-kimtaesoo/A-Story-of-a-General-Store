using Unity.Profiling;
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
