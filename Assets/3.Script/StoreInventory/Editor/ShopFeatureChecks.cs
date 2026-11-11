using System;
using System.Collections.Generic;
namespace GeneralStore.Inventory.Verification
{
    // 판매/돈/개별 물건 보존과, UI 메시의 순수 좌표 계산을 검증합니다.
    public static class ShopFeatureChecks
    {
        static int count;
        static void Check(bool value,string rule){count++;if(!value)throw new Exception("상점 검사 실패: "+rule);}
        static InventoryModel Model(long gold=100)
        {
            var save=new InventorySave { gold=gold };
            save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=8});
            save.containers.Add(new ContainerRecord{containerId="display_1",columns=6,rows=4});
            save.containers.Add(new ContainerRecord{containerId="display_2",columns=6,rows=4});
            return new InventoryModel(InventoryModelChecks.Fixture(),save);
        }
        static MoveRequest Place(InventoryModel model,string source,string uid,int x=0,int y=0)
        {return new MoveRequest{sourceContainer=source,targetContainer="display_1",instanceId=uid,x=x,y=y,rotation=0,expectedRevision=model.Revision};}
        public static int Run()
        {
            count=0;string error;long revenue;
            InventoryModel model=Model();var session=new RecommendationSession(model);
            Check(!session.TryEnter("손님",1000,1.5,true,out error),"진열 없으면 방문 거절");
            Check(model.TryAdd("warehouse","C001","common",2,80,out error),"물건 두 개 준비");
            var items=model.GetContainer("warehouse").items;string first=items[0].instanceId,second=items[1].instanceId;
            Check(model.TryListOnDisplay(Place(model,"warehouse",first),150,out error),"진열과 가격을 함께 확정");
            int quote;Check(model.TryGetOfferPrice("display_1",first,out quote)&&quote==150,"진열가 조회");
            Check(session.TryEnter("손님",1000,1.5,true,out error),"진열 후 방문");
            Check(session.TryRecommend("display_1",first,out error),"진열 물건 추천");
            Check(!session.TryRecommend("display_1",first,out error),"동일 물건 중복 추천 차단");
            Check(session.TryRecommend("warehouse",second,out error),"창고 물건 추천");
            Check(session.Offers()[1].price==100,"창고 추천은 읽기 전용 기준가");
            long before=model.Revision;
            Check(session.TrySubmit(out revenue,out error)&&revenue==250,"손님 수락과 판매");
            Check(model.Gold==350&&model.Revision==before+1,"돈과 재고를 한 번만 확정");
            Check(model.GetItem("display_1",first)==null&&model.GetItem("warehouse",second)==null,"정확한 두 uid만 차감");
            Check(model.Snapshot().listings.Count==0&&!session.CustomerPresent&&session.Offers().Count==0,"판매 후 진열/손님/목록 정리");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==350,"버튼 연타 이중 지급 차단");

            model=Model();session=new RecommendationSession(model);model.TryAdd("warehouse","C001","common",1,80,out error);
            first=model.GetContainer("warehouse").items[0].instanceId;
            Check(model.TryListOnDisplay(Place(model,"warehouse",first),300,out error),"고가 진열");
            session.TryEnter("손님",1000,1.5,true,out error);session.TryRecommend("display_1",first,out error);
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==100&&model.GetItem("display_1",first)!=null,"가격 거절 시 유지");
            var moveBack=new MoveRequest{sourceContainer="display_1",targetContainer="warehouse",instanceId=first,x=0,y=0,rotation=0,expectedRevision=model.Revision};
            Check(model.TryMove(moveBack,out error)&&model.Snapshot().listings.Count==0,"창고 회수 시 진열 가격 해제");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==100,"이동한 추천 물건 판매 거절");
            Check(model.TryGetOfferPrice("warehouse",first,out quote)&&quote==100,"창고 회수 후 기준가 조회");
            Check(!model.TryListOnDisplay(Place(model,"warehouse",first),-1,out error),"음수 진열가 차단");
            Check(model.GetItem("warehouse",first)!=null,"진열 실패 시 원래 물건 유지");

            model=Model();model.TryAdd("warehouse","C001","common",2,80,out error);items=model.GetContainer("warehouse").items;
            first=items[0].instanceId;second=items[1].instanceId;model.TryListOnDisplay(Place(model,"warehouse",first),100,out error);
            session=new RecommendationSession(model);session.TryEnter("손님",150,2,true,out error);
            session.TryRecommend("display_1",first,out error);session.TryRecommend("warehouse",second,out error);
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==100,"예산 초과 시 판매 거절");
            session.Remove(second);Check(session.TrySubmit(out revenue,out error)&&model.Gold==200,"목록 수정 후 판매 가능");
            Check(model.GetItem("warehouse",second)!=null,"목록에서 뺀 다른 물건은 유지");
            var snapshot=model.Snapshot();var restored=Model(0);
            Check(restored.TryRestore(snapshot,out error)&&restored.Gold==200,"돈과 재고 함께 저장 복원");

            var badSave=restored.Snapshot();badSave.gold=-1;
            Check(!restored.TryRestore(badSave,out error)&&restored.Gold==200,"음수 돈 저장 거절");
            badSave=restored.Snapshot();badSave.listings.Add(new DisplayListing{instanceId="missing",price=100});
            Check(!restored.TryRestore(badSave,out error),"없는 물건 진열 등록 거절");
            model=Model();model.TryAdd("warehouse","C001","common",1,80,out error);first=model.GetContainer("warehouse").items[0].instanceId;
            model.TryListOnDisplay(Place(model,"warehouse",first),100,out error);session=new RecommendationSession(model);
            session.TryEnter("손님",1000,1.5,true,out error);session.TryRecommend("display_1",first,out error);
            Check(model.TryListOnDisplay(Place(model,"display_1",first),110,out error),"진열 다시 확인으로 가격 변경");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==100,"추천 후 가격 변경도 판매 거절");
            session.Remove(first);session.TryRecommend("display_1",first,out error);
            Check(session.TrySubmit(out revenue,out error)&&model.Gold==210,"새 가격으로 재추천 후 판매 가능");
            model=Model(long.MaxValue-50);model.TryAdd("warehouse","C001","common",1,80,out error);first=model.GetContainer("warehouse").items[0].instanceId;
            Check(!model.TrySell(new[]{new SaleLine{containerId="warehouse",instanceId=first,price=100}},model.Revision,out revenue,out error),"돈 범위 초과 거절");
            Check(model.Gold==long.MaxValue-50&&model.GetItem("warehouse",first)!=null,"금액 실패 시 재고도 유지");
            model=Model();model.TryAdd("warehouse","C001","common",1,80,out error);first=model.GetContainer("warehouse").items[0].instanceId;
            var line=new SaleLine{containerId="warehouse",instanceId=first,price=100};
            Check(!model.TrySell(new[]{line,line},model.Revision,out revenue,out error)&&model.GetItem("warehouse",first)!=null,"같은 uid 중복 판매 차단");
            Check(!model.TrySell(new[]{new SaleLine{containerId="warehouse",instanceId=first,price=999}},model.Revision,out revenue,out error),"임의 가격 변조 차단");

            var catalog=InventoryModelChecks.Fixture();var random=new Random(81);
            foreach(string id in new[]{"C001","sample_rope","sample_lframe","sample_cframe"})
                for(int rotation=0;rotation<4;rotation++)
                {
                    ItemFootprint shape=catalog.Shape(id);var placeholder=IconMeshGeometry.Build(shape,rotation,false,1);
                    double area=0;foreach(IconQuad quad in placeholder)area+=(quad.x1-quad.x0)*(quad.y1-quad.y0);
                    Check(placeholder.Count==shape.CellCount&&Math.Abs(area-shape.CellCount)<.0001,"한 메시의 점유 면적 보존");
                    for(int attempt=0;attempt<25;attempt++)
                    {
                        float aspect=(float)(.2+random.NextDouble()*4);
                        foreach(IconQuad quad in IconMeshGeometry.Build(shape,rotation,true,aspect))
                        {
                            Check(shape.Contains(rotation,(int)Math.Floor((quad.x0+quad.x1)*.5f),(int)Math.Floor((quad.y0+quad.y1)*.5f)),"메시가 모양의 구멍을 채우지 않음");
                            foreach(IconPoint uv in new[]{quad.topLeft,quad.topRight,quad.bottomRight,quad.bottomLeft})
                                Check(uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1,"회전/비율 변경 후 UV 범위");
                        }
                    }
                }
            return count;
        }
    }
}
