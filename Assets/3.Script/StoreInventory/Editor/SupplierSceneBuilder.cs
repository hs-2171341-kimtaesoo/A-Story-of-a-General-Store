#if UNITY_EDITOR
using GeneralStore.Inventory;
using UnityEngine;
using UnityEngine.UI;
using UI=InventorySceneBuilder;
public static class SupplierSceneBuilder
{
    static SupplierListRowView SupplierTemplate(Transform parent)
    {
        var image=UI.Box("Supplier Row Template",parent,0,0,508,166,new Color(.25f,.19f,.12f));var row=image.gameObject.AddComponent<SupplierListRowView>();
        row.selectButton=image.gameObject.AddComponent<Button>();row.selectButton.targetGraphic=image;row.tradeButton=row.selectButton;
        row.portrait=UI.Box("Supplier Emblem Sprite",image.transform,12,20,92,110,Color.white);row.portrait.raycastTarget=false;
        row.nameLabel=UI.Label("Name",image.transform,120,12,350,32,"",25);row.descriptionLabel=UI.Label("Description",image.transform,120,50,350,44,"",18);
        row.statusLabel=UI.Label("Status",image.transform,120,126,350,32,"",16);
        row.sampleIcons=new global::UnityEngine.UI.Image[2];for(int n=0;n<2;n++){row.sampleIcons[n]=UI.Box("Sample Icon "+n,image.transform,122+n*60,90,44,36,Color.white);row.sampleIcons[n].raycastTarget=false;}
        UI.Label("Arrow",image.transform,474,64,30,36,"›",30);
        image.gameObject.AddComponent<LayoutElement>().preferredHeight=166;image.gameObject.SetActive(false);return row;
    }
    static SupplierOfferRowView OfferTemplate(Transform parent)
    {
        var image=UI.Box("Supplier Offer Template",parent,0,0,508,110,new Color(.24f,.18f,.11f));var row=image.gameObject.AddComponent<SupplierOfferRowView>();
        row.icon=UI.Box("Item Icon",image.transform,8,16,64,72,Color.white);row.icon.raycastTarget=false;
        row.nameLabel=UI.Label("Name",image.transform,82,8,234,30,"",21);row.priceLabel=UI.Label("Unit Price",image.transform,82,42,196,28,"",17);
        row.stockLabel=UI.Label("Remaining Stock",image.transform,82,76,196,26,"",16);
        row.minusButton=UI.ButtonBox("−",image.transform,288,40,36,36,null);row.quantityLabel=UI.Label("Quantity",image.transform,326,40,50,36,"0",21);row.quantityLabel.alignment=TextAnchor.MiddleCenter;
        row.plusButton=UI.ButtonBox("+",image.transform,378,40,36,36,null);row.subtotalLabel=UI.Label("Subtotal",image.transform,420,36,86,56,"",18);
        image.gameObject.AddComponent<LayoutElement>().preferredHeight=110;image.gameObject.SetActive(false);return row;
    }
    public static void Build(Transform parent,SupplierUI ui)
    {
        var panel=SideMenuSceneBuilder.Panel(parent,"Supplier Window","거래처",out ui.window);
        var list=UI.Rect("Supplier List Page",panel,0,0,560,860);ui.listPage=list.gameObject;
        UI.Label("Subtitle",list,24,78,512,42,"주문할 거래처를 선택하세요.",21);
        ui.supplierList=SideMenuSceneBuilder.List(list,"Supplier List",24,136,512,538,true);
        UI.Label("Delivery",list,24,700,512,44,"주문 물품은 내일 새벽 도착합니다.",20);UI.ButtonBox("닫기",list,140,778,280,56,ui.Close);
        var trade=UI.Rect("Supplier Trade Page",panel,0,0,560,860);ui.tradePage=trade.gameObject;
        UI.ButtonBox("‹ 목록으로",trade,24,76,180,38,ui.ShowList);
        ui.portrait=UI.Box("Supplier Emblem Sprite",trade,28,128,88,88,Color.white);ui.portrait.raycastTarget=false;
        ui.titleLabel=UI.Label("Supplier Name",trade,130,128,396,36,"",26);ui.greetingLabel=UI.Label("Greeting",trade,130,170,396,56,"",18);
        ui.offerList=SideMenuSceneBuilder.List(trade,"Offer List",24,242,512,360,true);
        ui.cycleLabel=UI.Label("Cycle And Delivery",trade,24,614,512,42,"",18);
        ui.totalLabel=UI.Label("Order Totals",trade,24,668,512,94,"",24);
        ui.buyButton=UI.ButtonBox("주문하기",trade,24,778,280,56,ui.Buy);UI.ButtonBox("닫기",trade,320,778,216,56,ui.Close);
        ui.testSelectionLabel=UI.Label("Test Selection",parent,0,0,420,36,"",16);ui.testSelectionLabel.gameObject.SetActive(false);
        UI.ButtonBox("거래처 해금 테스트",parent,0,0,200,50,ui.UnlockNext);UI.ButtonBox("거래처 중단 테스트",parent,0,0,200,50,ui.BlockSelected);UI.ButtonBox("거래처 재개 테스트",parent,0,0,200,50,ui.ResumeSelected);
        ui.supplierTemplate=SupplierTemplate(parent);ui.offerTemplate=OfferTemplate(parent);ui.window.SetActive(false);trade.gameObject.SetActive(false);
    }
}
#endif
