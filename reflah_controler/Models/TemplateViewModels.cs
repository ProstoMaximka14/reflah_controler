using System.Collections.Generic;

namespace reflah_controler.Models
{
    /// <summary>
    /// ViewModel для страницы управления шаблонами
    /// </summary>
    public class TemplatesViewModel
    {
        public List<TemplateWithItems> AboutTemplates { get; set; } = new();
        public List<TemplateWithItems> ResultTemplates { get; set; } = new();
        public List<TemplateWithItems> EngineTemplates { get; set; } = new();
        public List<TemplateWithItems> PriceTemplates { get; set; } = new();
        public List<TemplateWithItems> GraficTemplates { get; set; } = new();
        public List<TemplateWithItems> AdditionalPriceTemplates { get; set; } = new();

        public List<AboutModel> AllAbout { get; set; } = new();
        public List<ResultModel> AllResults { get; set; } = new();
        public List<EngineControlModel> AllEngineControls { get; set; } = new();
        public List<PriceModel> AllPrices { get; set; } = new();
        public List<GraficModel> AllGrafics { get; set; } = new();
        public List<AdditionalPriceModel> AllAdditionalPrices { get; set; } = new();
    }

    /// <summary>
    /// Шаблон с его записями
    /// </summary>
    public class TemplateWithItems
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Ids { get; set; }
        public string Type { get; set; }
        public string UsedInCars { get; set; }
        public int SortOrder { get; set; }
        public List<TemplateItemDetail> Items { get; set; } = new();
        public bool IsNested { get; set; }
        public int? ParentTemplateId { get; set; }
    }

    /// <summary>
    /// Детальная информация о записи в шаблоне
    /// </summary>
    public class TemplateItemDetail
    {
        public int Id { get; set; }
        public string DisplayText { get; set; }

        // Для текстовых блоков (about, result, engine)
        public string TextRu { get; set; }
        public string TextEng { get; set; }
        public string TextGer { get; set; }

        // Для цен
        public string NameRu { get; set; }
        public string NameEng { get; set; }
        public string NameGer { get; set; }
        public string BasePrice { get; set; }
        public string ProPrice { get; set; }
        public string BasePriceEng { get; set; }
        public string ProPriceEng { get; set; }
        public string BasePriceGer { get; set; }
        public string ProPriceGer { get; set; }



        // ✅ ДЛЯ ДОПОЛНИТЕЛЬНЫХ ЦЕН
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

        public string FreeTemplateIds { get; set; }   // ID шаблонов через запятую
        public string BaseTemplateIds { get; set; }   // ID шаблонов через запятую
        public string ProTemplateIds { get; set; }

        // Для графиков
        public string GraficName { get; set; }
        public string GraficImage { get; set; }
        public string GraficNameEng { get; set; }
        public string GraficNameGer { get; set; }
        public string GraficDescriptionRu { get; set; }
        public string GraficDescriptionEng { get; set; }
        public string GraficDescriptionGer { get; set; }



        public bool IsTemplate { get; set; } 
        public int? TemplateId { get; set; }  
        public List<TemplateItemDetail> Children { get; set; } = new();
    }

    /// <summary>
    /// Модель для создания/редактирования шаблона
    /// </summary>
    public class TemplateEditorModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string SelectedIds { get; set; }
        public List<SimpleItem> AvailableItems { get; set; } = new();
    }

    /// <summary>
    /// Простая модель для отображения элементов в списке
    /// </summary>
    public class SimpleItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsSelected { get; set; }
    }

    // TemplateViewModels.cs — добавить
    public class ReorderItemsModel
    {
        public int TemplateId { get; set; }
        public string Type { get; set; }
        public string OrderedIds { get; set; } // "101,102,103"
    }
}
