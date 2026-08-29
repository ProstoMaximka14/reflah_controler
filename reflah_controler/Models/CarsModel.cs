namespace reflah_controler.Models
{
    public class ReflashCarModel
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Generation { get; set; }
        public string Engine { get; set; }
        public string Image { get; set; }
        public string AboutRu { get; set; }
        public string ResultRu { get; set; }
        public string EngineControlRu { get; set; }
        public string PriceRu { get; set; }
        public string grafic { get; set; }

        public string additional_price_ru { get; set; }

        public List<AboutModel> Abouts { get; set; } = new List<AboutModel>();
        public List<ResultModel> PriResults { get; set; } = new List<ResultModel>();
        public List<EngineControlModel> Engine_controlers { get; set; } = new List<EngineControlModel>();
        public List<PriceModel> Prices { get; set; } = new List<PriceModel>();
        public List<GraficModel> Grafics { get; set; } = new List<GraficModel>();

        public string old_url { get; set; }

        public int SortOrder { get; set; }
    }
}