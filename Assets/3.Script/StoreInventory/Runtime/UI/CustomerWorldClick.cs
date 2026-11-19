using UnityEngine;
using UnityEngine.EventSystems;
namespace GeneralStore.Inventory
{
    public sealed class CustomerWorldClick:MonoBehaviour,IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler,ICanvasRaycastFilter
    {
        public StagedCustomerInteraction interaction;public SceneCounterOcclusion counter;
        public bool IsRaycastLocationValid(Vector2 point,Camera camera)
        {return interaction!=null&&interaction.AtCounter&&!interaction.Engaged&&(counter==null||!counter.Blocks(point,camera));}
        public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&interaction!=null)interaction.Respond();}
        public void OnPointerEnter(PointerEventData e){if(interaction!=null&&interaction.AtCounter&&!interaction.Engaged)interaction.sceneImage.color=new Color(1,.88f,.65f,1);}
        public void OnPointerExit(PointerEventData e){if(interaction!=null&&interaction.AtCounter)interaction.sceneImage.color=Color.white;}
    }
}
