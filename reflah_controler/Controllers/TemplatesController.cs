using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using reflah_controler.Hubs;
using reflah_controler.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace reflah_controler.Controllers
{
    [Route("Home")]
    public partial class TemplatesController : AppController
    {
        public TemplatesController(IConfiguration configuration, IHubContext<DatabaseHub> hubContext)
            : base(configuration, hubContext)
        {
        }
        // ============================================
        // ПОЛУЧЕНИЕ ВСЕХ ЗАПИСЕЙ ПО ТИПУ
        // ============================================

        [HttpGet("GetItemsByType")]
        public IActionResult GetItemsByType(string type)
        {
            switch (type)
            {
                case "about":
                    return Json(GetAboutsFromDatabase());
                case "result":
                    return Json(GetResultsFromDatabase());
                case "engine":
                    return Json(GetEngineControlsFromDatabase());
                case "price":
                    return Json(GetPricesFromDatabase());
                case "grafic":
                    return Json(GetGraficsFromDatabase());
                case "additional":
                    return Json(GetAdditionalPricesFromDatabase());
                default:
                    return Json(new List<object>());
            }
        }

        // ============================================
        // УПРАВЛЕНИЕ ШАБЛОНАМИ
        // ============================================

        [HttpGet("Templates")]
        public async Task<IActionResult> Templates(string activeTab = "about")
        {
            List<PriceModel> allPrices = null;
            List<TemplatePriceModel> templatePrices = null;
            try
            {
                allPrices = GetPricesFromDatabase();
                templatePrices = GetTemplatePricesFromDatabase();
                var viewModel = new TemplatesViewModel
                {
                    // Шаблоны
                    AboutTemplates = await GetTemplatesWithItemsAsync("about"),
                    ResultTemplates = await GetTemplatesWithItemsAsync("result"),
                    EngineTemplates = await GetTemplatesWithItemsAsync("engine"),
                    PriceTemplates = await GetTemplatesWithItemsAsync("price"),
                    GraficTemplates = await GetTemplatesWithItemsAsync("grafic"),
                    AdditionalPriceTemplates = await GetTemplatesWithItemsAsync("additional"),

                    // ✅ ВСЕ ЗАПИСИ ДЛЯ SELECT
                    AllAbout = GetAboutsFromDatabase(),
                    AllResults = GetResultsFromDatabase(),
                    AllEngineControls = GetEngineControlsFromDatabase(),
                    AllPrices = GetPricesFromDatabase(),
                    AllGrafics = GetGraficsFromDatabase(),
                    AllAdditionalPrices = GetAdditionalPricesFromDatabase()
                };

                ViewBag.ActiveTab = activeTab ?? "about";
                ViewBag.TemplatePrices = templatePrices;
                ViewBag.AllPrices = allPrices;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка загрузки шаблонов: {ex.Message}";
                return View(new TemplatesViewModel());
            }
        }

        // ============================================
        // УДАЛЕНИЕ ЗАПИСИ ПО ID
        // ============================================
        [HttpPost("DeleteItemById")]
        public async Task<IActionResult> DeleteItemById([FromBody] DeleteItemRequest request)
        {
            try
            {
                string recordTable = GetRecordTable(request.Type);

                // Проверяем, используется ли запись в шаблонах
                string templateTable = GetTemplateTable(request.Type);
                string idsColumn = request.Type == "price" ? "prices" : (request.Type == "additional" ? "price_ids" : "ids");

                string connectionString = GetConnectionString();
                bool isUsed = false;

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Проверяем, есть ли ID в каком-либо шаблоне
                    string checkQuery = $"SELECT COUNT(*) FROM {templateTable} WHERE FIND_IN_SET(@id, {idsColumn})";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@id", request.Id);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count > 0) isUsed = true;
                    }

                    // Проверяем, используется ли в машинах (для дополнительных цен)
                    if (!isUsed && request.Type == "additional")
                    {
                        string checkCarQuery = $"SELECT COUNT(*) FROM reflash_cars WHERE FIND_IN_SET(@id, additional_price_ru)";
                        using (var checkCmd = new MySqlCommand(checkCarQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", request.Id);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                            if (count > 0) isUsed = true;
                        }
                    }

                    if (isUsed)
                    {
                        return Json(new { success = false, message = "Запись используется в шаблоне или машине" });
                    }

                    // Удаляем запись
                    string deleteQuery = $"DELETE FROM {recordTable} WHERE id = @id";
                    using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", request.Id);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }

                    // Удаляем из global_ids
                    DeleteGlobalId(request.Id);
                }

                await NotifyReaderSite();
                return Json(new
                {
                    success = true,
                    returnTab = request.ReturnTab ?? request.Type  // <<< ИЗМЕНЕНО
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // УДАЛЕНИЕ НЕИСПОЛЬЗУЕМЫХ ЗАПИСЕЙ
        // ============================================
        [HttpPost("DeleteUnusedItems")]
        public async Task<IActionResult> DeleteUnusedItems([FromBody] DeleteUnusedRequest request)
        {
            try
            {
                string recordTable = GetRecordTable(request.Type);
                string templateTable = GetTemplateTable(request.Type);
                string idsColumn = request.Type == "price" ? "prices" : (request.Type == "additional" ? "price_ids" : "ids");

                string connectionString = GetConnectionString();
                List<int> deletedIds = new List<int>();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Получаем все ID в таблице записей
                    string getAllQuery = $"SELECT id FROM {recordTable}";
                    var allIds = new List<int>();
                    using (var getAllCmd = new MySqlCommand(getAllQuery, connection))
                    using (var reader = await getAllCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            allIds.Add(reader.GetInt32(0));
                        }
                    }

                    // Для каждого ID проверяем, используется ли он
                    foreach (var id in allIds)
                    {
                        bool isUsed = false;

                        // Проверяем в шаблонах
                        string checkQuery = $"SELECT COUNT(*) FROM {templateTable} WHERE FIND_IN_SET(@id, {idsColumn})";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", id);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                            if (count > 0) isUsed = true;
                        }

                        // Проверяем в машинах (только для additional)
                        if (!isUsed && request.Type == "additional")
                        {
                            string checkCarQuery = $"SELECT COUNT(*) FROM reflash_cars WHERE FIND_IN_SET(@id, additional_price_ru)";
                            using (var checkCmd = new MySqlCommand(checkCarQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@id", id);
                                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                                if (count > 0) isUsed = true;
                            }
                        }

                        // Если не используется - удаляем
                        if (!isUsed)
                        {
                            string deleteQuery = $"DELETE FROM {recordTable} WHERE id = @id";
                            using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                            {
                                deleteCmd.Parameters.AddWithValue("@id", id);
                                await deleteCmd.ExecuteNonQueryAsync();
                            }
                            DeleteGlobalId(id);
                            deletedIds.Add(id);
                        }
                    }
                }

                await NotifyReaderSite();
                return Json(new
                {
                    success = true,
                    deletedCount = deletedIds.Count,
                    returnTab = request.ReturnTab ?? request.Type  // <<< ИЗМЕНЕНО
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // НЕИСПОЛЬЗУЕМЫЕ ЗАПИСИ ДЛЯ ШАБЛОНОВ
        // ==========================================

        [HttpGet("GetUnusedItems")]
        public IActionResult GetUnusedItems(string type)
        {
            try
            {
                var unusedItems = GetUnusedItemsList(type);
                return Json(unusedItems.Select(item => new
                {
                    id = item.Id,
                    displayText = item.DisplayText
                }));
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private List<TemplateItemDetail> GetUnusedItemsList(string type)
        {
            var allItems = GetAllItemsForType(type);
            var usedIds = new HashSet<int>();
            string connectionString = GetConnectionString();

            string templateTable = GetTemplateTable(type);
            string idsColumn = type == "price" ? "prices" : (type == "additional" ? "price_ids" : "ids");

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = $"SELECT {idsColumn} FROM {templateTable}";
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ids = reader[0]?.ToString();
                        if (!string.IsNullOrEmpty(ids))
                        {
                            foreach (var id in ids.Split(',').Select(int.Parse))
                            {
                                usedIds.Add(id);
                            }
                        }
                    }
                }
            }

            return allItems
                .Where(item => !usedIds.Contains(item.Id))
                .ToList();
        }

        // ==========================================
        // ПОЛУЧЕНИЕ ЗАПИСИ ПО ID (для модального окна)
        // ==========================================
        [HttpGet("GetItemById")]
        public IActionResult GetItemById(string type, int id)
        {
            try
            {
                object item = type switch
                {
                    "about" => GetAboutByIdSync(id),
                    "result" => GetResultByIdSync(id),
                    "engine" => GetEngineControlByIdSync(id),
                    "price" => GetPriceById(id),
                    "grafic" => GetGraficByIdSync(id),
                    "additional" => GetAdditionalPriceById(id),
                    _ => null
                };

                if (item == null)
                    return Json(new { success = false, message = "Запись не найдена" });

                return Json(new { success = true, item = item });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // ==========================================
        // ПОЛУЧЕНИЕ ОПЦИЙ ИЗ ШАБЛОНОВ ЦЕН
        // ==========================================

        [HttpGet("GetTemplatePriceOptions")]
        public IActionResult GetTemplatePriceOptions()
        {
            try
            {
                var result = new List<TemplatePriceOptionDto>();
                var templates = GetTemplatePricesFromDatabase();

                foreach (var template in templates)
                {
                    var priceIds = string.IsNullOrEmpty(template.Prices)
                        ? new List<int>()
                        : template.Prices.Split(',').Select(int.Parse).ToList();

                    var prices = GetPricesFromDatabase()
                        .Where(p => priceIds.Contains(p.Id))
                        .ToList();

                    result.Add(new TemplatePriceOptionDto
                    {
                        TemplateId = template.Id,
                        TemplateName = template.Name,
                        Prices = prices.Select(p => new PriceOptionItemDto
                        {
                            Id = p.Id,
                            Name = p.NameRu ?? p.NameEng ?? p.NameGer ?? "(без названия)",
                            BasePrice = p.BasePrice,
                            ProPrice = p.ProPrice
                        }).ToList()
                    });
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("GetPriceByIdforAdi/{id?}")]
        public IActionResult GetPriceByIdforAdi(int id)
        {
            try
            {
                var price = GetPricesFromDatabase().FirstOrDefault(p => p.Id == id);
                if (price == null)
                {
                    return Json(new { success = false, message = "Опция не найдена" });
                }

                return Json(new
                {
                    success = true,
                    id = price.Id,
                    name = price.NameRu ?? price.NameEng ?? price.NameGer ?? "(без названия)",
                    basePrice = price.BasePrice,
                    proPrice = price.ProPrice
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // ==========================================
        // ОБНОВЛЕНИЕ ЗАПИСИ (из модального окна)
        // ==========================================
        [HttpPost("UpdateItem")]
        public async Task<IActionResult> UpdateItem(
            string type,
            int id,
            string returnTab = null,
            string textRu = "",
            string textEng = "",
            string textGer = "",
            string nameRu = "",
            string nameEng = "",
            string nameGer = "",
            string basePrice = "",
            string proPrice = "",
            string basePriceEng = "",
            string proPriceEng = "",
            string basePriceGer = "",
            string proPriceGer = "",
            string infoRu = "",
            string infoEng = "",
            string infoGer = "",
            string name = "",
            string nameEngGrafic = "",
            string nameGerGrafic = "",
            string descriptionRu = "",
            string descriptionEng = "",
            string descriptionGer = "",
            string image = "",
            string priceRubl = "",
            string priceDolar = "",
            string priceEuro = "",
            int priceControler = 0,
            int unselectedPriceMode = 0)
        {
            string recordTable = GetRecordTable(type);
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "";

                    switch (type)
                    {
                        case "about":
                        case "result":
                        case "engine":
                            query = $"UPDATE {recordTable} SET text_ru = @ru, text_eng = @eng, text_ger = @ger WHERE id = @id";
                            using (var cmd = new MySqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.Parameters.AddWithValue("@ru", textRu ?? "");
                                cmd.Parameters.AddWithValue("@eng", textEng ?? "");
                                cmd.Parameters.AddWithValue("@ger", textGer ?? "");
                                await cmd.ExecuteNonQueryAsync();
                            }
                            break;

                        case "price":
                            query = $@"UPDATE {recordTable} SET 
                        name_ru = @name_ru, name_eng = @name_eng, name_ger = @name_ger,
                        base_price = @base_price, pro_price = @pro_price,
                        base_price_eng = @base_price_eng, pro_price_eng = @pro_price_eng,
                        base_price_ger = @base_price_ger, pro_price_ger = @pro_price_ger,
                        info_ru = @info_ru, info_eng = @info_eng, info_ger = @info_ger
                        WHERE id = @id";
                            using (var cmd = new MySqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.Parameters.AddWithValue("@name_ru", nameRu ?? "");
                                cmd.Parameters.AddWithValue("@name_eng", nameEng ?? "");
                                cmd.Parameters.AddWithValue("@name_ger", nameGer ?? "");
                                cmd.Parameters.AddWithValue("@base_price", basePrice ?? "");
                                cmd.Parameters.AddWithValue("@pro_price", proPrice ?? "");
                                cmd.Parameters.AddWithValue("@base_price_eng", basePriceEng ?? "");
                                cmd.Parameters.AddWithValue("@pro_price_eng", proPriceEng ?? "");
                                cmd.Parameters.AddWithValue("@base_price_ger", basePriceGer ?? "");
                                cmd.Parameters.AddWithValue("@pro_price_ger", proPriceGer ?? "");
                                cmd.Parameters.AddWithValue("@info_ru", infoRu ?? "");
                                cmd.Parameters.AddWithValue("@info_eng", infoEng ?? "");
                                cmd.Parameters.AddWithValue("@info_ger", infoGer ?? "");
                                await cmd.ExecuteNonQueryAsync();
                            }
                            break;

                        case "grafic":
                            query = $@"UPDATE {recordTable} SET 
                        name = @name, name_eng = @name_eng, name_ger = @name_ger,
                        description_ru = @description_ru, description_eng = @description_eng, description_ger = @description_ger,
                        image = @image
                        WHERE id = @id";
                            using (var cmd = new MySqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.Parameters.AddWithValue("@name", name ?? "");
                                cmd.Parameters.AddWithValue("@name_eng", nameEngGrafic ?? "");
                                cmd.Parameters.AddWithValue("@name_ger", nameGerGrafic ?? "");
                                cmd.Parameters.AddWithValue("@description_ru", descriptionRu ?? "");
                                cmd.Parameters.AddWithValue("@description_eng", descriptionEng ?? "");
                                cmd.Parameters.AddWithValue("@description_ger", descriptionGer ?? "");
                                cmd.Parameters.AddWithValue("@image", image ?? "");
                                await cmd.ExecuteNonQueryAsync();
                            }
                            break;

                        case "additional":
                            query = $@"UPDATE {recordTable} SET 
                        name_ru = @name_ru, name_eng = @name_eng, name_ger = @name_ger,
                        price_rubl = @price_rubl, price_dolar = @price_dolar, price_euro = @price_euro,
                        info_ru = @info_ru, info_eng = @info_eng, info_ger = @info_ger,
                        price_controler = @price_controler,
                        unselected_price_mode = @unselected_price_mode
                        WHERE id = @id";
                            using (var cmd = new MySqlCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@id", id);
                                cmd.Parameters.AddWithValue("@name_ru", nameRu ?? "");
                                cmd.Parameters.AddWithValue("@name_eng", nameEng ?? "");
                                cmd.Parameters.AddWithValue("@name_ger", nameGer ?? "");
                                cmd.Parameters.AddWithValue("@price_rubl", priceRubl ?? "");
                                cmd.Parameters.AddWithValue("@price_dolar", priceDolar ?? "");
                                cmd.Parameters.AddWithValue("@price_euro", priceEuro ?? "");
                                cmd.Parameters.AddWithValue("@info_ru", infoRu ?? "");
                                cmd.Parameters.AddWithValue("@info_eng", infoEng ?? "");
                                cmd.Parameters.AddWithValue("@info_ger", infoGer ?? "");
                                cmd.Parameters.AddWithValue("@price_controler", priceControler);
                                cmd.Parameters.AddWithValue("@unselected_price_mode", unselectedPriceMode);
                                await cmd.ExecuteNonQueryAsync();
                            }
                            break;

                        default:
                            return Json(new { success = false, message = "Неизвестный тип" });
                    }
                }

                await NotifyReaderSite();
                var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
                return Json(new { success = true, returnTab = activeTab });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // ПОЛУЧЕНИЕ МАШИН, ИСПОЛЬЗУЮЩИХ ШАБЛОН
        // ==========================================

        [HttpGet("GetCarsUsingTemplate")]
        public async Task<IActionResult> GetCarsUsingTemplate(string type, int templateId)
        {
            try
            {
                string connectionString = GetConnectionString();
                var cars = new List<object>();

                // Определяем таблицу шаблона
                string templateTable = GetTemplateTable(type);
                string idsColumn = type == "price" ? "prices" : (type == "additional" ? "price_ids" : "ids");

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Получаем used_in_cars из шаблона
                    string getUsedQuery = $"SELECT used_in_cars FROM {templateTable} WHERE id = @id";
                    string usedInCars = "";
                    using (var cmd = new MySqlCommand(getUsedQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", templateId);
                        var result = await cmd.ExecuteScalarAsync();
                        usedInCars = result?.ToString() ?? "";
                    }

                    if (string.IsNullOrEmpty(usedInCars))
                    {
                        return Json(new { success = true, cars = new List<object>() });
                    }

                    var carIds = usedInCars.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToList();

                    if (carIds.Count == 0)
                    {
                        return Json(new { success = true, cars = new List<object>() });
                    }

                    // Получаем данные машин
                    string ids = string.Join(",", carIds);
                    string query = $"SELECT id, brand, model, generation FROM reflash_cars WHERE id IN ({ids}) ORDER BY brand, model";

                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            cars.Add(new
                            {
                                id = reader.GetInt32("id"),
                                brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? "" : reader.GetString("brand"),
                                model = reader.IsDBNull(reader.GetOrdinal("model")) ? "" : reader.GetString("model"),
                                generation = reader.IsDBNull(reader.GetOrdinal("generation")) ? "" : reader.GetString("generation")
                            });
                        }
                    }
                }

                return Json(new { success = true, cars = cars });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // ПОИСК ШАБЛОНОВ В РЕАЛЬНОМ ВРЕМЕНИ (AJAX)
        // ==========================================

        [HttpGet("SearchTemplates")]
        public async Task<IActionResult> SearchTemplates(string searchTerm, string activeTab = "about")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return Json(new
                    {
                        about = new List<object>(),
                        result = new List<object>(),
                        engine = new List<object>(),
                        price = new List<object>(),
                        grafic = new List<object>(),
                        additional = new List<object>(),
                        total = 0
                    });
                }

                var term = searchTerm.Trim().ToLowerInvariant();

                // ===== ЗАГРУЖАЕМ ШАБЛОНЫ ИЗ БД =====
                var aboutTemplates = GetTemplateAboutsFromDatabase()
                    .Where(t => t.Name.ToLowerInvariant().Contains(term))
                    .Select(t => new { id = t.Id, name = t.Name, type = "about", count = GetAboutItemsFromTemplate(t.Id).Count })
                    .ToList();

                var resultTemplates = GetTemplateResultsFromDatabase()
                    .Where(t => t.Name.ToLowerInvariant().Contains(term))
                    .Select(t => new { id = t.Id, name = t.Name, type = "result", count = GetResultItemsFromTemplate(t.Id).Count })
                    .ToList();

                var engineTemplates = GetTemplateEngineControlsFromDatabase()
                    .Where(t => t.Name.ToLowerInvariant().Contains(term))
                    .Select(t => new { id = t.Id, name = t.Name, type = "engine", count = GetEngineControlItemsFromTemplate(t.Id).Count })
                    .ToList();

                var priceTemplates = GetTemplatePricesFromDatabase()
                    .Where(t => t.Name.ToLowerInvariant().Contains(term))
                    .Select(t => new { id = t.Id, name = t.Name, type = "price", count = GetPriceItemsFromTemplate(t.Id).Count })
                    .ToList();

                var graficTemplates = GetTemplateGraficsFromDatabase()
                    .Where(t => t.Name.ToLowerInvariant().Contains(term))
                    .Select(t => new { id = t.Id, name = t.Name, type = "grafic", count = GetGraficItemsFromTemplate(t.Id).Count })
                    .ToList();

                var additionalTemplates = GetTemplateAdditionalPricesFromDatabase()
                    .Where(t => t.Name.ToLowerInvariant().Contains(term))
                    .Select(t => new { id = t.Id, name = t.Name, type = "additional", count = GetAdditionalPriceItemsFromTemplate(t.Id).Count })
                    .ToList();

                return Json(new
                {
                    about = aboutTemplates,
                    result = resultTemplates,
                    engine = engineTemplates,
                    price = priceTemplates,
                    grafic = graficTemplates,
                    additional = additionalTemplates,
                    total = aboutTemplates.Count + resultTemplates.Count + engineTemplates.Count +
                            priceTemplates.Count + graficTemplates.Count + additionalTemplates.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
