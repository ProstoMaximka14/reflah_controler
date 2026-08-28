namespace reflah_controler.Models
{
    public class TemplateEngineControlModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Ids { get; set; }
        public string UsedInCars { get; set; }
        public int SortOrder { get; set; } = 0;
    }
}