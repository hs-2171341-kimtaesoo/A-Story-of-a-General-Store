using UnityEngine;
using UnityEngine.UI;

namespace GeneralStore.Inventory
{
    // 초록/빨강도 타일별 오브젝트 대신 한 UI 메시로 그립니다.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PlacementPreviewGraphic : global::UnityEngine.UI.MaskableGraphic
    {
        // 이미 만들어 둔 씬/프리팹에는 RequireComponent 추가만으로 누락이 복구되지 않을 수 있습니다.
        // 마스크 등록(base.OnEnable) 전에 렌더러를 보장하여 RectMask2D 오류를 막습니다.
        protected override void OnEnable()
        {
            if (GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }

        PlacementPreview preview;
        float cellSize;
        public void Show(PlacementPreview value,float cell) { preview=value;cellSize=cell;SetVerticesDirty(); }
        public void ClearPreview() { preview=null;SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); if(preview==null) return; Rect rect=GetPixelAdjustedRect();
            foreach(PlacementCell cell in preview.cells)
            {
                Color tint=preview.allowed?new Color(.1f,1,.35f,.5f):cell.state==PlacementCellState.Free
                    ?new Color(1,.15f,.15f,.28f):new Color(1,.05f,.05f,.7f);
                float x=rect.xMin+(float)cell.x*cellSize+1,y=rect.yMax-(float)cell.y*cellSize-1,size=cellSize-2;
                int start=helper.currentVertCount;
                Add(helper,x,y,tint);Add(helper,x+size,y,tint);Add(helper,x+size,y-size,tint);Add(helper,x,y-size,tint);
                helper.AddTriangle(start,start+1,start+2);helper.AddTriangle(start+2,start+3,start);
            }
        }
        static void Add(VertexHelper helper,float x,float y,Color tint)
        { UIVertex vertex=UIVertex.simpleVert;vertex.position=new Vector3(x,y,0);vertex.color=tint;vertex.uv0=Vector2.zero;helper.AddVert(vertex); }
    }
}
