namespace reflah_controler.Models
{
    public class AdditionalPriceModel
    {
        public int Id { get; set; }
        public string NameRu { get; set; }
        public string NameEng { get; set; }
        public string NameGer { get; set; }
        public string PriceRubl { get; set; }
        public string PriceDolar { get; set; }
        public string PriceEuro { get; set; }
        public string InfoRu { get; set; }
        public string InfoEng { get; set; }
        public string InfoGer { get; set; }
        public int SortOrder { get; set; }
        public int PriceControler { get; set; }

        public string FreePriceIds { get; set; }   // ID цен для бесплатных опций
        public string BasePriceIds { get; set; }   // ID цен с base_price
        public string ProPriceIds { get; set; }    // ID цен с pro_price

        public int UnselectedPriceMode { get; set; }
    }
}