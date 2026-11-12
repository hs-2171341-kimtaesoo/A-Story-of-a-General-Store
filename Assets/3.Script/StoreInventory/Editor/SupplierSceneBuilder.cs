#if UNITY_EDITOR
using GeneralStore.Inventory;
using UnityEngine;
using UnityEngine.UI;
using UI=InventorySceneBuilder;
public static class SupplierSceneBuilder
{
    static RectTransform List(Transform parent,string name,float x,float y,float w,float h)
    {
        var viewport=UI.Box(name,parent,x,y,w,h,new Color(.09f,.075f,.055f));viewport.gameObject.AddComponent<RectMask2D>();
        var content=UI.Rect("Content",viewport.transform,0,0,w,h);var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing=8;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
        return content;
    }
    static SupplierListRowView SupplierTemplate(Transform parent)
    {
        var image=UI.Box("Supplier Row Template",parent,0,0,944,140,new Color(.19f,.15f,.1f));var row=image.gameObject.AddComponent<SupplierListRowView>();
        row.selectButton=image.gameObject.AddComponent<Button>();row.selectButton.targetGraphic=image;
        row.portrait=UI.Box("Portrait",image.transform,12,12,104,112,Color.white);row.portrait.raycastTarget=false;
        row.nameLabel=UI.Label("Name",image.transform,134,10,560,34,"",25);
        row.descriptionLabel=UI.Label("Description",image.transform,134,48,560,36,"",21);
        row.statusLabel=UI.Label("Status",image.transform,134,88,590,42,"",19);
        row.tradeButton=UI.ButtonBox("거래하기",image.transform,736,44,190,58,null);
        image.gameObject.AddComponent<LayoutElement>().preferredHeight=140;image.gameObject.SetActive(false);return row;
    }
    static SupplierOfferRowView OfferTemplate(Transform parent)
    {
        var image=UI.Box("Supplier Offer Template",parent,0,0,944,76,new Color(.16f,.13f,.09f));var row=image.gameObject.AddComponent<SupplierOfferRowView>();
        row.icon=UI.Box("Icon",image.transform,8,8,60,60,Color.white);row.icon.raycastTarget=false;
        row.nameLabel=UI.Label("Name",image.transform,80,16,350,44,"",22);
        row.priceLabel=UI.Label("Price",image.transform,438,16,170,44,"",22);row.stockLabel=UI.Label("Stock",image.transform,626,16,90,44,"",22);
        row.minusButton=UI.ButtonBox("−",image.transform,734,16,44,44,null);row.quantityLabel=UI.Label("Quantity",image.transform,790,16,76,44,"0",22);row.quantityLabel.alignment=TextAnchor.MiddleCenter;
        row.plusButton=UI.ButtonBox("+",image.transform,880,16,44,44,null);
        image.gameObject.AddComponent<LayoutElement>().preferredHeight=76;image.gameObject.SetActive(false);return row;
    }
    public static void Build(Transform parent,SupplierUI ui)
    {
        var overlay=UI.Box("Supplier Window",parent,0,0,1920,1080,new Color(0,0,0,.6f));ui.window=overlay.gameObject;
        var panel=UI.Box("Supplier Panel",overlay.transform,456,120,1008,840,new Color(.13f,.1f,.065f));
        UI.ButtonBox("×",panel.transform,944,12,44,44,ui.Close);
        var list=UI.Rect("Supplier List Page",panel.transform,0,0,1008,700);ui.listPage=list.gameObject;
        UI.Label("Title",list,24,18,800,44,"거래처 화면",30);UI.Label("Hint",list,24,70,944,40,"등록된 거래처에서 정기적으로 물품을 구입할 수 있습니다.",21);
        ui.supplierList=List(list,"Supplier List",24,128,944,550);
        var trade=UI.Rect("Supplier Trade Page",panel.transform,0,0,1008,700);ui.tradePage=trade.gameObject;
        ui.portrait=UI.Box("Portrait",trade,24,20,96,100,Color.white);ui.portrait.raycastTarget=false;
        ui.titleLabel=UI.Label("Name",trade,140,18,760,36,"",26);ui.greetingLabel=UI.Label("Greeting",trade,140,60,760,62,"",22);
        ui.cycleLabel=UI.Label("Cycle",trade,24,134,944,36,"",21);
        UI.Label("Columns",trade,24,180,944,36,"물품명                                단가                   보유 수량          구매 수량",20);
        ui.offerList=List(trade,"Offer List",24,224,944,350);
        ui.totalLabel=UI.Label("Total",trade,24,596,590,44,"",24);
        ui.buyButton=UI.ButtonBox("구매하기",trade,656,590,310,54,ui.Buy);
        UI.ButtonBox("거래처 목록으로",trade,24,652,310,42,ui.ShowList);
        UI.Label("Delivery",trade,350,652,610,42,"구입한 물건은 다음 날 새벽 합배송됩니다.",20);
        ui.testSelectionLabel=UI.Label("Test Selection",panel.transform,24,712,944,34,"",19);
        UI.ButtonBox("다음 거래처 해금 테스트",panel.transform,24,760,300,52,ui.UnlockNext);
        UI.ButtonBox("선택 거래처 중단 테스트",panel.transform,340,760,300,52,ui.BlockSelected);
        UI.ButtonBox("선택 거래처 재개 테스트",panel.transform,656,760,310,52,ui.ResumeSelected);
        ui.supplierTemplate=SupplierTemplate(parent);ui.offerTemplate=OfferTemplate(parent);
        overlay.gameObject.SetActive(false);trade.gameObject.SetActive(false);
    }
}
#endif
