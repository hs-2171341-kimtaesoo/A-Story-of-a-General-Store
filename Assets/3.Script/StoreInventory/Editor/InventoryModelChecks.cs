using System;
using System.Collections.Generic;
using System.Text;

namespace GeneralStore.Inventory.Verification
{
    // Unity 화면과 무관한 실제 모델 검증입니다. 같은 파일을 외부 C# 실행과 Unity 메뉴에서 사용합니다.
    public static class InventoryModelChecks
    {
        static int checks;
        static void Check(bool condition, string rule)
        { checks++; if (!condition) throw new Exception("실패: " + rule); }
        public static ItemCatalog Fixture(bool swordCanRotate = true)
        {
            return new ItemCatalog(new ItemDatabase { items = new[] {
                Definition("C001",2,2,true,new[]{"common","superior"}),
                Definition("E001",2,1,true,new[]{"none"}),
                Definition("sample_rope",3,1,true,new[]{"common"}),
                Definition("sample_sword",1,3,swordCanRotate,new[]{"common"}),
                Definition("sample_bag",3,2,true,new[]{"common"}),
                Frame("sample_lframe",new[]{new GridCell(0,0),new GridCell(0,1),new GridCell(0,2),new GridCell(1,2),new GridCell(2,2)}),
                Frame("sample_cframe",new[]{new GridCell(0,0),new GridCell(1,0),new GridCell(2,0),new GridCell(0,1),new GridCell(0,2),new GridCell(1,2),new GridCell(2,2)})
            } });
        }
        static ItemDefinition Frame(string id, GridCell[] cells)
        {
            ItemDefinition definition = Definition(id,3,3,true,new[]{"common"});
            definition.occupiedCells = cells; return definition;
        }
        static ItemDefinition Definition(string id, int width, int height, bool rotate, string[] codes)
        {
            var qualities = new QualityDefinition[codes.Length];
            for (int i = 0; i < codes.Length; i++) qualities[i] = new QualityDefinition {
                quality = codes[i], name = id, priceMultiplier = 1, iconKey = id, explanation = "test" };
            return new ItemDefinition { id = id, type = "test", basePrice = 100,
                width = width, height = height, canRotate = rotate, qualities = qualities };
        }
        static InventoryModel NewModel(ItemCatalog catalog, int columns = 6, int rows = 4)
        {
            var save = new InventorySave();
            save.containers.Add(new ContainerRecord { containerId = "warehouse", columns = columns, rows = rows });
            save.containers.Add(new ContainerRecord { containerId = "display_preview", columns = 6, rows = 4 });
            return new InventoryModel(catalog, save);
        }
        static MoveRequest Move(InventoryModel model, string source, string uid, string target, int x, int y, int rotation)
        {
            return new MoveRequest { sourceContainer = source, targetContainer = target, instanceId = uid,
                x = x, y = y, rotation = rotation, expectedRevision = model.Revision };
        }
        static string Fingerprint(InventoryModel model)
        {
            var text = new StringBuilder();
            foreach (ContainerRecord c in model.Snapshot().containers)
            {
                text.Append(c.containerId).Append(':').Append(c.columns).Append(',').Append(c.rows);
                foreach (ItemRecord item in c.items) text.Append('|').Append(item.instanceId).Append(',').Append(item.itemId)
                    .Append(',').Append(item.quality).Append(',').Append(item.x).Append(',').Append(item.y)
                    .Append(',').Append(item.rotation).Append(',').Append(item.purchasePrice);
            }
            return text.ToString();
        }
        static void Geometry(InventoryModel model)
        {
            var ids = new HashSet<string>();
            foreach (ContainerRecord container in model.Snapshot().containers)
            {
                var occupied = new HashSet<int>();
                foreach (ItemRecord item in container.items)
                {
                    Check(ids.Add(item.instanceId), "전역 개별 ID 중복 없음");
                    foreach (GridCell offset in model.Cells(item))
                    {
                        int x = item.x + offset.x, y = item.y + offset.y;
                        Check(x >= 0 && x < container.columns && y >= 0 && y < container.rows, "모든 점유 칸이 경계 안");
                        Check(occupied.Add(y * container.columns + x), "모든 점유 칸이 서로 다름");
                    }
                }
            }
        }
        public static int Run()
        {
            checks = 0; ItemCatalog catalog = Fixture();
            InventoryModel model = NewModel(catalog); string error;
            Check(model.TryAdd("warehouse", "C001", "common", 1, 80, out error), "첫 입고");
            Check(model.TryAdd("warehouse", "C001", "common", 1, 100, out error), "동일 종류/품질 재입고");
            var items = model.GetContainer("warehouse").items; ItemRecord first = items[0], second = items[1];
            Check(items.Count == 2 && first.instanceId != second.instanceId, "낱개 ID 분리");
            Check(first.purchasePrice == 80 && second.purchasePrice == 100, "개별 매입가 보존");
            string before = Fingerprint(model); long revision = model.Revision;
            Check(!model.TryMove(Move(model,"warehouse",first.instanceId,"warehouse",second.x,second.y,0),out error), "같은 물건 위에도 중첩 금지");
            Check(before == Fingerprint(model) && revision == model.Revision, "실패 후 원본 유지");
            var request = Move(model,"warehouse",first.instanceId,"display_preview",0,0,0);
            Check(model.CanMove(request,out error) && before == Fingerprint(model) && revision == model.Revision, "미리보기는 변경 없음");
            Check(model.TryMove(request,out error), "다른 컨테이너 이동");
            Check(model.GetItem("display_preview",first.instanceId).purchasePrice == 80
                && model.GetContainer("warehouse").items.Count == 1, "이동 후 ID/매입가/개수 보존");
            Check(model.TryRemove("display_preview",first.instanceId,out error), "특정 물건 한 개 제거");
            Check(!model.TryRemove("display_preview",first.instanceId,out error), "중복 차감 거절");
            Check(model.GetItem("warehouse",second.instanceId) != null, "다른 동일 상품은 유지");
            InventoryModel tight = NewModel(catalog,3,2);
            Check(!tight.TryAdd("warehouse","C001","common",2,80,out error), "다 들어갈 수 없으면 전체 입고 거절");
            Check(tight.GetContainer("warehouse").items.Count == 0 && tight.Revision == 0, "부분 입고 롤백");
            Check(tight.TryAdd("warehouse","C001","common",1,80,out error), "한 개는 입고 가능");
            Check(!tight.TryAdd("warehouse","C001","common",1,80,out error), "같은 상품도 빈 공간 필요");
            Check(!tight.TryAdd("warehouse","C001","common",0,80,out error), "0개 입고 거절");
            Check(!tight.TryAdd("warehouse","C001","missing",1,80,out error), "없는 품질 거절");
            InventoryModel rotating = NewModel(catalog);
            Check(rotating.TryAdd("warehouse","sample_rope","common",1,40,out error), "3x1 입고");
            ItemRecord rope = rotating.GetContainer("warehouse").items[0];
            Check(rotating.TryMove(Move(rotating,"warehouse",rope.instanceId,"warehouse",5,0,1),out error), "1x3 회전 이동");
            ItemRecord turned = rotating.GetItem("warehouse",rope.instanceId);
            Check(rotating.Width(turned)==1 && rotating.Height(turned)==3, "회전 크기");
            Check(rotating.TryMove(Move(rotating,"warehouse",rope.instanceId,"warehouse",3,3,0),out error), "회전 해제");
            Check(!rotating.TryMove(Move(rotating,"warehouse",rope.instanceId,"warehouse",4,3,0),out error), "경계 밖 거절");
            var stale = Move(rotating,"warehouse",rope.instanceId,"display_preview",0,0,0);
            Check(rotating.TryAdd("display_preview","E001","none",1,0,out error), "외부 재고 변경");
            Check(!rotating.TryMove(stale,out error), "오래된 요청 거절");
            InventoryModel locked = NewModel(Fixture(false));
            Check(locked.TryAdd("warehouse","sample_sword","common",1,20,out error), "회전 잠금 물건 입고");
            ItemRecord sword = locked.GetContainer("warehouse").items[0];
            Check(!locked.TryMove(Move(locked,"warehouse",sword.instanceId,"display_preview",0,0,1),out error), "회전 잠금 검사");
            InventorySave snapshot = model.Snapshot();
            InventoryModel restored = NewModel(catalog);
            Check(restored.TryRestore(snapshot,out error) && Fingerprint(restored)==Fingerprint(model), "저장 스냅샷 복원");
            snapshot.containers[0].items[0].x = -1;
            before = Fingerprint(restored);
            Check(!restored.TryRestore(snapshot,out error) && before==Fingerprint(restored), "잘못된 저장 후 원본 유지");
            snapshot = restored.Snapshot(); snapshot.containers[0].items.Add(snapshot.containers[0].items[0].Copy());
            Check(!restored.TryRestore(snapshot,out error), "저장 중복 ID 거절");
            snapshot = restored.Snapshot(); ItemRecord overlapping = snapshot.containers[0].items[0].Copy();
            overlapping.instanceId = Guid.NewGuid().ToString("N"); snapshot.containers[0].items.Add(overlapping);
            Check(!restored.TryRestore(snapshot,out error), "다른 ID라도 겹치는 저장 거절");
            snapshot = restored.Snapshot(); snapshot.format = "old-stack-format";
            Check(!restored.TryRestore(snapshot,out error), "다른 포맷 거절");
            // 반환한 조회 객체를 수정해도 모델의 원본은 바뀌지 않아야 합니다.
            ItemRecord copy = model.GetItem("warehouse",second.instanceId); copy.purchasePrice=999;
            Check(model.GetItem("warehouse",second.instanceId).purchasePrice==100,"조회는 복사본");

            ShapeChecks(catalog);
            InventoryModel demo=NewModel(catalog,10,8);
            Check(demo.TryAdd("warehouse","C001","common",2,80,out error),"데모 일반 포션 2개");
            Check(demo.TryAdd("warehouse","C001","common",1,100,out error),"데모 추가 포션");
            Check(demo.TryAdd("warehouse","C001","superior",1,110,out error),"데모 상급 포션");
            Check(demo.TryAdd("warehouse","E001","none",2,0,out error),"데모 빵");
            Check(demo.TryAdd("warehouse","sample_rope","common",1,40,out error),"데모 밧줄");
            Check(demo.TryAdd("warehouse","sample_sword","common",1,90,out error),"데모 검");
            Check(demo.TryAdd("warehouse","sample_bag","common",1,150,out error),"데모 가방");
            Check(demo.TryAdd("warehouse","sample_lframe","common",1,50,out error),"데모 ㄱ자");
            Check(demo.TryAdd("warehouse","sample_cframe","common",1,70,out error),"데모 ㄷ자");
            Check(demo.GetContainer("warehouse").items.Count==11,"데모 전체 11개 배치");
            Geometry(demo);
            // 무작위 이동 후에도 ID/매입가/전체 물건 수와 실제 점유 칸을 독립적으로 검사합니다.
            var random = new Random(4242); InventoryModel fuzz = NewModel(catalog,8,6);
            fuzz.TryAdd("warehouse","C001","common",2,80,out error);
            fuzz.TryAdd("warehouse","sample_rope","common",1,40,out error);
            fuzz.TryAdd("warehouse","sample_sword","common",1,90,out error);
            fuzz.TryAdd("warehouse","sample_bag","common",1,150,out error);
            Check(fuzz.TryAdd("warehouse","sample_lframe","common",1,50,out error),"무작위 ㄱ자 준비");
            Check(fuzz.TryAdd("warehouse","sample_cframe","common",1,70,out error),"무작위 ㄷ자 준비");
            var prices = new Dictionary<string,int>();
            foreach (ItemRecord item in fuzz.GetContainer("warehouse").items) prices.Add(item.instanceId,item.purchasePrice);
            for (int iteration=0; iteration<1000; iteration++)
            {
                var candidates = new List<ItemRecord>(); var locations = new List<string>();
                foreach (ContainerRecord container in fuzz.Snapshot().containers)
                    foreach (ItemRecord item in container.items) { candidates.Add(item); locations.Add(container.containerId); }
                int index=random.Next(candidates.Count); string targetId=random.Next(2)==0?"warehouse":"display_preview";
                ContainerRecord target=fuzz.GetContainer(targetId);
                MoveRequest move=Move(fuzz,locations[index],candidates[index].instanceId,targetId,
                    random.Next(-1,target.columns+1),random.Next(-1,target.rows+1),random.Next(4));
                before=Fingerprint(fuzz); revision=fuzz.Revision;
                bool preview=fuzz.CanMove(move,out error);
                Check(Fingerprint(fuzz)==before && fuzz.Revision==revision,"무작위 미리보기도 읽기 전용");
                bool success=fuzz.TryMove(move,out error); Check(preview==success,"미리보기/실제 판정 일치");
                if (!success) Check(Fingerprint(fuzz)==before && fuzz.Revision==revision,"무작위 실패도 원본 유지");
                int count=0;
                foreach (ContainerRecord container in fuzz.Snapshot().containers)
                    foreach (ItemRecord item in container.items)
                    { count++; Check(prices.ContainsKey(item.instanceId) && prices[item.instanceId]==item.purchasePrice,"무작위 이동 후 ID/매입가 보존"); }
                Check(count==prices.Count,"무작위 이동 후 물건 수 보존"); Geometry(fuzz);
            }
            return checks;
        }
        static void ShapeChecks(ItemCatalog catalog)
        {
            string error;
            ItemFootprint shape = catalog.Shape("sample_lframe");
            Check(shape.CellCount == 5 && shape.Width(0) == 3 && shape.Height(0) == 3,"ㄱ자는 바깥 9칸/실제 5칸");
            Check(!shape.Contains(0,1,0) && shape.Contains(0,0,0),"ㄱ자의 빈 부분/채운 부분 구분");
            GridCell[] copied = shape.Cells(0); copied[0] = new GridCell(99,99);
            Check(shape.Contains(0,0,0),"외부 좌표 배열 수정으로 정의가 변하지 않음");
            for (int direction=0;direction<4;direction++)
            {
                var keys=new HashSet<string>();
                foreach (GridCell cell in shape.Cells(direction))
                {
                    Check(cell.x>=0 && cell.x<shape.Width(direction) && cell.y>=0 && cell.y<shape.Height(direction),"회전 좌표 범위");
                    Check(keys.Add(cell.x+","+cell.y),"회전 점유 중복 없음");
                    Check(shape.Contains(direction,cell.x,cell.y),"클릭 마스크와 점유 좌표 일치");
                }
                Check(keys.Count==5,"모든 방향에서 점유 칸 수 보존");
                for(int y=0;y<shape.Height(direction);y++) for(int x=0;x<shape.Width(direction);x++)
                    Check(shape.Contains(direction,x,y)==keys.Contains(x+","+y),"회전된 빈칸/점유 칸 클릭 마스크 일치");
                Check(!shape.Contains(direction,-1,0) && !shape.Contains(direction,shape.Width(direction),0),"모양 밖 클릭 거절");
            }
            foreach (GridCell origin in shape.Cells(0))
            {
                int x=origin.x,y=origin.y;
                for(int direction=0;direction<4;direction++)
                { int previousX=x; x=shape.Height(direction)-1-y; y=previousX; }
                Check(x==origin.x && y==origin.y,"네 번 회전 후 잡은 칸 복원");
            }
            // 같은 3x3 바깥 사각형 안에 ㄱ자와 2x2 포션을 함께 넣습니다.
            InventoryModel nested=NewModel(catalog,3,3);
            Check(nested.TryAdd("warehouse","sample_lframe","common",1,50,out error),"ㄱ자 입고");
            Check(nested.TryAdd("warehouse","C001","common",1,80,out error),"ㄱ자 빈 공간에 포션 배치");
            ContainerRecord container=nested.GetContainer("warehouse");
            ItemRecord frame=container.items.Find(i=>i.itemId=="sample_lframe");
            ItemRecord potion=container.items.Find(i=>i.itemId=="C001");
            Check(potion.x==1 && potion.y==0 && container.items.Count==2,"서로 겹친 바깥 사각형 허용");
            PlacementPreview good=nested.Preview(Move(nested,"warehouse",frame.instanceId,"warehouse",0,0,0));
            Check(good.allowed && good.cells.Count==5 && good.cells.TrueForAll(c=>c.state==PlacementCellState.Free),"정상 위치는 점유 5칸만 초록");
            PlacementPreview bad=nested.Preview(Move(nested,"warehouse",frame.instanceId,"warehouse",0,0,1));
            Check(!bad.allowed && bad.cells.FindAll(c=>c.state==PlacementCellState.Occupied).Count==2,"회전 충돌 칸 정확히 검출");
            long before=nested.Revision;
            Check(!nested.TryMove(Move(nested,"warehouse",frame.instanceId,"warehouse",0,0,1),out error) && nested.Revision==before,"빨간 위치 실제 배치 거절");
            PlacementPreview outside=nested.Preview(Move(nested,"warehouse",frame.instanceId,"warehouse",-1,0,0));
            Check(!outside.allowed && outside.cells.Exists(c=>c.state==PlacementCellState.Outside),"격자 밖 칸 표시");
            Check(!nested.CanMove(Move(nested,"warehouse",frame.instanceId,"warehouse",int.MaxValue,int.MaxValue,0),out error),"큰 좌표도 오버플로 없이 거절");
            Check(!nested.CanMove(Move(nested,"warehouse",frame.instanceId,"warehouse",0,0,4),out error),"잘못된 회전 거절");
            Geometry(nested);
            InventoryModel cShape=NewModel(catalog,3,3);
            Check(cShape.TryAdd("warehouse","sample_cframe","common",1,70,out error),"ㄷ자 입고");
            Check(cShape.TryAdd("warehouse","E001","none",1,0,out error),"ㄷ자 빈 2x1에 빵 배치");
            ItemRecord bread=cShape.GetContainer("warehouse").items.Find(i=>i.itemId=="E001");
            Check(bread.x==1 && bread.y==1,"ㄷ자 내부 위치");
            Geometry(cShape);
            InventoryModel restored=NewModel(catalog);
            Check(restored.TryRestore(nested.Snapshot(),out error),"서로 끼워진 모양 저장 복원");
            Geometry(restored);
            bool rejected=false;
            try { new ItemFootprint(2,2,new[]{new GridCell(0,0),new GridCell(0,0),new GridCell(1,1)}); }
            catch(ArgumentException) { rejected=true; }
            Check(rejected,"중복 점유 좌표 거절");
            rejected=false;
            try { new ItemFootprint(2,2,new[]{new GridCell(0,0),new GridCell(2,1)}); }
            catch(ArgumentException) { rejected=true; }
            Check(rejected,"범위 밖 점유 좌표 거절");
            rejected=false;
            try { new ItemFootprint(3,3,new[]{new GridCell(0,0),new GridCell(1,1)}); }
            catch(ArgumentException) { rejected=true; }
            Check(rejected,"불필요한 바깥 여백 거절");
        }

    }
}
