namespace reflah_controler.Models
{
    public class TemplateAdditionalPriceModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PriceIds { get; set; } // "1,2,3"
        public string UsedInCars { get; set; }
        public int SortOrder { get; set; } = 0;
    }
}
