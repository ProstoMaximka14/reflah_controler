using System.Collections.Generic;

namespace reflah_controler.Models
{
    public class EditCarViewModel
    {
        public ReflashCarModel Car { get; set; }

        public List<CarBlockItem> AboutItems { get; set; } = new();
        public List<CarBlockItem> ResultItems { get; set; } = new();
        public List<CarBlockItem> EngineItems { get; set; } = new();
        public List<CarBlockItem> PriceItems { get; set; } = new();
        public List<CarBlockItem> GraficItems { get; set; } = new();
        public List<CarBlockItem> AdditionalPriceItems { get; set; } = new();

        public List<TemplateOption> AboutTemplates { get; set; } = new();
        public List<TemplateOption> ResultTemplates { get; set; } = new();
        public List<TemplateOption> EngineTemplates { get; set; } = new();
        public List<TemplateOption> PriceTemplates { get; set; } = new();
        public List<TemplateOption> GraficTemplates { get; set; } = new();
        public List<TemplateOption> AdditionalPriceTemplates { get; set; } = new();


    }

    public class CarBlockItem
    {
        public int Id { get; set; }
        public bool IsTemplate { get; set; }
        public string TemplateName { get; set; }
        public List<CarBlockPreview> TemplateChildren { get; set; } = new();

        // text blocks (about / result / engine)
        public string TextRu { get; set; }
        public string TextEng { get; set; }
        public string TextGer { get; set; }

        // ���� ����� (price)
        public string NameRu { get; set; }
        public string NameEng { get; set; }
        public string NameGer { get; set; }
        public string BasePrice { get; set; }
        public string ProPrice { get; set; }
        public string BasePriceEng { get; set; }
        public string ProPriceEng { get; set; }
        public string BasePriceGer { get; set; }
        public string ProPriceGer { get; set; }


        // ����
        public string PriceRubl { get; set; }
        public string PriceDolar { get; set; }
        public string PriceEuro { get; set; }
        public string InfoRu { get; set; }
        public string InfoEng { get; set; }
        public string InfoGer { get; set; }
        public int PriceControler { get; set; }
        public string FreePriceIds { get; set; } 
        public string BasePriceIds { get; set; }   
        public string ProPriceIds { get; set; }

        public int UnselectedPriceMode { get; set; }

        // grafic
        public string Name { get; set; }
        public string Image { get; set; }    
        public string GraficNameEng { get; set; }
        public string GraficNameGer { get; set; }
        public string DescriptionRu { get; set; }
        public string DescriptionEng { get; set; }
        public string DescriptionGer { get; set; }
    }

    public class CarBlockPreview
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public class TemplateOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IdsPreview { get; set; }
    }

    public class CarBlockTemplatePartialModel
    {
        public int CarId { get; set; }
        public string Block { get; set; }
        public CarBlockItem Item { get; set; }
    }
}