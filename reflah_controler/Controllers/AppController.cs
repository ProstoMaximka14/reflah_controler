using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using reflah_controler.Hubs;
using reflah_controler.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace reflah_controler.Controllers
{
    public abstract class AppController : Controller
    {
        protected readonly IConfiguration _configuration;
        protected readonly IHubContext<DatabaseHub> _hubContext;
        protected readonly string _sharedUploadsPath;
        protected readonly string _readerSiteUrl;
        protected readonly CatalogStore Catalog;

        protected AppController(IConfiguration configuration, IHubContext<DatabaseHub> hubContext)
        {
            _configuration = configuration;
            _hubContext = hubContext;
            _sharedUploadsPath = _configuration["UploadSettings:SharedUploadsPath"] ?? @"C:\fotos";
            _readerSiteUrl = _configuration["ReaderSite:Url"] ?? "http://localhost:80";
            Catalog = new CatalogStore(GetConnectionString());
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            Catalog.OnError = msg => TempData["Error"] = msg;
            base.OnActionExecuting(context);
        }

        protected string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";
        }

        protected async Task NotifyReaderSite()
        {
            try
            {
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using var client = new HttpClient(handler);
                await client.PostAsync($"{_readerSiteUrl}/api/db-notify", null);

                Console.WriteLine($"✅ [{DateTime.Now}] Уведомление отправлено читающему сайту");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [{DateTime.Now}] Ошибка отправки: {ex.Message}");
            }
        }

        protected int CreateGlobalId(string sourceTable)
        {
            string connectionString = GetConnectionString();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO global_ids (source_table) VALUES (@sourceTable); SELECT LAST_INSERT_ID();";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@sourceTable", sourceTable);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        protected void DeleteGlobalId(int id)
        {
            string connectionString = GetConnectionString();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM global_ids WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        protected bool GlobalIdExists(int id)
        {
            string connectionString = GetConnectionString();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM global_ids WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        protected string GetGlobalIdSourceTable(int id)
        {
            string connectionString = GetConnectionString();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT source_table FROM global_ids WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    return command.ExecuteScalar()?.ToString();
                }
            }
        }

        protected List<ReflashCarModel> GetCarsFromDatabase() => Catalog.GetCarsFromDatabase();
        protected ReflashCarModel GetCarById(int id) => Catalog.GetCarById(id);
        protected List<PriceModel> GetPricesFromDatabase() => Catalog.GetPricesFromDatabase();
        protected PriceModel GetPriceById(int id) => Catalog.GetPriceById(id);
        protected List<TemplatePriceModel> GetTemplatePricesFromDatabase() => Catalog.GetTemplatePricesFromDatabase();
        protected TemplatePriceModel GetTemplatePriceById(int id) => Catalog.GetTemplatePriceById(id);
        protected List<AboutModel> GetAboutsFromDatabase() => Catalog.GetAboutsFromDatabase();
        protected AboutModel GetAboutById(int id) => Catalog.GetAboutById(id);
        protected AboutModel GetAboutByIdSync(int id) => Catalog.GetAboutById(id);
        protected List<TemplateAboutModel> GetTemplateAboutsFromDatabase() => Catalog.GetTemplateAboutsFromDatabase();
        protected List<ResultModel> GetResultsFromDatabase() => Catalog.GetResultsFromDatabase();
        protected ResultModel GetResultByIdSync(int id) => Catalog.GetResultById(id);
        protected List<TemplateResultModel> GetTemplateResultsFromDatabase() => Catalog.GetTemplateResultsFromDatabase();
        protected List<EngineControlModel> GetEngineControlsFromDatabase() => Catalog.GetEngineControlsFromDatabase();
        protected EngineControlModel GetEngineControlByIdSync(int id) => Catalog.GetEngineControlById(id);
        protected List<TemplateEngineControlModel> GetTemplateEngineControlsFromDatabase() => Catalog.GetTemplateEngineControlsFromDatabase();
        protected List<GraficModel> GetGraficsFromDatabase() => Catalog.GetGraficsFromDatabase();
        protected GraficModel GetGraficById(int id) => Catalog.GetGraficById(id);
        protected GraficModel GetGraficByIdSync(int id) => Catalog.GetGraficById(id);
        protected List<TemplateGraficModel> GetTemplateGraficsFromDatabase() => Catalog.GetTemplateGraficsFromDatabase();
        protected List<AdditionalPriceModel> GetAdditionalPricesFromDatabase() => Catalog.GetAdditionalPricesFromDatabase();
        protected AdditionalPriceModel GetAdditionalPriceById(int id) => Catalog.GetAdditionalPriceById(id);
        protected List<TemplateAdditionalPriceModel> GetTemplateAdditionalPricesFromDatabase() => Catalog.GetTemplateAdditionalPricesFromDatabase();

        protected static string FirstNonEmpty(params string[] values) => CatalogStore.FirstNonEmpty(values);
        protected static List<int> ParseIdList(string idsString) => CatalogStore.ParseIdList(idsString);
        protected static string AppendIdToList(string existing, int id) => CatalogStore.AppendIdToList(existing, id);
        protected static string RemoveIdFromList(string existing, int id) => CatalogStore.RemoveIdFromList(existing, id);
        protected string GetTemplateTable(string type) => Catalog.GetTemplateTable(type);
        protected string GetRecordTable(string type) => Catalog.GetRecordTable(type);
        protected string ReorderIdsString(string idsString, List<int> newOrder) => Catalog.ReorderIdsString(idsString, newOrder);

        protected Task<AboutModel> GetAboutByIdAsync(MySqlConnection connection, int id) => Catalog.GetAboutByIdAsync(connection, id);
        protected Task<ResultModel> GetResultByIdAsync(MySqlConnection connection, int id) => Catalog.GetResultByIdAsync(connection, id);
        protected Task<EngineControlModel> GetEngineControlByIdAsync(MySqlConnection connection, int id) => Catalog.GetEngineControlByIdAsync(connection, id);
        protected Task<PriceModel> GetPriceByIdAsync(MySqlConnection connection, int id) => Catalog.GetPriceByIdAsync(connection, id);
        protected Task<GraficModel> GetGraficByIdAsync(MySqlConnection connection, int id) => Catalog.GetGraficByIdAsync(connection, id);
    }
}
