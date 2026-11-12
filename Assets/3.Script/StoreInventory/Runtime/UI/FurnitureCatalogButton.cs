using UnityEngine;
namespace GeneralStore.Inventory
{
    // 생성된 가구 목록 버튼은 ID만 보관합니다. 가구/가격 정보는 공통 모델에서 읽습니다.
    public sealed class FurnitureCatalogButton : MonoBehaviour
    {
        public UpgradeUI upgrade;public string typeId;
        public void Select(){upgrade.SelectType(typeId);}
    }
}
