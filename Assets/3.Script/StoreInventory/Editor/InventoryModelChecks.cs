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
                Definition("sample_lframe",3,3,true,new[]{"common"}),
                Definition("sample_cframe",3,3,true,new[]{"common"})
            } });
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
            save.containers.Add(new ContainerRecord { containerId = "test_storage", columns = columns, rows = rows });
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
            // 돈 추가 버튼은 품목/배치를 바꾸지 않고, 잘못된 금액과 오버플로를 거절합니다.
            var money=NewModel(catalog);string moneyError;long goldBefore=money.Gold;string inventoryBefore=Fingerprint(money);
            Check(money.TryAddDebugGold(10000,out moneyError)&&money.Gold==goldBefore+10000,"테스트 소지금 추가");
            Check(Fingerprint(money)==inventoryBefore,"돈 추가는 재고 배치 보존");
            long moneyRevision=money.Revision;
            Check(!money.TryAddDebugGold(0,out moneyError)&&!money.TryAddDebugGold(-1,out moneyError)&&money.Revision==moneyRevision,"0/음수 돈 추가 거절");
            Check(money.TryAddDebugGold(long.MaxValue-money.Gold,out moneyError),"소지금 경계값 도달");moneyRevision=money.Revision;
            Check(!money.TryAddDebugGold(1,out moneyError)&&money.Gold==long.MaxValue&&money.Revision==moneyRevision,"돈 오버플로 원자적 거절");
            InventoryModel model = NewModel(catalog); string error;
            Check(model.TryAdd("test_storage", "C001", "common", 1, 80, out error), "첫 입고");
            Check(model.TryAdd("test_storage", "C001", "common", 1, 100, out error), "동일 종류/품질 재입고");
            var items = model.GetContainer("test_storage").items; ItemRecord first = items[0], second = items[1];
            Check(items.Count == 2 && first.instanceId != second.instanceId, "낱개 ID 분리");
            Check(first.purchasePrice == 80 && second.purchasePrice == 100, "개별 매입가 보존");
            string before = Fingerprint(model); long revision = model.Revision;
            Check(!model.TryMove(Move(model,"test_storage",first.instanceId,"test_storage",second.x,second.y,0),out error), "같은 물건 위에도 중첩 금지");
            Check(before == Fingerprint(model) && revision == model.Revision, "실패 후 원본 유지");
            var request = Move(model,"test_storage",first.instanceId,"display_preview",0,0,0);
            Check(model.CanMove(request,out error) && before == Fingerprint(model) && revision == model.Revision, "미리보기는 변경 없음");
            Check(model.TryMove(request,out error), "다른 컨테이너 이동");
            Check(model.GetItem("display_preview",first.instanceId).purchasePrice == 80
                && model.GetContainer("test_storage").items.Count == 1, "이동 후 ID/매입가/개수 보존");
            Check(model.TryRemove("display_preview",first.instanceId,out error), "특정 물건 한 개 제거");
            Check(!model.TryRemove("display_preview",first.instanceId,out error), "중복 차감 거절");
            Check(model.GetItem("test_storage",second.instanceId) != null, "다른 동일 상품은 유지");
            InventoryModel tight = NewModel(catalog,3,2);
            Check(!tight.TryAdd("test_storage","C001","common",2,80,out error), "다 들어갈 수 없으면 전체 입고 거절");
            Check(tight.GetContainer("test_storage").items.Count == 0 && tight.Revision == 0, "부분 입고 롤백");
            Check(tight.TryAdd("test_storage","C001","common",1,80,out error), "한 개는 입고 가능");
            Check(!tight.TryAdd("test_storage","C001","common",1,80,out error), "같은 상품도 빈 공간 필요");
            Check(!tight.TryAdd("test_storage","C001","common",0,80,out error), "0개 입고 거절");
            Check(!tight.TryAdd("test_storage","C001","missing",1,80,out error), "없는 품질 거절");
            InventoryModel rotating = NewModel(catalog);
            Check(rotating.TryAdd("test_storage","sample_rope","common",1,40,out error), "3x1 입고");
            ItemRecord rope = rotating.GetContainer("test_storage").items[0];
            Check(rotating.TryMove(Move(rotating,"test_storage",rope.instanceId,"test_storage",5,0,1),out error), "1x3 회전 이동");
            ItemRecord turned = rotating.GetItem("test_storage",rope.instanceId);
            Check(rotating.Width(turned)==1 && rotating.Height(turned)==3, "회전 크기");
            Check(rotating.TryMove(Move(rotating,"test_storage",rope.instanceId,"test_storage",3,3,0),out error), "회전 해제");
            Check(!rotating.TryMove(Move(rotating,"test_storage",rope.instanceId,"test_storage",4,3,0),out error), "경계 밖 거절");
            var stale = Move(rotating,"test_storage",rope.instanceId,"display_preview",0,0,0);
            Check(rotating.TryAdd("display_preview","E001","none",1,0,out error), "외부 재고 변경");
            Check(!rotating.TryMove(stale,out error), "오래된 요청 거절");
            InventoryModel locked = NewModel(Fixture(false));
            Check(locked.TryAdd("test_storage","sample_sword","common",1,20,out error), "회전 잠금 물건 입고");
            ItemRecord sword = locked.GetContainer("test_storage").items[0];
            Check(!locked.TryMove(Move(locked,"test_storage",sword.instanceId,"display_preview",0,0,1),out error), "회전 잠금 검사");
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
            ItemRecord copy = model.GetItem("test_storage",second.instanceId); copy.purchasePrice=999;
            Check(model.GetItem("test_storage",second.instanceId).purchasePrice==100,"조회는 복사본");

            ShapeChecks(catalog);
            InventoryModel demo=NewModel(catalog,10,8);
            Check(demo.TryAdd("test_storage","C001","common",2,80,out error),"데모 일반 포션 2개");
            Check(demo.TryAdd("test_storage","C001","common",1,100,out error),"데모 추가 포션");
            Check(demo.TryAdd("test_storage","C001","superior",1,110,out error),"데모 상급 포션");
            Check(demo.TryAdd("test_storage","E001","none",2,0,out error),"데모 빵");
            Check(demo.TryAdd("test_storage","sample_rope","common",1,40,out error),"데모 밧줄");
            Check(demo.TryAdd("test_storage","sample_sword","common",1,90,out error),"데모 검");
            Check(demo.TryAdd("test_storage","sample_bag","common",1,150,out error),"데모 가방");
            Check(demo.TryAdd("test_storage","sample_lframe","common",1,50,out error),"데모 사각 프레임 A");
            Check(demo.TryAdd("test_storage","sample_cframe","common",1,70,out error),"데모 사각 프레임 B");
            Check(demo.GetContainer("test_storage").items.Count==11,"데모 전체 11개 배치");
            Geometry(demo);
            // 무작위 이동 후에도 ID/매입가/전체 물건 수와 실제 점유 칸을 독립적으로 검사합니다.
            var random = new Random(4242); InventoryModel fuzz = NewModel(catalog,8,6);
            fuzz.TryAdd("test_storage","C001","common",2,80,out error);
            fuzz.TryAdd("test_storage","sample_rope","common",1,40,out error);
            fuzz.TryAdd("test_storage","sample_sword","common",1,90,out error);
            fuzz.TryAdd("test_storage","sample_bag","common",1,150,out error);
            Check(fuzz.TryAdd("test_storage","sample_lframe","common",1,50,out error),"무작위 사각 프레임 A");
            Check(fuzz.TryAdd("test_storage","sample_cframe","common",1,70,out error),"무작위 사각 프레임 B");
            var prices = new Dictionary<string,int>();
            foreach (ItemRecord item in fuzz.GetContainer("test_storage").items) prices.Add(item.instanceId,item.purchasePrice);
            for (int iteration=0; iteration<1000; iteration++)
            {
                var candidates = new List<ItemRecord>(); var locations = new List<string>();
                foreach (ContainerRecord container in fuzz.Snapshot().containers)
                    foreach (ItemRecord item in container.items) { candidates.Add(item); locations.Add(container.containerId); }
                int index=random.Next(candidates.Count); string targetId=random.Next(2)==0?"test_storage":"display_preview";
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
            string error;var shape=catalog.Shape("sample_lframe");
            Check(shape.CellCount==9&&shape.Contains(0,1,0)&&shape.Contains(0,1,1),"3x3 전체9칸 점유");
            var copied=shape.Cells(0);copied[0]=new GridCell(99,99);Check(shape.Contains(0,0,0),"외부 배열 수정은 정의에 영향 없음");
            foreach(var id in new[]{"C001","sample_rope","sample_lframe","sample_cframe"})
            {
                shape=catalog.Shape(id);
                for(int rotation=0;rotation<4;rotation++)
                {
                    Check(shape.CellCount==shape.Width(rotation)*shape.Height(rotation),"모든 회전에서 사각형 전체 점유");
                    var keys=new HashSet<string>();foreach(var cell in shape.Cells(rotation)){Check(shape.Contains(rotation,cell.x,cell.y),"클릭/배치 영역 일치");Check(keys.Add(cell.x+","+cell.y),"좌표 중복 없음");}
                    Check(keys.Count==shape.CellCount&&!shape.Contains(rotation,-1,0)&&!shape.Contains(rotation,shape.Width(rotation),0),"전체 면적과 경계");
                }
            }
            var full=NewModel(catalog,3,3);Check(full.TryAdd("test_storage","sample_lframe","common",1,50,out error),"3x3 프레임 입고");
            long revision=full.Revision;Check(!full.TryAdd("test_storage","C001","common",1,80,out error)&&full.Revision==revision,"과거 구멍 자리에 추가 배치 불가");
            var frame=full.GetContainer("test_storage").items[0];var preview=full.Preview(Move(full,"test_storage",frame.instanceId,"test_storage",0,0,0));Check(preview.allowed&&preview.cells.Count==9,"미리보기9칸");
            var blocked=NewModel(catalog,6,3);Check(blocked.TryAdd("test_storage","sample_lframe","common",2,50,out error),"두 사각형 입고");frame=blocked.GetContainer("test_storage").items[0];
            preview=blocked.Preview(Move(blocked,"test_storage",frame.instanceId,"test_storage",1,0,0));Check(!preview.allowed&&preview.cells.FindAll(x=>x.state==PlacementCellState.Occupied).Count==3,"충돌한 전체3칸 표시");
            Check(!blocked.CanMove(Move(blocked,"test_storage",frame.instanceId,"test_storage",int.MaxValue,int.MaxValue,0),out error),"좌표 오버플로 거절");
            Check(!blocked.CanMove(Move(blocked,"test_storage",frame.instanceId,"test_storage",0,0,4),out error),"잘못된 회전 거절");
            foreach(int bad in new[]{0,65}){bool rejected=false;try{new ItemFootprint(bad,3);}catch(ArgumentException){rejected=true;}Check(rejected,"잘못된 직사각형 크기 거절");}
            // Simulates a valid pre-v10 interlocking layout without retaining the old shape system.
            var old=new InventorySave{version=9,gold=123,unpaidCosts=7,lastSettledDay=2};var box=new ContainerRecord{containerId="test_storage",columns=3,rows=3};old.containers.Add(box);
            box.items.Add(new ItemRecord{instanceId="old-frame",itemId="sample_lframe",quality="common",purchasePrice=50});
            box.items.Add(new ItemRecord{instanceId="old-potion",itemId="C001",quality="superior",x=1,y=0,purchasePrice=77});
            var migrated=NewModel(catalog);Check(migrated.TryRestore(old,out error),"이전 끼워 넣기 저장 자동 전환");
            Check(migrated.Snapshot().version==13&&migrated.GetItem("test_storage","old-frame").x==0&&migrated.GetItem(InventoryModel.TemporaryStorageId,"old-potion").purchasePrice==77,"UID/매입가/기존 유효 위치 유지 및 넘침 택배 이동");
            Check(migrated.Gold==123&&migrated.UnpaidCosts==7&&old.version==9&&old.containers[0].items.Count==2,"원본 저장/돈/미납 유지");Geometry(migrated);
            var current=old.Copy();current.version=10;revision=migrated.Revision;Check(!migrated.TryRestore(current,out error)&&migrated.Revision==revision,"현재 저장의 겹침은 복구하지 않고 원자적으로 거절");
            var duplicate=old.Copy();duplicate.containers[0].items.Add(duplicate.containers[0].items[0].Copy());Check(!migrated.TryRestore(duplicate,out error)&&migrated.Revision==revision,"구형 저장 중복UID도 거절");
        }
    }
}
