#if UNITY_EDITOR
using System;
using GeneralStore.Inventory;
using GeneralStore.Inventory.Verification;
using UnityEngine;
public static class ScrollableInventoryChecks
{
    static int count;static void Check(bool value,string note){count++;if(!value)throw new Exception("스크롤 인벤토리: "+note);}
    public static int Run()
    {
        count=0;var catalog=InventoryModelChecks.Fixture();var displays=new DisplayCatalog(JsonUtility.FromJson<DisplayDatabase>(Resources.Load<TextAsset>("StoreInventory/display_fixtures").text));
        var old=new InventorySave{version=12,gold=1000};old.containers.Add(new ContainerRecord{containerId="warehouse",columns=8,rows=5});
        old.containers[0].items.Add(new ItemRecord{instanceId="kept",itemId="C001",quality="common",purchasePrice=77,x=6,y=3});
        string[] types={"side_shelf","cubby_cabinet","drawer_cabinet","single_stand","weapon_rack"};int[] columns={6,8,8,5,7},rows={10,6,6,8,8};
        for(int n=0;n<types.Length;n++){string id="display_owned_"+(n+1);old.displays.Add(new DisplayInstallation{containerId=id,typeId=types[n],locationId=""});old.containers.Add(new ContainerRecord{containerId=id,columns=columns[n],rows=rows[n]});}
        old.containers[1].items.Add(new ItemRecord{instanceId="side-kept",itemId="C001",quality="common",purchasePrice=81,x=4,y=8});
        old.containers[2].items.Add(new ItemRecord{instanceId="cubby-kept",itemId="E001",quality="none",purchasePrice=13,x=6,y=5});
        old.containers[3].items.Add(new ItemRecord{instanceId="drawer-kept",itemId="C001",quality="common",purchasePrice=83,x=6,y=4});
        old.listings.Add(new DisplayListing{instanceId="side-kept",price=199});old.listings.Add(new DisplayListing{instanceId="cubby-kept",price=29});old.listings.Add(new DisplayListing{instanceId="drawer-kept",price=211});
        var model=new InventoryModel(catalog,old,null,displays);
        Check(model.GetContainer("warehouse").rows==7&&old.containers[0].rows==5,"기존 상자를 복사본에서만7행 확장");
        Check(model.GetItem("warehouse","kept").purchasePrice==77&&model.GetItem("warehouse","kept").x==6,"기존 UID/비용/좌표 보존");
        Check(model.GetContainer(model.OwnedDisplayId("side_shelf",0)).columns==8,"안쪽 선반8열");
        Check(model.GetContainer(model.OwnedDisplayId("drawer_cabinet",0)).rows==12&&model.GetContainer(model.OwnedDisplayId("cubby_cabinet",0)).rows==12,"6층 총12행");
        Check(model.GetItem("display_owned_1","side-kept").x==4&&model.GetItem("display_owned_1","side-kept").y==8&&model.GetItem("display_owned_1","side-kept").purchasePrice==81,"선반 기존 물품 위치/매입가 보존");
        Check(model.GetItem("display_owned_2","cubby-kept").y==5&&model.GetItem("display_owned_3","drawer-kept").purchasePrice==83,"칸막이/서랍 확장 시 물품 보존");
        int price;Check(model.DisplayListingCount==3&&model.TryGetOfferPrice("display_owned_1","side-kept",out price)&&price==199&&old.listings[0].price==199,"판매가와 원본 저장 보존");
        var drawer=displays.Type("drawer_cabinet");var wide=catalog.Shape("sample_bag").Cells(0);
        Check(DisplayPartitionRules.Fits(drawer,wide,0,0),"상부는 넓은 물건 허용");Check(!DisplayPartitionRules.Fits(drawer,wide,0,6),"하부2×2 칸막이 통과 금지");
        Check(DisplayPartitionRules.Fits(drawer,catalog.Shape("C001").Cells(0),6,10),"하부 마지막2×2칸 배치");
        string error;Check(model.TryRestore(model.Snapshot(),out error),error);var bad=model.Snapshot();bad.containers[0].rows=5;Check(!model.TryRestore(bad,out error),"새 형식의 임의 축소는 거절");
        var data=JsonUtility.FromJson<DisplayWindowArtDatabase>(Resources.Load<TextAsset>("StoreInventory/display_window_art").text);
        Check(data.entries.Length==12,"전용 프레임12종");int scrolls=0;
        foreach(var art in data.entries){DisplayWindowArtRules.Validate(art,displays.Type(art.typeId));if(art.scrolling){scrolls++;Check(art.floors.Length==(art.typeId=="side_shelf"||art.typeId=="shelf"?5:6),"전체 층수 유지");}}
        Check(scrolls==4,"스크롤 진열4종");return count;
    }
}
#endif
