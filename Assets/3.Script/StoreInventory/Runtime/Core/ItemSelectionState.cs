using System.Collections.Generic;
namespace GeneralStore.Inventory
{
    // UI의 창별 선택 상태입니다. 인벤토리 저장/판매/예약 데이터와 섞지 않습니다.
    public sealed class ItemSelectionState
    {
        readonly Dictionary<string,string> values=new Dictionary<string,string>();
        public string Get(string container){string value;return container!=null&&values.TryGetValue(container,out value)?value:null;}
        public void Set(string container,string uid){if(container==null)return;if(uid==null)values.Remove(container);else values[container]=uid;}
        public void Clear(string container){if(container!=null)values.Remove(container);}
        public void ClearAll(){values.Clear();}
    }
}
