using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using reflah_controler.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using reflah_controler.Models;

namespace reflah_controler.Controllers
{
    public partial class HomeController
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

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Length > 120 ? v.Substring(0, 120) + "…" : v;
            }
            return "(пусто)";
        }

        private static List<int> ParseIdList(string idsString)
        {
            if (string.IsNullOrWhiteSpace(idsString))
                return new List<int>();

            return idsString
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();
        }

        private static string AppendIdToList(string existing, int id)
        {
            var list = ParseIdList(existing);
            if (!list.Contains(id))
                list.Add(id);
            return string.Join(",", list);
        }

        private static string RemoveIdFromList(string existing, int id)
        {
            var list = ParseIdList(existing).Where(x => x != id).ToList();
            return string.Join(",", list);
        }

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
        // АСИНХРОННЫЕ МЕТОДЫ ДЛЯ ПОЛУЧЕНИЯ ЗАПИСЕЙ ПО ID
        // ============================================

        private async Task<AboutModel> GetAboutByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand("SELECT id, text_ru, text_eng, text_ger FROM about WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new AboutModel
                        {
                            Id = reader.GetInt32("id"),
                            TextRu = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                            TextEng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                            TextGer = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger")
                        };
                    }
                }
            }
            return null;
        }

        private async Task<ResultModel> GetResultByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand("SELECT id, text_ru, text_eng, text_ger FROM result WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new ResultModel
                        {
                            Id = reader.GetInt32("id"),
                            TextRu = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                            TextEng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                            TextGer = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger")
                        };
                    }
                }
            }
            return null;
        }

        private async Task<EngineControlModel> GetEngineControlByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand("SELECT id, text_ru, text_eng, text_ger FROM engine_control WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new EngineControlModel
                        {
                            Id = reader.GetInt32("id"),
                            TextRu = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                            TextEng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                            TextGer = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger")
                        };
                    }
                }
            }
            return null;
        }

        private async Task<PriceModel> GetPriceByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand(
                "SELECT id, name_ru, name_eng, name_ger, base_price, pro_price, " +
                "base_price_eng, pro_price_eng, base_price_ger, pro_price_ger, " +
                "info_ru, info_eng, info_ger FROM price WHERE id = @id",
                connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new PriceModel
                        {
                            Id = reader.GetInt32("id"),
                            NameRu = reader.IsDBNull(reader.GetOrdinal("name_ru")) ? "" : reader.GetString("name_ru"),
                            NameEng = reader.IsDBNull(reader.GetOrdinal("name_eng")) ? "" : reader.GetString("name_eng"),
                            NameGer = reader.IsDBNull(reader.GetOrdinal("name_ger")) ? "" : reader.GetString("name_ger"),
                            BasePrice = reader.IsDBNull(reader.GetOrdinal("base_price")) ? "" : reader.GetString("base_price"),
                            ProPrice = reader.IsDBNull(reader.GetOrdinal("pro_price")) ? "" : reader.GetString("pro_price"),
                            BasePriceEng = reader.IsDBNull(reader.GetOrdinal("base_price_eng")) ? "" : reader.GetString("base_price_eng"),
                            ProPriceEng = reader.IsDBNull(reader.GetOrdinal("pro_price_eng")) ? "" : reader.GetString("pro_price_eng"),
                            BasePriceGer = reader.IsDBNull(reader.GetOrdinal("base_price_ger")) ? "" : reader.GetString("base_price_ger"),
                            ProPriceGer = reader.IsDBNull(reader.GetOrdinal("pro_price_ger")) ? "" : reader.GetString("pro_price_ger"),
                            InfoRu = reader.IsDBNull(reader.GetOrdinal("info_ru")) ? "" : reader.GetString("info_ru"),
                            InfoEng = reader.IsDBNull(reader.GetOrdinal("info_eng")) ? "" : reader.GetString("info_eng"),
                            InfoGer = reader.IsDBNull(reader.GetOrdinal("info_ger")) ? "" : reader.GetString("info_ger")
                        };
                    }
                }
            }
            return null;
        }

        private async Task<GraficModel> GetGraficByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand("SELECT id, name, name_eng, name_ger, image, " +
                "description_ru, description_eng, description_ger FROM grafic WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new GraficModel
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                            NameEng = reader.IsDBNull(reader.GetOrdinal("name_eng")) ? "" : reader.GetString("name_eng"),
                            NameGer = reader.IsDBNull(reader.GetOrdinal("name_ger")) ? "" : reader.GetString("name_ger"),
                            Image = reader.IsDBNull(reader.GetOrdinal("image")) ? "" : reader.GetString("image"),
                            DescriptionRu = reader.IsDBNull(reader.GetOrdinal("description_ru")) ? "" : reader.GetString("description_ru"),
                            DescriptionEng = reader.IsDBNull(reader.GetOrdinal("description_eng")) ? "" : reader.GetString("description_eng"),
                            DescriptionGer = reader.IsDBNull(reader.GetOrdinal("description_ger")) ? "" : reader.GetString("description_ger")
                        };
                    }
                }
            }
            return null;
        }

        // ============================================
        // МЕТОДЫ ДЛЯ ПОЛУЧЕНИЯ СПИСКОВ (PreviewCar)
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        public class RemoveGraficImageRequest
        {
            public int ItemId { get; set; }
            public int CarId { get; set; }
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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
        // МЕТОДЫ ДЛЯ ПОЛУЧЕНИЯ ОБЪЕКТОВ ПО ID (синхронные)
        // ============================================

        private AboutModel GetAboutByIdSync(int id)
        {
            AboutModel about = null;
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM about WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            about = new AboutModel
                            {
                                Id = reader.GetInt32("id"),
                                TextRu = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                                TextEng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                                TextGer = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger")
                            };
                        }
                    }
                }
            }
            return about;
        }

        private ResultModel GetResultByIdSync(int id)
        {
            ResultModel result = null;
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM result WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result = new ResultModel
                            {
                                Id = reader.GetInt32("id"),
                                TextRu = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                                TextEng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                                TextGer = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger")
                            };
                        }
                    }
                }
            }
            return result;
        }

        private EngineControlModel GetEngineControlByIdSync(int id)
        {
            EngineControlModel control = null;
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM engine_control WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            control = new EngineControlModel
                            {
                                Id = reader.GetInt32("id"),
                                TextRu = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                                TextEng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                                TextGer = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger")
                            };
                        }
                    }
                }
            }
            return control;
        }

        private GraficModel GetGraficByIdSync(int id)
        {
            GraficModel grafic = null;
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT id, name, name_eng, name_ger, image, 
                                description_ru, description_eng, description_ger 
                        FROM grafic WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            grafic = new GraficModel
                            {
                                Id = reader.GetInt32("id"),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                NameEng = reader.IsDBNull(reader.GetOrdinal("name_eng")) ? "" : reader.GetString("name_eng"),
                                NameGer = reader.IsDBNull(reader.GetOrdinal("name_ger")) ? "" : reader.GetString("name_ger"),
                                Image = reader.IsDBNull(reader.GetOrdinal("image")) ? "" : reader.GetString("image"),
                                DescriptionRu = reader.IsDBNull(reader.GetOrdinal("description_ru")) ? "" : reader.GetString("description_ru"),
                                DescriptionEng = reader.IsDBNull(reader.GetOrdinal("description_eng")) ? "" : reader.GetString("description_eng"),
                                DescriptionGer = reader.IsDBNull(reader.GetOrdinal("description_ger")) ? "" : reader.GetString("description_ger")
                            };
                        }
                    }
                }
            }
            return grafic;
        }

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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        // ==========================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ==========================================

        private string GetTemplateTable(string type)
        {
            return type switch
            {
                "about" => "template_about",
                "result" => "template_result",
                "engine" => "template_engine_control",
                "price" => "template_price",
                "grafic" => "template_grafic",
                "additional" => "template_additional_prices",
                _ => ""
            };
        }

        private string GetRecordTable(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return ""; // ← Возвращаем пустую строку вместо исключения
            }
            return type switch
            {
                "about" => "about",
                "result" => "result",
                "engine" => "engine_control",
                "price" => "price",
                "grafic" => "grafic",
                "additional" => "additional_prices",
                _ => ""
            };
        }

        // HomeController.EditCarBlocks.cs — добавить методы

        /// <summary>
        /// Обновляет порядок ID в строке
        /// </summary>
        private string ReorderIdsString(string idsString, List<int> newOrder)
        {
            if (string.IsNullOrEmpty(idsString))
                return "";

            var existingIds = ParseIdList(idsString);
            var ordered = newOrder.Where(id => existingIds.Contains(id)).ToList();

            // Добавляем ID, которые есть в существующем списке, но не были переданы
            foreach (var id in existingIds)
            {
                if (!ordered.Contains(id))
                    ordered.Add(id);
            }

            return string.Join(",", ordered);
        }

        /// <summary>
        /// Сохраняет порядок записей в блоке автомобиля
        /// </summary>
        [HttpPost]
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
        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        // ============================================
        // ДОПОЛНИТЕЛЬНЫЕ ЦЕНЫ - ПРИВЯЗКА ШАБЛОНА
        // ============================================

        [HttpPost]
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



        public class MoveItemRequest
        {
            public int CarId { get; set; }
            public string Block { get; set; }
            public int ItemId { get; set; }
            public string Direction { get; set; }
        }

        [HttpPost]
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

        // ==========================================
        // КЛАСС ДЛЯ ЗАПРОСА ПЕРЕМЕЩЕНИЯ В ШАБЛОНЕ
        // ==========================================
        public class TemplateMoveItemRequest
        {
            public string Type { get; set; }
            public int TemplateId { get; set; }
            public int ItemId { get; set; }
            public string Direction { get; set; }
            public string ReturnTab { get; set; }
        }

        // ==========================================
        // ПЕРЕМЕЩЕНИЕ ЗАПИСИ В ШАБЛОНЕ (ВВЕРХ/ВНИЗ)
        // ==========================================
        [HttpPost]
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

        [HttpPost]
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