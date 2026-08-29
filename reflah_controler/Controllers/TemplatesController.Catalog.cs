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
    public partial class TemplatesController : AppController
    {
        // ============================================
        // Р¦Р•РќР« (PRICE)
        // ============================================

        [HttpGet("Prices")]
        public IActionResult Prices()
        {
            List<PriceModel> prices = GetPricesFromDatabase();
            return View(prices);
        }

        [HttpGet("CreatePrice")]
        public IActionResult CreatePrice()
        {
            return View("CreatePrice", new PriceModel());
        }

        [HttpGet("EditPrice/{id?}")]
        public IActionResult EditPrice(int id)
        {
            PriceModel price = GetPriceById(id);
            if (price == null)
            {
                TempData["Error"] = "Р¦РµРЅР° РЅРµ РЅР°Р№РґРµРЅР°";
                return RedirectToAction("Prices");
            }
            return View(price);
        }

        [HttpPost("AddPrice")]
        public async Task<IActionResult> AddPrice(PriceModel price)
        {
            int globalId = CreateGlobalId("price");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO price 
                        (id, name_ru, name_eng, name_ger, base_price, pro_price,
                        base_price_eng, pro_price_eng, base_price_ger, pro_price_ger,
                        info_ru, info_eng, info_ger)
                    VALUES 
                        (@id, @name_ru, @name_eng, @name_ger, @base_price, @pro_price,
                        @base_price_eng, @pro_price_eng, @base_price_ger, @pro_price_ger,
                        @info_ru, @info_eng, @info_ger)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@name_ru", price.NameRu ?? "");
                        command.Parameters.AddWithValue("@name_eng", price.NameEng ?? "");
                        command.Parameters.AddWithValue("@name_ger", price.NameGer ?? "");
                        command.Parameters.AddWithValue("@base_price", price.BasePrice ?? "");
                        command.Parameters.AddWithValue("@pro_price", price.ProPrice ?? "");
                        command.Parameters.AddWithValue("@base_price_eng", price.BasePriceEng ?? "");
                        command.Parameters.AddWithValue("@pro_price_eng", price.ProPriceEng ?? "");
                        command.Parameters.AddWithValue("@base_price_ger", price.BasePriceGer ?? "");
                        command.Parameters.AddWithValue("@pro_price_ger", price.ProPriceGer ?? "");
                        command.Parameters.AddWithValue("@info_ru", price.InfoRu ?? "");
                        command.Parameters.AddWithValue("@info_eng", price.InfoEng ?? "");
                        command.Parameters.AddWithValue("@info_ger", price.InfoGer ?? "");

                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "Р¦РµРЅР° СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅР°";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Prices");
        }

        [HttpPost("UpdatePrice")]
        public async Task<IActionResult> UpdatePrice(PriceModel price)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"UPDATE price SET 
                        name_ru = @name_ru, 
                        name_eng = @name_eng, 
                        name_ger = @name_ger, 
                        base_price = @base_price, 
                        pro_price = @pro_price,
                        base_price_eng = @base_price_eng,
                        pro_price_eng = @pro_price_eng,
                        base_price_ger = @base_price_ger,
                        pro_price_ger = @pro_price_ger,
                        info_ru = @info_ru,
                        info_eng = @info_eng,
                        info_ger = @info_ger
                        WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", price.Id);
                        command.Parameters.AddWithValue("@name_ru", price.NameRu ?? "");
                        command.Parameters.AddWithValue("@name_eng", price.NameEng ?? "");
                        command.Parameters.AddWithValue("@name_ger", price.NameGer ?? "");
                        command.Parameters.AddWithValue("@base_price", price.BasePrice ?? "");
                        command.Parameters.AddWithValue("@pro_price", price.ProPrice ?? "");
                        command.Parameters.AddWithValue("@base_price_eng", price.BasePriceEng ?? "");
                        command.Parameters.AddWithValue("@pro_price_eng", price.ProPriceEng ?? "");
                        command.Parameters.AddWithValue("@base_price_ger", price.BasePriceGer ?? "");
                        command.Parameters.AddWithValue("@pro_price_ger", price.ProPriceGer ?? "");
                        command.Parameters.AddWithValue("@info_ru", price.InfoRu ?? "");
                        command.Parameters.AddWithValue("@info_eng", price.InfoEng ?? "");
                        command.Parameters.AddWithValue("@info_ger", price.InfoGer ?? "");

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = "Р¦РµРЅР° СѓСЃРїРµС€РЅРѕ РѕР±РЅРѕРІР»РµРЅР°";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Р¦РµРЅР° РЅРµ РЅР°Р№РґРµРЅР°";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Prices");
        }

        [HttpPost("DeletePrice")]
        public async Task<IActionResult> DeletePrice(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM price WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                DeleteGlobalId(id);

                TempData["Message"] = "Р¦РµРЅР° СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅР°";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Prices");
        }

        // ============================================
        // РЁРђР‘Р›РћРќР« Р¦Р•Рќ (TEMPLATE_PRICE)
        // ============================================

        [HttpGet("TemplatePrices")]
        public IActionResult TemplatePrices()
        {
            List<TemplatePriceModel> templates = GetTemplatePricesFromDatabase();
            return View(templates);
        }

        [HttpGet("CreateTemplatePrice")]
        public IActionResult CreateTemplatePrice()
        {
            return View("CreateTemplatePrice", new TemplatePriceModel());
        }

        [HttpGet("EditTemplatePrice/{id?}")]
        public IActionResult EditTemplatePrice(int id)
        {
            TemplatePriceModel template = GetTemplatePriceById(id);
            if (template == null)
            {
                TempData["Error"] = "РЁР°Р±Р»РѕРЅ РЅРµ РЅР°Р№РґРµРЅ";
                return RedirectToAction("TemplatePrices");
            }
            return View(template);
        }

        [HttpPost("AddTemplatePrice")]
        public async Task<IActionResult> AddTemplatePrice(TemplatePriceModel template)
        {
            int globalId = CreateGlobalId("template_price");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "INSERT INTO template_price (id, name, prices) VALUES (@id, @name, @prices)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@name", template.Name ?? "");
                        command.Parameters.AddWithValue("@prices", template.Prices ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "РЁР°Р±Р»РѕРЅ С†РµРЅ СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplatePrices");
        }

        [HttpPost("UpdateTemplatePrice")]
        public async Task<IActionResult> UpdateTemplatePrice(TemplatePriceModel template)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "UPDATE template_price SET name = @name, prices = @prices WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", template.Id);
                        command.Parameters.AddWithValue("@name", template.Name ?? "");
                        command.Parameters.AddWithValue("@prices", template.Prices ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "РЁР°Р±Р»РѕРЅ С†РµРЅ СѓСЃРїРµС€РЅРѕ РѕР±РЅРѕРІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplatePrices");
        }

        [HttpPost("DeleteTemplatePrice")]
        public async Task<IActionResult> DeleteTemplatePrice(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM template_price WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);

                TempData["Message"] = "РЁР°Р±Р»РѕРЅ С†РµРЅ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplatePrices");
        }

        // ============================================
        // РћРџРРЎРђРќРРЇ (ABOUT)
        // ============================================

        [HttpGet("Abouts")]
        public IActionResult Abouts()
        {
            List<AboutModel> abouts = GetAboutsFromDatabase();
            return View(abouts);
        }

        [HttpGet("CreateAbout")]
        public IActionResult CreateAbout()
        {
            return View("CreateAbout", new AboutModel());
        }

        [HttpGet("EditAbout/{id?}")]
        public IActionResult EditAbout(int id)
        {
            AboutModel about = GetAboutById(id);
            if (about == null)
            {
                TempData["Error"] = "РћРїРёСЃР°РЅРёРµ РЅРµ РЅР°Р№РґРµРЅРѕ";
                return RedirectToAction("Abouts");
            }
            return View(about);
        }

        [HttpPost("AddAbout")]
        public async Task<IActionResult> AddAbout(AboutModel about)
        {
            int globalId = CreateGlobalId("about");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO about (id, text_ru, text_eng, text_ger) 
                                     VALUES (@id, @text_ru, @text_eng, @text_ger)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@text_ru", about.TextRu ?? "");
                        command.Parameters.AddWithValue("@text_eng", about.TextEng ?? "");
                        command.Parameters.AddWithValue("@text_ger", about.TextGer ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "РћРїРёСЃР°РЅРёРµ СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅРѕ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Abouts");
        }

        [HttpPost("UpdateAbout")]
        public async Task<IActionResult> UpdateAbout(AboutModel about)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"UPDATE about SET text_ru = @text_ru, text_eng = @text_eng, text_ger = @text_ger WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", about.Id);
                        command.Parameters.AddWithValue("@text_ru", about.TextRu ?? "");
                        command.Parameters.AddWithValue("@text_eng", about.TextEng ?? "");
                        command.Parameters.AddWithValue("@text_ger", about.TextGer ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "РћРїРёСЃР°РЅРёРµ СѓСЃРїРµС€РЅРѕ РѕР±РЅРѕРІР»РµРЅРѕ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Abouts");
        }

        [HttpPost("DeleteAbout")]
        public async Task<IActionResult> DeleteAbout(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM about WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);

                TempData["Message"] = "РћРїРёСЃР°РЅРёРµ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅРѕ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Abouts");
        }

        // ============================================
        // РЁРђР‘Р›РћРќР« РћРџРРЎРђРќРР™ (TEMPLATE_ABOUT)
        // ============================================

        [HttpGet("TemplateAbouts")]
        public IActionResult TemplateAbouts()
        {
            List<TemplateAboutModel> templates = GetTemplateAboutsFromDatabase();
            return View(templates);
        }

        [HttpPost("ReorderTemplates")]
        public async Task<IActionResult> ReorderTemplates([FromBody] ReorderTemplatesRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Type) || request.TemplateIds == null || request.TemplateIds.Count == 0)
                return Json(new { success = false, message = "Некорректные данные" });

            string type = request.Type;
            List<int> templateIds = request.TemplateIds;
            // ===== ЭТО ДОЛЖНО ПОЯВИТЬСЯ В OUTPUT =====
            System.Diagnostics.Debug.WriteLine("=== ReorderTemplates ВЫЗВАН ===");
            System.Diagnostics.Debug.WriteLine($"Type: {type}");
            System.Diagnostics.Debug.WriteLine($"TemplateIds: {string.Join(", ", templateIds)}");
            // =========================================
            try
            {
                string table = GetTemplateTable(type);
                string connectionString = GetConnectionString();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    for (int i = 0; i < templateIds.Count; i++)
                    {
                        string query = $"UPDATE {table} SET sort_order = @sort_order WHERE id = @id";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@sort_order", i);
                            cmd.Parameters.AddWithValue("@id", templateIds[i]);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                await NotifyReaderSite();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("AddTemplateAbout")]
        public async Task<IActionResult> AddTemplateAbout(string name, string ids)
        {
            int globalId = CreateGlobalId("template_about");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "INSERT INTO template_about (id, name, ids, used_in_cars) VALUES (@id, @name, @ids, '')";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@ids", ids ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "РЁР°Р±Р»РѕРЅ РѕРїРёСЃР°РЅРёР№ СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateAbouts");
        }

        [HttpPost("DeleteTemplateAbout")]
        public async Task<IActionResult> DeleteTemplateAbout(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM template_about WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);

                TempData["Message"] = "РЁР°Р±Р»РѕРЅ РѕРїРёСЃР°РЅРёР№ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateAbouts");
        }

        // ============================================
        // Р Р•Р—РЈР›Р¬РўРђРўР« (RESULT)
        // ============================================

        [HttpGet("Results")]
        public IActionResult Results()
        {
            List<ResultModel> results = GetResultsFromDatabase();
            return View(results);
        }

        [HttpPost("AddResult")]
        public async Task<IActionResult> AddResult(ResultModel result)
        {
            int globalId = CreateGlobalId("result");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO result (id, text_ru, text_eng, text_ger) 
                                     VALUES (@id, @text_ru, @text_eng, @text_ger)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@text_ru", result.TextRu ?? "");
                        command.Parameters.AddWithValue("@text_eng", result.TextEng ?? "");
                        command.Parameters.AddWithValue("@text_ger", result.TextGer ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "Р РµР·СѓР»СЊС‚Р°С‚ СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Results");
        }

        [HttpPost("DeleteResult")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM result WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);

                TempData["Message"] = "Р РµР·СѓР»СЊС‚Р°С‚ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Results");
        }

        // ============================================
        // РЁРђР‘Р›РћРќР« Р Р•Р—РЈР›Р¬РўРђРўРћР’ (TEMPLATE_RESULT)
        // ============================================

        [HttpGet("TemplateResults")]
        public IActionResult TemplateResults()
        {
            List<TemplateResultModel> templates = GetTemplateResultsFromDatabase();
            return View(templates);
        }

        [HttpPost("AddTemplateResult")]
        public async Task<IActionResult> AddTemplateResult(string name, string ids)
        {
            int globalId = CreateGlobalId("template_result");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "INSERT INTO template_result (id, name, ids, used_in_cars) VALUES (@id, @name, @ids, '')";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@ids", ids ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "РЁР°Р±Р»РѕРЅ СЂРµР·СѓР»СЊС‚Р°С‚РѕРІ СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateResults");
        }

        [HttpPost("DeleteTemplateResult")]
        public async Task<IActionResult> DeleteTemplateResult(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM template_result WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);

                TempData["Message"] = "РЁР°Р±Р»РѕРЅ СЂРµР·СѓР»СЊС‚Р°С‚РѕРІ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateResults");
        }

        // ============================================
        // Р‘Р›РћРљР РЈРџР РђР’Р›Р•РќРРЇ (ENGINE_CONTROL)
        // ============================================

        [HttpGet("EngineControls")]
        public IActionResult EngineControls()
        {
            List<EngineControlModel> controls = GetEngineControlsFromDatabase();
            return View(controls);
        }

        [HttpPost("AddEngineControl")]
        public async Task<IActionResult> AddEngineControl(EngineControlModel control)
        {
            int globalId = CreateGlobalId("engine_control");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO engine_control (id, text_ru, text_eng, text_ger) 
                                     VALUES (@id, @text_ru, @text_eng, @text_ger)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@text_ru", control.TextRu ?? "");
                        command.Parameters.AddWithValue("@text_eng", control.TextEng ?? "");
                        command.Parameters.AddWithValue("@text_ger", control.TextGer ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "Р‘Р»РѕРє СѓРїСЂР°РІР»РµРЅРёСЏ СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("EngineControls");
        }

        [HttpPost("DeleteEngineControl")]
        public async Task<IActionResult> DeleteEngineControl(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM engine_control WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);

                TempData["Message"] = "Р‘Р»РѕРє СѓРїСЂР°РІР»РµРЅРёСЏ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("EngineControls");
        }

        // ============================================
        // РЁРђР‘Р›РћРќР« Р‘Р›РћРљРћР’ РЈРџР РђР’Р›Р•РќРРЇ (TEMPLATE_ENGINE_CONTROL)
        // ============================================

        [HttpGet("TemplateEngineControls")]
        public IActionResult TemplateEngineControls()
        {
            List<TemplateEngineControlModel> templates = GetTemplateEngineControlsFromDatabase();
            return View(templates);
        }

        [HttpPost("AddTemplateEngineControl")]
        public async Task<IActionResult> AddTemplateEngineControl(string name, string ids)
        {
            int globalId = CreateGlobalId("template_engine_control");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "INSERT INTO template_engine_control (id, name, ids, used_in_cars) VALUES (@id, @name, @ids, '')";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@ids", ids ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "РЁР°Р±Р»РѕРЅ Р±Р»РѕРєРѕРІ СѓРїСЂР°РІР»РµРЅРёСЏ СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateEngineControls");
        }

        [HttpPost("DeleteTemplateEngineControl")]
        public async Task<IActionResult> DeleteTemplateEngineControl(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM template_engine_control WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);

                TempData["Message"] = "РЁР°Р±Р»РѕРЅ Р±Р»РѕРєРѕРІ СѓРїСЂР°РІР»РµРЅРёСЏ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateEngineControls");
        }

        // ============================================
        // Р“Р РђР¤РРљР (GRAFIC)
        // ============================================

        [HttpGet("Grafics")]
        public IActionResult Grafics()
        {
            List<GraficModel> grafics = GetGraficsFromDatabase();
            return View(grafics);
        }

        [HttpGet("CreateGrafic")]
        public IActionResult CreateGrafic()
        {
            return View("CreateGrafic", new GraficModel());
        }

        [HttpGet("EditGrafic/{id?}")]
        public IActionResult EditGrafic(int id)
        {
            GraficModel grafic = GetGraficById(id);
            if (grafic == null)
            {
                TempData["Error"] = "Р“СЂР°С„РёРє РЅРµ РЅР°Р№РґРµРЅ";
                return RedirectToAction("Grafics");
            }
            return View(grafic);
        }

        [HttpPost("AddGrafic")]
        public async Task<IActionResult> AddGrafic(GraficModel grafic, IFormFile imageFile)
        {
            int globalId = CreateGlobalId("grafic");
            string fileName = null;

            if (imageFile != null && imageFile.Length > 0)
            {
                var graficsPath = Path.Combine(_sharedUploadsPath, "grafics");

                if (!Directory.Exists(graficsPath))
                {
                    Directory.CreateDirectory(graficsPath);
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Р Р°Р·СЂРµС€РµРЅС‹ С‚РѕР»СЊРєРѕ С„Р°Р№Р»С‹ РёР·РѕР±СЂР°Р¶РµРЅРёР№ (JPG, PNG, GIF, WebP)";
                    DeleteGlobalId(globalId);
                    return RedirectToAction("Grafics");
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Р¤Р°Р№Р» СЃР»РёС€РєРѕРј Р±РѕР»СЊС€РѕР№ (РјР°РєСЃРёРјСѓРј 5MB)";
                    DeleteGlobalId(globalId);
                    return RedirectToAction("Grafics");
                }

                fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(graficsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
            }

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO grafic 
                        (id, name, name_eng, name_ger, image,
                        description_ru, description_eng, description_ger)
                    VALUES 
                        (@id, @name, @name_eng, @name_ger, @image,
                        @description_ru, @description_eng, @description_ger)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@name", grafic.Name ?? "");
                        command.Parameters.AddWithValue("@name_eng", grafic.NameEng ?? "");
                        command.Parameters.AddWithValue("@name_ger", grafic.NameGer ?? "");
                        command.Parameters.AddWithValue("@image", fileName ?? "");
                        command.Parameters.AddWithValue("@description_ru", grafic.DescriptionRu ?? "");
                        command.Parameters.AddWithValue("@description_eng", grafic.DescriptionEng ?? "");
                        command.Parameters.AddWithValue("@description_ger", grafic.DescriptionGer ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "Р“СЂР°С„РёРє СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                if (!string.IsNullOrEmpty(fileName))
                {
                    var filePath = Path.Combine(_sharedUploadsPath, "grafics", fileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        try { System.IO.File.Delete(filePath); } catch { }
                    }
                }
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Grafics");
        }

        [HttpPost("UpdateGrafic")]
        public async Task<IActionResult> UpdateGrafic(GraficModel grafic, IFormFile imageFile)
        {
            string currentImage = grafic.Image;
            string fileName = currentImage;

            if (imageFile != null && imageFile.Length > 0)
            {
                var graficsPath = Path.Combine(_sharedUploadsPath, "grafics");

                if (!Directory.Exists(graficsPath))
                {
                    Directory.CreateDirectory(graficsPath);
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Р Р°Р·СЂРµС€РµРЅС‹ С‚РѕР»СЊРєРѕ С„Р°Р№Р»С‹ РёР·РѕР±СЂР°Р¶РµРЅРёР№";
                    return RedirectToAction("Grafics");
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Р¤Р°Р№Р» СЃР»РёС€РєРѕРј Р±РѕР»СЊС€РѕР№ (РјР°РєСЃРёРјСѓРј 5MB)";
                    return RedirectToAction("Grafics");
                }

                fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(graficsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(currentImage))
                {
                    var oldFilePath = Path.Combine(graficsPath, currentImage);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try { System.IO.File.Delete(oldFilePath); } catch { }
                    }
                }
            }

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"UPDATE grafic SET 
                        name = @name,
                        name_eng = @name_eng,
                        name_ger = @name_ger,
                        image = @image,
                        description_ru = @description_ru,
                        description_eng = @description_eng,
                        description_ger = @description_ger
                        WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", grafic.Id);
                        command.Parameters.AddWithValue("@name", grafic.Name ?? "");
                        command.Parameters.AddWithValue("@name_eng", grafic.NameEng ?? "");
                        command.Parameters.AddWithValue("@name_ger", grafic.NameGer ?? "");
                        command.Parameters.AddWithValue("@image", fileName ?? "");
                        command.Parameters.AddWithValue("@description_ru", grafic.DescriptionRu ?? "");
                        command.Parameters.AddWithValue("@description_eng", grafic.DescriptionEng ?? "");
                        command.Parameters.AddWithValue("@description_ger", grafic.DescriptionGer ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "Р“СЂР°С„РёРє СѓСЃРїРµС€РЅРѕ РѕР±РЅРѕРІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Grafics");
        }

        [HttpPost("DeleteGrafic")]
        public async Task<IActionResult> DeleteGrafic(int id)
        {
            GraficModel grafic = GetGraficById(id);

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM grafic WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);

                if (grafic != null && !string.IsNullOrEmpty(grafic.Image))
                {
                    var filePath = Path.Combine(_sharedUploadsPath, "grafics", grafic.Image);
                    if (System.IO.File.Exists(filePath))
                    {
                        try { System.IO.File.Delete(filePath); } catch { }
                    }
                }

                TempData["Message"] = "Р“СЂР°С„РёРє СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("Grafics");
        }

        // ============================================
        // РЁРђР‘Р›РћРќР« Р“Р РђР¤РРљРћР’ (TEMPLATE_GRAFIC)
        // ============================================

        [HttpGet("TemplateGrafics")]
        public IActionResult TemplateGrafics()
        {
            List<TemplateGraficModel> templates = GetTemplateGraficsFromDatabase();
            return View(templates);
        }

        [HttpPost("AddTemplateGrafic")]
        public async Task<IActionResult> AddTemplateGrafic(string name, string ids)
        {
            int globalId = CreateGlobalId("template_grafic");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "INSERT INTO template_grafic (id, name, ids, used_in_cars) VALUES (@id, @name, @ids, '')";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@ids", ids ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "РЁР°Р±Р»РѕРЅ РіСЂР°С„РёРєРѕРІ СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateGrafics");
        }

        [HttpPost("DeleteTemplateGrafic")]
        public async Task<IActionResult> DeleteTemplateGrafic(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM template_grafic WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);

                TempData["Message"] = "РЁР°Р±Р»РѕРЅ РіСЂР°С„РёРєРѕРІ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateGrafics");
        }

        // ============================================
        // Р”РћРџРћР›РќРРўР•Р›Р¬РќР«Р• Р¦Р•РќР« (ADDITIONAL PRICES)
        // ============================================

        [HttpGet("AdditionalPrices")]
        public IActionResult AdditionalPrices()
        {
            List<AdditionalPriceModel> prices = GetAdditionalPricesFromDatabase();
            return View(prices);
        }

        [HttpGet("CreateAdditionalPrice")]
        public IActionResult CreateAdditionalPrice()
        {
            return View("CreateAdditionalPrice", new AdditionalPriceModel());
        }

        [HttpGet("EditAdditionalPrice/{id?}")]
        public IActionResult EditAdditionalPrice(int id)
        {
            AdditionalPriceModel price = GetAdditionalPriceById(id);
            if (price == null)
            {
                TempData["Error"] = "Р¦РµРЅР° РЅРµ РЅР°Р№РґРµРЅР°";
                return RedirectToAction("AdditionalPrices");
            }
            return View(price);
        }

        [HttpPost("AddAdditionalPrice")]
        public async Task<IActionResult> AddAdditionalPrice(AdditionalPriceModel price)
        {
            int globalId = CreateGlobalId("additional_prices");

            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO additional_prices 
                (id, name_ru, name_eng, name_ger, 
                price_rubl, price_dolar, price_euro,
                info_ru, info_eng, info_ger, sort_order, price_controler,
                free_price_ids, base_price_ids, pro_price_ids, unselected_price_mode) 
            VALUES 
                (@id, @name_ru, @name_eng, @name_ger, 
                @price_rubl, @price_dolar, @price_euro,
                @info_ru, @info_eng, @info_ger, @sort_order, @price_controler,
                @free_price_ids, @base_price_ids, @pro_price_ids, @unselected_price_mode)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", globalId);
                        command.Parameters.AddWithValue("@name_ru", price.NameRu ?? "");
                        command.Parameters.AddWithValue("@name_eng", price.NameEng ?? "");
                        command.Parameters.AddWithValue("@name_ger", price.NameGer ?? "");
                        command.Parameters.AddWithValue("@price_rubl", price.PriceRubl ?? "");
                        command.Parameters.AddWithValue("@price_dolar", price.PriceDolar ?? "");
                        command.Parameters.AddWithValue("@price_euro", price.PriceEuro ?? "");
                        command.Parameters.AddWithValue("@info_ru", price.InfoRu ?? "");
                        command.Parameters.AddWithValue("@info_eng", price.InfoEng ?? "");
                        command.Parameters.AddWithValue("@info_ger", price.InfoGer ?? "");
                        command.Parameters.AddWithValue("@sort_order", price.SortOrder);
                        command.Parameters.AddWithValue("@price_controler", price.PriceControler);
                        // ===== РќРћР’Р«Р• РџРћР›РЇ =====
                        command.Parameters.AddWithValue("@free_price_ids", price.FreePriceIds ?? "");
                        command.Parameters.AddWithValue("@base_price_ids", price.BasePriceIds ?? "");
                        command.Parameters.AddWithValue("@pro_price_ids", price.ProPriceIds ?? "");

                        command.Parameters.AddWithValue("@unselected_price_mode", price.UnselectedPriceMode);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ С†РµРЅР° СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅР°";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("AdditionalPrices");
        }

        [HttpPost("UpdateAdditionalPrice")]
        public async Task<IActionResult> UpdateAdditionalPrice(AdditionalPriceModel price)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"UPDATE additional_prices SET 
                name_ru = @name_ru,
                name_eng = @name_eng,
                name_ger = @name_ger,
                price_rubl = @price_rubl,
                price_dolar = @price_dolar,
                price_euro = @price_euro,
                info_ru = @info_ru,
                info_eng = @info_eng,
                info_ger = @info_ger,
                sort_order = @sort_order,
                price_controler = @price_controler,
                free_price_ids = @free_price_ids,
                base_price_ids = @base_price_ids,
                pro_price_ids = @pro_price_ids,
                unselected_price_mode = @unselected_price_mode
                WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", price.Id);
                        command.Parameters.AddWithValue("@name_ru", price.NameRu ?? "");
                        command.Parameters.AddWithValue("@name_eng", price.NameEng ?? "");
                        command.Parameters.AddWithValue("@name_ger", price.NameGer ?? "");
                        command.Parameters.AddWithValue("@price_rubl", price.PriceRubl ?? "");
                        command.Parameters.AddWithValue("@price_dolar", price.PriceDolar ?? "");
                        command.Parameters.AddWithValue("@price_euro", price.PriceEuro ?? "");
                        command.Parameters.AddWithValue("@info_ru", price.InfoRu ?? "");
                        command.Parameters.AddWithValue("@info_eng", price.InfoEng ?? "");
                        command.Parameters.AddWithValue("@info_ger", price.InfoGer ?? "");
                        command.Parameters.AddWithValue("@sort_order", price.SortOrder);
                        command.Parameters.AddWithValue("@price_controler", price.PriceControler);
                        // ===== РќРћР’Р«Р• РџРћР›РЇ =====
                        command.Parameters.AddWithValue("@free_price_ids", price.FreePriceIds ?? "");
                        command.Parameters.AddWithValue("@base_price_ids", price.BasePriceIds ?? "");
                        command.Parameters.AddWithValue("@pro_price_ids", price.ProPriceIds ?? "");

                        command.Parameters.AddWithValue("@unselected_price_mode", price.UnselectedPriceMode);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = "Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ С†РµРЅР° СѓСЃРїРµС€РЅРѕ РѕР±РЅРѕРІР»РµРЅР°";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ С†РµРЅР° РЅРµ РЅР°Р№РґРµРЅР°";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("AdditionalPrices");
        }

        [HttpPost("DeleteAdditionalPrice")]
        public async Task<IActionResult> DeleteAdditionalPrice(int id)
        {
            string connectionString = GetConnectionString();
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM additional_prices WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                DeleteGlobalId(id);
                TempData["Message"] = "Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ С†РµРЅР° СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅР°";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL: {ex.Message}";
            }

            return RedirectToAction("AdditionalPrices");
        }

        // ============================================
        // РЁРђР‘Р›РћРќР« Р”РћРџРћР›РќРРўР•Р›Р¬РќР«РҐ Р¦Р•Рќ (TEMPLATE_ADDITIONAL_PRICES)
        // ============================================

        // ============================================
        // РњР•РўРћР”Р« Р”Р›РЇ РџРћР›РЈР§Р•РќРРЇ ITEMS РР— РЁРђР‘Р›РћРќРћР’ (Р”Р›РЇ Templates.cshtml)
        // ============================================

        private List<TemplateItemDetail> GetAboutItemsFromTemplate(int templateId)
        {
            var template = GetTemplateAboutsFromDatabase().FirstOrDefault(t => t.Id == templateId);
            if (template == null || string.IsNullOrEmpty(template.Ids))
                return new List<TemplateItemDetail>();

            var ids = template.Ids.Split(',').Select(int.Parse).ToList();
            var allAbout = GetAboutsFromDatabase();
            var dict = allAbout.ToDictionary(a => a.Id);

            return ids
                .Where(id => dict.ContainsKey(id))
                .Select(id => {
                    var a = dict[id];
                    return new TemplateItemDetail
                    {
                        Id = a.Id,
                        DisplayText = a.TextRu ?? a.TextEng ?? a.TextGer ?? "(РїСѓСЃС‚Рѕ)",
                        TextRu = a.TextRu,
                        TextEng = a.TextEng,
                        TextGer = a.TextGer
                    };
                })
                .ToList();
        }

        private List<TemplateItemDetail> GetResultItemsFromTemplate(int templateId)
        {
            var template = GetTemplateResultsFromDatabase().FirstOrDefault(t => t.Id == templateId);
            if (template == null || string.IsNullOrEmpty(template.Ids))
                return new List<TemplateItemDetail>();

            var ids = template.Ids.Split(',').Select(int.Parse).ToList();
            var allResults = GetResultsFromDatabase();
            var dict = allResults.ToDictionary(r => r.Id);

            return ids
                .Where(id => dict.ContainsKey(id))
                .Select(id => {
                    var r = dict[id];
                    return new TemplateItemDetail
                    {
                        Id = r.Id,
                        DisplayText = r.TextRu ?? r.TextEng ?? r.TextGer ?? "(РїСѓСЃС‚Рѕ)",
                        TextRu = r.TextRu,
                        TextEng = r.TextEng,
                        TextGer = r.TextGer
                    };
                })
                .ToList();
        }

        private List<TemplateItemDetail> GetEngineControlItemsFromTemplate(int templateId)
        {
            var template = GetTemplateEngineControlsFromDatabase().FirstOrDefault(t => t.Id == templateId);
            if (template == null || string.IsNullOrEmpty(template.Ids))
                return new List<TemplateItemDetail>();

            var ids = template.Ids.Split(',').Select(int.Parse).ToList();
            var allControls = GetEngineControlsFromDatabase();
            var dict = allControls.ToDictionary(e => e.Id);

            return ids
                .Where(id => dict.ContainsKey(id))
                .Select(id => {
                    var e = dict[id];
                    return new TemplateItemDetail
                    {
                        Id = e.Id,
                        DisplayText = e.TextRu ?? e.TextEng ?? e.TextGer ?? "(РїСѓСЃС‚Рѕ)",
                        TextRu = e.TextRu,
                        TextEng = e.TextEng,
                        TextGer = e.TextGer
                    };
                })
                .ToList();
        }

        private List<TemplateItemDetail> GetPriceItemsFromTemplate(int templateId)
        {
            var template = GetTemplatePricesFromDatabase().FirstOrDefault(t => t.Id == templateId);
            if (template == null || string.IsNullOrEmpty(template.Prices))
                return new List<TemplateItemDetail>();

            // РџРѕР»СѓС‡Р°РµРј ID РІ РїРѕСЂСЏРґРєРµ РёР· С€Р°Р±Р»РѕРЅР°
            var ids = template.Prices.Split(',').Select(int.Parse).ToList();
            var allPrices = GetPricesFromDatabase();

            // РЎРѕР·РґР°С‘Рј СЃР»РѕРІР°СЂСЊ РґР»СЏ Р±С‹СЃС‚СЂРѕРіРѕ РґРѕСЃС‚СѓРїР°
            var priceDict = allPrices.ToDictionary(p => p.Id);

            // РР”РЃРњ Р’ РџРћР РЇР”РљР• ID РР— РЁРђР‘Р›РћРќРђ!
            return ids
                .Where(id => priceDict.ContainsKey(id))
                .Select(id => {
                    var p = priceDict[id];
                    return new TemplateItemDetail
                    {
                        Id = p.Id,
                        DisplayText = p.NameRu ?? p.NameEng ?? p.NameGer ?? "(Р±РµР· РЅР°Р·РІР°РЅРёСЏ)",
                        NameRu = p.NameRu,
                        NameEng = p.NameEng,
                        NameGer = p.NameGer,
                        BasePrice = p.BasePrice,
                        ProPrice = p.ProPrice,
                        BasePriceEng = p.BasePriceEng,
                        ProPriceEng = p.ProPriceEng,
                        BasePriceGer = p.BasePriceGer,
                        ProPriceGer = p.ProPriceGer,
                        InfoRu = p.InfoRu,
                        InfoEng = p.InfoEng,
                        InfoGer = p.InfoGer
                    };
                })
                .ToList();
        }

        private List<TemplateItemDetail> GetGraficItemsFromTemplate(int templateId)
        {
            var template = GetTemplateGraficsFromDatabase().FirstOrDefault(t => t.Id == templateId);
            if (template == null || string.IsNullOrEmpty(template.Ids))
                return new List<TemplateItemDetail>();

            var ids = template.Ids.Split(',').Select(int.Parse).ToList();
            var allGrafics = GetGraficsFromDatabase();
            var dict = allGrafics.ToDictionary(g => g.Id);

            return ids
                .Where(id => dict.ContainsKey(id))
                .Select(id => {
                    var g = dict[id];
                    return new TemplateItemDetail
                    {
                        Id = g.Id,
                        DisplayText = g.Name ?? "(Р±РµР· РЅР°Р·РІР°РЅРёСЏ)",
                        GraficName = g.Name,
                        GraficNameEng = g.NameEng,
                        GraficNameGer = g.NameGer,
                        GraficImage = g.Image,
                        GraficDescriptionRu = g.DescriptionRu,
                        GraficDescriptionEng = g.DescriptionEng,
                        GraficDescriptionGer = g.DescriptionGer
                    };
                })
                .ToList();
        }

        private List<TemplateItemDetail> GetAdditionalPriceItemsFromTemplate(int templateId)
        {
            var template = GetTemplateAdditionalPricesFromDatabase().FirstOrDefault(t => t.Id == templateId);
            if (template == null || string.IsNullOrEmpty(template.PriceIds))
                return new List<TemplateItemDetail>();

            var ids = template.PriceIds.Split(',').Select(int.Parse).ToList();
            var allPrices = GetAdditionalPricesFromDatabase();
            var dict = allPrices.ToDictionary(p => p.Id);

            return ids
                .Where(id => dict.ContainsKey(id))
                .Select(id => {
                    var p = dict[id];
                    return new TemplateItemDetail
                    {
                        Id = p.Id,
                        DisplayText = p.NameRu ?? p.NameEng ?? p.NameGer ?? "(Р±РµР· РЅР°Р·РІР°РЅРёСЏ)",
                        NameRu = p.NameRu,
                        NameEng = p.NameEng,
                        NameGer = p.NameGer,
                        PriceRubl = p.PriceRubl,
                        PriceDolar = p.PriceDolar,
                        PriceEuro = p.PriceEuro,
                        InfoRu = p.InfoRu,
                        InfoEng = p.InfoEng,
                        InfoGer = p.InfoGer,
                        PriceControler = p.PriceControler,
                        // ===== РќРћР’Р«Р• РџРћР›РЇ =====
                        FreePriceIds = p.FreePriceIds,
                        BasePriceIds = p.BasePriceIds,
                        ProPriceIds = p.ProPriceIds,
                        UnselectedPriceMode = p.UnselectedPriceMode
                    };
                })
                .ToList();
        }

        // ============================================
        // РџРћР›РЈР§Р•РќРР• РЁРђР‘Р›РћРќРћР’ РЎ Р—РђРџРРЎРЇРњР Р”Р›РЇ РЎРўР РђРќРР¦Р« TEMPLATES
        // ============================================

        private async Task<List<TemplateWithItems>> GetTemplatesWithItemsAsync(string type)
        {
            var result = new List<TemplateWithItems>();

            switch (type)
            {
                case "about":
                    var aboutTemplates = GetTemplateAboutsFromDatabase()
                .OrderBy(t => t.SortOrder)
                .ToList();
                    foreach (var t in aboutTemplates)
                    {
                        var carNames = await GetCarNamesForTemplate(t.Id, type);
                        result.Add(new TemplateWithItems
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Ids = t.Ids,
                            Type = type,
                            SortOrder = t.SortOrder,
                            UsedInCars = carNames,
                            Items = GetAboutItemsFromTemplate(t.Id)
                        });
                    }
                    break;

                case "result":
                    var resultTemplates = GetTemplateResultsFromDatabase()
                .OrderBy(t => t.SortOrder)  
                .ToList();
                    foreach (var t in resultTemplates)
                    {
                        var carNames = await GetCarNamesForTemplate(t.Id, type);
                        result.Add(new TemplateWithItems
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Ids = t.Ids,
                            Type = type,
                            SortOrder = t.SortOrder,
                            UsedInCars = carNames,
                            Items = GetResultItemsFromTemplate(t.Id)
                        });
                    }
                    break;

                case "engine":
                    var engineTemplates = GetTemplateEngineControlsFromDatabase()
                .OrderBy(t => t.SortOrder)  
                .ToList();
                    foreach (var t in engineTemplates)
                    {
                        var carNames = await GetCarNamesForTemplate(t.Id, type);
                        result.Add(new TemplateWithItems
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Ids = t.Ids,
                            Type = type,
                            SortOrder = t.SortOrder,
                            UsedInCars = carNames,
                            Items = GetEngineControlItemsFromTemplate(t.Id)
                        });
                    }
                    break;

                case "price":
                    var priceTemplates = GetTemplatePricesFromDatabase()
                .OrderBy(t => t.SortOrder) 
                .ToList();
                    foreach (var t in priceTemplates)
                    {
                        var carNames = await GetCarNamesForTemplate(t.Id, type);
                        result.Add(new TemplateWithItems
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Ids = t.Prices,
                            Type = type,
                            SortOrder = t.SortOrder,
                            UsedInCars = carNames,
                            Items = GetPriceItemsFromTemplate(t.Id)
                        });
                    }
                    break;

                case "grafic":
                    var graficTemplates = GetTemplateGraficsFromDatabase()
                .OrderBy(t => t.SortOrder) 
                .ToList();
                    foreach (var t in graficTemplates)
                    {
                        var carNames = await GetCarNamesForTemplate(t.Id, type);
                        result.Add(new TemplateWithItems
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Ids = t.Ids,
                            Type = type,
                            SortOrder = t.SortOrder,
                            UsedInCars = carNames,
                            Items = GetGraficItemsFromTemplate(t.Id)
                        });
                    }
                    break;

                case "additional":
                    var additionalTemplates = GetTemplateAdditionalPricesFromDatabase()
                .OrderBy(t => t.SortOrder)  // <-- ДОБАВИТЬ
                .ToList();
                    foreach (var t in additionalTemplates)
                    {
                        var carNames = await GetCarNamesForTemplate(t.Id, type);
                        result.Add(new TemplateWithItems
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Ids = t.PriceIds,
                            Type = type,
                            SortOrder = t.SortOrder,
                            UsedInCars = carNames,
                            Items = GetAdditionalPriceItemsFromTemplate(t.Id)
                        });
                    }
                    break;
            }

            return result;
        }

        // ==========================================
        // РџРћР›РЈР§Р•РќРР• РЎРџРРЎРљРђ РњРђРЁРРќ, РРЎРџРћР›Р¬Р—РЈР®Р©РРҐ РЁРђР‘Р›РћРќ
        // ==========================================

        private async Task<List<int>> GetCarsUsingTemplate(int templateId, string type)
        {
            var carIds = new List<int>();
            string templateTable = GetTemplateTable(type);
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = $"SELECT used_in_cars FROM {templateTable} WHERE id = @id";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@id", templateId);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result != null && !string.IsNullOrEmpty(result.ToString()))
                    {
                        var idsString = result.ToString();
                        carIds = idsString
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(int.Parse)
                            .ToList();
                    }
                }
            }

            return carIds;
        }

        private async Task<string> GetCarNamesForTemplate(int templateId, string type)
        {
            var carIds = await GetCarsUsingTemplate(templateId, type);
            if (carIds.Count == 0) return "РќРµ РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ";

            string connectionString = GetConnectionString();
            var carNames = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string ids = string.Join(",", carIds);
                string query = $"SELECT brand, model FROM reflash_cars WHERE id IN ({ids})";

                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string brand = reader.GetString("brand");
                        string model = reader.GetString("model");
                        carNames.Add($"{brand} {model}");
                    }
                }
            }

            return carNames.Count > 0 ? string.Join(", ", carNames) : "РќРµ РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ";
        }

        // ============================================
        // РЎРћР—Р”РђРќРР• РЁРђР‘Р›РћРќРћР’ РЎ Р—РђРџРРЎРЇРњР (РЎ РџРћР”Р”Р•Р Р–РљРћР™ EXISTING_ID)
        // ============================================

        [HttpPost("CreateAboutTemplateWithItems")]
        public async Task<IActionResult> CreateAboutTemplateWithItems(string templateName, List<AboutItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Р—Р°РїРѕР»РЅРёС‚Рµ РЅР°Р·РІР°РЅРёРµ С€Р°Р±Р»РѕРЅР° Рё РґРѕР±Р°РІСЊС‚Рµ С…РѕС‚СЏ Р±С‹ РѕРґРЅСѓ Р·Р°РїРёСЃСЊ";
                return RedirectToAction("Templates", new { activeTab = "about" });
            }

            int templateId = CreateGlobalId("template_about");
            string connectionString = GetConnectionString();
            List<int> itemIds = new List<int>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var item in items)
                {
                    int itemId;

                    // ===== РџР РћР’Р•Р РЇР•Рњ: Р•РЎР›Р РЈРљРђР—РђРќ EXISTING_ID, РРЎРџРћР›Р¬Р—РЈР•Рњ РЎРЈР©Р•РЎРўР’РЈР®Р©РЈР® Р—РђРџРРЎР¬ =====
                    if (item.ExistingId.HasValue && item.ExistingId.Value > 0)
                    {
                        // РџСЂРѕРІРµСЂСЏРµРј, СЃСѓС‰РµСЃС‚РІСѓРµС‚ Р»Рё Р·Р°РїРёСЃСЊ СЃ С‚Р°РєРёРј ID
                        string checkQuery = "SELECT COUNT(*) FROM about WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.ExistingId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"Р—Р°РїРёСЃСЊ СЃ ID {item.ExistingId.Value} РЅРµ РЅР°Р№РґРµРЅР° РІ С‚Р°Р±Р»РёС†Рµ about";
                                return RedirectToAction("Templates", new { activeTab = "about" });
                            }
                        }

                        // ===== РРЎРџРћР›Р¬Р—РЈР•Рњ РЎРЈР©Р•РЎРўР’РЈР®Р©РЈР® Р—РђРџРРЎР¬ =====
                        itemId = item.ExistingId.Value;

                        // РћР±РЅРѕРІР»СЏРµРј Р·Р°РїРёСЃСЊ, РµСЃР»Рё РїРµСЂРµРґР°РЅС‹ РЅРѕРІС‹Рµ РґР°РЅРЅС‹Рµ
                        bool hasData = !string.IsNullOrEmpty(item.TextRu) || !string.IsNullOrEmpty(item.TextEng) || !string.IsNullOrEmpty(item.TextGer);

                        if (hasData)
                        {
                            string updateQuery = "UPDATE about SET text_ru = @text_ru, text_eng = @text_eng, text_ger = @text_ger WHERE id = @id";
                            using (var updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@id", itemId);
                                updateCmd.Parameters.AddWithValue("@text_ru", item.TextRu ?? "");
                                updateCmd.Parameters.AddWithValue("@text_eng", item.TextEng ?? "");
                                updateCmd.Parameters.AddWithValue("@text_ger", item.TextGer ?? "");
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    else
                    {
                        // ===== РЎРћР—Р”РђРЃРњ РќРћР’РЈР® Р—РђРџРРЎР¬ =====
                        itemId = CreateGlobalId("about");
                        string insertQuery = "INSERT INTO about (id, text_ru, text_eng, text_ger) VALUES (@id, @text_ru, @text_eng, @text_ger)";
                        using (var insertCmd = new MySqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@id", itemId);
                            insertCmd.Parameters.AddWithValue("@text_ru", item.TextRu ?? "");
                            insertCmd.Parameters.AddWithValue("@text_eng", item.TextEng ?? "");
                            insertCmd.Parameters.AddWithValue("@text_ger", item.TextGer ?? "");
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    itemIds.Add(itemId);
                }

                // РЎРѕС…СЂР°РЅСЏРµРј С€Р°Р±Р»РѕРЅ
                string ids = string.Join(",", itemIds);
                string templateQuery = "INSERT INTO template_about (id, name, ids) VALUES (@id, @name, @ids)";
                using (var cmd = new MySqlCommand(templateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", templateId);
                    cmd.Parameters.AddWithValue("@name", templateName ?? "");
                    cmd.Parameters.AddWithValue("@ids", ids);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["Message"] = $"РЁР°Р±Р»РѕРЅ '{templateName}' СѓСЃРїРµС€РЅРѕ СЃРѕР·РґР°РЅ СЃ {itemIds.Count} Р·Р°РїРёСЃСЏРјРё";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "about" });
        }

        [HttpPost("CreateResultTemplateWithItems")]
        public async Task<IActionResult> CreateResultTemplateWithItems(string templateName, List<ResultItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Р—Р°РїРѕР»РЅРёС‚Рµ РЅР°Р·РІР°РЅРёРµ С€Р°Р±Р»РѕРЅР° Рё РґРѕР±Р°РІСЊС‚Рµ С…РѕС‚СЏ Р±С‹ РѕРґРЅСѓ Р·Р°РїРёСЃСЊ";
                return RedirectToAction("Templates", new { activeTab = "result" });
            }

            int templateId = CreateGlobalId("template_result");
            string connectionString = GetConnectionString();
            List<int> itemIds = new List<int>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var item in items)
                {
                    int itemId;

                    if (item.ExistingId.HasValue && item.ExistingId.Value > 0)
                    {
                        string checkQuery = "SELECT COUNT(*) FROM result WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.ExistingId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"Р—Р°РїРёСЃСЊ СЃ ID {item.ExistingId.Value} РЅРµ РЅР°Р№РґРµРЅР° РІ С‚Р°Р±Р»РёС†Рµ result";
                                return RedirectToAction("Templates", new { activeTab = "result" });
                            }
                        }

                        itemId = item.ExistingId.Value;

                        bool hasData = !string.IsNullOrEmpty(item.TextRu) || !string.IsNullOrEmpty(item.TextEng) || !string.IsNullOrEmpty(item.TextGer);

                        if (hasData)
                        {
                            string updateQuery = "UPDATE result SET text_ru = @text_ru, text_eng = @text_eng, text_ger = @text_ger WHERE id = @id";
                            using (var updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@id", itemId);
                                updateCmd.Parameters.AddWithValue("@text_ru", item.TextRu ?? "");
                                updateCmd.Parameters.AddWithValue("@text_eng", item.TextEng ?? "");
                                updateCmd.Parameters.AddWithValue("@text_ger", item.TextGer ?? "");
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    else
                    {
                        itemId = CreateGlobalId("result");
                        string insertQuery = "INSERT INTO result (id, text_ru, text_eng, text_ger) VALUES (@id, @text_ru, @text_eng, @text_ger)";
                        using (var insertCmd = new MySqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@id", itemId);
                            insertCmd.Parameters.AddWithValue("@text_ru", item.TextRu ?? "");
                            insertCmd.Parameters.AddWithValue("@text_eng", item.TextEng ?? "");
                            insertCmd.Parameters.AddWithValue("@text_ger", item.TextGer ?? "");
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    itemIds.Add(itemId);
                }

                string ids = string.Join(",", itemIds);
                string templateQuery = "INSERT INTO template_result (id, name, ids) VALUES (@id, @name, @ids)";
                using (var cmd = new MySqlCommand(templateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", templateId);
                    cmd.Parameters.AddWithValue("@name", templateName ?? "");
                    cmd.Parameters.AddWithValue("@ids", ids);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["Message"] = $"РЁР°Р±Р»РѕРЅ '{templateName}' СѓСЃРїРµС€РЅРѕ СЃРѕР·РґР°РЅ СЃ {itemIds.Count} Р·Р°РїРёСЃСЏРјРё";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "result" });
        }

        [HttpPost("CreateEngineTemplateWithItems")]
        public async Task<IActionResult> CreateEngineTemplateWithItems(string templateName, List<EngineItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Р—Р°РїРѕР»РЅРёС‚Рµ РЅР°Р·РІР°РЅРёРµ С€Р°Р±Р»РѕРЅР° Рё РґРѕР±Р°РІСЊС‚Рµ С…РѕС‚СЏ Р±С‹ РѕРґРЅСѓ Р·Р°РїРёСЃСЊ";
                return RedirectToAction("Templates", new { activeTab = "engine" });
            }

            int templateId = CreateGlobalId("template_engine_control");
            string connectionString = GetConnectionString();
            List<int> itemIds = new List<int>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var item in items)
                {
                    int itemId;

                    if (item.ExistingId.HasValue && item.ExistingId.Value > 0)
                    {
                        string checkQuery = "SELECT COUNT(*) FROM engine_control WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.ExistingId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"Р—Р°РїРёСЃСЊ СЃ ID {item.ExistingId.Value} РЅРµ РЅР°Р№РґРµРЅР° РІ С‚Р°Р±Р»РёС†Рµ engine_control";
                                return RedirectToAction("Templates", new { activeTab = "engine" });
                            }
                        }

                        itemId = item.ExistingId.Value;

                        bool hasData = !string.IsNullOrEmpty(item.TextRu) || !string.IsNullOrEmpty(item.TextEng) || !string.IsNullOrEmpty(item.TextGer);

                        if (hasData)
                        {
                            string updateQuery = "UPDATE engine_control SET text_ru = @text_ru, text_eng = @text_eng, text_ger = @text_ger WHERE id = @id";
                            using (var updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@id", itemId);
                                updateCmd.Parameters.AddWithValue("@text_ru", item.TextRu ?? "");
                                updateCmd.Parameters.AddWithValue("@text_eng", item.TextEng ?? "");
                                updateCmd.Parameters.AddWithValue("@text_ger", item.TextGer ?? "");
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    else
                    {
                        itemId = CreateGlobalId("engine_control");
                        string insertQuery = "INSERT INTO engine_control (id, text_ru, text_eng, text_ger) VALUES (@id, @text_ru, @text_eng, @text_ger)";
                        using (var insertCmd = new MySqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@id", itemId);
                            insertCmd.Parameters.AddWithValue("@text_ru", item.TextRu ?? "");
                            insertCmd.Parameters.AddWithValue("@text_eng", item.TextEng ?? "");
                            insertCmd.Parameters.AddWithValue("@text_ger", item.TextGer ?? "");
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    itemIds.Add(itemId);
                }

                string ids = string.Join(",", itemIds);
                string templateQuery = "INSERT INTO template_engine_control (id, name, ids) VALUES (@id, @name, @ids)";
                using (var cmd = new MySqlCommand(templateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", templateId);
                    cmd.Parameters.AddWithValue("@name", templateName ?? "");
                    cmd.Parameters.AddWithValue("@ids", ids);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["Message"] = $"РЁР°Р±Р»РѕРЅ '{templateName}' СѓСЃРїРµС€РЅРѕ СЃРѕР·РґР°РЅ СЃ {itemIds.Count} Р·Р°РїРёСЃСЏРјРё";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "engine" });
        }

        [HttpPost("CreatePriceTemplateWithItems")]
        public async Task<IActionResult> CreatePriceTemplateWithItems(string templateName, List<PriceItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Р—Р°РїРѕР»РЅРёС‚Рµ РЅР°Р·РІР°РЅРёРµ С€Р°Р±Р»РѕРЅР° Рё РґРѕР±Р°РІСЊС‚Рµ С…РѕС‚СЏ Р±С‹ РѕРґРЅСѓ Р·Р°РїРёСЃСЊ";
                return RedirectToAction("Templates", new { activeTab = "price" });
            }

            int templateId = CreateGlobalId("template_price");
            string connectionString = GetConnectionString();
            List<int> itemIds = new List<int>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var item in items)
                {
                    int itemId;

                    if (item.ExistingId.HasValue && item.ExistingId.Value > 0)
                    {
                        string checkQuery = "SELECT COUNT(*) FROM price WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.ExistingId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"Р—Р°РїРёСЃСЊ СЃ ID {item.ExistingId.Value} РЅРµ РЅР°Р№РґРµРЅР° РІ С‚Р°Р±Р»РёС†Рµ price";
                                return RedirectToAction("Templates", new { activeTab = "price" });
                            }
                        }

                        itemId = item.ExistingId.Value;

                        bool hasData = !string.IsNullOrEmpty(item.NameRu) || !string.IsNullOrEmpty(item.NameEng) ||
                                       !string.IsNullOrEmpty(item.NameGer) || !string.IsNullOrEmpty(item.BasePrice) ||
                                       !string.IsNullOrEmpty(item.ProPrice) || !string.IsNullOrEmpty(item.BasePriceEng) ||
                                       !string.IsNullOrEmpty(item.ProPriceEng) || !string.IsNullOrEmpty(item.BasePriceGer) ||
                                       !string.IsNullOrEmpty(item.ProPriceGer) || !string.IsNullOrEmpty(item.InfoRu) ||
                                       !string.IsNullOrEmpty(item.InfoEng) || !string.IsNullOrEmpty(item.InfoGer);

                        if (hasData)
                        {
                            string updateQuery = @"UPDATE price SET 
                                name_ru = @name_ru, name_eng = @name_eng, name_ger = @name_ger,
                                base_price = @base_price, pro_price = @pro_price,
                                base_price_eng = @base_price_eng, pro_price_eng = @pro_price_eng,
                                base_price_ger = @base_price_ger, pro_price_ger = @pro_price_ger,
                                info_ru = @info_ru, info_eng = @info_eng, info_ger = @info_ger
                                WHERE id = @id";
                            using (var updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@id", itemId);
                                updateCmd.Parameters.AddWithValue("@name_ru", item.NameRu ?? "");
                                updateCmd.Parameters.AddWithValue("@name_eng", item.NameEng ?? "");
                                updateCmd.Parameters.AddWithValue("@name_ger", item.NameGer ?? "");
                                updateCmd.Parameters.AddWithValue("@base_price", item.BasePrice ?? "");
                                updateCmd.Parameters.AddWithValue("@pro_price", item.ProPrice ?? "");
                                updateCmd.Parameters.AddWithValue("@base_price_eng", item.BasePriceEng ?? "");
                                updateCmd.Parameters.AddWithValue("@pro_price_eng", item.ProPriceEng ?? "");
                                updateCmd.Parameters.AddWithValue("@base_price_ger", item.BasePriceGer ?? "");
                                updateCmd.Parameters.AddWithValue("@pro_price_ger", item.ProPriceGer ?? "");
                                updateCmd.Parameters.AddWithValue("@info_ru", item.InfoRu ?? "");
                                updateCmd.Parameters.AddWithValue("@info_eng", item.InfoEng ?? "");
                                updateCmd.Parameters.AddWithValue("@info_ger", item.InfoGer ?? "");
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    else
                    {
                        itemId = CreateGlobalId("price");
                        string insertQuery = @"INSERT INTO price (id, name_ru, name_eng, name_ger, 
                                base_price, pro_price, base_price_eng, pro_price_eng, 
                                base_price_ger, pro_price_ger, info_ru, info_eng, info_ger) 
                            VALUES (@id, @name_ru, @name_eng, @name_ger, 
                                @base_price, @pro_price, @base_price_eng, @pro_price_eng, 
                                @base_price_ger, @pro_price_ger, @info_ru, @info_eng, @info_ger)";
                        using (var insertCmd = new MySqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@id", itemId);
                            insertCmd.Parameters.AddWithValue("@name_ru", item.NameRu ?? "");
                            insertCmd.Parameters.AddWithValue("@name_eng", item.NameEng ?? "");
                            insertCmd.Parameters.AddWithValue("@name_ger", item.NameGer ?? "");
                            insertCmd.Parameters.AddWithValue("@base_price", item.BasePrice ?? "");
                            insertCmd.Parameters.AddWithValue("@pro_price", item.ProPrice ?? "");
                            insertCmd.Parameters.AddWithValue("@base_price_eng", item.BasePriceEng ?? "");
                            insertCmd.Parameters.AddWithValue("@pro_price_eng", item.ProPriceEng ?? "");
                            insertCmd.Parameters.AddWithValue("@base_price_ger", item.BasePriceGer ?? "");
                            insertCmd.Parameters.AddWithValue("@pro_price_ger", item.ProPriceGer ?? "");
                            insertCmd.Parameters.AddWithValue("@info_ru", item.InfoRu ?? "");
                            insertCmd.Parameters.AddWithValue("@info_eng", item.InfoEng ?? "");
                            insertCmd.Parameters.AddWithValue("@info_ger", item.InfoGer ?? "");
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    itemIds.Add(itemId);
                }

                string ids = string.Join(",", itemIds);
                string templateQuery = "INSERT INTO template_price (id, name, prices) VALUES (@id, @name, @prices)";
                using (var cmd = new MySqlCommand(templateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", templateId);
                    cmd.Parameters.AddWithValue("@name", templateName ?? "");
                    cmd.Parameters.AddWithValue("@prices", ids);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["Message"] = $"РЁР°Р±Р»РѕРЅ '{templateName}' СѓСЃРїРµС€РЅРѕ СЃРѕР·РґР°РЅ СЃ {itemIds.Count} Р·Р°РїРёСЃСЏРјРё";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "price" });
        }

        [HttpPost("CreateGraficTemplateWithItems")]
        public async Task<IActionResult> CreateGraficTemplateWithItems(string templateName, List<GraficItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Р—Р°РїРѕР»РЅРёС‚Рµ РЅР°Р·РІР°РЅРёРµ С€Р°Р±Р»РѕРЅР° Рё РґРѕР±Р°РІСЊС‚Рµ С…РѕС‚СЏ Р±С‹ РѕРґРЅСѓ Р·Р°РїРёСЃСЊ";
                return RedirectToAction("Templates", new { activeTab = "grafic" });
            }

            int templateId = CreateGlobalId("template_grafic");
            string connectionString = GetConnectionString();
            List<int> itemIds = new List<int>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var item in items)
                {
                    int itemId;

                    if (item.ExistingId.HasValue && item.ExistingId.Value > 0)
                    {
                        string checkQuery = "SELECT COUNT(*) FROM grafic WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.ExistingId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"Р—Р°РїРёСЃСЊ СЃ ID {item.ExistingId.Value} РЅРµ РЅР°Р№РґРµРЅР° РІ С‚Р°Р±Р»РёС†Рµ grafic";
                                return RedirectToAction("Templates", new { activeTab = "grafic" });
                            }
                        }

                        itemId = item.ExistingId.Value;

                        bool hasData = !string.IsNullOrEmpty(item.GraficName) || !string.IsNullOrEmpty(item.GraficNameEng) ||
                                       !string.IsNullOrEmpty(item.GraficNameGer) || !string.IsNullOrEmpty(item.GraficDescriptionRu) ||
                                       !string.IsNullOrEmpty(item.GraficDescriptionEng) || !string.IsNullOrEmpty(item.GraficDescriptionGer);

                        if (hasData)
                        {
                            string updateQuery = @"UPDATE grafic SET 
                                name = @name, name_eng = @name_eng, name_ger = @name_ger,
                                description_ru = @description_ru, description_eng = @description_eng, description_ger = @description_ger
                                WHERE id = @id";
                            using (var updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@id", itemId);
                                updateCmd.Parameters.AddWithValue("@name", item.GraficName ?? "");
                                updateCmd.Parameters.AddWithValue("@name_eng", item.GraficNameEng ?? "");
                                updateCmd.Parameters.AddWithValue("@name_ger", item.GraficNameGer ?? "");
                                updateCmd.Parameters.AddWithValue("@description_ru", item.GraficDescriptionRu ?? "");
                                updateCmd.Parameters.AddWithValue("@description_eng", item.GraficDescriptionEng ?? "");
                                updateCmd.Parameters.AddWithValue("@description_ger", item.GraficDescriptionGer ?? "");
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    else
                    {
                        itemId = CreateGlobalId("grafic");
                        string fileName = null;

                        if (item.GraficImageFile != null && item.GraficImageFile.Length > 0)
                        {
                            var graficsPath = Path.Combine(_sharedUploadsPath, "grafics");
                            if (!Directory.Exists(graficsPath)) Directory.CreateDirectory(graficsPath);
                            var ext = Path.GetExtension(item.GraficImageFile.FileName).ToLower();
                            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

                            if (allowed.Contains(ext) && item.GraficImageFile.Length <= 5 * 1024 * 1024)
                            {
                                fileName = $"{Guid.NewGuid()}{ext}";
                                var filePath = Path.Combine(graficsPath, fileName);
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await item.GraficImageFile.CopyToAsync(stream);
                                }
                            }
                        }

                        string insertQuery = @"INSERT INTO grafic (id, name, name_eng, name_ger, image,
                                description_ru, description_eng, description_ger) 
                            VALUES (@id, @name, @name_eng, @name_ger, @image,
                                @description_ru, @description_eng, @description_ger)";
                        using (var insertCmd = new MySqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@id", itemId);
                            insertCmd.Parameters.AddWithValue("@name", item.GraficName ?? "");
                            insertCmd.Parameters.AddWithValue("@name_eng", item.GraficNameEng ?? "");
                            insertCmd.Parameters.AddWithValue("@name_ger", item.GraficNameGer ?? "");
                            insertCmd.Parameters.AddWithValue("@image", fileName ?? "");
                            insertCmd.Parameters.AddWithValue("@description_ru", item.GraficDescriptionRu ?? "");
                            insertCmd.Parameters.AddWithValue("@description_eng", item.GraficDescriptionEng ?? "");
                            insertCmd.Parameters.AddWithValue("@description_ger", item.GraficDescriptionGer ?? "");
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    itemIds.Add(itemId);
                }

                string ids = string.Join(",", itemIds);
                string templateQuery = "INSERT INTO template_grafic (id, name, ids) VALUES (@id, @name, @ids)";
                using (var cmd = new MySqlCommand(templateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", templateId);
                    cmd.Parameters.AddWithValue("@name", templateName ?? "");
                    cmd.Parameters.AddWithValue("@ids", ids);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["Message"] = $"РЁР°Р±Р»РѕРЅ '{templateName}' СѓСЃРїРµС€РЅРѕ СЃРѕР·РґР°РЅ СЃ {itemIds.Count} Р·Р°РїРёСЃСЏРјРё";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "grafic" });
        }

        [HttpPost("CreateAdditionalPriceTemplateWithItems")]
        public async Task<IActionResult> CreateAdditionalPriceTemplateWithItems(string templateName, List<AdditionalPriceItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Р—Р°РїРѕР»РЅРёС‚Рµ РЅР°Р·РІР°РЅРёРµ С€Р°Р±Р»РѕРЅР° Рё РґРѕР±Р°РІСЊС‚Рµ С…РѕС‚СЏ Р±С‹ РѕРґРЅСѓ Р·Р°РїРёСЃСЊ";
                return RedirectToAction("Templates", new { activeTab = "additional" });
            }

            int templateId = CreateGlobalId("template_additional_prices");
            string connectionString = GetConnectionString();
            List<int> itemIds = new List<int>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var item in items)
                {
                    int itemId;

                    // ============================================================
                    // Р’РђР РРђРќРў 1: РРЎРџРћР›Р¬Р—РЈР•Рњ РЎРЈР©Р•РЎРўР’РЈР®Р©РЈР® Р—РђРџРРЎР¬ РџРћ ID
                    // ============================================================
                    if (item.ExistingId.HasValue && item.ExistingId.Value > 0)
                    {
                        // РџСЂРѕРІРµСЂСЏРµРј, СЃСѓС‰РµСЃС‚РІСѓРµС‚ Р»Рё Р·Р°РїРёСЃСЊ
                        string checkQuery = "SELECT COUNT(*) FROM additional_prices WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.ExistingId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"Р—Р°РїРёСЃСЊ СЃ ID {item.ExistingId.Value} РЅРµ РЅР°Р№РґРµРЅР° РІ С‚Р°Р±Р»РёС†Рµ additional_prices";
                                return RedirectToAction("Templates", new { activeTab = "additional" });
                            }
                        }

                        itemId = item.ExistingId.Value;

                        // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё РґР°РЅРЅС‹Рµ РґР»СЏ РѕР±РЅРѕРІР»РµРЅРёСЏ
                        bool hasData = !string.IsNullOrEmpty(item.NameRu) || !string.IsNullOrEmpty(item.NameEng) ||
                                       !string.IsNullOrEmpty(item.NameGer) || !string.IsNullOrEmpty(item.PriceRubl) ||
                                       !string.IsNullOrEmpty(item.PriceDolar) || !string.IsNullOrEmpty(item.PriceEuro) ||
                                       !string.IsNullOrEmpty(item.InfoRu) || !string.IsNullOrEmpty(item.InfoEng) ||
                                       !string.IsNullOrEmpty(item.InfoGer) || item.PriceControler != 0 ||
                                       !string.IsNullOrEmpty(item.FreePriceIds) || !string.IsNullOrEmpty(item.BasePriceIds) ||
                                       !string.IsNullOrEmpty(item.ProPriceIds);

                        if (hasData)
                        {
                            // ===== РћР§РР©РђР•Рњ Р”РЈР‘Р›РРљРђРўР« =====
                            string freeIds = CleanDuplicateIds(item.FreePriceIds ?? "");
                            string baseIds = CleanDuplicateIds(item.BasePriceIds ?? "");
                            string proIds = CleanDuplicateIds(item.ProPriceIds ?? "");

                            string updateQuery = @"UPDATE additional_prices SET 
                        name_ru = @name_ru,
                        name_eng = @name_eng,
                        name_ger = @name_ger,
                        price_rubl = @price_rubl,
                        price_dolar = @price_dolar,
                        price_euro = @price_euro,
                        info_ru = @info_ru,
                        info_eng = @info_eng,
                        info_ger = @info_ger,
                        price_controler = @price_controler,
                        unselected_price_mode = @unselected_price_mode,
                        free_price_ids = @free_price_ids,
                        base_price_ids = @base_price_ids,
                        pro_price_ids = @pro_price_ids
                        WHERE id = @id";

                            using (var updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@id", itemId);
                                updateCmd.Parameters.AddWithValue("@name_ru", item.NameRu ?? "");
                                updateCmd.Parameters.AddWithValue("@name_eng", item.NameEng ?? "");
                                updateCmd.Parameters.AddWithValue("@name_ger", item.NameGer ?? "");
                                updateCmd.Parameters.AddWithValue("@price_rubl", item.PriceRubl ?? "");
                                updateCmd.Parameters.AddWithValue("@price_dolar", item.PriceDolar ?? "");
                                updateCmd.Parameters.AddWithValue("@price_euro", item.PriceEuro ?? "");
                                updateCmd.Parameters.AddWithValue("@info_ru", item.InfoRu ?? "");
                                updateCmd.Parameters.AddWithValue("@info_eng", item.InfoEng ?? "");
                                updateCmd.Parameters.AddWithValue("@info_ger", item.InfoGer ?? "");
                                updateCmd.Parameters.AddWithValue("@price_controler", item.PriceControler);
                                updateCmd.Parameters.AddWithValue("@unselected_price_mode", item.UnselectedPriceMode);
                                updateCmd.Parameters.AddWithValue("@free_price_ids", freeIds);
                                updateCmd.Parameters.AddWithValue("@base_price_ids", baseIds);
                                updateCmd.Parameters.AddWithValue("@pro_price_ids", proIds);
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    // ============================================================
                    // Р’РђР РРђРќРў 2: Р”РћР‘РђР’Р›РЇР•Рњ Р’Р›РћР–Р•РќРќР«Р™ РЁРђР‘Р›РћРќ
                    // ============================================================
                    else if (item.TemplateId.HasValue && item.TemplateId.Value > 0)
                    {
                        // РџСЂРѕРІРµСЂСЏРµРј, СЃСѓС‰РµСЃС‚РІСѓРµС‚ Р»Рё С€Р°Р±Р»РѕРЅ
                        string checkQuery = "SELECT COUNT(*) FROM template_additional_prices WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.TemplateId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"РЁР°Р±Р»РѕРЅ СЃ ID {item.TemplateId.Value} РЅРµ РЅР°Р№РґРµРЅ";
                                return RedirectToAction("Templates", new { activeTab = "additional" });
                            }
                        }

                        // РџСЂРѕСЃС‚Рѕ РёСЃРїРѕР»СЊР·СѓРµРј ID СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРіРѕ С€Р°Р±Р»РѕРЅР°
                        itemId = item.TemplateId.Value;
                    }
                    // ============================================================
                    // Р’РђР РРђРќРў 3: РЎРћР—Р”РђРЃРњ РќРћР’РЈР® Р—РђРџРРЎР¬
                    // ============================================================
                    else
                    {
                        itemId = CreateGlobalId("additional_prices");

                        // ===== РћР§РР©РђР•Рњ Р”РЈР‘Р›РРљРђРўР« =====
                        string freeIds = CleanDuplicateIds(item.FreePriceIds ?? "");
                        string baseIds = CleanDuplicateIds(item.BasePriceIds ?? "");
                        string proIds = CleanDuplicateIds(item.ProPriceIds ?? "");

                        string insertQuery = @"INSERT INTO additional_prices 
                    (id, name_ru, name_eng, name_ger,
                     price_rubl, price_dolar, price_euro,
                     info_ru, info_eng, info_ger,
                     sort_order, price_controler,
                     unselected_price_mode,
                     free_price_ids, base_price_ids, pro_price_ids) 
                VALUES 
                    (@id, @name_ru, @name_eng, @name_ger,
                     @price_rubl, @price_dolar, @price_euro,
                     @info_ru, @info_eng, @info_ger,
                     0, @price_controler,
                    @unselected_price_mode,
                     @free_price_ids, @base_price_ids, @pro_price_ids)";

                        using (var insertCmd = new MySqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@id", itemId);
                            insertCmd.Parameters.AddWithValue("@name_ru", item.NameRu ?? "");
                            insertCmd.Parameters.AddWithValue("@name_eng", item.NameEng ?? "");
                            insertCmd.Parameters.AddWithValue("@name_ger", item.NameGer ?? "");
                            insertCmd.Parameters.AddWithValue("@price_rubl", item.PriceRubl ?? "");
                            insertCmd.Parameters.AddWithValue("@price_dolar", item.PriceDolar ?? "");
                            insertCmd.Parameters.AddWithValue("@price_euro", item.PriceEuro ?? "");
                            insertCmd.Parameters.AddWithValue("@info_ru", item.InfoRu ?? "");
                            insertCmd.Parameters.AddWithValue("@info_eng", item.InfoEng ?? "");
                            insertCmd.Parameters.AddWithValue("@info_ger", item.InfoGer ?? "");
                            insertCmd.Parameters.AddWithValue("@price_controler", item.PriceControler);
                            insertCmd.Parameters.AddWithValue("@unselected_price_mode", item.UnselectedPriceMode);
                            insertCmd.Parameters.AddWithValue("@free_price_ids", freeIds);
                            insertCmd.Parameters.AddWithValue("@base_price_ids", baseIds);
                            insertCmd.Parameters.AddWithValue("@pro_price_ids", proIds);
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Р”РѕР±Р°РІР»СЏРµРј ID РІ СЃРїРёСЃРѕРє
                    if (!itemIds.Contains(itemId))
                    {
                        itemIds.Add(itemId);
                    }
                }

                // РЎРѕС…СЂР°РЅСЏРµРј С€Р°Р±Р»РѕРЅ
                string ids = string.Join(",", itemIds);
                string templateQuery = "INSERT INTO template_additional_prices (id, name, price_ids) VALUES (@id, @name, @price_ids)";
                using (var cmd = new MySqlCommand(templateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", templateId);
                    cmd.Parameters.AddWithValue("@name", templateName ?? "");
                    cmd.Parameters.AddWithValue("@price_ids", ids);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["Message"] = $"РЁР°Р±Р»РѕРЅ '{templateName}' СѓСЃРїРµС€РЅРѕ СЃРѕР·РґР°РЅ СЃ {itemIds.Count} Р·Р°РїРёСЃСЏРјРё";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "additional" });
        }

        private string CleanDuplicateIds(string ids)
        {
            if (string.IsNullOrEmpty(ids)) return "";
            return string.Join(",", ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct());
        }

        // ==========================================
        // РЈРџР РђР’Р›Р•РќРР• РЁРђР‘Р›РћРќРђРњР
        // ==========================================

        [HttpPost("UpdateTemplateName")]
        public async Task<IActionResult> UpdateTemplateName(int id, string type, string name, string returnTab = null)
        {
            string table = GetTemplateTable(type);
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = $"UPDATE {table} SET name = @name WHERE id = @id";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@name", name ?? "");
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["Message"] = "РќР°Р·РІР°РЅРёРµ С€Р°Р±Р»РѕРЅР° РѕР±РЅРѕРІР»РµРЅРѕ";
            await NotifyReaderSite();
            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }

        [HttpPost("DeleteTemplate")]
        public async Task<IActionResult> DeleteTemplate(int id, string type, string returnTab = null)
        {
            string table = GetTemplateTable(type);
            string connectionString = GetConnectionString();

            string idsString = null;
            string idsColumn = type == "price" ? "prices" : (type == "additional" ? "price_ids" : "ids");

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = $"SELECT {idsColumn} FROM {table} WHERE id = @id";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    var result = await cmd.ExecuteScalarAsync();
                    idsString = result?.ToString();
                }

                string deleteQuery = $"DELETE FROM {table} WHERE id = @id";
                using (var cmd = new MySqlCommand(deleteQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (!string.IsNullOrEmpty(idsString))
                {
                    var itemIds = idsString.Split(',').Select(int.Parse).ToList();
                    string recordTable = GetRecordTable(type);
                    foreach (var itemId in itemIds)
                    {
                        string deleteItemQuery = $"DELETE FROM {recordTable} WHERE id = @id";
                        using (var cmd = new MySqlCommand(deleteItemQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@id", itemId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        DeleteGlobalId(itemId);
                    }
                }

                DeleteGlobalId(id);
            }

            TempData["Message"] = "РЁР°Р±Р»РѕРЅ СѓРґР°Р»РµРЅ";
            await NotifyReaderSite();
            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }

        [HttpPost("RemoveItemFromTemplate")]
        public async Task<IActionResult> RemoveItemFromTemplate(int templateId, int itemId, string type, string returnTab = null)
        {
            string templateTable = GetTemplateTable(type);
            string recordTable = GetRecordTable(type);
            string idsColumn = type == "price" ? "prices" : (type == "additional" ? "price_ids" : "ids");

            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string getQuery = $"SELECT {idsColumn} FROM {templateTable} WHERE id = @id";
                string currentIds = null;
                using (var getCmd = new MySqlCommand(getQuery, connection))
                {
                    getCmd.Parameters.AddWithValue("@id", templateId);
                    var result = await getCmd.ExecuteScalarAsync();
                    currentIds = result?.ToString();
                }

                if (!string.IsNullOrEmpty(currentIds))
                {
                    var ids = currentIds.Split(',').Select(int.Parse).ToList();
                    ids.Remove(itemId);
                    string newIds = string.Join(",", ids);

                    string updateQuery = $"UPDATE {templateTable} SET {idsColumn} = @ids WHERE id = @id";
                    using (var updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@ids", newIds);
                        updateCmd.Parameters.AddWithValue("@id", templateId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }

                string deleteQuery = $"DELETE FROM {recordTable} WHERE id = @id";
                using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                {
                    deleteCmd.Parameters.AddWithValue("@id", itemId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                DeleteGlobalId(itemId);
            }

            TempData["Message"] = "Р—Р°РїРёСЃСЊ СѓРґР°Р»РµРЅР° РёР· С€Р°Р±Р»РѕРЅР°";
            await NotifyReaderSite();
            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }

        [HttpPost("UpdateTemplateItem")]
        public async Task<IActionResult> UpdateTemplateItem(
            int templateId,
            int itemId,
            string type,
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
            int priceControler = 0,                    // <-- Р”РћР‘РђР’РРўР¬
            int unselectedPriceMode = 0,
            string unselectedPriceModeHidden = "",
            string[] freePriceIds = null,      // в†ђ РР—РњР•РќР•РќРћ РќРђ РњРђРЎРЎРР’
            string[] basePriceIds = null,      // в†ђ РР—РњР•РќР•РќРћ РќРђ РњРђРЎРЎРР’
            string[] proPriceIds = null)
        {
            string recordTable = GetRecordTable(type);
            string connectionString = GetConnectionString();

            string query = "";
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                switch (type)
                {
                    case "about":
                    case "result":
                    case "engine":
                        query = $"UPDATE {recordTable} SET text_ru = @ru, text_eng = @eng, text_ger = @ger WHERE id = @id";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@id", itemId);
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
                            cmd.Parameters.AddWithValue("@id", itemId);
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

                    case "additional":
                        int finalUnselectedMode = !string.IsNullOrEmpty(unselectedPriceModeHidden)
        ? int.Parse(unselectedPriceModeHidden)
        : unselectedPriceMode;
                        // ===== Р•РЎР›Р PRICE CONTROLER РќР• 2 - РћР§РР©РђР•Рњ ID =====
                        string freeIds;
                        string baseIds;
                        string proIds;

                        if (priceControler != 2)
                        {
                            // РЎР±СЂР°СЃС‹РІР°РµРј РІСЃРµ ID, РµСЃР»Рё priceControler РЅРµ 2
                            freeIds = "";
                            baseIds = "";
                            proIds = "";
                        }
                        else
                        {
                            // РРЅР°С‡Рµ РёСЃРїРѕР»СЊР·СѓРµРј РїРµСЂРµРґР°РЅРЅС‹Рµ Р·РЅР°С‡РµРЅРёСЏ
                            freeIds = freePriceIds != null && freePriceIds.Length > 0
                                ? string.Join(",", freePriceIds)
                                : "";
                            baseIds = basePriceIds != null && basePriceIds.Length > 0
                                ? string.Join(",", basePriceIds)
                                : "";
                            proIds = proPriceIds != null && proPriceIds.Length > 0
                                ? string.Join(",", proPriceIds)
                                : "";
                        }

                        query = $@"UPDATE {recordTable} SET 
        name_ru = @name_ru,
        name_eng = @name_eng,
        name_ger = @name_ger,
        price_rubl = @price_rubl,
        price_dolar = @price_dolar,
        price_euro = @price_euro,
        info_ru = @info_ru,
        info_eng = @info_eng,
        info_ger = @info_ger,
        price_controler = @price_controler,
        unselected_price_mode = @unselected_price_mode,
        free_price_ids = @free_price_ids,
        base_price_ids = @base_price_ids,
        pro_price_ids = @pro_price_ids
        WHERE id = @id";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@id", itemId);
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
                            cmd.Parameters.AddWithValue("@unselected_price_mode", finalUnselectedMode);
                            cmd.Parameters.AddWithValue("@free_price_ids", freeIds ?? "");
                            cmd.Parameters.AddWithValue("@base_price_ids", baseIds ?? "");
                            cmd.Parameters.AddWithValue("@pro_price_ids", proIds ?? "");
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
                            cmd.Parameters.AddWithValue("@id", itemId);
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
                }
            }

            TempData["Message"] = "Р—Р°РїРёСЃСЊ РѕР±РЅРѕРІР»РµРЅР°";
            await NotifyReaderSite();

            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }

        [HttpPost("TemplateReorderItems")]
        public async Task<IActionResult> TemplateReorderItems(int templateId, string type, string orderedIds)
        {
            string templateTable = GetTemplateTable(type);
            string idsColumn = type == "price" ? "prices" : (type == "additional" ? "price_ids" : "ids");

            string connectionString = GetConnectionString();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // РџРѕР»СѓС‡Р°РµРј С‚РµРєСѓС‰РёРµ ID
                string getQuery = $"SELECT {idsColumn} FROM {templateTable} WHERE id = @id";
                string currentIds = "";
                using (var getCmd = new MySqlCommand(getQuery, connection))
                {
                    getCmd.Parameters.AddWithValue("@id", templateId);
                    var result = await getCmd.ExecuteScalarAsync();
                    currentIds = result?.ToString() ?? "";
                }

                var newValue = ReorderIdsString(currentIds, ParseIdList(orderedIds));

                string updateQuery = $"UPDATE {templateTable} SET {idsColumn} = @ids WHERE id = @id";
                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@ids", newValue);
                    updateCmd.Parameters.AddWithValue("@id", templateId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }

            await NotifyReaderSite();
            return Json(new { success = true });
        }

        // ==========================================
        // Р”РћР‘РђР’Р›Р•РќРР• Р—РђРџРРЎР Р’ РЁРђР‘Р›РћРќ (РџРћ ID РР›Р РќРћР’РђРЇ)
        // ==========================================

        // HomeController.EditCarBlocks.cs вЂ” РїРѕР»РЅС‹Р№ РѕР±РЅРѕРІР»С‘РЅРЅС‹Р№ РјРµС‚РѕРґ

        [HttpPost("AddItemToTemplate")]
        public async Task<IActionResult> AddItemToTemplate(
            int templateId,
            string type,
            int? existingItemId = null,
            int? existingTemplateId = null,
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
            string graficName = "",
            string graficNameEng = "",
            string graficNameGer = "",
            string graficDescriptionRu = "",
            string graficDescriptionEng = "",
            string graficDescriptionGer = "",
            string priceRubl = "",
            string priceDolar = "",
            string priceEuro = "",
            int priceControler = 0,
            int unselectedPriceMode = 0,              // <-- Р”РћР‘РђР’РРўР¬
            string[] freePriceIds = null,             // <-- Р”РћР‘РђР’РРўР¬
            string[] basePriceIds = null,             // <-- Р”РћР‘РђР’РРўР¬
            string[] proPriceIds = null,
            string returnTab = null)
        {
            string connectionString = GetConnectionString();
            int newItemId;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // ============================================================
                // Р’РђР РРђРќРў 1: Р”РћР‘РђР’Р›Р•РќРР• РџРћ ID РЎРЈР©Р•РЎРўР’РЈР®Р©Р•Р™ Р—РђРџРРЎР (РљРћРџРР РћР’РђРќРР•)
                // ============================================================
                if (existingItemId.HasValue && existingItemId.Value > 0)
                {
                    string recordTable = GetRecordTable(type);

                    // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ Р·Р°РїРёСЃСЊ СЃСѓС‰РµСЃС‚РІСѓРµС‚
                    string checkQuery = $"SELECT COUNT(*) FROM {recordTable} WHERE id = @id";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@id", existingItemId.Value);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            TempData["Error"] = $"Р—Р°РїРёСЃСЊ СЃ ID {existingItemId.Value} РЅРµ РЅР°Р№РґРµРЅР° РІ С‚Р°Р±Р»РёС†Рµ {recordTable}";
                            return RedirectToAction("Templates");
                        }
                    }

                    // РљРѕРїРёСЂСѓРµРј Р·Р°РїРёСЃСЊ вЂ” СЃРѕР·РґР°С‘Рј РєРѕРїРёСЋ СЃ РЅРѕРІС‹Рј ID
                    newItemId = CopyRecord(existingItemId.Value, recordTable);
                }

                // ============================================================
                // Р’РђР РРђРќРў 2: Р”РћР‘РђР’Р›Р•РќРР• Р’Р›РћР–Р•РќРќРћР“Рћ РЁРђР‘Р›РћРќРђ
                // ============================================================
                else if (existingTemplateId.HasValue && existingTemplateId.Value > 0)
                {
                    string templateTable = GetTemplateTable(type);

                    // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ С€Р°Р±Р»РѕРЅ СЃСѓС‰РµСЃС‚РІСѓРµС‚
                    string checkQuery = $"SELECT COUNT(*) FROM {templateTable} WHERE id = @id";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@id", existingTemplateId.Value);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            TempData["Error"] = $"РЁР°Р±Р»РѕРЅ СЃ ID {existingTemplateId.Value} РЅРµ РЅР°Р№РґРµРЅ";
                            return RedirectToAction("Templates");
                        }
                    }

                    // РџСЂРѕСЃС‚Рѕ РёСЃРїРѕР»СЊР·СѓРµРј ID СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРіРѕ С€Р°Р±Р»РѕРЅР° (РѕРЅ СѓР¶Рµ РµСЃС‚СЊ РІ global_ids)
                    newItemId = existingTemplateId.Value;
                }

                // ============================================================
                // Р’РђР РРђРќРў 3: РЎРћР—Р”РђРќРР• РќРћР’РћР™ Р—РђРџРРЎР
                // ============================================================
                else
                {
                    string recordTable = GetRecordTable(type);
                    newItemId = CreateGlobalId(recordTable);

                    string insertQuery = "";

                    switch (type)
                    {
                        case "about":
                        case "result":
                        case "engine":
                            insertQuery = $@"INSERT INTO {recordTable} 
                        (id, text_ru, text_eng, text_ger) 
                        VALUES (@id, @text_ru, @text_eng, @text_ger)";
                            break;

                        case "price":
                            insertQuery = $@"INSERT INTO {recordTable} 
                        (id, name_ru, name_eng, name_ger, 
                         base_price, pro_price, 
                         base_price_eng, pro_price_eng, 
                         base_price_ger, pro_price_ger,
                         info_ru, info_eng, info_ger) 
                        VALUES (@id, @name_ru, @name_eng, @name_ger, 
                                @base_price, @pro_price, 
                                @base_price_eng, @pro_price_eng, 
                                @base_price_ger, @pro_price_ger,
                                @info_ru, @info_eng, @info_ger)";
                            break;

                        case "grafic":
                            insertQuery = $@"INSERT INTO {recordTable} 
                        (id, name, name_eng, name_ger, 
                         description_ru, description_eng, description_ger) 
                        VALUES (@id, @name, @name_eng, @name_ger, 
                                @description_ru, @description_eng, @description_ger)";
                            break;

                        case "additional":
                            insertQuery = $@"INSERT INTO {recordTable} 
        (id, name_ru, name_eng, name_ger, 
         price_rubl, price_dolar, price_euro, 
         info_ru, info_eng, info_ger, 
         price_controler, unselected_price_mode,
         free_price_ids, base_price_ids, pro_price_ids) 
        VALUES (@id, @name_ru, @name_eng, @name_ger, 
                @price_rubl, @price_dolar, @price_euro, 
                @info_ru, @info_eng, @info_ger, 
                @price_controler, @unselected_price_mode,
                @free_price_ids, @base_price_ids, @pro_price_ids)";
                            break;


                        default:
                            DeleteGlobalId(newItemId);
                            TempData["Error"] = $"РќРµРёР·РІРµСЃС‚РЅС‹Р№ С‚РёРї: {type}";
                            return RedirectToAction("Templates");
                    }

                    using (var insertCmd = new MySqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@id", newItemId);

                        switch (type)
                        {
                            case "about":
                            case "result":
                            case "engine":
                                insertCmd.Parameters.AddWithValue("@text_ru", textRu ?? "");
                                insertCmd.Parameters.AddWithValue("@text_eng", textEng ?? "");
                                insertCmd.Parameters.AddWithValue("@text_ger", textGer ?? "");
                                break;

                            case "price":
                                insertCmd.Parameters.AddWithValue("@name_ru", nameRu ?? "");
                                insertCmd.Parameters.AddWithValue("@name_eng", nameEng ?? "");
                                insertCmd.Parameters.AddWithValue("@name_ger", nameGer ?? "");
                                insertCmd.Parameters.AddWithValue("@base_price", basePrice ?? "");
                                insertCmd.Parameters.AddWithValue("@pro_price", proPrice ?? "");
                                insertCmd.Parameters.AddWithValue("@base_price_eng", basePriceEng ?? "");
                                insertCmd.Parameters.AddWithValue("@pro_price_eng", proPriceEng ?? "");
                                insertCmd.Parameters.AddWithValue("@base_price_ger", basePriceGer ?? "");
                                insertCmd.Parameters.AddWithValue("@pro_price_ger", proPriceGer ?? "");
                                insertCmd.Parameters.AddWithValue("@info_ru", infoRu ?? "");
                                insertCmd.Parameters.AddWithValue("@info_eng", infoEng ?? "");
                                insertCmd.Parameters.AddWithValue("@info_ger", infoGer ?? "");
                                break;

                            case "grafic":
                                insertCmd.Parameters.AddWithValue("@name", graficName ?? "");
                                insertCmd.Parameters.AddWithValue("@name_eng", graficNameEng ?? "");
                                insertCmd.Parameters.AddWithValue("@name_ger", graficNameGer ?? "");
                                insertCmd.Parameters.AddWithValue("@description_ru", graficDescriptionRu ?? "");
                                insertCmd.Parameters.AddWithValue("@description_eng", graficDescriptionEng ?? "");
                                insertCmd.Parameters.AddWithValue("@description_ger", graficDescriptionGer ?? "");
                                break;

                            case "additional":
                                insertCmd.Parameters.AddWithValue("@name_ru", nameRu ?? "");
                                insertCmd.Parameters.AddWithValue("@name_eng", nameEng ?? "");
                                insertCmd.Parameters.AddWithValue("@name_ger", nameGer ?? "");
                                insertCmd.Parameters.AddWithValue("@price_rubl", priceRubl ?? "");
                                insertCmd.Parameters.AddWithValue("@price_dolar", priceDolar ?? "");
                                insertCmd.Parameters.AddWithValue("@price_euro", priceEuro ?? "");
                                insertCmd.Parameters.AddWithValue("@info_ru", infoRu ?? "");
                                insertCmd.Parameters.AddWithValue("@info_eng", infoEng ?? "");
                                insertCmd.Parameters.AddWithValue("@info_ger", infoGer ?? "");
                                insertCmd.Parameters.AddWithValue("@price_controler", priceControler);
                                insertCmd.Parameters.AddWithValue("@unselected_price_mode", unselectedPriceMode);                          // <-- Р”РћР‘РђР’РРўР¬
                                insertCmd.Parameters.AddWithValue("@free_price_ids", freePriceIds != null ? string.Join(",", freePriceIds) : "");   // <-- Р”РћР‘РђР’РРўР¬
                                insertCmd.Parameters.AddWithValue("@base_price_ids", basePriceIds != null ? string.Join(",", basePriceIds) : "");   // <-- Р”РћР‘РђР’РРўР¬
                                insertCmd.Parameters.AddWithValue("@pro_price_ids", proPriceIds != null ? string.Join(",", proPriceIds) : "");
                                break;
                        }

                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                // ============================================================
                // Р”РћР‘РђР’Р›РЇР•Рњ ID Р’ РЁРђР‘Р›РћРќ
                // ============================================================
                string templateTableName = GetTemplateTable(type);
                string idsColumn = type == "price" ? "prices" : (type == "additional" ? "price_ids" : "ids");

                // РџРѕР»СѓС‡Р°РµРј С‚РµРєСѓС‰РёР№ СЃРїРёСЃРѕРє ID РІ С€Р°Р±Р»РѕРЅРµ
                string getQuery = $"SELECT {idsColumn} FROM {templateTableName} WHERE id = @id";
                string currentIds = "";
                using (var getCmd = new MySqlCommand(getQuery, connection))
                {
                    getCmd.Parameters.AddWithValue("@id", templateId);
                    var result = await getCmd.ExecuteScalarAsync();
                    currentIds = result?.ToString() ?? "";
                }

                // Р”РѕР±Р°РІР»СЏРµРј РЅРѕРІС‹Р№ ID, РµСЃР»Рё РµРіРѕ РµС‰С‘ РЅРµС‚
                var idsList = string.IsNullOrEmpty(currentIds)
                    ? new List<int>()
                    : currentIds.Split(',').Select(int.Parse).ToList();

                if (!idsList.Contains(newItemId))
                {
                    idsList.Add(newItemId);
                }

                string newIds = string.Join(",", idsList);

                // РћР±РЅРѕРІР»СЏРµРј С€Р°Р±Р»РѕРЅ
                string updateQuery = $"UPDATE {templateTableName} SET {idsColumn} = @ids WHERE id = @id";
                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@ids", newIds);
                    updateCmd.Parameters.AddWithValue("@id", templateId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }

            // РћРїСЂРµРґРµР»СЏРµРј СЃРѕРѕР±С‰РµРЅРёРµ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ СЃРїРѕСЃРѕР±Р° РґРѕР±Р°РІР»РµРЅРёСЏ
            string message;
            if (existingItemId.HasValue && existingItemId.Value > 0)
            {
                message = $"Р—Р°РїРёСЃСЊ #{existingItemId.Value} СЃРєРѕРїРёСЂРѕРІР°РЅР° РІ С€Р°Р±Р»РѕРЅ (РЅРѕРІР°СЏ ID: {newItemId})";
            }
            else if (existingTemplateId.HasValue && existingTemplateId.Value > 0)
            {
                message = $"Р’Р»РѕР¶РµРЅРЅС‹Р№ С€Р°Р±Р»РѕРЅ #{existingTemplateId.Value} РґРѕР±Р°РІР»РµРЅ РІ С€Р°Р±Р»РѕРЅ";
            }
            else
            {
                message = "РќРѕРІР°СЏ Р·Р°РїРёСЃСЊ СЃРѕР·РґР°РЅР° Рё РґРѕР±Р°РІР»РµРЅР° РІ С€Р°Р±Р»РѕРЅ";
            }

            TempData["Message"] = message;
            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            TempData["Message"] = message;
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }

        // ==========================================
        // Р”РћР‘РђР’Р›Р•РќРР• РЁРђР‘Р›РћРќРђ Р’Рћ Р’РЎР• РњРђРЁРРќР«
        // ==========================================

        [HttpPost("AddTemplateToAllCars")]
        public async Task<IActionResult> AddTemplateToAllCars(int templateId, string type, string returnTab = null)
        {
            try
            {
                string connectionString = GetConnectionString();
                var carIds = new List<int>();

                // РџРѕР»СѓС‡Р°РµРј РІСЃРµ ID РјР°С€РёРЅ
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT id FROM reflash_cars ORDER BY id";
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            carIds.Add(reader.GetInt32("id"));
                        }
                    }
                }

                if (carIds.Count == 0)
                {
                    TempData["Error"] = "РќРµС‚ РјР°С€РёРЅ РІ Р±Р°Р·Рµ РґР°РЅРЅС‹С…";
                    return RedirectToAction("Templates", new { activeTab = type });
                }

                int addedCount = 0;
                string columnName = type switch
                {
                    "about" => "about_ru",
                    "result" => "result_ru",
                    "engine" => "engine_control_ru",
                    "price" => "price_ru",
                    "grafic" => "grafic",
                    "additional" => "additional_price_ru",
                    _ => throw new ArgumentException($"РќРµРёР·РІРµСЃС‚РЅС‹Р№ С‚РёРї: {type}")
                };

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    foreach (var carId in carIds)
                    {
                        // РџРѕР»СѓС‡Р°РµРј С‚РµРєСѓС‰РµРµ Р·РЅР°С‡РµРЅРёРµ РїРѕР»СЏ
                        string getQuery = $"SELECT {columnName} FROM reflash_cars WHERE id = @id";
                        string currentValue = "";
                        using (var getCmd = new MySqlCommand(getQuery, connection))
                        {
                            getCmd.Parameters.AddWithValue("@id", carId);
                            var result = await getCmd.ExecuteScalarAsync();
                            currentValue = result?.ToString() ?? "";
                        }

                        // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё СѓР¶Рµ СЌС‚РѕС‚ С€Р°Р±Р»РѕРЅ РІ СЃРїРёСЃРєРµ
                        var idsList = string.IsNullOrEmpty(currentValue)
                            ? new List<int>()
                            : currentValue.Split(',').Select(int.Parse).ToList();

                        if (!idsList.Contains(templateId))
                        {
                            idsList.Add(templateId);
                            string newValue = string.Join(",", idsList);

                            // РћР±РЅРѕРІР»СЏРµРј Р·Р°РїРёСЃСЊ
                            string updateQuery = $"UPDATE reflash_cars SET {columnName} = @value WHERE id = @id";
                            using (var updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@value", newValue);
                                updateCmd.Parameters.AddWithValue("@id", carId);
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                            addedCount++;
                        }
                    }

                    // РћР±РЅРѕРІР»СЏРµРј used_in_cars РІ С€Р°Р±Р»РѕРЅРµ
                    await UpdateUsedInCarsForAllCars(templateId, type, carIds);
                }

                await NotifyReaderSite();
                TempData["Message"] = $"РЁР°Р±Р»РѕРЅ РґРѕР±Р°РІР»РµРЅ РІ {addedCount} РјР°С€РёРЅ РёР· {carIds.Count}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° РїСЂРё РґРѕР±Р°РІР»РµРЅРёРё С€Р°Р±Р»РѕРЅР° РІРѕ РІСЃРµ РјР°С€РёРЅС‹: {ex.Message}";
            }

            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }

        // ==========================================
        // РћР‘РќРћР’Р›Р•РќРР• USED_IN_CARS Р”Р›РЇ Р’РЎР•РҐ РњРђРЁРРќ
        // ==========================================

        private async Task UpdateUsedInCarsForAllCars(int templateId, string type, List<int> carIds)
        {
            string templateTable = type switch
            {
                "about" => "template_about",
                "result" => "template_result",
                "engine" => "template_engine_control",
                "price" => "template_price",
                "grafic" => "template_grafic",
                "additional" => "template_additional_prices",
                _ => throw new ArgumentException($"РќРµРёР·РІРµСЃС‚РЅС‹Р№ С‚РёРї: {type}")
            };

            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // РџРѕР»СѓС‡Р°РµРј С‚РµРєСѓС‰РµРµ Р·РЅР°С‡РµРЅРёРµ used_in_cars
                string getQuery = $"SELECT used_in_cars FROM {templateTable} WHERE id = @id";
                string currentUsed = "";
                using (var getCmd = new MySqlCommand(getQuery, connection))
                {
                    getCmd.Parameters.AddWithValue("@id", templateId);
                    var result = await getCmd.ExecuteScalarAsync();
                    currentUsed = result?.ToString() ?? "";
                }

                var existingCarIds = string.IsNullOrEmpty(currentUsed)
                    ? new List<int>()
                    : currentUsed.Split(',').Select(int.Parse).ToList();

                // Р”РѕР±Р°РІР»СЏРµРј РІСЃРµ ID РјР°С€РёРЅ, РєРѕС‚РѕСЂС‹С… РµС‰Рµ РЅРµС‚ РІ СЃРїРёСЃРєРµ
                foreach (var carId in carIds)
                {
                    if (!existingCarIds.Contains(carId))
                    {
                        existingCarIds.Add(carId);
                    }
                }

                string newUsed = existingCarIds.Any() ? string.Join(",", existingCarIds) : "";

                // РЎРѕС…СЂР°РЅСЏРµРј РѕР±РЅРѕРІР»РµРЅРЅРѕРµ Р·РЅР°С‡РµРЅРёРµ
                string updateQuery = $"UPDATE {templateTable} SET used_in_cars = @used_in_cars WHERE id = @id";
                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@used_in_cars", newUsed);
                    updateCmd.Parameters.AddWithValue("@id", templateId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }
        }
        // HomeController.EditCarBlocks.cs вЂ” РґРѕР±Р°РІРёС‚СЊ РјРµС‚РѕРґ

        /// <summary>
        /// РљРѕРїРёСЂСѓРµС‚ Р·Р°РїРёСЃСЊ РїРѕ ID Рё РІРѕР·РІСЂР°С‰Р°РµС‚ ID РЅРѕРІРѕР№ РєРѕРїРёРё
        /// </summary>
        private int CopyRecord(int sourceId, string recordTable)
        {
            string connectionString = GetConnectionString();
            int newId = 0;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // 1. РџРѕР»СѓС‡Р°РµРј РґР°РЅРЅС‹Рµ РёСЃС…РѕРґРЅРѕР№ Р·Р°РїРёСЃРё
                string selectQuery = $"SELECT * FROM `{recordTable}` WHERE id = @id";
                List<string> columnNames = null;
                Dictionary<string, object> values = null;

                using (var cmd = new MySqlCommand(selectQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", sourceId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new Exception($"Р—Р°РїРёСЃСЊ СЃ ID {sourceId} РЅРµ РЅР°Р№РґРµРЅР° РІ С‚Р°Р±Р»РёС†Рµ {recordTable}");

                        columnNames = Enumerable.Range(0, reader.FieldCount)
                            .Select(i => reader.GetName(i))
                            .Where(name => !name.Equals("id", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        values = new Dictionary<string, object>();
                        foreach (var col in columnNames)
                        {
                            values[col] = reader[col];
                        }
                    }
                }

                // 2. РЎРћР—Р”РђРЃРњ РќРћР’Р«Р™ ID Р РџР РћР’Р•Р РЇР•Рњ, Р§РўРћ РћРќ РЎР’РћР‘РћР”Р•Рќ
                bool created = false;
                int attempts = 0;

                while (!created && attempts < 10)
                {
                    attempts++;
                    newId = CreateGlobalId(recordTable);

                    // РџСЂРѕРІРµСЂСЏРµРј, С‡С‚Рѕ ID РЅРµ Р·Р°РЅСЏС‚ РІ С‚Р°Р±Р»РёС†Рµ
                    string checkFree = $"SELECT COUNT(*) FROM `{recordTable}` WHERE id = @id";
                    using (var checkCmd = new MySqlCommand(checkFree, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@id", newId);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            // ID Р·Р°РЅСЏС‚ вЂ” СѓРґР°Р»СЏРµРј РёР· global_ids Рё РїСЂРѕР±СѓРµРј РЅРѕРІС‹Р№
                            DeleteGlobalId(newId);
                            continue;
                        }
                    }

                    // 3. Р’РЎРўРђР’Р›РЇР•Рњ РљРћРџРР® РЎ РЈРљРђР—РђРќРР•Рњ ID
                    var insertQuery = $"INSERT INTO `{recordTable}` (id, {string.Join(", ", columnNames)}) VALUES (@id, {string.Join(", ", columnNames.Select(c => $"@{c}"))})";

                    using (var insertCmd = new MySqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@id", newId);
                        foreach (var col in columnNames)
                        {
                            var value = values.ContainsKey(col) ? values[col] : DBNull.Value;
                            insertCmd.Parameters.AddWithValue($"@{col}", value ?? DBNull.Value);
                        }
                        insertCmd.ExecuteNonQuery();
                        created = true;
                    }
                }

                if (!created)
                    throw new Exception("РќРµ СѓРґР°Р»РѕСЃСЊ СЃРѕР·РґР°С‚СЊ РєРѕРїРёСЋ Р·Р°РїРёСЃРё");
            }

            return newId;
        }
        // ==========================================
        // РљР›РђРЎРЎ Р”Р›РЇ Р—РђРџР РћРЎРђ РџР•Р Р•РњР•Р©Р•РќРРЇ Р’ РЁРђР‘Р›РћРќР•
        // ==========================================

        // ==========================================
        // РџР•Р Р•РњР•Р©Р•РќРР• Р—РђРџРРЎР Р’ РЁРђР‘Р›РћРќР• (Р’Р’Р•Р РҐ/Р’РќРР—)
        // ==========================================
        [HttpPost("TemplateMoveItem")]
        public async Task<IActionResult> TemplateMoveItem([FromBody] TemplateMoveItemRequest request)
        {
            try
            {
                // Р”РѕР±Р°РІР»СЏРµРј Р»РѕРіРёСЂРѕРІР°РЅРёРµ РґР»СЏ РѕС‚Р»Р°РґРєРё
                System.Diagnostics.Debug.WriteLine($"=== TemplateMoveItem ===");
                System.Diagnostics.Debug.WriteLine($"Type: '{request.Type}'");
                System.Diagnostics.Debug.WriteLine($"TemplateId: {request.TemplateId}");
                System.Diagnostics.Debug.WriteLine($"ItemId: {request.ItemId}");
                System.Diagnostics.Debug.WriteLine($"Direction: '{request.Direction}'");

                string templateTable = GetTemplateTable(request.Type);
                string idsColumn = request.Type switch
                {
                    "price" => "prices",
                    "additional" => "price_ids",
                    _ => "ids"
                };

                System.Diagnostics.Debug.WriteLine($"Table: {templateTable}, Column: {idsColumn}");

                string connectionString = GetConnectionString();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // РџРѕР»СѓС‡Р°РµРј С‚РµРєСѓС‰РёРµ ID
                    string getQuery = $"SELECT {idsColumn} FROM {templateTable} WHERE id = @id";
                    string currentIds = "";
                    using (var getCmd = new MySqlCommand(getQuery, connection))
                    {
                        getCmd.Parameters.AddWithValue("@id", request.TemplateId);
                        var result = await getCmd.ExecuteScalarAsync();
                        currentIds = result?.ToString() ?? "";
                    }

                    System.Diagnostics.Debug.WriteLine($"Current IDs: '{currentIds}'");

                    if (string.IsNullOrEmpty(currentIds))
                        return Json(new { success = false, message = "РќРµС‚ Р·Р°РїРёСЃРµР№ РІ С€Р°Р±Р»РѕРЅРµ" });

                    var ids = ParseIdList(currentIds);
                    var index = ids.IndexOf(request.ItemId);

                    if (index == -1)
                        return Json(new { success = false, message = $"Р—Р°РїРёСЃСЊ {request.ItemId} РЅРµ РЅР°Р№РґРµРЅР° РІ С€Р°Р±Р»РѕРЅРµ" });

                    // РџРµСЂРµРјРµС‰Р°РµРј
                    if (request.Direction == "up" && index > 0)
                    {
                        (ids[index], ids[index - 1]) = (ids[index - 1], ids[index]);
                    }
                    else if (request.Direction == "down" && index < ids.Count - 1)
                    {
                        (ids[index], ids[index + 1]) = (ids[index + 1], ids[index]);
                    }
                    else
                    {
                        return Json(new { success = true, message = "РџРµСЂРµРјРµС‰РµРЅРёРµ РЅРµ С‚СЂРµР±СѓРµС‚СЃСЏ" });
                    }

                    string newValue = string.Join(",", ids);

                    string updateQuery = $"UPDATE {templateTable} SET {idsColumn} = @ids WHERE id = @id";
                    using (var updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@ids", newValue);
                        updateCmd.Parameters.AddWithValue("@id", request.TemplateId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }

                await NotifyReaderSite();
                return Json(new
                {
                    success = true,
                    message = "Р—Р°РїРёСЃСЊ РїРµСЂРµРјРµС‰РµРЅР°",
                    returnTab = request.ReturnTab ?? request.Type  // <<< РР—РњР•РќР•РќРћ
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"вќЊ ERROR: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private List<TemplateItemDetail> GetAllItemsForType(string type)
        {
            return type switch
            {
                "about" => GetAboutsFromDatabase()
                    .Select(a => new TemplateItemDetail
                    {
                        Id = a.Id,
                        DisplayText = a.TextRu ?? a.TextEng ?? a.TextGer ?? "(РїСѓСЃС‚Рѕ)"
                    }).ToList(),
                "result" => GetResultsFromDatabase()
                    .Select(r => new TemplateItemDetail
                    {
                        Id = r.Id,
                        DisplayText = r.TextRu ?? r.TextEng ?? r.TextGer ?? "(РїСѓСЃС‚Рѕ)"
                    }).ToList(),
                "engine" => GetEngineControlsFromDatabase()
                    .Select(e => new TemplateItemDetail
                    {
                        Id = e.Id,
                        DisplayText = e.TextRu ?? e.TextEng ?? e.TextGer ?? "(РїСѓСЃС‚Рѕ)"
                    }).ToList(),
                "price" => GetPricesFromDatabase()
                    .Select(p => new TemplateItemDetail
                    {
                        Id = p.Id,
                        DisplayText = p.NameRu ?? p.NameEng ?? p.NameGer ?? "(Р±РµР· РЅР°Р·РІР°РЅРёСЏ)"
                    }).ToList(),
                "grafic" => GetGraficsFromDatabase()
                    .Select(g => new TemplateItemDetail
                    {
                        Id = g.Id,
                        DisplayText = g.Name ?? "(Р±РµР· РЅР°Р·РІР°РЅРёСЏ)"
                    }).ToList(),
                "additional" => GetAdditionalPricesFromDatabase()
                    .Select(p => new TemplateItemDetail
                    {
                        Id = p.Id,
                        DisplayText = p.NameRu ?? p.NameEng ?? p.NameGer ?? "(Р±РµР· РЅР°Р·РІР°РЅРёСЏ)"
                    }).ToList(),
                _ => new List<TemplateItemDetail>()
            };
        }

        // ==========================================
        // РЈР”РђР›Р•РќРР• Р—РђРџРРЎР РР— Р‘Р” (РЎ РџР РћР’Р•Р РљРћР™)
        // ==========================================

        [HttpPost("DeleteItemFromTemplate")]
        public async Task<IActionResult> DeleteItemFromTemplate(int templateId, int itemId, string type, string returnTab = null)
        {
            try
            {
                string recordTable = GetRecordTable(type);
                string connectionString = GetConnectionString();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // ===== 1. РЈРґР°Р»СЏРµРј Р·Р°РїРёСЃСЊ РёР· С‚Р°Р±Р»РёС†С‹ =====
                    string deleteQuery = $"DELETE FROM {recordTable} WHERE id = @id";
                    using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", itemId);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }

                    // ===== 2. РЈРґР°Р»СЏРµРј ID РёР· С€Р°Р±Р»РѕРЅРѕРІ =====
                    string templateTable = GetTemplateTable(type);
                    string idsColumn = type == "price" ? "prices" : (type == "additional" ? "price_ids" : "ids");

                    // РџРѕР»СѓС‡Р°РµРј РІСЃРµ С€Р°Р±Р»РѕРЅС‹
                    string getAllTemplatesQuery = $"SELECT id, {idsColumn} FROM {templateTable}";
                    var templates = new List<(int Id, string Ids)>();

                    using (var getCmd = new MySqlCommand(getAllTemplatesQuery, connection))
                    {
                        using (var reader = await getCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int tId = reader.GetInt32("id");
                                string ids = reader.IsDBNull(reader.GetOrdinal(idsColumn)) ? "" : reader.GetString(idsColumn);
                                templates.Add((tId, ids));
                            }
                        }
                    }

                    // ===== 3. РћР±РЅРѕРІР»СЏРµРј С€Р°Р±Р»РѕРЅС‹ (СѓР¶Рµ РќР• РІРЅСѓС‚СЂРё DataReader) =====
                    foreach (var template in templates)
                    {
                        if (!string.IsNullOrEmpty(template.Ids))
                        {
                            var list = template.Ids.Split(',').Select(int.Parse).ToList();
                            if (list.Remove(itemId))
                            {
                                string newIds = string.Join(",", list);
                                string updateQuery = $"UPDATE {templateTable} SET {idsColumn} = @ids WHERE id = @id";
                                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@ids", newIds);
                                    updateCmd.Parameters.AddWithValue("@id", template.Id);
                                    await updateCmd.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }

                    // ===== 4. РЈРґР°Р»СЏРµРј РёР· РіР»РѕР±Р°Р»СЊРЅС‹С… ID =====
                    DeleteGlobalId(itemId);
                }

                TempData["Message"] = $"Р—Р°РїРёСЃСЊ #{itemId} СѓРґР°Р»РµРЅР° РёР· Р±Р°Р·С‹ РґР°РЅРЅС‹С…";
                await NotifyReaderSite();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° РїСЂРё СѓРґР°Р»РµРЅРёРё Р·Р°РїРёСЃРё: {ex.Message}";
            }

            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }
    }
}
