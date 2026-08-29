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
    public partial class CarsController : AppController
    {
        // ============================================
        // РЕДАКТИРОВАНИЕ МАШИНЫ: БЛОКИ КОНТЕНТА
        // ============================================

        private static readonly Dictionary<string, (string Column, string RecordTable, string TemplateTable)> BlockMeta =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["about"] = ("about_ru", "about", "template_about"),
                ["result"] = ("result_ru", "result", "template_result"),
                ["engine"] = ("engine_control_ru", "engine_control", "template_engine_control"),
                ["price"] = ("price_ru", "price", "template_price"),
                ["grafic"] = ("grafic", "grafic", "template_grafic"),
                ["additional"] = ("additional_price_ru", "additional_prices", "template_additional_prices")
            };

        // ============================================
        // ПОСТРОЕНИЕ МОДЕЛИ ДЛЯ РЕДАКТИРОВАНИЯ
        // ============================================

        private async Task<EditCarViewModel> BuildEditCarViewModelAsync(ReflashCarModel car)
        {
            var vm = new EditCarViewModel { Car = car };

            vm.AboutItems = await ResolveBlockItemsAsync(car.AboutRu, "about");
            vm.ResultItems = await ResolveBlockItemsAsync(car.ResultRu, "result");
            vm.EngineItems = await ResolveBlockItemsAsync(car.EngineControlRu, "engine");
            vm.PriceItems = await ResolveBlockItemsAsync(car.PriceRu, "price");
            vm.GraficItems = await ResolveBlockItemsAsync(car.grafic, "grafic");
            vm.AdditionalPriceItems = await ResolveAdditionalPriceItemsAsync(car.additional_price_ru);

            vm.AboutTemplates = GetTemplateAboutsFromDatabase()
                .Select(t => new TemplateOption { Id = t.Id, Name = t.Name, IdsPreview = t.Ids }).ToList();
            vm.ResultTemplates = GetTemplateResultsFromDatabase()
                .Select(t => new TemplateOption { Id = t.Id, Name = t.Name, IdsPreview = t.Ids }).ToList();
            vm.EngineTemplates = GetTemplateEngineControlsFromDatabase()
                .Select(t => new TemplateOption { Id = t.Id, Name = t.Name, IdsPreview = t.Ids }).ToList();
            vm.PriceTemplates = GetTemplatePricesFromDatabase()
                .Select(t => new TemplateOption { Id = t.Id, Name = t.Name, IdsPreview = t.Prices }).ToList();
            vm.GraficTemplates = GetTemplateGraficsFromDatabase()
                .Select(t => new TemplateOption { Id = t.Id, Name = t.Name, IdsPreview = t.Ids }).ToList();
            vm.AdditionalPriceTemplates = GetTemplateAdditionalPricesFromDatabase()
                .Select(t => new TemplateOption { Id = t.Id, Name = t.Name, IdsPreview = t.PriceIds }).ToList();

            return vm;
        }

        // ============================================
        // РАЗРЕШЕНИЕ БЛОКОВ (РАСКРЫТИЕ ШАБЛОНОВ)
        // ============================================

        // HomeController.EditCarBlocks.cs — обновить ResolveBlockItemsAsync

        private async Task<List<CarBlockItem>> ResolveBlockItemsAsync(string idsString, string block)
        {
            var items = new List<CarBlockItem>();
            if (string.IsNullOrWhiteSpace(idsString) || !BlockMeta.ContainsKey(block))
                return items;

            var meta = BlockMeta[block];
            var ids = ParseIdList(idsString);
            string connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            foreach (var id in ids)
            {
                string sourceTable = null;
                using (var cmd = new MySqlCommand("SELECT source_table FROM global_ids WHERE id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    sourceTable = (await cmd.ExecuteScalarAsync())?.ToString();
                }

                if (string.IsNullOrEmpty(sourceTable))
                    continue;

                if (sourceTable == meta.RecordTable)
                {
                    var item = await LoadRecordItemAsync(connection, block, id);
                    if (item != null)
                        items.Add(item);
                }
                else if (sourceTable == meta.TemplateTable)
                {
                    // ===== ВЛОЖЕННЫЙ ШАБЛОН =====
                    var templateItem = await LoadTemplateItemAsync(connection, block, id);
                    if (templateItem != null)
                        items.Add(templateItem);
                }
            }

            return items;
        }

        // HomeController.EditCarBlocks.cs — добавить метод

        private async Task<CarBlockItem> LoadTemplateItemAsync(MySqlConnection connection, string block, int id)
        {
            string name = null;
            string linkedIds = null;
            string table = BlockMeta[block].TemplateTable;
            string idsColumn = block.Equals("price", StringComparison.OrdinalIgnoreCase) ? "prices" : "ids";

            using (var cmd = new MySqlCommand($"SELECT name, {idsColumn} FROM {table} WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                    linkedIds = reader.IsDBNull(1) ? "" : reader.GetString(1);
                }
            }

            if (name == null)
                return null;

            // ===== РЕКУРСИВНО РАЗВОРАЧИВАЕМ ВСЕ ВЛОЖЕННЫЕ ЭЛЕМЕНТЫ =====
            var children = new List<CarBlockPreview>();
            foreach (var linkedId in ParseIdList(linkedIds))
            {
                // Проверяем, что это за ID — запись или шаблон
                string sourceTable = null;
                using (var cmd = new MySqlCommand("SELECT source_table FROM global_ids WHERE id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", linkedId);
                    sourceTable = (await cmd.ExecuteScalarAsync())?.ToString();
                }

                if (string.IsNullOrEmpty(sourceTable))
                    continue;

                if (sourceTable == BlockMeta[block].RecordTable)
                {
                    var child = await LoadRecordItemAsync(connection, block, linkedId);
                    if (child != null)
                    {
                        string title = block.ToLowerInvariant() switch
                        {
                            "price" => $"{child.NameRu} (база: {child.BasePrice} / про: {child.ProPrice})",
                            "grafic" => child.Name,
                            _ => FirstNonEmpty(child.TextRu, child.TextEng, child.TextGer)
                        };
                        children.Add(new CarBlockPreview { Id = linkedId, Title = title });
                    }
                }
                else if (sourceTable == BlockMeta[block].TemplateTable)
                {
                    // ===== ВЛОЖЕННЫЙ ШАБЛОН =====
                    var nestedTemplate = await LoadTemplateItemAsync(connection, block, linkedId);
                    if (nestedTemplate != null)
                    {
                        children.Add(new CarBlockPreview
                        {
                            Id = linkedId,
                            Title = $"📁 {nestedTemplate.TemplateName} ({nestedTemplate.TemplateChildren.Count} записей)"
                        });
                    }
                }
            }

            return new CarBlockItem
            {
                Id = id,
                IsTemplate = true,
                TemplateName = name,
                TemplateChildren = children
            };
        }

        // ============================================
        // ЗАГРУЗКА ЗАПИСЕЙ
        // ============================================

        private async Task<CarBlockItem> LoadRecordItemAsync(MySqlConnection connection, string block, int id)
        {
            switch (block.ToLowerInvariant())
            {
                case "about":
                    {
                        var m = await GetAboutByIdAsync(connection, id);
                        if (m == null) return null;
                        return new CarBlockItem
                        {
                            Id = m.Id,
                            IsTemplate = false,
                            TextRu = m.TextRu,
                            TextEng = m.TextEng,
                            TextGer = m.TextGer
                        };
                    }
                case "result":
                    {
                        var m = await GetResultByIdAsync(connection, id);
                        if (m == null) return null;
                        return new CarBlockItem
                        {
                            Id = m.Id,
                            IsTemplate = false,
                            TextRu = m.TextRu,
                            TextEng = m.TextEng,
                            TextGer = m.TextGer
                        };
                    }
                case "engine":
                    {
                        var m = await GetEngineControlByIdAsync(connection, id);
                        if (m == null) return null;
                        return new CarBlockItem
                        {
                            Id = m.Id,
                            IsTemplate = false,
                            TextRu = m.TextRu,
                            TextEng = m.TextEng,
                            TextGer = m.TextGer
                        };
                    }
                case "price":
                    {
                        var m = await GetPriceByIdAsync(connection, id);
                        if (m == null) return null;
                        return new CarBlockItem
                        {
                            Id = m.Id,
                            IsTemplate = false,
                            NameRu = m.NameRu,
                            NameEng = m.NameEng,
                            NameGer = m.NameGer,
                            BasePrice = m.BasePrice,
                            ProPrice = m.ProPrice,
                            BasePriceEng = m.BasePriceEng,
                            ProPriceEng = m.ProPriceEng,
                            BasePriceGer = m.BasePriceGer,
                            ProPriceGer = m.ProPriceGer,
                            InfoRu = m.InfoRu,
                            InfoEng = m.InfoEng,
                            InfoGer = m.InfoGer
                        };
                    }
                case "grafic":
                    {
                        var m = await GetGraficByIdAsync(connection, id);
                        if (m == null) return null;
                        return new CarBlockItem
                        {
                            Id = m.Id,
                            IsTemplate = false,
                            Name = m.Name,
                            Image = m.Image,
                            NameEng = m.NameEng,
                            NameGer = m.NameGer,
                            DescriptionRu = m.DescriptionRu,
                            DescriptionEng = m.DescriptionEng,
                            DescriptionGer = m.DescriptionGer
                        };
                    }
                default:
                    return null;
            }
        }

        

        // ============================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ============================================

        private string GetCarBlockValue(ReflashCarModel car, string block)
        {
            return block.ToLowerInvariant() switch
            {
                "about" => car.AboutRu,
                "result" => car.ResultRu,
                "engine" => car.EngineControlRu,
                "price" => car.PriceRu,
                "grafic" => car.grafic,
                "additional" => car.additional_price_ru,
                "additional-price" => car.additional_price_ru,
                _ => ""
            };
        }

        private void UpdateCarBlockColumn(int carId, string column, string value)
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "about_ru", "result_ru", "engine_control_ru", "price_ru", "grafic", "additional_price_ru"
            };

            if (!allowed.Contains(column))
                throw new ArgumentException($"Недопустимая колонка: {column}");

            string connectionString = GetConnectionString();
            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            using var cmd = new MySqlCommand(
                $"UPDATE reflash_cars SET `{column}` = @value WHERE id = @id",
                connection);

            cmd.Parameters.AddWithValue("@value", value ?? "");
            cmd.Parameters.AddWithValue("@id", carId);
            cmd.ExecuteNonQuery();
        }

        // ============================================
        // МЕТОДЫ ДЛЯ ПОЛУЧЕНИЯ ITEMS ИЗ ШАБЛОНОВ (ДЛЯ Templates.cshtml)
        // ============================================
        private async Task<List<ResultModel>> GetResultListByIdsAsync(string idsString)
        {
            var result = new List<ResultModel>();
            if (string.IsNullOrEmpty(idsString)) return result;

            var ids = ParseIdList(idsString);
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var cache = new Dictionary<int, ResultModel>();

                foreach (var id in ids)
                {
                    string sourceTable = null;
                    using (var cmd = new MySqlCommand("SELECT source_table FROM global_ids WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        sourceTable = (await cmd.ExecuteScalarAsync())?.ToString();
                    }

                    if (string.IsNullOrEmpty(sourceTable)) continue;

                    if (sourceTable == "result")
                    {
                        if (!cache.ContainsKey(id))
                        {
                            var item = await GetResultByIdAsync(connection, id);
                            if (item != null) cache[id] = item;
                        }
                        if (cache.ContainsKey(id)) result.Add(cache[id]);
                    }
                    else if (sourceTable == "template_result")
                    {
                        string linkedIds = null;
                        using (var cmd = new MySqlCommand("SELECT ids FROM template_result WHERE id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            linkedIds = (await cmd.ExecuteScalarAsync())?.ToString();
                        }

                        if (!string.IsNullOrEmpty(linkedIds))
                        {
                            foreach (var linkedId in ParseIdList(linkedIds))
                            {
                                if (!cache.ContainsKey(linkedId))
                                {
                                    var item = await GetResultByIdAsync(connection, linkedId);
                                    if (item != null) cache[linkedId] = item;
                                }
                                if (cache.ContainsKey(linkedId)) result.Add(cache[linkedId]);
                            }
                        }
                    }
                }
            }
            return result;
        }

        private async Task<List<AboutModel>> GetAboutListByIdsAsync(string idsString)
        {
            var result = new List<AboutModel>();
            if (string.IsNullOrEmpty(idsString)) return result;

            var ids = ParseIdList(idsString);
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var cache = new Dictionary<int, AboutModel>();

                foreach (var id in ids)
                {
                    string sourceTable = null;
                    using (var cmd = new MySqlCommand("SELECT source_table FROM global_ids WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        sourceTable = (await cmd.ExecuteScalarAsync())?.ToString();
                    }

                    if (string.IsNullOrEmpty(sourceTable)) continue;

                    if (sourceTable == "about")
                    {
                        if (!cache.ContainsKey(id))
                        {
                            var item = await GetAboutByIdAsync(connection, id);
                            if (item != null) cache[id] = item;
                        }
                        if (cache.ContainsKey(id)) result.Add(cache[id]);
                    }
                    else if (sourceTable == "template_about")
                    {
                        string linkedIds = null;
                        using (var cmd = new MySqlCommand("SELECT ids FROM template_about WHERE id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            linkedIds = (await cmd.ExecuteScalarAsync())?.ToString();
                        }

                        if (!string.IsNullOrEmpty(linkedIds))
                        {
                            foreach (var linkedId in ParseIdList(linkedIds))
                            {
                                if (!cache.ContainsKey(linkedId))
                                {
                                    var item = await GetAboutByIdAsync(connection, linkedId);
                                    if (item != null) cache[linkedId] = item;
                                }
                                if (cache.ContainsKey(linkedId)) result.Add(cache[linkedId]);
                            }
                        }
                    }
                }
            }
            return result;
        }

        private async Task<List<EngineControlModel>> GetEngineControlListByIdsAsync(string idsString)
        {
            var result = new List<EngineControlModel>();
            if (string.IsNullOrEmpty(idsString)) return result;

            var ids = ParseIdList(idsString);
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var cache = new Dictionary<int, EngineControlModel>();

                foreach (var id in ids)
                {
                    string sourceTable = null;
                    using (var cmd = new MySqlCommand("SELECT source_table FROM global_ids WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        sourceTable = (await cmd.ExecuteScalarAsync())?.ToString();
                    }

                    if (string.IsNullOrEmpty(sourceTable)) continue;

                    if (sourceTable == "engine_control")
                    {
                        if (!cache.ContainsKey(id))
                        {
                            var item = await GetEngineControlByIdAsync(connection, id);
                            if (item != null) cache[id] = item;
                        }
                        if (cache.ContainsKey(id)) result.Add(cache[id]);
                    }
                    else if (sourceTable == "template_engine_control")
                    {
                        string linkedIds = null;
                        using (var cmd = new MySqlCommand("SELECT ids FROM template_engine_control WHERE id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            linkedIds = (await cmd.ExecuteScalarAsync())?.ToString();
                        }

                        if (!string.IsNullOrEmpty(linkedIds))
                        {
                            foreach (var linkedId in ParseIdList(linkedIds))
                            {
                                if (!cache.ContainsKey(linkedId))
                                {
                                    var item = await GetEngineControlByIdAsync(connection, linkedId);
                                    if (item != null) cache[linkedId] = item;
                                }
                                if (cache.ContainsKey(linkedId)) result.Add(cache[linkedId]);
                            }
                        }
                    }
                }
            }
            return result;
        }

        private async Task<List<PriceModel>> GetPriceListByIdsAsync(string idsString)
        {
            var result = new List<PriceModel>();
            if (string.IsNullOrEmpty(idsString)) return result;

            var ids = ParseIdList(idsString);
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var cache = new Dictionary<int, PriceModel>();

                foreach (var id in ids)
                {
                    string sourceTable = null;
                    using (var cmd = new MySqlCommand("SELECT source_table FROM global_ids WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        sourceTable = (await cmd.ExecuteScalarAsync())?.ToString();
                    }

                    if (string.IsNullOrEmpty(sourceTable)) continue;

                    if (sourceTable == "price")
                    {
                        if (!cache.ContainsKey(id))
                        {
                            var item = await GetPriceByIdAsync(connection, id);
                            if (item != null) cache[id] = item;
                        }
                        if (cache.ContainsKey(id)) result.Add(cache[id]);
                    }
                    else if (sourceTable == "template_price")
                    {
                        string linkedIds = null;
                        using (var cmd = new MySqlCommand("SELECT prices FROM template_price WHERE id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            linkedIds = (await cmd.ExecuteScalarAsync())?.ToString();
                        }

                        if (!string.IsNullOrEmpty(linkedIds))
                        {
                            foreach (var linkedId in ParseIdList(linkedIds))
                            {
                                if (!cache.ContainsKey(linkedId))
                                {
                                    var item = await GetPriceByIdAsync(connection, linkedId);
                                    if (item != null) cache[linkedId] = item;
                                }
                                if (cache.ContainsKey(linkedId)) result.Add(cache[linkedId]);
                            }
                        }
                    }
                }
            }
            return result;
        }

        private async Task<List<GraficModel>> GetGraficListByIdsAsync(string idsString)
        {
            var result = new List<GraficModel>();
            if (string.IsNullOrEmpty(idsString)) return result;

            var ids = ParseIdList(idsString);
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var cache = new Dictionary<int, GraficModel>();

                foreach (var id in ids)
                {
                    string sourceTable = null;
                    using (var cmd = new MySqlCommand("SELECT source_table FROM global_ids WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        sourceTable = (await cmd.ExecuteScalarAsync())?.ToString();
                    }

                    if (string.IsNullOrEmpty(sourceTable)) continue;

                    if (sourceTable == "grafic")
                    {
                        if (!cache.ContainsKey(id))
                        {
                            var item = await GetGraficByIdAsync(connection, id);
                            if (item != null) cache[id] = item;
                        }
                        if (cache.ContainsKey(id)) result.Add(cache[id]);
                    }
                    else if (sourceTable == "template_grafic")
                    {
                        string linkedIds = null;
                        using (var cmd = new MySqlCommand("SELECT ids FROM template_grafic WHERE id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            linkedIds = (await cmd.ExecuteScalarAsync())?.ToString();
                        }

                        if (!string.IsNullOrEmpty(linkedIds))
                        {
                            foreach (var linkedId in ParseIdList(linkedIds))
                            {
                                if (!cache.ContainsKey(linkedId))
                                {
                                    var item = await GetGraficByIdAsync(connection, linkedId);
                                    if (item != null) cache[linkedId] = item;
                                }
                                if (cache.ContainsKey(linkedId)) result.Add(cache[linkedId]);
                            }
                        }
                    }
                }
            }
            return result;
        }

        // ============================================
        // МЕТОДЫ ДЛЯ РЕДАКТИРОВАНИЯ ЗАПИСЕЙ (JSON)
        // ============================================

        [HttpPost("EditCarSaveTextRecord")]
        public async Task<IActionResult> EditCarSaveTextRecord(int carId, string block, int id, string textRu, string textEng, string textGer)
        {
            if (!BlockMeta.TryGetValue(block, out var meta) || block is not ("about" or "result" or "engine"))
            {
                return Json(new { success = false, message = "Неверный тип блока" });
            }

            string connectionString = GetConnectionString();
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                using var cmd = new MySqlCommand(
                    $"UPDATE `{meta.RecordTable}` SET text_ru = @ru, text_eng = @eng, text_ger = @ger WHERE id = @id",
                    connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@ru", textRu ?? "");
                cmd.Parameters.AddWithValue("@eng", textEng ?? "");
                cmd.Parameters.AddWithValue("@ger", textGer ?? "");
                await cmd.ExecuteNonQueryAsync();

                await NotifyReaderSite();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("EditCarSaveGraficRecord")]
        public async Task<IActionResult> EditCarSaveGraficRecord(
            int carId,
            int id,
            string name,
            IFormFile imageFile,
            string nameEng = "",
            string nameGer = "",
            string descriptionRu = "",
            string descriptionEng = "",
            string descriptionGer = "")
        {
            string connectionString = GetConnectionString();
            string fileName = null;

            try
            {
                // Получаем текущее изображение из БД
                string currentImage = null;
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new MySqlCommand("SELECT image FROM grafic WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        currentImage = (await cmd.ExecuteScalarAsync())?.ToString();
                    }
                }

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
                        return Json(new { success = false, message = "Разрешены только JPG, PNG, GIF, WebP" });
                    }

                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "Файл слишком большой (макс. 5MB)" });
                    }

                    fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(graficsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Удаляем старое изображение
                    if (!string.IsNullOrEmpty(currentImage))
                    {
                        var oldFilePath = Path.Combine(graficsPath, currentImage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try { System.IO.File.Delete(oldFilePath); } catch { }
                        }
                    }
                }
                else
                {
                    // Если currentImage != null, используем его
                    if (!string.IsNullOrEmpty(currentImage))
                    {
                        fileName = currentImage;
                    }
                }

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"UPDATE grafic SET 
                        name = @name,
                        name_eng = @name_eng,
                        name_ger = @name_ger,
                        description_ru = @description_ru,
                        description_eng = @description_eng,
                        description_ger = @description_ger,
                        image = @image
                    WHERE id = @id";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.Parameters.AddWithValue("@name_eng", nameEng ?? "");
                        cmd.Parameters.AddWithValue("@name_ger", nameGer ?? "");
                        cmd.Parameters.AddWithValue("@description_ru", descriptionRu ?? "");
                        cmd.Parameters.AddWithValue("@description_eng", descriptionEng ?? "");
                        cmd.Parameters.AddWithValue("@description_ger", descriptionGer ?? "");
                        cmd.Parameters.AddWithValue("@image", fileName ?? "");
                        await cmd.ExecuteNonQueryAsync();
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

        // ============================================
        // ПРИВЯЗКА ШАБЛОНА (ОСНОВНЫЕ БЛОКИ)
        // ============================================

        [HttpPost("EditCarAttachTemplate")]
        public async Task<IActionResult> EditCarAttachTemplate(int carId, string block, int templateId)
        {
            if (!BlockMeta.TryGetValue(block, out var meta))
            {
                TempData["Error"] = "Неизвестный блок";
                return RedirectToAction("EditCar", new { id = carId });
            }

            var car = GetCarById(carId);
            if (car == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("Cars");
            }

            var current = GetCarBlockValue(car, block) ?? "";
            var updated = AppendIdToList(current, templateId);
            UpdateCarBlockColumn(carId, meta.Column, updated);

            // ===== ДОБАВЛЯЕМ ID МАШИНЫ В used_in_cars ШАБЛОНА =====
            await UpdateUsedInCars(carId, block, templateId, true);

            TempData["Message"] = "Шаблон привязан к автомобилю";
            await NotifyReaderSite();

            return Redirect($"/Home/EditCar/{carId}?block={block}#block-{block}");
        }

        // ============================================
        // ОТВЯЗЫВАНИЕ/УДАЛЕНИЕ ЗАПИСИ (ОСНОВНЫЕ БЛОКИ)
        // ============================================

        [HttpPost("EditCarDetachItem")]
        public async Task<IActionResult> EditCarDetachItem(int carId, string block, int itemId)
        {
            if (!BlockMeta.TryGetValue(block, out var meta))
            {
                TempData["Error"] = "Неизвестный блок";
                return RedirectToAction("EditCar", new { id = carId });
            }

            var car = GetCarById(carId);
            if (car == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("Cars");
            }

            var current = GetCarBlockValue(car, block) ?? "";

            string sourceTable = GetGlobalIdSourceTable(itemId);
            bool isTemplate = sourceTable == meta.TemplateTable;

            if (isTemplate)
            {
                // ТОЛЬКО ОТВЯЗЫВАЕМ ШАБЛОН, НЕ УДАЛЯЕМ
                await UpdateUsedInCars(carId, block, itemId, false);
                TempData["Message"] = "Шаблон отвязан от автомобиля";
            }
            else
            {
                // Для обычных записей - удаляем из БД
                if (block == "grafic")
                {
                    await DeleteGraficFileAsync(itemId);
                }

                await DeleteRecordFromTableAsync(itemId, meta.RecordTable);
                DeleteGlobalId(itemId);
                TempData["Message"] = "Запись удалена из базы данных";
            }

            var updated = RemoveIdFromList(current, itemId);
            UpdateCarBlockColumn(carId, meta.Column, updated);

            await NotifyReaderSite();
            return Redirect($"/Home/EditCar/{carId}?block={block}#block-{block}");
        }

        // ============================================
        // УДАЛЕНИЕ ИЗОБРАЖЕНИЯ ГРАФИКА
        // ============================================

        [HttpPost("RemoveGraficImage")]
        public async Task<IActionResult> RemoveGraficImage([FromBody] RemoveGraficImageRequest request)
        {
            try
            {
                string connectionString = GetConnectionString();

                string currentImage = null;
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new MySqlCommand("SELECT image FROM grafic WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", request.ItemId);
                        var result = await cmd.ExecuteScalarAsync();
                        currentImage = result?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(currentImage))
                {
                    var filePath = Path.Combine(_sharedUploadsPath, "grafics", currentImage);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new MySqlCommand(
                        "UPDATE grafic SET image = '' WHERE id = @id",
                        connection))
                    {
                        cmd.Parameters.AddWithValue("@id", request.ItemId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


        // ============================================
        // УДАЛЕНИЕ ЗАПИСЕЙ ИЗ БД (ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ)
        // ============================================

        private async Task DeleteGraficFileAsync(int id)
        {
            string connectionString = GetConnectionString();
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT image FROM grafic WHERE id = @id";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && !string.IsNullOrEmpty(result.ToString()))
                    {
                        var filePath = Path.Combine(_sharedUploadsPath, "grafics", result.ToString());
                        if (System.IO.File.Exists(filePath))
                        {
                            try { System.IO.File.Delete(filePath); }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Не удалось удалить файл: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        private async Task DeleteRecordFromTableAsync(int id, string table)
        {
            string connectionString = GetConnectionString();
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = $"DELETE FROM `{table}` WHERE id = @id";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // ============================================
        // ДОБАВЛЕНИЕ ЗАПИСЕЙ (ВОЗВРАЩАЮТ JSON)
        // ============================================

        [HttpPost("EditCarAddTextRecord")]
        public async Task<IActionResult> EditCarAddTextRecord(int carId, string block, string textRu, string textEng, string textGer)
        {
            if (!BlockMeta.TryGetValue(block, out var meta) || block is not ("about" or "result" or "engine"))
            {
                return Json(new { success = false, message = "Неверный тип блока" });
            }

            var car = GetCarById(carId);
            if (car == null)
            {
                return Json(new { success = false, message = "Автомобиль не найден" });
            }

            int globalId = CreateGlobalId(meta.RecordTable);
            string connectionString = GetConnectionString();
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                using var cmd = new MySqlCommand(
                    $"INSERT INTO `{meta.RecordTable}` (id, text_ru, text_eng, text_ger) VALUES (@id, @ru, @eng, @ger)",
                    connection);
                cmd.Parameters.AddWithValue("@id", globalId);
                cmd.Parameters.AddWithValue("@ru", textRu ?? "");
                cmd.Parameters.AddWithValue("@eng", textEng ?? "");
                cmd.Parameters.AddWithValue("@ger", textGer ?? "");
                await cmd.ExecuteNonQueryAsync();

                var updated = AppendIdToList(GetCarBlockValue(car, block), globalId);
                UpdateCarBlockColumn(carId, meta.Column, updated);

                await NotifyReaderSite();

                return Json(new { success = true, id = globalId });
            }
            catch (Exception ex)
            {
                DeleteGlobalId(globalId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // ДОБАВЛЕНИЕ ЦЕНЫ
        // ============================================

        [HttpPost("EditCarAddPriceRecord")]
        public async Task<IActionResult> EditCarAddPriceRecord(
            int carId,
            string nameRu,
            string nameEng,
            string nameGer,
            string basePrice,
            string proPrice,
            string basePriceEng,
            string proPriceEng,
            string basePriceGer,
            string proPriceGer)
        {
            var car = GetCarById(carId);
            if (car == null)
            {
                return Json(new { success = false, message = "Автомобиль не найден" });
            }

            int globalId = CreateGlobalId("price");
            string connectionString = GetConnectionString();
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                using var cmd = new MySqlCommand(
                    @"INSERT INTO price (id, name_ru, name_eng, name_ger, base_price, pro_price,
                                        base_price_eng, pro_price_eng, base_price_ger, pro_price_ger)
                      VALUES (@id, @name_ru, @name_eng, @name_ger, @base_price, @pro_price,
                              @base_price_eng, @pro_price_eng, @base_price_ger, @pro_price_ger)",
                    connection);
                cmd.Parameters.AddWithValue("@id", globalId);
                cmd.Parameters.AddWithValue("@name_ru", nameRu ?? "");
                cmd.Parameters.AddWithValue("@name_eng", nameEng ?? "");
                cmd.Parameters.AddWithValue("@name_ger", nameGer ?? "");
                cmd.Parameters.AddWithValue("@base_price", basePrice ?? "");
                cmd.Parameters.AddWithValue("@pro_price", proPrice ?? "");
                cmd.Parameters.AddWithValue("@base_price_eng", basePriceEng ?? "");
                cmd.Parameters.AddWithValue("@pro_price_eng", proPriceEng ?? "");
                cmd.Parameters.AddWithValue("@base_price_ger", basePriceGer ?? "");
                cmd.Parameters.AddWithValue("@pro_price_ger", proPriceGer ?? "");
                await cmd.ExecuteNonQueryAsync();

                var updated = AppendIdToList(car.PriceRu, globalId);
                UpdateCarBlockColumn(carId, "price_ru", updated);

                await NotifyReaderSite();

                return Json(new { success = true, id = globalId });
            }
            catch (Exception ex)
            {
                DeleteGlobalId(globalId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // СОХРАНЕНИЕ ЦЕНЫ
        // ============================================

        [HttpPost("EditCarSavePriceRecord")]
        public async Task<IActionResult> EditCarSavePriceRecord(
            int carId,
            int id,
            string nameRu,
            string nameEng,
            string nameGer,
            string basePrice,
            string proPrice,
            string basePriceEng,
            string proPriceEng,
            string basePriceGer,
            string proPriceGer,
            string infoRu,
            string infoEng,
            string infoGer)
        {
            string connectionString = GetConnectionString();
            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                using var cmd = new MySqlCommand(
                    @"UPDATE price SET 
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
                      WHERE id = @id",
                    connection);
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

                await NotifyReaderSite();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // ДОБАВЛЕНИЕ ГРАФИКА
        // ============================================

        [HttpPost("EditCarAddGraficRecord")]
        public async Task<IActionResult> EditCarAddGraficRecord(
            int carId,
            string name,
            IFormFile imageFile,
            string nameEng = "",
            string nameGer = "",
            string descriptionRu = "",
            string descriptionEng = "",
            string descriptionGer = "")
        {
            var car = GetCarById(carId);
            if (car == null)
            {
                return Json(new { success = false, message = "Автомобиль не найден" });
            }

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
                    return Json(new { success = false, message = "Разрешены только JPG, PNG, GIF, WebP" });
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Файл слишком большой (макс. 5MB)" });
                }

                fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(graficsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
            }

            int globalId = CreateGlobalId("grafic");
            string connectionString = GetConnectionString();
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new MySqlCommand(
                        @"INSERT INTO grafic (id, name, name_eng, name_ger, image, 
                            description_ru, description_eng, description_ger) 
                        VALUES (@id, @name, @name_eng, @name_ger, @image,
                            @description_ru, @description_eng, @description_ger)",
                        connection))
                    {
                        cmd.Parameters.AddWithValue("@id", globalId);
                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.Parameters.AddWithValue("@name_eng", nameEng ?? "");
                        cmd.Parameters.AddWithValue("@name_ger", nameGer ?? "");
                        cmd.Parameters.AddWithValue("@image", fileName ?? "");
                        cmd.Parameters.AddWithValue("@description_ru", descriptionRu ?? "");
                        cmd.Parameters.AddWithValue("@description_eng", descriptionEng ?? "");
                        cmd.Parameters.AddWithValue("@description_ger", descriptionGer ?? "");
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                var updated = AppendIdToList(car.grafic, globalId);
                UpdateCarBlockColumn(carId, "grafic", updated);

                await NotifyReaderSite();

                return Json(new { success = true, id = globalId });
            }
            catch (Exception ex)
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
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ (БЛОК В РЕДАКТИРОВАНИИ МАШИНЫ)
        // ============================================

        private async Task<List<CarBlockItem>> ResolveAdditionalPriceItemsAsync(string idsString)
        {
            var items = new List<CarBlockItem>();
            if (string.IsNullOrWhiteSpace(idsString)) return items;

            var ids = ParseIdList(idsString);
            string connectionString = GetConnectionString();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            foreach (var id in ids)
            {
                string sourceTable = null;
                using (var cmd = new MySqlCommand("SELECT source_table FROM global_ids WHERE id = @id", connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    sourceTable = (await cmd.ExecuteScalarAsync())?.ToString();
                }

                if (string.IsNullOrEmpty(sourceTable)) continue;

                if (sourceTable == "additional_prices")
                {
                    var item = await LoadAdditionalPriceItemAsync(connection, id);
                    if (item != null) items.Add(item);
                }
                else if (sourceTable == "template_additional_prices")
                {
                    var item = await LoadAdditionalPriceTemplateItemAsync(connection, id);
                    if (item != null) items.Add(item);
                }
            }

            return items;
        }

        private async Task<CarBlockItem> LoadAdditionalPriceItemAsync(MySqlConnection connection, int id)
        {
            using var cmd = new MySqlCommand(
                @"SELECT id, name_ru, name_eng, name_ger, 
            price_rubl, price_dolar, price_euro,
            info_ru, info_eng, info_ger, price_controler,
            free_price_ids, base_price_ids, pro_price_ids, unselected_price_mode
        FROM additional_prices WHERE id = @id",
                connection);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CarBlockItem
                {
                    Id = reader.GetInt32("id"),
                    IsTemplate = false,
                    NameRu = reader.IsDBNull(reader.GetOrdinal("name_ru")) ? "" : reader.GetString("name_ru"),
                    NameEng = reader.IsDBNull(reader.GetOrdinal("name_eng")) ? "" : reader.GetString("name_eng"),
                    NameGer = reader.IsDBNull(reader.GetOrdinal("name_ger")) ? "" : reader.GetString("name_ger"),
                    PriceRubl = reader.IsDBNull(reader.GetOrdinal("price_rubl")) ? "" : reader.GetString("price_rubl"),
                    PriceDolar = reader.IsDBNull(reader.GetOrdinal("price_dolar")) ? "" : reader.GetString("price_dolar"),
                    PriceEuro = reader.IsDBNull(reader.GetOrdinal("price_euro")) ? "" : reader.GetString("price_euro"),
                    InfoRu = reader.IsDBNull(reader.GetOrdinal("info_ru")) ? "" : reader.GetString("info_ru"),
                    InfoEng = reader.IsDBNull(reader.GetOrdinal("info_eng")) ? "" : reader.GetString("info_eng"),
                    InfoGer = reader.IsDBNull(reader.GetOrdinal("info_ger")) ? "" : reader.GetString("info_ger"),
                    PriceControler = reader.IsDBNull(reader.GetOrdinal("price_controler")) ? 0 : reader.GetInt32("price_controler"),
                    // ===== НОВЫЕ ПОЛЯ =====
                    FreePriceIds = reader.IsDBNull(reader.GetOrdinal("free_price_ids")) ? "" : reader.GetString("free_price_ids"),
                    BasePriceIds = reader.IsDBNull(reader.GetOrdinal("base_price_ids")) ? "" : reader.GetString("base_price_ids"),
                    ProPriceIds = reader.IsDBNull(reader.GetOrdinal("pro_price_ids")) ? "" : reader.GetString("pro_price_ids"),

                    UnselectedPriceMode = reader.IsDBNull(reader.GetOrdinal("unselected_price_mode")) ? 0 : reader.GetInt32("unselected_price_mode")
                };
            }
            return null;
        }

        private async Task<CarBlockItem> LoadAdditionalPriceTemplateItemAsync(MySqlConnection connection, int id)
        {
            string name = null;
            string linkedIds = null;

            using (var cmd = new MySqlCommand("SELECT name, price_ids FROM template_additional_prices WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    name = reader.IsDBNull(0) ? "" : reader.GetString(0);
                    linkedIds = reader.IsDBNull(1) ? "" : reader.GetString(1);
                }
            }

            if (name == null) return null;

            var children = new List<CarBlockPreview>();
            foreach (var linkedId in ParseIdList(linkedIds))
            {
                var child = await LoadAdditionalPriceItemAsync(connection, linkedId);
                if (child == null) continue;

                string title = $"{child.NameRu} ({child.PriceRubl ?? "?"} ₽)";
                children.Add(new CarBlockPreview { Id = linkedId, Title = title });
            }

            return new CarBlockItem
            {
                Id = id,
                IsTemplate = true,
                TemplateName = name,
                TemplateChildren = children
            };
        }

        // ============================================
        // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ - ОТВЯЗЫВАНИЕ/УДАЛЕНИЕ
        // ============================================

        [HttpPost("EditCarDetachAdditionalPrice")]
        public async Task<IActionResult> EditCarDetachAdditionalPrice(int carId, int itemId)
        {
            var car = GetCarById(carId);
            if (car == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("Cars");
            }

            var current = car.additional_price_ru ?? "";

            string sourceTable = GetGlobalIdSourceTable(itemId);
            bool isTemplate = sourceTable == "template_additional_prices";

            if (isTemplate)
            {
                // ===== ТОЛЬКО ОТВЯЗЫВАЕМ ШАБЛОН, НО НЕ УДАЛЯЕМ ЕГО =====
                await UpdateUsedInCars(carId, "additional", itemId, false);

                // ❌ НЕ УДАЛЯЕМ ШАБЛОН ИЗ БД!
                // await DeleteRecordFromTableAsync(itemId, "template_additional_prices");
                // ❌ НЕ УДАЛЯЕМ ИЗ GLOBAL_IDS
                // DeleteGlobalId(itemId);

                TempData["Message"] = "Шаблон дополнительных цен отвязан от автомобиля";
            }
            else
            {
                await DeleteRecordFromTableAsync(itemId, "additional_prices");
                DeleteGlobalId(itemId);
                TempData["Message"] = "Запись дополнительной цены удалена из базы данных";
            }

            var updated = RemoveIdFromList(current, itemId);
            UpdateCarBlockColumn(carId, "additional_price_ru", updated);

            await NotifyReaderSite();
            return Redirect($"/Home/EditCar/{carId}?block=additional#block-additional-price");
        }

        // ============================================
        // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ - ДОБАВЛЕНИЕ
        // ============================================

        [HttpPost("EditCarSaveAdditionalPriceRecord")]
        public async Task<IActionResult> EditCarSaveAdditionalPriceRecord(
    int carId,
    int id,
    string nameRu,
    string nameEng,
    string nameGer,
    string priceRubl,
    string priceDolar,
    string priceEuro,
    string infoRu,
    string infoEng,
    string infoGer,
    int priceControler,
    int unselectedPriceMode,
    string[] freePriceIds = null,    // ← ИЗМЕНЕНО НА МАССИВ
    string[] basePriceIds = null,    // ← ИЗМЕНЕНО НА МАССИВ
    string[] proPriceIds = null)     // ← ИЗМЕНЕНО НА МАССИВ
        {
            try
            {
                // Преобразуем массивы в строки через запятую
                string freeIds = freePriceIds != null && freePriceIds.Length > 0
                    ? string.Join(",", freePriceIds)
                    : "";
                string baseIds = basePriceIds != null && basePriceIds.Length > 0
                    ? string.Join(",", basePriceIds)
                    : "";
                string proIds = proPriceIds != null && proPriceIds.Length > 0
                    ? string.Join(",", proPriceIds)
                    : "";

                string connectionString = GetConnectionString();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using var cmd = new MySqlCommand(
                    @"UPDATE additional_prices SET 
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
                free_price_ids = @free_price_ids,
                base_price_ids = @base_price_ids,
                pro_price_ids = @pro_price_ids,
                unselected_price_mode = @unselected_price_mode
            WHERE id = @id",
                    connection);

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
                // ===== ИСПОЛЬЗУЕМ ПРЕОБРАЗОВАННЫЕ СТРОКИ =====
                cmd.Parameters.AddWithValue("@free_price_ids", freeIds);
                cmd.Parameters.AddWithValue("@base_price_ids", baseIds);
                cmd.Parameters.AddWithValue("@pro_price_ids", proIds);

                cmd.Parameters.AddWithValue("@unselected_price_mode", unselectedPriceMode);
                await cmd.ExecuteNonQueryAsync();

                try { await NotifyReaderSite(); } catch { }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ - СОХРАНЕНИЕ
        // ============================================

        // ============================================
        // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ - ДОБАВЛЕНИЕ
        // ============================================

        [HttpPost("EditCarAddAdditionalPriceRecord")]
        public async Task<IActionResult> EditCarAddAdditionalPriceRecord(
            int carId,
            string nameRu,
            string nameEng,
            string nameGer,
            string priceRubl,
            string priceDolar,
            string priceEuro,
            string infoRu,
            string infoEng,
            string infoGer,
            int priceControler,
            int unselectedPriceMode,
            string[] freePriceIds = null,
            string[] basePriceIds = null,
            string[] proPriceIds = null)
        {
            try
            {
                var car = GetCarById(carId);
                if (car == null)
                {
                    return Json(new { success = false, message = "Автомобиль не найден" });
                }

                // Преобразуем массивы в строки через запятую
                string freeIds = freePriceIds != null && freePriceIds.Length > 0 ? string.Join(",", freePriceIds) : "";
                string baseIds = basePriceIds != null && basePriceIds.Length > 0 ? string.Join(",", basePriceIds) : "";
                string proIds = proPriceIds != null && proPriceIds.Length > 0 ? string.Join(",", proPriceIds) : "";

                int globalId = CreateGlobalId("additional_prices");
                string connectionString = GetConnectionString();

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using var cmd = new MySqlCommand(
                    @"INSERT INTO additional_prices 
                (id, name_ru, name_eng, name_ger, 
                price_rubl, price_dolar, price_euro,
                info_ru, info_eng, info_ger, sort_order, price_controler,
                free_price_ids, base_price_ids, pro_price_ids, unselected_price_mode)
            VALUES 
                (@id, @name_ru, @name_eng, @name_ger, 
                @price_rubl, @price_dolar, @price_euro,
                @info_ru, @info_eng, @info_ger, 0, @price_controler,
                @free_price_ids, @base_price_ids, @pro_price_ids, @unselected_price_mode)",
                    connection);

                cmd.Parameters.AddWithValue("@id", globalId);
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
                cmd.Parameters.AddWithValue("@free_price_ids", freeIds);
                cmd.Parameters.AddWithValue("@base_price_ids", baseIds);
                cmd.Parameters.AddWithValue("@pro_price_ids", proIds);

                cmd.Parameters.AddWithValue("@unselected_price_mode", unselectedPriceMode);

                await cmd.ExecuteNonQueryAsync();

                var updated = AppendIdToList(car.additional_price_ru, globalId);
                UpdateCarBlockColumn(carId, "additional_price_ru", updated);

                try { await NotifyReaderSite(); } catch { }

                return Json(new { success = true, id = globalId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ - ПРИВЯЗКА ШАБЛОНА
        // ============================================


        // ============================================
        // ШАБЛОНЫ ДОПОЛНИТЕЛЬНЫХ ЦЕН - ПОЛУЧЕНИЕ
        // ============================================


        // ============================================
        // МЕТОДЫ ДЛЯ ПОЛУЧЕНИЯ ТЕКУЩИХ ЦЕН (для GetAdditionalPricesFromDatabase)
        // ============================================


        // ============================================
        // ОБНОВЛЕНИЕ USED_IN_CARS ПРИ ПРИВЯЗКЕ ШАБЛОНА
        // ============================================

        private async Task UpdateUsedInCars(int carId, string block, int templateId, bool add)
        {
            string templateTable = block switch
            {
                "about" => "template_about",
                "result" => "template_result",
                "engine" => "template_engine_control",
                "price" => "template_price",
                "grafic" => "template_grafic",
                "additional" => "template_additional_prices",
                _ => throw new ArgumentException($"Неизвестный блок: {block}")
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

                // Обновляем список ID машин
                var carIds = string.IsNullOrEmpty(currentUsed)
                    ? new List<int>()
                    : currentUsed.Split(',').Select(int.Parse).ToList();

                if (add)
                {
                    if (!carIds.Contains(carId))
                        carIds.Add(carId);
                }
                else
                {
                    carIds.Remove(carId);
                }

                string newUsed = carIds.Any() ? string.Join(",", carIds) : "";

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

        // ============================================
        // СОЗДАНИЕ ШАБЛОНОВ С ЗАПИСЯМИ (С ПОДДЕРЖКОЙ EXISTING_ID)
        // ============================================
        /// <summary>
        /// Сохраняет порядок записей в блоке автомобиля
        /// </summary>
        [HttpPost("EditCarReorderItems")]
        public async Task<IActionResult> EditCarReorderItems(int carId, string block, string orderedIds)
        {
            try
            {
                // Логируем для отладки
                Console.WriteLine($"=== EditCarReorderItems ===");
                Console.WriteLine($"carId: {carId}");
                Console.WriteLine($"block: '{block}'");
                Console.WriteLine($"orderedIds: '{orderedIds}'");

                string columnName;

                // Определяем колонку для обновления
                switch (block?.ToLowerInvariant() ?? "")
                {
                    case "about":
                        columnName = "about_ru";
                        break;
                    case "result":
                        columnName = "result_ru";
                        break;
                    case "engine":
                        columnName = "engine_control_ru";
                        break;
                    case "price":
                        columnName = "price_ru";
                        break;
                    case "grafic":
                        columnName = "grafic";
                        break;
                    case "additional":
                    case "additional-price":
                        columnName = "additional_price_ru";
                        break;
                    default:
                        Console.WriteLine($"❌ Неизвестный блок: '{block}'");
                        return Json(new { success = false, message = $"Неизвестный блок: '{block}'" });
                }

                var car = GetCarById(carId);
                if (car == null)
                {
                    return Json(new { success = false, message = "Автомобиль не найден" });
                }

                var current = GetCarBlockValue(car, block) ?? "";
                Console.WriteLine($"Текущие ID: '{current}'");

                if (string.IsNullOrWhiteSpace(current))
                {
                    return Json(new { success = true, message = "Нет записей для сортировки" });
                }

                var parsedOrder = ParseIdList(orderedIds);
                Console.WriteLine($"Разобранный порядок: {string.Join(",", parsedOrder)}");

                if (parsedOrder.Count == 0)
                {
                    return Json(new { success = false, message = "Нет ID для сортировки" });
                }

                var newValue = ReorderIdsString(current, parsedOrder);
                Console.WriteLine($"Новое значение: '{newValue}'");

                if (newValue == current)
                {
                    return Json(new { success = true, message = "Порядок не изменился" });
                }

                UpdateCarBlockColumn(carId, columnName, newValue);

                await NotifyReaderSite();
                return Json(new { success = true, message = "Порядок сохранён" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Сохраняет порядок записей в шаблоне
        /// </summary>
        // ============================================
        // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ - ПРИВЯЗКА ШАБЛОНА
        // ============================================

        [HttpPost("EditCarAttachAdditionalPriceTemplate")]
        public async Task<IActionResult> EditCarAttachAdditionalPriceTemplate(int carId, int templateId)
        {
            var car = GetCarById(carId);
            if (car == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("EditCar", new { id = carId });
            }

            var current = car.additional_price_ru ?? "";
            var updated = AppendIdToList(current, templateId);
            UpdateCarBlockColumn(carId, "additional_price_ru", updated);

            // ===== ДОБАВЛЯЕМ ID МАШИНЫ В used_in_cars ШАБЛОНА =====
            await UpdateUsedInCars(carId, "additional", templateId, true);

            TempData["Message"] = "Шаблон дополнительных цен привязан к автомобилю";
            await NotifyReaderSite();

            return Redirect($"/Home/EditCar/{carId}?block=additional#block-additional-price");
        }

        [HttpPost("EditCarMoveItem")]
        public async Task<IActionResult> EditCarMoveItem([FromBody] MoveItemRequest request)
        {
            try
            {
                int carId = request.CarId;
                string block = request.Block;
                int itemId = request.ItemId;
                string direction = request.Direction;

                System.Diagnostics.Debug.WriteLine($"=== EditCarMoveItem ===");
                System.Diagnostics.Debug.WriteLine($"carId: {carId}, block: '{block}', itemId: {itemId}, direction: '{direction}'");

                string columnName = block?.ToLowerInvariant() switch
                {
                    "about" => "about_ru",
                    "result" => "result_ru",
                    "engine" => "engine_control_ru",
                    "price" => "price_ru",
                    "grafic" => "grafic",
                    "additional" or "additional-price" => "additional_price_ru",
                    _ => null
                };

                if (string.IsNullOrEmpty(columnName))
                {
                    return Json(new { success = false, message = $"Неизвестный блок: '{block}'" });
                }

                var car = GetCarById(carId);
                if (car == null)
                {
                    return Json(new { success = false, message = "Автомобиль не найден" });
                }

                var current = GetCarBlockValue(car, block) ?? "";
                var ids = ParseIdList(current);

                if (ids.Count == 0)
                {
                    return Json(new { success = false, message = "Нет записей" });
                }

                var index = ids.IndexOf(itemId);
                if (index == -1)
                {
                    return Json(new { success = false, message = $"Запись с ID {itemId} не найдена" });
                }

                if (direction == "up" && index == 0)
                {
                    return Json(new { success = false, message = "Запись уже на первом месте" });
                }
                if (direction == "down" && index == ids.Count - 1)
                {
                    return Json(new { success = false, message = "Запись уже на последнем месте" });
                }

                if (direction == "up")
                {
                    (ids[index], ids[index - 1]) = (ids[index - 1], ids[index]);
                }
                else if (direction == "down")
                {
                    (ids[index], ids[index + 1]) = (ids[index + 1], ids[index]);
                }

                var newValue = string.Join(",", ids);
                UpdateCarBlockColumn(carId, columnName, newValue);

                await NotifyReaderSite();
                return Json(new { success = true, message = "Запись перемещена" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
