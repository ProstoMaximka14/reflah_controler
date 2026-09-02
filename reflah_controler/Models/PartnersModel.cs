namespace reflah_controler.Models
{
    public class PartnersModel
    {
        public int Id { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string photo { get; set; }
        public string vk { get; set; }
        public string website { get; set; }
        public string city { get; set; }
        public string street { get; set; }
        public string house { get; set; }
        public string longitude { get; set; }
        public string latitude { get; set; }

        // ===== НОВЫЕ ПОЛЯ =====
        public string vk_group { get; set; }      // Группа ВК
        public string telegram { get; set; }      // Telegram
        public string whatsapp { get; set; }      // WhatsApp
        public string email { get; set; }         // Email
        public string point_name { get; set; }    // Точка (название офиса)

        public string info { get; set; }
    }
}