using UnityEngine;
namespace GeneralStore.Inventory
{public sealed class DebugPanelToggle:MonoBehaviour{public GameObject window;public void Toggle(){window.SetActive(!window.activeSelf);}}}
