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
        // ЦЕНЫ (PRICE)
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
                TempData["Error"] = "Цена не найдена";
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
                TempData["Message"] = "Цена успешно добавлена";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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
                            TempData["Message"] = "Цена успешно обновлена";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Цена не найдена";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "Цена успешно удалена";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("Prices");
        }

        // ============================================
        // ШАБЛОНЫ ЦЕН (TEMPLATE_PRICE)
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
                TempData["Error"] = "Шаблон не найден";
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
                TempData["Message"] = "Шаблон цен успешно добавлен";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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
                TempData["Message"] = "Шаблон цен успешно обновлен";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "Шаблон цен успешно удален";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplatePrices");
        }

        // ============================================
        // ОПИСАНИЯ (ABOUT)
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
                TempData["Error"] = "Описание не найдено";
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
                TempData["Message"] = "Описание успешно добавлено";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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
                TempData["Message"] = "Описание успешно обновлено";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "Описание успешно удалено";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("Abouts");
        }

        // ============================================
        // ШАБЛОНЫ ОПИСАНИЙ (TEMPLATE_ABOUT)
        // ============================================

        [HttpGet("TemplateAbouts")]
        public IActionResult TemplateAbouts()
        {
            List<TemplateAboutModel> templates = GetTemplateAboutsFromDatabase();
            return View(templates);
        }

        [HttpPost("ReorderTemplates")]
        public async Task<IActionResult> ReorderTemplates(string type, List<int> templateIds)
        {
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
                TempData["Message"] = "Шаблон описаний успешно добавлен";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "Шаблон описаний успешно удален";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateAbouts");
        }

        // ============================================
        // РЕЗУЛЬТАТЫ (RESULT)
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
                TempData["Message"] = "Результат успешно добавлен";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "Результат успешно удален";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("Results");
        }

        // ============================================
        // ШАБЛОНЫ РЕЗУЛЬТАТОВ (TEMPLATE_RESULT)
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
                TempData["Message"] = "Шаблон результатов успешно добавлен";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "Шаблон результатов успешно удален";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateResults");
        }

        // ============================================
        // БЛОКИ УПРАВЛЕНИЯ (ENGINE_CONTROL)
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
                TempData["Message"] = "Блок управления успешно добавлен";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "Блок управления успешно удален";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("EngineControls");
        }

        // ============================================
        // ШАБЛОНЫ БЛОКОВ УПРАВЛЕНИЯ (TEMPLATE_ENGINE_CONTROL)
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
                TempData["Message"] = "Шаблон блоков управления успешно добавлен";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "Шаблон блоков управления успешно удален";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateEngineControls");
        }

        // ============================================
        // ГРАФИКИ (GRAFIC)
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
                TempData["Error"] = "График не найден";
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
                    TempData["Error"] = "Разрешены только файлы изображений (JPG, PNG, GIF, WebP)";
                    DeleteGlobalId(globalId);
                    return RedirectToAction("Grafics");
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Файл слишком большой (максимум 5MB)";
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
                TempData["Message"] = "График успешно добавлен";
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
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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
                    TempData["Error"] = "Разрешены только файлы изображений";
                    return RedirectToAction("Grafics");
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Файл слишком большой (максимум 5MB)";
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
                TempData["Message"] = "График успешно обновлен";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "График успешно удален";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("Grafics");
        }

        // ============================================
        // ШАБЛОНЫ ГРАФИКОВ (TEMPLATE_GRAFIC)
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
                TempData["Message"] = "Шаблон графиков успешно добавлен";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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

                TempData["Message"] = "Шаблон графиков успешно удален";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("TemplateGrafics");
        }

        // ============================================
        // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ (ADDITIONAL PRICES)
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
                TempData["Error"] = "Цена не найдена";
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
                        // ===== НОВЫЕ ПОЛЯ =====
                        command.Parameters.AddWithValue("@free_price_ids", price.FreePriceIds ?? "");
                        command.Parameters.AddWithValue("@base_price_ids", price.BasePriceIds ?? "");
                        command.Parameters.AddWithValue("@pro_price_ids", price.ProPriceIds ?? "");

                        command.Parameters.AddWithValue("@unselected_price_mode", price.UnselectedPriceMode);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["Message"] = "Дополнительная цена успешно добавлена";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                DeleteGlobalId(globalId);
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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
                        // ===== НОВЫЕ ПОЛЯ =====
                        command.Parameters.AddWithValue("@free_price_ids", price.FreePriceIds ?? "");
                        command.Parameters.AddWithValue("@base_price_ids", price.BasePriceIds ?? "");
                        command.Parameters.AddWithValue("@pro_price_ids", price.ProPriceIds ?? "");

                        command.Parameters.AddWithValue("@unselected_price_mode", price.UnselectedPriceMode);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = "Дополнительная цена успешно обновлена";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Дополнительная цена не найдена";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
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
                TempData["Message"] = "Дополнительная цена успешно удалена";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("AdditionalPrices");
        }

        // ============================================
        // ШАБЛОНЫ ДОПОЛНИТЕЛЬНЫХ ЦЕН (TEMPLATE_ADDITIONAL_PRICES)
        // ============================================

        // ============================================
        // МЕТОДЫ ДЛЯ ПОЛУЧЕНИЯ ITEMS ИЗ ШАБЛОНОВ (ДЛЯ Templates.cshtml)
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
                        DisplayText = a.TextRu ?? a.TextEng ?? a.TextGer ?? "(пусто)",
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
                        DisplayText = r.TextRu ?? r.TextEng ?? r.TextGer ?? "(пусто)",
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
                        DisplayText = e.TextRu ?? e.TextEng ?? e.TextGer ?? "(пусто)",
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

            // Получаем ID в порядке из шаблона
            var ids = template.Prices.Split(',').Select(int.Parse).ToList();
            var allPrices = GetPricesFromDatabase();

            // Создаём словарь для быстрого доступа
            var priceDict = allPrices.ToDictionary(p => p.Id);

            // ИДЁМ В ПОРЯДКЕ ID ИЗ ШАБЛОНА!
            return ids
                .Where(id => priceDict.ContainsKey(id))
                .Select(id => {
                    var p = priceDict[id];
                    return new TemplateItemDetail
                    {
                        Id = p.Id,
                        DisplayText = p.NameRu ?? p.NameEng ?? p.NameGer ?? "(без названия)",
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
                        DisplayText = g.Name ?? "(без названия)",
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
                        DisplayText = p.NameRu ?? p.NameEng ?? p.NameGer ?? "(без названия)",
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
                        // ===== НОВЫЕ ПОЛЯ =====
                        FreePriceIds = p.FreePriceIds,
                        BasePriceIds = p.BasePriceIds,
                        ProPriceIds = p.ProPriceIds,
                        UnselectedPriceMode = p.UnselectedPriceMode
                    };
                })
                .ToList();
        }

        // ============================================
        // ПОЛУЧЕНИЕ ШАБЛОНОВ С ЗАПИСЯМИ ДЛЯ СТРАНИЦЫ TEMPLATES
        // ============================================

        private async Task<List<TemplateWithItems>> GetTemplatesWithItemsAsync(string type)
        {
            var result = new List<TemplateWithItems>();

            switch (type)
            {
                case "about":
                    var aboutTemplates = GetTemplateAboutsFromDatabase();
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
                    var resultTemplates = GetTemplateResultsFromDatabase();
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
                    var engineTemplates = GetTemplateEngineControlsFromDatabase();
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
                    var priceTemplates = GetTemplatePricesFromDatabase();
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
                    var graficTemplates = GetTemplateGraficsFromDatabase();
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
                    var additionalTemplates = GetTemplateAdditionalPricesFromDatabase();
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
        // ПОЛУЧЕНИЕ СПИСКА МАШИН, ИСПОЛЬЗУЮЩИХ ШАБЛОН
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
            if (carIds.Count == 0) return "Не используется";

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

            return carNames.Count > 0 ? string.Join(", ", carNames) : "Не используется";
        }

        // ============================================
        // СОЗДАНИЕ ШАБЛОНОВ С ЗАПИСЯМИ (С ПОДДЕРЖКОЙ EXISTING_ID)
        // ============================================

        [HttpPost("CreateAboutTemplateWithItems")]
        public async Task<IActionResult> CreateAboutTemplateWithItems(string templateName, List<AboutItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Заполните название шаблона и добавьте хотя бы одну запись";
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

                    // ===== ПРОВЕРЯЕМ: ЕСЛИ УКАЗАН EXISTING_ID, ИСПОЛЬЗУЕМ СУЩЕСТВУЮЩУЮ ЗАПИСЬ =====
                    if (item.ExistingId.HasValue && item.ExistingId.Value > 0)
                    {
                        // Проверяем, существует ли запись с таким ID
                        string checkQuery = "SELECT COUNT(*) FROM about WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.ExistingId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"Запись с ID {item.ExistingId.Value} не найдена в таблице about";
                                return RedirectToAction("Templates", new { activeTab = "about" });
                            }
                        }

                        // ===== ИСПОЛЬЗУЕМ СУЩЕСТВУЮЩУЮ ЗАПИСЬ =====
                        itemId = item.ExistingId.Value;

                        // Обновляем запись, если переданы новые данные
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
                        // ===== СОЗДАЁМ НОВУЮ ЗАПИСЬ =====
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

                // Сохраняем шаблон
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

            TempData["Message"] = $"Шаблон '{templateName}' успешно создан с {itemIds.Count} записями";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "about" });
        }

        [HttpPost("CreateResultTemplateWithItems")]
        public async Task<IActionResult> CreateResultTemplateWithItems(string templateName, List<ResultItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Заполните название шаблона и добавьте хотя бы одну запись";
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
                                TempData["Error"] = $"Запись с ID {item.ExistingId.Value} не найдена в таблице result";
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

            TempData["Message"] = $"Шаблон '{templateName}' успешно создан с {itemIds.Count} записями";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "result" });
        }

        [HttpPost("CreateEngineTemplateWithItems")]
        public async Task<IActionResult> CreateEngineTemplateWithItems(string templateName, List<EngineItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Заполните название шаблона и добавьте хотя бы одну запись";
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
                                TempData["Error"] = $"Запись с ID {item.ExistingId.Value} не найдена в таблице engine_control";
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

            TempData["Message"] = $"Шаблон '{templateName}' успешно создан с {itemIds.Count} записями";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "engine" });
        }

        [HttpPost("CreatePriceTemplateWithItems")]
        public async Task<IActionResult> CreatePriceTemplateWithItems(string templateName, List<PriceItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Заполните название шаблона и добавьте хотя бы одну запись";
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
                                TempData["Error"] = $"Запись с ID {item.ExistingId.Value} не найдена в таблице price";
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

            TempData["Message"] = $"Шаблон '{templateName}' успешно создан с {itemIds.Count} записями";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "price" });
        }

        [HttpPost("CreateGraficTemplateWithItems")]
        public async Task<IActionResult> CreateGraficTemplateWithItems(string templateName, List<GraficItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Заполните название шаблона и добавьте хотя бы одну запись";
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
                                TempData["Error"] = $"Запись с ID {item.ExistingId.Value} не найдена в таблице grafic";
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

            TempData["Message"] = $"Шаблон '{templateName}' успешно создан с {itemIds.Count} записями";
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = "grafic" });
        }

        [HttpPost("CreateAdditionalPriceTemplateWithItems")]
        public async Task<IActionResult> CreateAdditionalPriceTemplateWithItems(string templateName, List<AdditionalPriceItemDto> items)
        {
            if (string.IsNullOrEmpty(templateName) || items == null || !items.Any())
            {
                TempData["Error"] = "Заполните название шаблона и добавьте хотя бы одну запись";
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
                    // ВАРИАНТ 1: ИСПОЛЬЗУЕМ СУЩЕСТВУЮЩУЮ ЗАПИСЬ ПО ID
                    // ============================================================
                    if (item.ExistingId.HasValue && item.ExistingId.Value > 0)
                    {
                        // Проверяем, существует ли запись
                        string checkQuery = "SELECT COUNT(*) FROM additional_prices WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.ExistingId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"Запись с ID {item.ExistingId.Value} не найдена в таблице additional_prices";
                                return RedirectToAction("Templates", new { activeTab = "additional" });
                            }
                        }

                        itemId = item.ExistingId.Value;

                        // Проверяем, есть ли данные для обновления
                        bool hasData = !string.IsNullOrEmpty(item.NameRu) || !string.IsNullOrEmpty(item.NameEng) ||
                                       !string.IsNullOrEmpty(item.NameGer) || !string.IsNullOrEmpty(item.PriceRubl) ||
                                       !string.IsNullOrEmpty(item.PriceDolar) || !string.IsNullOrEmpty(item.PriceEuro) ||
                                       !string.IsNullOrEmpty(item.InfoRu) || !string.IsNullOrEmpty(item.InfoEng) ||
                                       !string.IsNullOrEmpty(item.InfoGer) || item.PriceControler != 0 ||
                                       !string.IsNullOrEmpty(item.FreePriceIds) || !string.IsNullOrEmpty(item.BasePriceIds) ||
                                       !string.IsNullOrEmpty(item.ProPriceIds);

                        if (hasData)
                        {
                            // ===== ОЧИЩАЕМ ДУБЛИКАТЫ =====
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
                    // ВАРИАНТ 2: ДОБАВЛЯЕМ ВЛОЖЕННЫЙ ШАБЛОН
                    // ============================================================
                    else if (item.TemplateId.HasValue && item.TemplateId.Value > 0)
                    {
                        // Проверяем, существует ли шаблон
                        string checkQuery = "SELECT COUNT(*) FROM template_additional_prices WHERE id = @id";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@id", item.TemplateId.Value);
                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                TempData["Error"] = $"Шаблон с ID {item.TemplateId.Value} не найден";
                                return RedirectToAction("Templates", new { activeTab = "additional" });
                            }
                        }

                        // Просто используем ID существующего шаблона
                        itemId = item.TemplateId.Value;
                    }
                    // ============================================================
                    // ВАРИАНТ 3: СОЗДАЁМ НОВУЮ ЗАПИСЬ
                    // ============================================================
                    else
                    {
                        itemId = CreateGlobalId("additional_prices");

                        // ===== ОЧИЩАЕМ ДУБЛИКАТЫ =====
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

                    // Добавляем ID в список
                    if (!itemIds.Contains(itemId))
                    {
                        itemIds.Add(itemId);
                    }
                }

                // Сохраняем шаблон
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

            TempData["Message"] = $"Шаблон '{templateName}' успешно создан с {itemIds.Count} записями";
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
        // УПРАВЛЕНИЕ ШАБЛОНАМИ
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

            TempData["Message"] = "Название шаблона обновлено";
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

            TempData["Message"] = "Шаблон удален";
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

            TempData["Message"] = "Запись удалена из шаблона";
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
            int priceControler = 0,                    // <-- ДОБАВИТЬ
            int unselectedPriceMode = 0,
            string unselectedPriceModeHidden = "",
            string[] freePriceIds = null,      // ← ИЗМЕНЕНО НА МАССИВ
            string[] basePriceIds = null,      // ← ИЗМЕНЕНО НА МАССИВ
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
                        // ===== ЕСЛИ PRICE CONTROLER НЕ 2 - ОЧИЩАЕМ ID =====
                        string freeIds;
                        string baseIds;
                        string proIds;

                        if (priceControler != 2)
                        {
                            // Сбрасываем все ID, если priceControler не 2
                            freeIds = "";
                            baseIds = "";
                            proIds = "";
                        }
                        else
                        {
                            // Иначе используем переданные значения
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

            TempData["Message"] = "Запись обновлена";
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

                // Получаем текущие ID
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
        // ДОБАВЛЕНИЕ ЗАПИСИ В ШАБЛОН (ПО ID ИЛИ НОВАЯ)
        // ==========================================

        // HomeController.EditCarBlocks.cs — полный обновлённый метод

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
            int unselectedPriceMode = 0,              // <-- ДОБАВИТЬ
            string[] freePriceIds = null,             // <-- ДОБАВИТЬ
            string[] basePriceIds = null,             // <-- ДОБАВИТЬ
            string[] proPriceIds = null,
            string returnTab = null)
        {
            string connectionString = GetConnectionString();
            int newItemId;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // ============================================================
                // ВАРИАНТ 1: ДОБАВЛЕНИЕ ПО ID СУЩЕСТВУЮЩЕЙ ЗАПИСИ (КОПИРОВАНИЕ)
                // ============================================================
                if (existingItemId.HasValue && existingItemId.Value > 0)
                {
                    string recordTable = GetRecordTable(type);

                    // Проверяем, что запись существует
                    string checkQuery = $"SELECT COUNT(*) FROM {recordTable} WHERE id = @id";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@id", existingItemId.Value);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            TempData["Error"] = $"Запись с ID {existingItemId.Value} не найдена в таблице {recordTable}";
                            return RedirectToAction("Templates");
                        }
                    }

                    // Копируем запись — создаём копию с новым ID
                    newItemId = CopyRecord(existingItemId.Value, recordTable);
                }

                // ============================================================
                // ВАРИАНТ 2: ДОБАВЛЕНИЕ ВЛОЖЕННОГО ШАБЛОНА
                // ============================================================
                else if (existingTemplateId.HasValue && existingTemplateId.Value > 0)
                {
                    string templateTable = GetTemplateTable(type);

                    // Проверяем, что шаблон существует
                    string checkQuery = $"SELECT COUNT(*) FROM {templateTable} WHERE id = @id";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@id", existingTemplateId.Value);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count == 0)
                        {
                            TempData["Error"] = $"Шаблон с ID {existingTemplateId.Value} не найден";
                            return RedirectToAction("Templates");
                        }
                    }

                    // Просто используем ID существующего шаблона (он уже есть в global_ids)
                    newItemId = existingTemplateId.Value;
                }

                // ============================================================
                // ВАРИАНТ 3: СОЗДАНИЕ НОВОЙ ЗАПИСИ
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
                            TempData["Error"] = $"Неизвестный тип: {type}";
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
                                insertCmd.Parameters.AddWithValue("@unselected_price_mode", unselectedPriceMode);                          // <-- ДОБАВИТЬ
                                insertCmd.Parameters.AddWithValue("@free_price_ids", freePriceIds != null ? string.Join(",", freePriceIds) : "");   // <-- ДОБАВИТЬ
                                insertCmd.Parameters.AddWithValue("@base_price_ids", basePriceIds != null ? string.Join(",", basePriceIds) : "");   // <-- ДОБАВИТЬ
                                insertCmd.Parameters.AddWithValue("@pro_price_ids", proPriceIds != null ? string.Join(",", proPriceIds) : "");
                                break;
                        }

                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                // ============================================================
                // ДОБАВЛЯЕМ ID В ШАБЛОН
                // ============================================================
                string templateTableName = GetTemplateTable(type);
                string idsColumn = type == "price" ? "prices" : (type == "additional" ? "price_ids" : "ids");

                // Получаем текущий список ID в шаблоне
                string getQuery = $"SELECT {idsColumn} FROM {templateTableName} WHERE id = @id";
                string currentIds = "";
                using (var getCmd = new MySqlCommand(getQuery, connection))
                {
                    getCmd.Parameters.AddWithValue("@id", templateId);
                    var result = await getCmd.ExecuteScalarAsync();
                    currentIds = result?.ToString() ?? "";
                }

                // Добавляем новый ID, если его ещё нет
                var idsList = string.IsNullOrEmpty(currentIds)
                    ? new List<int>()
                    : currentIds.Split(',').Select(int.Parse).ToList();

                if (!idsList.Contains(newItemId))
                {
                    idsList.Add(newItemId);
                }

                string newIds = string.Join(",", idsList);

                // Обновляем шаблон
                string updateQuery = $"UPDATE {templateTableName} SET {idsColumn} = @ids WHERE id = @id";
                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@ids", newIds);
                    updateCmd.Parameters.AddWithValue("@id", templateId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }

            // Определяем сообщение в зависимости от способа добавления
            string message;
            if (existingItemId.HasValue && existingItemId.Value > 0)
            {
                message = $"Запись #{existingItemId.Value} скопирована в шаблон (новая ID: {newItemId})";
            }
            else if (existingTemplateId.HasValue && existingTemplateId.Value > 0)
            {
                message = $"Вложенный шаблон #{existingTemplateId.Value} добавлен в шаблон";
            }
            else
            {
                message = "Новая запись создана и добавлена в шаблон";
            }

            TempData["Message"] = message;
            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            TempData["Message"] = message;
            await NotifyReaderSite();
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }

        // ==========================================
        // ДОБАВЛЕНИЕ ШАБЛОНА ВО ВСЕ МАШИНЫ
        // ==========================================

        [HttpPost("AddTemplateToAllCars")]
        public async Task<IActionResult> AddTemplateToAllCars(int templateId, string type, string returnTab = null)
        {
            try
            {
                string connectionString = GetConnectionString();
                var carIds = new List<int>();

                // Получаем все ID машин
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
                    TempData["Error"] = "Нет машин в базе данных";
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
                    _ => throw new ArgumentException($"Неизвестный тип: {type}")
                };

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    foreach (var carId in carIds)
                    {
                        // Получаем текущее значение поля
                        string getQuery = $"SELECT {columnName} FROM reflash_cars WHERE id = @id";
                        string currentValue = "";
                        using (var getCmd = new MySqlCommand(getQuery, connection))
                        {
                            getCmd.Parameters.AddWithValue("@id", carId);
                            var result = await getCmd.ExecuteScalarAsync();
                            currentValue = result?.ToString() ?? "";
                        }

                        // Проверяем, есть ли уже этот шаблон в списке
                        var idsList = string.IsNullOrEmpty(currentValue)
                            ? new List<int>()
                            : currentValue.Split(',').Select(int.Parse).ToList();

                        if (!idsList.Contains(templateId))
                        {
                            idsList.Add(templateId);
                            string newValue = string.Join(",", idsList);

                            // Обновляем запись
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

                    // Обновляем used_in_cars в шаблоне
                    await UpdateUsedInCarsForAllCars(templateId, type, carIds);
                }

                await NotifyReaderSite();
                TempData["Message"] = $"Шаблон добавлен в {addedCount} машин из {carIds.Count}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при добавлении шаблона во все машины: {ex.Message}";
            }

            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }

        // ==========================================
        // ОБНОВЛЕНИЕ USED_IN_CARS ДЛЯ ВСЕХ МАШИН
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
                _ => throw new ArgumentException($"Неизвестный тип: {type}")
            };

            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Получаем текущее значение used_in_cars
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

                // Добавляем все ID машин, которых еще нет в списке
                foreach (var carId in carIds)
                {
                    if (!existingCarIds.Contains(carId))
                    {
                        existingCarIds.Add(carId);
                    }
                }

                string newUsed = existingCarIds.Any() ? string.Join(",", existingCarIds) : "";

                // Сохраняем обновленное значение
                string updateQuery = $"UPDATE {templateTable} SET used_in_cars = @used_in_cars WHERE id = @id";
                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@used_in_cars", newUsed);
                    updateCmd.Parameters.AddWithValue("@id", templateId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }
        }
        // HomeController.EditCarBlocks.cs — добавить метод

        /// <summary>
        /// Копирует запись по ID и возвращает ID новой копии
        /// </summary>
        private int CopyRecord(int sourceId, string recordTable)
        {
            string connectionString = GetConnectionString();
            int newId = 0;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // 1. Получаем данные исходной записи
                string selectQuery = $"SELECT * FROM `{recordTable}` WHERE id = @id";
                List<string> columnNames = null;
                Dictionary<string, object> values = null;

                using (var cmd = new MySqlCommand(selectQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@id", sourceId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new Exception($"Запись с ID {sourceId} не найдена в таблице {recordTable}");

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

                // 2. СОЗДАЁМ НОВЫЙ ID И ПРОВЕРЯЕМ, ЧТО ОН СВОБОДЕН
                bool created = false;
                int attempts = 0;

                while (!created && attempts < 10)
                {
                    attempts++;
                    newId = CreateGlobalId(recordTable);

                    // Проверяем, что ID не занят в таблице
                    string checkFree = $"SELECT COUNT(*) FROM `{recordTable}` WHERE id = @id";
                    using (var checkCmd = new MySqlCommand(checkFree, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@id", newId);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            // ID занят — удаляем из global_ids и пробуем новый
                            DeleteGlobalId(newId);
                            continue;
                        }
                    }

                    // 3. ВСТАВЛЯЕМ КОПИЮ С УКАЗАНИЕМ ID
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
                    throw new Exception("Не удалось создать копию записи");
            }

            return newId;
        }
        // ==========================================
        // КЛАСС ДЛЯ ЗАПРОСА ПЕРЕМЕЩЕНИЯ В ШАБЛОНЕ
        // ==========================================

        // ==========================================
        // ПЕРЕМЕЩЕНИЕ ЗАПИСИ В ШАБЛОНЕ (ВВЕРХ/ВНИЗ)
        // ==========================================
        [HttpPost("TemplateMoveItem")]
        public async Task<IActionResult> TemplateMoveItem([FromBody] TemplateMoveItemRequest request)
        {
            try
            {
                // Добавляем логирование для отладки
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

                    // Получаем текущие ID
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
                        return Json(new { success = false, message = "Нет записей в шаблоне" });

                    var ids = ParseIdList(currentIds);
                    var index = ids.IndexOf(request.ItemId);

                    if (index == -1)
                        return Json(new { success = false, message = $"Запись {request.ItemId} не найдена в шаблоне" });

                    // Перемещаем
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
                        return Json(new { success = true, message = "Перемещение не требуется" });
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
                    message = "Запись перемещена",
                    returnTab = request.ReturnTab ?? request.Type  // <<< ИЗМЕНЕНО
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR: {ex.Message}");
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
                        DisplayText = a.TextRu ?? a.TextEng ?? a.TextGer ?? "(пусто)"
                    }).ToList(),
                "result" => GetResultsFromDatabase()
                    .Select(r => new TemplateItemDetail
                    {
                        Id = r.Id,
                        DisplayText = r.TextRu ?? r.TextEng ?? r.TextGer ?? "(пусто)"
                    }).ToList(),
                "engine" => GetEngineControlsFromDatabase()
                    .Select(e => new TemplateItemDetail
                    {
                        Id = e.Id,
                        DisplayText = e.TextRu ?? e.TextEng ?? e.TextGer ?? "(пусто)"
                    }).ToList(),
                "price" => GetPricesFromDatabase()
                    .Select(p => new TemplateItemDetail
                    {
                        Id = p.Id,
                        DisplayText = p.NameRu ?? p.NameEng ?? p.NameGer ?? "(без названия)"
                    }).ToList(),
                "grafic" => GetGraficsFromDatabase()
                    .Select(g => new TemplateItemDetail
                    {
                        Id = g.Id,
                        DisplayText = g.Name ?? "(без названия)"
                    }).ToList(),
                "additional" => GetAdditionalPricesFromDatabase()
                    .Select(p => new TemplateItemDetail
                    {
                        Id = p.Id,
                        DisplayText = p.NameRu ?? p.NameEng ?? p.NameGer ?? "(без названия)"
                    }).ToList(),
                _ => new List<TemplateItemDetail>()
            };
        }

        // ==========================================
        // УДАЛЕНИЕ ЗАПИСИ ИЗ БД (С ПРОВЕРКОЙ)
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

                    // ===== 1. Удаляем запись из таблицы =====
                    string deleteQuery = $"DELETE FROM {recordTable} WHERE id = @id";
                    using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", itemId);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }

                    // ===== 2. Удаляем ID из шаблонов =====
                    string templateTable = GetTemplateTable(type);
                    string idsColumn = type == "price" ? "prices" : (type == "additional" ? "price_ids" : "ids");

                    // Получаем все шаблоны
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

                    // ===== 3. Обновляем шаблоны (уже НЕ внутри DataReader) =====
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

                    // ===== 4. Удаляем из глобальных ID =====
                    DeleteGlobalId(itemId);
                }

                TempData["Message"] = $"Запись #{itemId} удалена из базы данных";
                await NotifyReaderSite();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при удалении записи: {ex.Message}";
            }

            var activeTab = string.IsNullOrEmpty(returnTab) ? type : returnTab;
            return RedirectToAction("Templates", new { activeTab = activeTab });
        }
    }
}
