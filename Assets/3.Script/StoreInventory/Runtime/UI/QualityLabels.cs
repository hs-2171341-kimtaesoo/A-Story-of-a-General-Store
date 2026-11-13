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
