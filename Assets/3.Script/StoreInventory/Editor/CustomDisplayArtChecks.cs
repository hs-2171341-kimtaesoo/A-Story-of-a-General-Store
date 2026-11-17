using System;
using GeneralStore.Inventory;
namespace GeneralStore.Inventory.Verification
{
    public static class CustomDisplayArtChecks
    {
        static int count;static void Check(bool ok,string message){count++;if(!ok)throw new Exception("전용 진열창 검사: "+message);}
        public static int Run(ItemCatalog items,DisplayCatalog catalog,DisplayWindowArtDatabase data)
        {
            count=0;Check(data!=null&&data.entries!=null&&data.entries.Length==3,"전용 창3개");
            foreach(var art in data.entries)
            {
                var type=catalog.Type(art.typeId);DisplayWindowArtRules.Validate(art,type);Check(true,"모든 논리 칸이 화면에 한 번씩 대응");
                foreach(var section in art.sections)
                    for(int row=0;row<section.rows;row++)for(int column=0;column<section.columns;column++)
                    {
                        float px=section.rect.x+(column+.5f)*section.rect.width/section.columns,py=section.rect.y+(row+.5f)*section.rect.height/section.rows;
                        int x=(int)Math.Floor((px-section.rect.x)/(section.rect.width/section.columns))+section.column;
                        int y=(int)Math.Floor((py-section.rect.y)/(section.rect.height/section.rows))+section.row;
                        Check(x==column+section.column&&y==row+section.row,"화면 셀 중심과 전역 재고 좌표 왕복");
                    }
            }
            Check(catalog.Type("mannequin").columns==5&&catalog.Type("mannequin").rows==5,"마네킹5×5");
            Check(catalog.Type("table").columns==6&&catalog.Type("table").rows==6,"문 달린 진열장36칸");
            Check(catalog.Type("weapon_rack").columns==7&&catalog.Type("weapon_rack").rows==8,"무기 거치대56칸");
            var save=new InventorySave{gold=100000};save.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=5});catalog.AddInitialContainers(save,true);
            var model=new InventoryModel(items,save,null,catalog);string error;Check(model.TryAdd("warehouse","C001","common",1,80,out error),"포션 준비");
            var uid=model.GetContainer("warehouse").items[0].instanceId;string cabinet=model.OwnedDisplayId("table",0);long before=model.Revision;
            var request=new MoveRequest{sourceContainer="warehouse",targetContainer=cabinet,instanceId=uid,x=0,y=1,expectedRevision=before};
            Check(!model.Preview(request).allowed&&!model.TryMove(request,out error)&&model.Revision==before,"상하 선반 경계 통과 거절/원본 유지");
            request.y=2;Check(model.TryMove(request,out error)&&model.GetItem(cabinet,uid).y==2,"아래쪽 격자로 이동해도 같은 컨테이너/UID");
            Check(model.TryAdd("warehouse","sample_sword","common",1,90,out error),"무기 준비");uid=model.GetContainer("warehouse").items[0].instanceId;
            request=new MoveRequest{sourceContainer="warehouse",targetContainer=model.OwnedDisplayId("weapon_rack",0),instanceId=uid,x=1,y=0,rotation=1,expectedRevision=model.Revision};
            Check(!model.TryMove(request,out error),"회전한 장검의 세로 기둥 횡단 거절");request.rotation=0;
            Check(model.TryMove(request,out error),"세로 장검을 왼쪽 구역에 진열");
            Check(model.TryRestore(model.Snapshot(),out error),"새 크기의12개 진열장 저장 복원");return count;
        }
    }
}
