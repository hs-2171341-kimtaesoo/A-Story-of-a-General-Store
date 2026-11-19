using UnityEngine;
using UnityEngine.UI;
namespace GeneralStore.Inventory
{
    public sealed class DrawerChevronGraphic:global::UnityEngine.UI.MaskableGraphic
    {
        public WarehouseDrawer drawer;
        protected override void OnEnable(){base.OnEnable();if(drawer!=null)drawer.StateChanged+=Changed;}
        protected override void OnDisable(){if(drawer!=null)drawer.StateChanged-=Changed;base.OnDisable();}
        void Changed(bool opened){SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var rect=GetPixelAdjustedRect();bool down=drawer!=null&&drawer.IsOpen;
            var v=UIVertex.simpleVert;v.color=color;
            v.position=new Vector3(rect.xMin,down?rect.yMax:rect.yMin,0);helper.AddVert(v);
            v.position=new Vector3(rect.xMax,down?rect.yMax:rect.yMin,0);helper.AddVert(v);
            v.position=new Vector3((rect.xMin+rect.xMax)*.5f,down?rect.yMin:rect.yMax,0);helper.AddVert(v);helper.AddTriangle(0,1,2);
        }
    }
}
