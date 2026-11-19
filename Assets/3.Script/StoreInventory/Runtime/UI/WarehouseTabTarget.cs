using UnityEngine;
namespace GeneralStore.Inventory
{
    // 공용 드래그 레이캐스트가 읽는 표식. 자체 Update가 없습니다.
    public sealed class WarehouseTabTarget:MonoBehaviour{public WarehouseUI warehouse;public string containerId;}
}
