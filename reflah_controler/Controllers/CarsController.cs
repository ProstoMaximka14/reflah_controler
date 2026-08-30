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
    public partial class CarsController : AppController
    {
        public CarsController(IConfiguration configuration, IHubContext<DatabaseHub> hubContext)
            : base(configuration, hubContext)
        {
        }
        // ============================================
        // РђР’РўРћРњРћР‘РР›Р
        // ============================================

        [HttpGet("Cars")]
        public IActionResult Cars()
        {
            List<ReflashCarModel> cars = GetCarsFromDatabase();
            return View(cars);
        }

        [HttpGet("EditCar/{id?}")]
        public async Task<IActionResult> EditCar(int id)
        {
            ReflashCarModel car = GetCarById(id);
            if (car == null)
            {
                TempData["Error"] = "РђРІС‚РѕРјРѕР±РёР»СЊ РЅРµ РЅР°Р№РґРµРЅ";
                return RedirectToAction("Cars");
            }

            // ===== РџРћР›РЈР§РђР•Рњ Р’РЎР• ID РћРџР¦РР™ РР— РњРђРЁРРќР« (РІРєР»СЋС‡Р°СЏ С€Р°Р±Р»РѕРЅС‹) =====
            var allPriceIds = await GetAllPriceIdsFromCarAsync(car);
            var allPrices = GetPricesFromDatabase();
            var filteredPrices = allPrices.Where(p => allPriceIds.Contains(p.Id)).ToList();

            ViewBag.AllPrices = filteredPrices;

            var vm = await BuildEditCarViewModelAsync(car);
            return View(vm);
        }

        private async Task<List<int>> GetAllPriceIdsFromCarAsync(ReflashCarModel car)
        {
            var result = new List<int>();

            if (string.IsNullOrEmpty(car.PriceRu))
                return result;

            // РџРѕР»СѓС‡Р°РµРј ID РёР· СЃС‚СЂРѕРєРё
            var ids = ParseIdList(car.PriceRu);
            result.AddRange(ids);

            // РџСЂРѕРІРµСЂСЏРµРј, РµСЃС‚СЊ Р»Рё СЃСЂРµРґРё РЅРёС… С€Р°Р±Р»РѕРЅС‹
            string connectionString = GetConnectionString();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var id in ids)
                {
                    string sourceTable = null;
                    using (var cmd = new MySqlCommand("SELECT source_table FROM global_ids WHERE id = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        sourceTable = (await cmd.ExecuteScalarAsync())?.ToString();
                    }

                    if (sourceTable == "template_price")
                    {
                        // Р­С‚Рѕ С€Р°Р±Р»РѕРЅ вЂ” РїРѕР»СѓС‡Р°РµРј РІСЃРµ ID РёР· РЅРµРіРѕ
                        string linkedIds = null;
                        using (var cmd = new MySqlCommand("SELECT prices FROM template_price WHERE id = @id", connection))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            linkedIds = (await cmd.ExecuteScalarAsync())?.ToString();
                        }

                        if (!string.IsNullOrEmpty(linkedIds))
                        {
                            var linkedPriceIds = ParseIdList(linkedIds);
                            result.AddRange(linkedPriceIds);
                        }
                    }
                }
            }

            return result.Distinct().ToList();
        }

        [HttpGet("CreateCar")]
        public async Task<IActionResult> CreateCar()
        {
            ViewBag.AllAbouts = GetAboutsFromDatabase();
            ViewBag.AllResults = GetResultsFromDatabase();
            ViewBag.AllEngineControls = GetEngineControlsFromDatabase();
            ViewBag.AllPrices = GetPricesFromDatabase();
            ViewBag.AllGrafics = GetGraficsFromDatabase();

            return View("CreateCar", new ReflashCarModel());
        }

        private bool CarExists(string brand, string model, string generation, string engine, int? excludeId = null)
        {
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM reflash_cars WHERE brand = @brand AND model = @model AND generation = @generation AND engine = @engine";
                if (excludeId.HasValue)
                {
                    query += " AND id != @excludeId";
                }

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@brand", brand ?? "");
                    command.Parameters.AddWithValue("@model", model ?? "");
                    command.Parameters.AddWithValue("@generation", generation ?? "");
                    command.Parameters.AddWithValue("@engine", engine ?? "");
                    if (excludeId.HasValue)
                    {
                        command.Parameters.AddWithValue("@excludeId", excludeId.Value);
                    }

                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        [HttpPost("UpdateCar")]
        public async Task<IActionResult> UpdateCar(
    int Id,
    string Brand,
    string Model,
    string Generation,
    string Engine,
    IFormFile imageFile,
    bool removeImage = false,
    string AboutRu = "",
    string ResultRu = "",
    string EngineControlRu = "",
    string PriceRu = "",
    string grafic = "",
    string additional_price_ru = "",
    int SortOrder = 0)
        {
            string connectionString = GetConnectionString();

            var existingCar = GetCarById(Id);
            if (existingCar == null)
            {
                TempData["Error"] = "РђРІС‚РѕРјРѕР±РёР»СЊ РЅРµ РЅР°Р№РґРµРЅ";
                return RedirectToAction("Cars");
            }

            // ============================================================
            // РћР‘Р РђР‘РћРўРљРђ РР—РћР‘Р РђР–Р•РќРРЇ
            // ============================================================
            string imagePath = existingCar.Image ?? "";

            try
            {
                if (removeImage)
                {
                    imagePath = "";
                    if (existingCar != null && !string.IsNullOrEmpty(existingCar.Image) && !existingCar.Image.StartsWith("http"))
                    {
                        var oldFile = Path.Combine(_sharedUploadsPath, "cars", existingCar.Image);
                        if (System.IO.File.Exists(oldFile))
                        {
                            try { System.IO.File.Delete(oldFile); } catch { }
                        }
                    }
                }
                else if (imageFile != null && imageFile.Length > 0)
                {
                    var carsPath = Path.Combine(_sharedUploadsPath, "cars");
                    if (!Directory.Exists(carsPath))
                    {
                        Directory.CreateDirectory(carsPath);
                    }

                    var ext = Path.GetExtension(imageFile.FileName).ToLower();
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    if (!allowed.Contains(ext))
                    {
                        TempData["Error"] = "РќРµРїРѕРґРґРµСЂР¶РёРІР°РµРјС‹Р№ С„РѕСЂРјР°С‚ С„Р°Р№Р»Р°. Р Р°Р·СЂРµС€РµРЅС‹: JPG, PNG, GIF, WebP";
                        return RedirectToAction("EditCar", new { id = Id });
                    }

                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Р¤Р°Р№Р» СЃР»РёС€РєРѕРј Р±РѕР»СЊС€РѕР№. РњР°РєСЃРёРјР°Р»СЊРЅС‹Р№ СЂР°Р·РјРµСЂ: 5MB";
                        return RedirectToAction("EditCar", new { id = Id });
                    }

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(carsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    if (existingCar != null && !string.IsNullOrEmpty(existingCar.Image) && !existingCar.Image.StartsWith("http"))
                    {
                        var oldFile = Path.Combine(carsPath, existingCar.Image);
                        if (System.IO.File.Exists(oldFile))
                        {
                            try { System.IO.File.Delete(oldFile); } catch { }
                        }
                    }

                    imagePath = fileName;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° РїСЂРё СЃРѕС…СЂР°РЅРµРЅРёРё С„Р°Р№Р»Р°: {ex.Message}";
                return RedirectToAction("EditCar", new { id = Id });
            }

            // ============================================================
            // РћР‘Р РђР‘РћРўРљРђ SORT_ORDER вЂ” РџРћР›РќРђРЇ РџР•Р Р•РќРЈРњР•Р РђР¦РРЇ Р“Р РЈРџРџР«
            // ============================================================
            int finalSortOrder = SortOrder;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // РџРѕР»СѓС‡Р°РµРј РІСЃРµ РјР°С€РёРЅС‹ РІ СЌС‚РѕР№ РіСЂСѓРїРїРµ (РІРєР»СЋС‡Р°СЏ С‚РµРєСѓС‰СѓСЋ)
                    string getGroupQuery = @"SELECT id, sort_order FROM reflash_cars 
                                     WHERE brand = @brand AND model = @model AND generation = @generation 
                                     ORDER BY sort_order, id";
                    var groupCars = new List<(int Id, int SortOrder)>();

                    using (var cmd = new MySqlCommand(getGroupQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@brand", Brand ?? "");
                        cmd.Parameters.AddWithValue("@model", Model ?? "");
                        cmd.Parameters.AddWithValue("@generation", Generation ?? "");
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                groupCars.Add((reader.GetInt32("id"), reader.GetInt32("sort_order")));
                            }
                        }
                    }

                    // Р•СЃР»Рё РІ РіСЂСѓРїРїРµ РµСЃС‚СЊ РґСЂСѓРіРёРµ РјР°С€РёРЅС‹
                    if (groupCars.Count > 0)
                    {
                        // РќР°С…РѕРґРёРј С‚РµРєСѓС‰СѓСЋ РјР°С€РёРЅСѓ РІ СЃРїРёСЃРєРµ
                        var currentCar = groupCars.FirstOrDefault(c => c.Id == Id);
                        int currentSortOrder = currentCar.SortOrder;

                        // РљРѕСЂСЂРµРєС‚РёСЂСѓРµРј SortOrder
                        if (SortOrder < 1)
                        {
                            finalSortOrder = 1;
                        }
                        else if (SortOrder > groupCars.Count)
                        {
                            finalSortOrder = groupCars.Count;
                        }

                        // ===== РџРћР›РќРђРЇ РџР•Р Р•РќРЈРњР•Р РђР¦РРЇ Р“Р РЈРџРџР« =====
                        // РЈРґР°Р»СЏРµРј С‚РµРєСѓС‰СѓСЋ РјР°С€РёРЅСѓ РёР· СЃРїРёСЃРєР°
                        groupCars = groupCars.Where(c => c.Id != Id).ToList();

                        // Р’СЃС‚Р°РІР»СЏРµРј С‚РµРєСѓС‰СѓСЋ РјР°С€РёРЅСѓ РЅР° РЅРѕРІСѓСЋ РїРѕР·РёС†РёСЋ
                        if (finalSortOrder <= groupCars.Count)
                        {
                            groupCars.Insert(finalSortOrder - 1, (Id, finalSortOrder));
                        }
                        else
                        {
                            groupCars.Add((Id, finalSortOrder));
                        }

                        // РџРµСЂРµРЅСѓРјРµСЂРѕРІС‹РІР°РµРј РІСЃРµ РјР°С€РёРЅС‹ РІ РіСЂСѓРїРїРµ (РЅР°С‡РёРЅР°СЏ СЃ 1)
                        for (int i = 0; i < groupCars.Count; i++)
                        {
                            int newOrder = i + 1;
                            string updateQuery = "UPDATE reflash_cars SET sort_order = @sort_order WHERE id = @id";
                            using (var updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@sort_order", newOrder);
                                updateCmd.Parameters.AddWithValue("@id", groupCars[i].Id);
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // РћР±РЅРѕРІР»СЏРµРј finalSortOrder РґР»СЏ РІРѕР·РІСЂР°С‚Р°
                        finalSortOrder = groupCars.First(c => c.Id == Id).SortOrder;
                    }
                    else
                    {
                        // Р•СЃР»Рё РІ РіСЂСѓРїРїРµ С‚РѕР»СЊРєРѕ РѕРґРЅР° РјР°С€РёРЅР° вЂ” СЃС‚Р°РІРёРј 1
                        finalSortOrder = 1;
                    }

                    // ============================================================
                    // РћР‘РќРћР’Р›РЇР•Рњ РћРЎРўРђР›Р¬РќР«Р• РџРћР›РЇ Р—РђРџРРЎР
                    // ============================================================
                    string query = @"UPDATE reflash_cars SET 
                brand = @brand, 
                model = @model, 
                generation = @generation, 
                engine = @engine, 
                image = @image,
                about_ru = @about_ru,
                result_ru = @result_ru,
                engine_control_ru = @engine_control_ru,
                price_ru = @price_ru,
                grafic = @grafic,
                additional_price_ru = @additional_price_ru
                WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", Id);
                        command.Parameters.AddWithValue("@brand", Brand ?? "");
                        command.Parameters.AddWithValue("@model", Model ?? "");
                        command.Parameters.AddWithValue("@generation", Generation ?? "");
                        command.Parameters.AddWithValue("@engine", Engine ?? "");
                        command.Parameters.AddWithValue("@image", imagePath ?? "");
                        command.Parameters.AddWithValue("@about_ru", AboutRu ?? "");
                        command.Parameters.AddWithValue("@result_ru", ResultRu ?? "");
                        command.Parameters.AddWithValue("@engine_control_ru", EngineControlRu ?? "");
                        command.Parameters.AddWithValue("@price_ru", PriceRu ?? "");
                        command.Parameters.AddWithValue("@grafic", grafic ?? "");
                        command.Parameters.AddWithValue("@additional_price_ru", additional_price_ru ?? "");

                        await command.ExecuteNonQueryAsync();
                    }
                }

                await NotifyReaderSite();
                TempData["Message"] = $"РђРІС‚РѕРјРѕР±РёР»СЊ СѓСЃРїРµС€РЅРѕ РѕР±РЅРѕРІР»РµРЅ. РџРѕСЂСЏРґРѕРє: {finalSortOrder}";
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL РїСЂРё РѕР±РЅРѕРІР»РµРЅРёРё: {ex.Message}";
                return RedirectToAction("EditCar", new { id = Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР°: {ex.Message}";
                return RedirectToAction("EditCar", new { id = Id });
            }

            return RedirectToAction("EditCar", new { id = Id });
        }

        [HttpPost("DeleteCar")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            string connectionString = GetConnectionString();

            try
            {
                // РЎРЅР°С‡Р°Р»Р° РїРѕР»СѓС‡Р°РµРј РґР°РЅРЅС‹Рµ СѓРґР°Р»СЏРµРјРѕР№ РјР°С€РёРЅС‹
                ReflashCarModel carToDelete = GetCarById(id);

                if (carToDelete == null)
                {
                    TempData["Error"] = "РђРІС‚РѕРјРѕР±РёР»СЊ РЅРµ РЅР°Р№РґРµРЅ";
                    return RedirectToAction("Cars");
                }

                string brand = carToDelete.Brand;
                string model = carToDelete.Model;
                string generation = carToDelete.Generation;

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // РЈРґР°Р»СЏРµРј РјР°С€РёРЅСѓ
                    string deleteQuery = "DELETE FROM reflash_cars WHERE id = @id";
                    using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", id);
                        int rowsAffected = await deleteCmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            // ===== РџР•Р Р•РќРЈРњР•Р РћР’Р«Р’РђР•Рњ РћРЎРўРђР’РЁРР•РЎРЇ РњРђРЁРРќР« Р’ Р“Р РЈРџРџР• =====
                            // РџРѕР»СѓС‡Р°РµРј РІСЃРµ ID РѕСЃС‚Р°РІС€РёС…СЃСЏ РјР°С€РёРЅ РІ РіСЂСѓРїРїРµ, РѕС‚СЃРѕСЂС‚РёСЂРѕРІР°РЅРЅС‹Рµ РїРѕ sort_order
                            string getIdsQuery = @"SELECT id FROM reflash_cars 
                                           WHERE brand = @brand AND model = @model AND generation = @generation
                                           ORDER BY sort_order, id";
                            var remainingIds = new List<int>();

                            using (var getCmd = new MySqlCommand(getIdsQuery, connection))
                            {
                                getCmd.Parameters.AddWithValue("@brand", brand);
                                getCmd.Parameters.AddWithValue("@model", model);
                                getCmd.Parameters.AddWithValue("@generation", generation);
                                using (var reader = await getCmd.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        remainingIds.Add(reader.GetInt32("id"));
                                    }
                                }
                            }

                            // РћР±РЅРѕРІР»СЏРµРј sort_order РґР»СЏ РєР°Р¶РґРѕР№ РѕСЃС‚Р°РІС€РµР№СЃСЏ РјР°С€РёРЅС‹ (РЅР°С‡РёРЅР°СЏ СЃ 1)
                            for (int i = 0; i < remainingIds.Count; i++)
                            {
                                string updateQuery = "UPDATE reflash_cars SET sort_order = @sort_order WHERE id = @id";
                                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@sort_order", i + 1);
                                    updateCmd.Parameters.AddWithValue("@id", remainingIds[i]);
                                    await updateCmd.ExecuteNonQueryAsync();
                                }
                            }

                            TempData["Message"] = $"РђРІС‚РѕРјРѕР±РёР»СЊ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ, РїРѕСЂСЏРґРѕРє РІ РіСЂСѓРїРїРµ РѕР±РЅРѕРІР»С‘РЅ ({remainingIds.Count} РјР°С€РёРЅ)";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "РђРІС‚РѕРјРѕР±РёР»СЊ РЅРµ РЅР°Р№РґРµРЅ";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL РїСЂРё СѓРґР°Р»РµРЅРёРё: {ex.Message}";
            }

            return RedirectToAction("Cars");
        }

        [HttpPost("AddCar")]
        public async Task<IActionResult> AddCar(ReflashCarModel car, IFormFile imageFile)
        {
            if (CarExists(car.Brand, car.Model, car.Generation, car.Engine))
            {
                TempData["Error"] = "РђРІС‚РѕРјРѕР±РёР»СЊ СЃ С‚Р°РєРёРјРё РґР°РЅРЅС‹РјРё СѓР¶Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚!";
                return RedirectToAction("CreateCar");
            }

            string imagePath = "";

            if (imageFile != null && imageFile.Length > 0)
            {
                var carsPath = Path.Combine(_sharedUploadsPath, "cars");
                if (!Directory.Exists(carsPath))
                    Directory.CreateDirectory(carsPath);

                var ext = Path.GetExtension(imageFile.FileName).ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowed.Contains(ext))
                {
                    TempData["Error"] = "РќРµРїРѕРґРґРµСЂР¶РёРІР°РµРјС‹Р№ С„РѕСЂРјР°С‚ С„Р°Р№Р»Р°";
                    return RedirectToAction("CreateCar");
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Р¤Р°Р№Р» СЃР»РёС€РєРѕРј Р±РѕР»СЊС€РѕР№ (РјР°РєСЃ. 5MB)";
                    return RedirectToAction("CreateCar");
                }

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(carsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                imagePath = fileName;
            }

            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    int maxSortOrder = 0;
                    string maxQuery = @"SELECT COALESCE(MAX(sort_order), 0) 
                                FROM reflash_cars 
                                WHERE brand = @brand AND model = @model AND generation = @generation";
                    string query = @"INSERT INTO reflash_cars (
                        brand, model, generation, engine, image,
                        about_ru, result_ru, engine_control_ru, price_ru, grafic,
                        additional_price_ru
                    ) VALUES (
                        @brand, @model, @generation, @engine, @image,
                        @about_ru, @result_ru, @engine_control_ru, @price_ru, @grafic,
                        @additional_price_ru
                    );";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@brand", car.Brand ?? "");
                        command.Parameters.AddWithValue("@model", car.Model ?? "");
                        command.Parameters.AddWithValue("@generation", car.Generation ?? "");
                        command.Parameters.AddWithValue("@engine", car.Engine ?? "");
                        command.Parameters.AddWithValue("@image", imagePath ?? "");
                        command.Parameters.AddWithValue("@about_ru", car.AboutRu ?? "");
                        command.Parameters.AddWithValue("@result_ru", car.ResultRu ?? "");
                        command.Parameters.AddWithValue("@engine_control_ru", car.EngineControlRu ?? "");
                        command.Parameters.AddWithValue("@price_ru", car.PriceRu ?? "");
                        command.Parameters.AddWithValue("@grafic", car.grafic ?? "");
                        command.Parameters.AddWithValue("@additional_price_ru", car.additional_price_ru ?? "");

                        command.Parameters.AddWithValue("@sort_order", maxSortOrder + 1);

                        long newId = Convert.ToInt64(await command.ExecuteScalarAsync());

                        TempData["Message"] = $"РђРІС‚РѕРјРѕР±РёР»СЊ {car.Brand} {car.Model} СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                        await NotifyReaderSite();

                        return RedirectToAction("EditCar", new { id = newId });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР°: {ex.Message}";
                return RedirectToAction("CreateCar");
            }
        }

        [HttpPost("UpdateCarOldUrl")]
        public async Task<IActionResult> UpdateCarOldUrl(int id, string old_url)
        {
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "UPDATE reflash_cars SET old_url = @old_url WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@old_url", old_url ?? "");
                        await command.ExecuteNonQueryAsync();
                    }
                }

                await NotifyReaderSite();
                TempData["Message"] = "РЎС‚Р°СЂС‹Р№ URL СѓСЃРїРµС€РЅРѕ РѕР±РЅРѕРІР»С‘РЅ";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° РїСЂРё РѕР±РЅРѕРІР»РµРЅРёРё СЃС‚Р°СЂРѕРіРѕ URL: {ex.Message}";
            }

            return RedirectToAction("EditCar", new { id = id });
        }

        [HttpPost("ReorderGenerations")]
        public async Task<IActionResult> ReorderGenerations([FromBody] ReorderGenerationsRequest request)
        {
            if (request?.Generations == null || request.Generations.Count == 0)
                return Json(new { success = false, message = "Нет данных" });

            try
            {
                string connectionString = GetConnectionString();
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                for (int i = 0; i < request.Generations.Count; i++)
                {
                    string query = @"
                UPDATE reflash_cars 
                SET SortOrder2 = @sort_order 
                WHERE brand = @brand AND model = @model AND generation = @generation";

                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@sort_order", i);
                    cmd.Parameters.AddWithValue("@brand", request.Brand ?? "");
                    cmd.Parameters.AddWithValue("@model", request.Model ?? "");
                    cmd.Parameters.AddWithValue("@generation", request.Generations[i] ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }

                await NotifyReaderSite();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
