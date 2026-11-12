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

            // 예약은 물건을 옮기지 않습니다. 같은 품목이어도 UID별로 선택/미선택을 구분합니다.
            model=Model();session=new RecommendationSession(model);
            Check(model.TryAdd("warehouse","C001","common",8,80,out error),"예약 검사 재고 준비");
            items=model.GetContainer("warehouse").items;
            for(int n=0;n<6;n++) Check(model.TryListOnDisplay(Place(model,"warehouse",items[n].instanceId,(n%3)*2,(n/3)*2),100,out error),"여섯 낱개 진열");
            Check(!session.TryEnterShopping("손님",1000,1.5,new[]{"missing"},5,out error)&&!session.CustomerPresent,"선호 품목 없으면 방문 종료");
            Check(!session.TryEnterShopping("손님",99,1.5,new[]{"C001"},5,out error),"예산 이하 후보 없으면 방문 종료");
            Check(session.TryEnterShopping("손님",1000,1.5,new[]{"C001","C001"},5,out error),"선호 진열 상품 선택");
            Check(session.Purchases().Count==5&&session.Offers().Count==0,"최대 다섯 낱개 선택 및 선호 목록 중복 배제");
            first=session.Purchases()[0].instanceId;second=items[5].instanceId;
            Check(model.GetContainer("display_1").items.Count==6&&model.IsReserved(first)&&!model.IsReserved(second),"진열 위치 유지 및 선택한 UID만 잠금");
            session.BeginConversation(out error);
            Check(!session.TryRecommend("display_1",first,out error),"구매 예정 물건 재추천 차단");
            Check(!model.TryRemove("display_1",first,out error),"예약 상품 일반 출고 차단");
            var blocked=new MoveRequest{sourceContainer="display_1",targetContainer="warehouse",instanceId=first,x=0,y=0,expectedRevision=model.Revision};
            Check(!model.Preview(blocked).allowed&&!model.TryMove(blocked,out error),"미리보기와 이동 모두 예약 차단");
            Check(!model.TryListOnDisplay(Place(model,"display_1",first),120,out error),"예약 상품 재가격 설정 차단");
            Check(!model.TrySell(new[]{new SaleLine{containerId="display_1",instanceId=first,price=100}},model.Revision,out revenue,out error),"일반 판매 API로 예약 우회 차단");
            session.Remove(first);Check(session.Purchases().Count==5&&model.IsReserved(first),"추천 제거로 구매 예정 상품을 해제하지 않음");
            Check(session.TryRecommend("display_1",second,out error),"같은 품목의 미선택 UID는 추천 가능");
            Check(session.TrySubmit(out revenue,out error)&&revenue==600&&model.Gold==700,"구매 다섯 개와 추천 한 개를 함께 한 번만 결제");
            Check(model.GetContainer("display_1").items.Count==0&&!model.IsReserved(first)&&model.GetContainer("warehouse").items.Count==2,"판매 물건만 제거 및 예약 정리");
            Check(!session.TrySubmit(out revenue,out error)&&model.Gold==700,"예약 결제 연타 방지");
            Check(model.TryListOnDisplay(Place(model,"warehouse",items[6].instanceId),100,out error),"다음 손님용 진열");
            Check(session.TryEnterShopping("손님",1000,1.5,new[]{"C001"},1,out error),"한 개 선택 설정");
            first=session.Purchases()[0].instanceId;
            long savedGold=model.Gold;session.Leave();
            Check(!model.IsReserved(first)&&model.GetItem("display_1",first)!=null&&model.Gold==savedGold,"거래 거절 시 위치와 돈 유지하며 잠금만 해제");
            Check(session.TryEnterShopping("손님",1000,1.5,new[]{"C001"},1,out error)&&session.TrySubmit(out revenue,out error)&&revenue==100,"추천 없이 구매 예정 물건만 계산 가능");
            Check(model.TryListOnDisplay(Place(model,"warehouse",items[7].instanceId),100,out error),"불러오기 검사 진열");
            session.TryEnterShopping("손님",100,1.5,new[]{"C001"},1,out error);
            first=session.Purchases()[0].instanceId;
            var reservedSnapshot=model.Snapshot();
            var isolated=Model();Check(isolated.TryRestore(reservedSnapshot,out error)&&!isolated.IsReserved(first),"예약은 저장 파일에 포함되지 않음");
            Check(model.TryAdd("warehouse","C001","common",1,80,out error),"예산 초과 추천 준비");
            string extra=model.GetContainer("warehouse").items[0].instanceId;
            session.BeginConversation(out error);
            Check(session.TryRecommend("warehouse",extra,out error)&&!session.TrySubmit(out revenue,out error),"구매 예정 포함 총예산으로 추천 거절");
            Check(model.IsReserved(first)&&model.GetItem("display_1",first)!=null,"추천 거절 시 기존 구매 예정은 유지");
            session.Remove(extra);
            Check(session.TrySubmit(out revenue,out error)&&revenue==100,"거절된 추천 제거 후 기존 구매 결제");

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
