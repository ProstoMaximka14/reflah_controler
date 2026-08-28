namespace reflah_controler.Models
{
    // ==========================================
    // ОПИСАНИЯ (ABOUT)
    // ==========================================
    public class AboutItemDto
    {
        public string TextRu { get; set; }
        public string TextEng { get; set; }
        public string TextGer { get; set; }
        public int? ExistingId { get; set; }
        public int? TemplateId { get; set; }  // <<< НОВОЕ ПОЛЕ
    }

    // ==========================================
    // РЕЗУЛЬТАТЫ (RESULT)
    // ==========================================
    public class ResultItemDto
    {
        public string TextRu { get; set; }
        public string TextEng { get; set; }
        public string TextGer { get; set; }
        public int? ExistingId { get; set; }
        public int? TemplateId { get; set; }  // <<< НОВОЕ ПОЛЕ
    }

    // ==========================================
    // БЛОКИ УПРАВЛЕНИЯ (ENGINE CONTROL)
    // ==========================================
    public class EngineItemDto
    {
        public string TextRu { get; set; }
        public string TextEng { get; set; }
        public string TextGer { get; set; }
        public int? ExistingId { get; set; }
        public int? TemplateId { get; set; }  // <<< НОВОЕ ПОЛЕ
    }

    // ==========================================
    // ЦЕНЫ ОПЦИЙ (PRICE)
    // ==========================================
    public class PriceItemDto
    {
        public string NameRu { get; set; }
        public string NameEng { get; set; }
        public string NameGer { get; set; }
        public string BasePrice { get; set; }
        public string ProPrice { get; set; }
        public string BasePriceEng { get; set; }
        public string ProPriceEng { get; set; }
        public string BasePriceGer { get; set; }
        public string ProPriceGer { get; set; }
        public string InfoRu { get; set; }
        public string InfoEng { get; set; }
        public string InfoGer { get; set; }
        public int? ExistingId { get; set; }
        public int? TemplateId { get; set; }  // <<< НОВОЕ ПОЛЕ
    }

    // ==========================================
    // ГРАФИКИ (GRAFIC)
    // ==========================================
    public class GraficItemDto
    {
        public string GraficName { get; set; }
        public string GraficNameEng { get; set; }
        public string GraficNameGer { get; set; }
        public string GraficDescriptionRu { get; set; }
        public string GraficDescriptionEng { get; set; }
        public string GraficDescriptionGer { get; set; }
        public IFormFile GraficImageFile { get; set; }
        public int? ExistingId { get; set; }
        public int? TemplateId { get; set; }  // <<< НОВОЕ ПОЛЕ
    }

    // ==========================================
    // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ (ADDITIONAL PRICES)
    // ==========================================
    public class AdditionalPriceItemDto
    {
        public string NameRu { get; set; }
        public string NameEng { get; set; }
        public string NameGer { get; set; }
        public string PriceRubl { get; set; }
        public string PriceDolar { get; set; }
        public string PriceEuro { get; set; }
        public string InfoRu { get; set; }
        public string InfoEng { get; set; }
        public string InfoGer { get; set; }
        public int PriceControler { get; set; }
        public int UnselectedPriceMode { get; set; }
        public int? ExistingId { get; set; }
        public int? TemplateId { get; set; }  // <<< НОВОЕ ПОЛЕ
        public string FreePriceIds { get; set; }
        public string BasePriceIds { get; set; }
        public string ProPriceIds { get; set; }
    }

    // ==========================================
    // КОПИРОВАНИЕ ЗАПИСИ
    // ==========================================
    public class CopyItemDto
    {
        public int SourceId { get; set; }
        public string Type { get; set; }
    }
}