namespace GeneralStore.Inventory
{
    public static class QualityLabels
    {
        public static string Title(string value)
        {
            switch(value){case "poor":return "빈약";case "common":return "평범";case "superior":return "상급";case "perfect":return "완벽";case "masterpiece":return "걸작";case "legendary":return "전설";case "none":return "일반";default:return value;}
        }
    }
}

namespace GeneralStore.Inventory
{
    public static class QualityVisuals
    {
        public static UnityEngine.Color Color(string quality)
        {
            switch(quality)
            {
                case "superior":return new UnityEngine.Color(.2f,1,.3f,1);
                case "perfect":return new UnityEngine.Color(.15f,.5f,1,1);
                case "masterpiece":return new UnityEngine.Color(.7f,.3f,1,1);
                case "legendary":return new UnityEngine.Color(1,.5f,.05f,1);
                default:return UnityEngine.Color.white;
            }
        }
        public static void Apply(UnityEngine.UI.Graphic graphic,string quality,float width)
        {
            if(graphic==null)return;
            var effect=graphic.GetComponent<QualityOutlineEffect>();
            if(effect==null&&width<=0)return;
            if(effect==null)effect=graphic.gameObject.AddComponent<QualityOutlineEffect>();
            effect.Configure(Color(quality),width);
        }
    }
}
