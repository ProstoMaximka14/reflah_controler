namespace reflah_controler.Models
{
    public class PriceModel
    {
        public int Id { get; set; }
        public string NameRu { get; set; }
        public string NameEng { get; set; }
        public string NameGer { get; set; }
        public string BasePrice { get; set; }
        public string ProPrice { get; set; }

        // ===== НОВЫЕ ПОЛЯ (ЦЕНЫ В ВАЛЮТАХ) =====
        public string BasePriceEng { get; set; }
        public string ProPriceEng { get; set; }
        public string BasePriceGer { get; set; }
        public string ProPriceGer { get; set; }

        public string InfoRu { get; set; }
        public string InfoEng { get; set; }
        public string InfoGer { get; set; }
    }
}