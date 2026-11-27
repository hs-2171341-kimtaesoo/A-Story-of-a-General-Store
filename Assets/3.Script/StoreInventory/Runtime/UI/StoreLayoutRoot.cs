using UnityEngine;

namespace GeneralStore.Inventory
{
    // This is the editable game layout, not a runtime UI factory. No layout is built in Awake/Start.
    [DisallowMultipleComponent]
    public sealed class StoreLayoutRoot : MonoBehaviour
    {
        [HideInInspector] public int schemaVersion=17;
        public ButtonArtworkCatalog buttonArtwork;
        public global::UnityEngine.UI.Image backgroundImage;
        public InventoryRuntime inventory;
        public InventoryPanel panel;
        public DisplayManagementUI management;
        public UpgradeUI purchases;
        public ShopViewportLayout viewport;
        public StoreHotkeys hotkeys;
    }
}
