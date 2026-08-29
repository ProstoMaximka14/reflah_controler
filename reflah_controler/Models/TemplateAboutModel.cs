namespace reflah_controler.Models
{
    public class TemplateAboutModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Ids { get; set; } // "101,102,103"
        public string UsedInCars { get; set; }
        public int SortOrder { get; set; } = 0;
    }
}