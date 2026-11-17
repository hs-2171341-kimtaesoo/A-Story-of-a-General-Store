using System;
using GeneralStore.Inventory;
namespace GeneralStore.Inventory.Verification
{
    // 렌더링/포인터 입력이 아니라 순수 C# 데이터와 배치 수학을 검증합니다.
    public static class SceneLayerFeatureChecks
    {
        static int count;static void Check(bool ok,string message){count++;if(!ok)throw new Exception("레이어 검사: "+message);}
        public static int Run()
        {
            count=0;Check(SceneLayerOrder.Background<SceneLayerOrder.Furniture&&SceneLayerOrder.Furniture<SceneLayerOrder.Counter&&SceneLayerOrder.Counter<SceneLayerOrder.UI,"그리기 순서");
            var mask=new PackedHitMask(new PackedHitMaskData{width=2,height=2,bits=Convert.ToBase64String(new byte[]{9})});
            Check(mask.Contains(.1f,.1f)&&mask.Contains(.9f,.9f),"마스크 왼쪽위/오른쪽아래");
            Check(!mask.Contains(.9f,.1f)&&!mask.Contains(.1f,.9f),"투명 구역");
            Check(!mask.Contains(-.1f,0)&&!mask.Contains(1,1)&&!mask.Contains(float.NaN,0),"범위 밖 입력");
            bool invalid=false;try{new PackedHitMask(new PackedHitMaskData{width=16,height=16,bits=Convert.ToBase64String(new byte[]{0})});}catch(ArgumentException){invalid=true;}Check(invalid,"잘못된 데이터 길이 거절");
            var b=new[]{.1f,.1f,.9f,.9f};var outline=new[]{.05f,.05f,.95f,.95f};
            var p=FurnitureOutlineGeometry.Align(100,200,200,400,b,outline,1);
            Check(Math.Abs(p.x+outline[0]*p.width-19)<.001f&&Math.Abs(p.y+outline[1]*p.height-39)<.001f,"투명 여백과 확대 배율을 반영한 왼쪽위");
            Check(Math.Abs((outline[2]-outline[0])*p.width-162)<.001f&&Math.Abs((outline[3]-outline[1])*p.height-322)<.001f,"실제 외곽 경계 크기");
            var q=FurnitureOutlineGeometry.Align(100,200,400,400,b,outline,0);
            Check(Math.Abs(q.x+outline[0]*q.width-120)<.001f,"비율 유지의 가로 레터박스 반영");
            return count;
        }
    }
}
