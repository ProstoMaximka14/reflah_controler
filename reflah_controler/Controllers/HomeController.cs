using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using reflah_controler.Hubs;
using reflah_controler.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

namespace reflah_controler.Controllers
{
    //[Authorize]//
    public partial class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHubContext<DatabaseHub> _hubContext;
        private readonly string _sharedUploadsPath;
        private readonly string _readerSiteUrl;

        public HomeController(IConfiguration configuration, IHubContext<DatabaseHub> hubContext)
        {
            _configuration = configuration;
            _hubContext = hubContext;
            _sharedUploadsPath = _configuration["UploadSettings:SharedUploadsPath"] ?? @"C:\fotos";
            _readerSiteUrl = _configuration["ReaderSite:Url"] ?? "http://localhost:80";
        }

        // ============================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ============================================

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";
        }

        private async Task NotifyReaderSite()
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

        // ============================================
        // ГЛОБАЛЬНЫЕ ID (global_ids)
        // ============================================

        private int CreateGlobalId(string sourceTable)
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

        private void DeleteGlobalId(int id)
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

        private bool GlobalIdExists(int id)
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

        private string GetGlobalIdSourceTable(int id)
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

        // ============================================
        // АДМИНИСТРАТОРЫ
        // ============================================

        public IActionResult Index()
        {
            List<AdminsModel> admins = GetAdminsFromDatabase();
            return View(admins);
        }

        [HttpPost]
        public IActionResult UpdateAdmin(int id, string login, string password)
        {
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE admins SET login = @login, password = @password WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);
                        command.Parameters.AddWithValue("@id", id);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = "Администратор успешно обновлен";
                        }
                        else
                        {
                            TempData["Error"] = "Администратор не найден";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DeleteAdmin(int id)
        {
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM admins WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = "Администратор успешно удален";
                        }
                        else
                        {
                            TempData["Error"] = "Администратор не найден";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult AddAdmin(string login, string password)
        {
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO admins (login, password) VALUES (@login, @password)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = "Администратор успешно добавлен";
                        }
                        else
                        {
                            TempData["Error"] = "Не удалось добавить администратора";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        private List<AdminsModel> GetAdminsFromDatabase()
        {
            List<AdminsModel> admins = new List<AdminsModel>();
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM admins ORDER BY id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                admins.Add(new AdminsModel
                                {
                                    Id = reader.GetInt32("id"),
                                    Login = reader.GetString("login"),
                                    Password = reader.GetString("password")
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return admins;
        }

        // ============================================
        // АВТОМОБИЛИ
        // ============================================

        public IActionResult Cars()
        {
            List<ReflashCarModel> cars = GetCarsFromDatabase();
            return View(cars);
        }

        public async Task<IActionResult> EditCar(int id)
        {
            ReflashCarModel car = GetCarById(id);
            if (car == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("Cars");
            }

            // ===== ПОЛУЧАЕМ ВСЕ ID ОПЦИЙ ИЗ МАШИНЫ (включая шаблоны) =====
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

            // Получаем ID из строки
            var ids = ParseIdList(car.PriceRu);
            result.AddRange(ids);

            // Проверяем, есть ли среди них шаблоны
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
                        // Это шаблон — получаем все ID из него
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

        [HttpPost]
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
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("Cars");
            }

            // ============================================================
            // ОБРАБОТКА ИЗОБРАЖЕНИЯ
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
                        TempData["Error"] = "Неподдерживаемый формат файла. Разрешены: JPG, PNG, GIF, WebP";
                        return RedirectToAction("EditCar", new { id = Id });
                    }

                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Файл слишком большой. Максимальный размер: 5MB";
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
                TempData["Error"] = $"Ошибка при сохранении файла: {ex.Message}";
                return RedirectToAction("EditCar", new { id = Id });
            }

            // ============================================================
            // ОБРАБОТКА SORT_ORDER — ПОЛНАЯ ПЕРЕНУМЕРАЦИЯ ГРУППЫ
            // ============================================================
            int finalSortOrder = SortOrder;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Получаем все машины в этой группе (включая текущую)
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

                    // Если в группе есть другие машины
                    if (groupCars.Count > 0)
                    {
                        // Находим текущую машину в списке
                        var currentCar = groupCars.FirstOrDefault(c => c.Id == Id);
                        int currentSortOrder = currentCar.SortOrder;

                        // Корректируем SortOrder
                        if (SortOrder < 1)
                        {
                            finalSortOrder = 1;
                        }
                        else if (SortOrder > groupCars.Count)
                        {
                            finalSortOrder = groupCars.Count;
                        }

                        // ===== ПОЛНАЯ ПЕРЕНУМЕРАЦИЯ ГРУППЫ =====
                        // Удаляем текущую машину из списка
                        groupCars = groupCars.Where(c => c.Id != Id).ToList();

                        // Вставляем текущую машину на новую позицию
                        if (finalSortOrder <= groupCars.Count)
                        {
                            groupCars.Insert(finalSortOrder - 1, (Id, finalSortOrder));
                        }
                        else
                        {
                            groupCars.Add((Id, finalSortOrder));
                        }

                        // Перенумеровываем все машины в группе (начиная с 1)
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

                        // Обновляем finalSortOrder для возврата
                        finalSortOrder = groupCars.First(c => c.Id == Id).SortOrder;
                    }
                    else
                    {
                        // Если в группе только одна машина — ставим 1
                        finalSortOrder = 1;
                    }

                    // ============================================================
                    // ОБНОВЛЯЕМ ОСТАЛЬНЫЕ ПОЛЯ ЗАПИСИ
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
                TempData["Message"] = $"Автомобиль успешно обновлен. Порядок: {finalSortOrder}";
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при обновлении: {ex.Message}";
                return RedirectToAction("EditCar", new { id = Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
                return RedirectToAction("EditCar", new { id = Id });
            }

            return RedirectToAction("EditCar", new { id = Id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCar(int id)
        {
            string connectionString = GetConnectionString();

            try
            {
                // Сначала получаем данные удаляемой машины
                ReflashCarModel carToDelete = GetCarById(id);

                if (carToDelete == null)
                {
                    TempData["Error"] = "Автомобиль не найден";
                    return RedirectToAction("Cars");
                }

                string brand = carToDelete.Brand;
                string model = carToDelete.Model;
                string generation = carToDelete.Generation;

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Удаляем машину
                    string deleteQuery = "DELETE FROM reflash_cars WHERE id = @id";
                    using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCmd.Parameters.AddWithValue("@id", id);
                        int rowsAffected = await deleteCmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            // ===== ПЕРЕНУМЕРОВЫВАЕМ ОСТАВШИЕСЯ МАШИНЫ В ГРУППЕ =====
                            // Получаем все ID оставшихся машин в группе, отсортированные по sort_order
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

                            // Обновляем sort_order для каждой оставшейся машины (начиная с 1)
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

                            TempData["Message"] = $"Автомобиль успешно удален, порядок в группе обновлён ({remainingIds.Count} машин)";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Автомобиль не найден";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при удалении: {ex.Message}";
            }

            return RedirectToAction("Cars");
        }

        [HttpPost]
        public async Task<IActionResult> AddCar(ReflashCarModel car, IFormFile imageFile)
        {
            if (CarExists(car.Brand, car.Model, car.Generation, car.Engine))
            {
                TempData["Error"] = "Автомобиль с такими данными уже существует!";
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
                    TempData["Error"] = "Неподдерживаемый формат файла";
                    return RedirectToAction("CreateCar");
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Файл слишком большой (макс. 5MB)";
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

                        TempData["Message"] = $"Автомобиль {car.Brand} {car.Model} успешно добавлен";
                        await NotifyReaderSite();

                        return RedirectToAction("EditCar", new { id = newId });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
                return RedirectToAction("CreateCar");
            }
        }

        private List<ReflashCarModel> GetCarsFromDatabase()
        {
            List<ReflashCarModel> cars = new List<ReflashCarModel>();
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT id, brand, model, generation, engine, image, 
                            about_ru, result_ru, engine_control_ru, price_ru, grafic,
                            additional_price_ru, old_url, sort_order
                     FROM reflash_cars ORDER BY brand, model, generation";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32("id");
                                string brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? "" : reader.GetString("brand");
                                string model = reader.IsDBNull(reader.GetOrdinal("model")) ? "" : reader.GetString("model");
                                string generation = reader.IsDBNull(reader.GetOrdinal("generation")) ? "" : reader.GetString("generation");
                                int sortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order");

                                
                                if (sortOrder == 0)
                                {
                                    sortOrder = GetNextSortOrderAndUpdate(cars, brand, model, generation, id);
                                }

                                cars.Add(new ReflashCarModel
                                {
                                    Id = reader.GetInt32("id"),
                                    Brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? "" : reader.GetString("brand"),
                                    Model = reader.IsDBNull(reader.GetOrdinal("model")) ? "" : reader.GetString("model"),
                                    Generation = reader.IsDBNull(reader.GetOrdinal("generation")) ? "" : reader.GetString("generation"),
                                    Engine = reader.IsDBNull(reader.GetOrdinal("engine")) ? "" : reader.GetString("engine"),
                                    Image = reader.IsDBNull(reader.GetOrdinal("image")) ? "" : reader.GetString("image"),
                                    AboutRu = reader.IsDBNull(reader.GetOrdinal("about_ru")) ? "" : reader.GetString("about_ru"),
                                    ResultRu = reader.IsDBNull(reader.GetOrdinal("result_ru")) ? "" : reader.GetString("result_ru"),
                                    EngineControlRu = reader.IsDBNull(reader.GetOrdinal("engine_control_ru")) ? "" : reader.GetString("engine_control_ru"),
                                    PriceRu = reader.IsDBNull(reader.GetOrdinal("price_ru")) ? "" : reader.GetString("price_ru"),
                                    grafic = reader.IsDBNull(reader.GetOrdinal("grafic")) ? "" : reader.GetString("grafic"),
                                    additional_price_ru = reader.IsDBNull(reader.GetOrdinal("additional_price_ru")) ? "" : reader.GetString("additional_price_ru"),
                                    old_url = reader.IsDBNull(reader.GetOrdinal("old_url")) ? "" : reader.GetString("old_url"),
                                    SortOrder = sortOrder
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при загрузке автомобилей: {ex.Message}";
            }

            return cars;
        }

        private int GetNextSortOrderAndUpdate(List<ReflashCarModel> cars, string brand, string model, string generation, int carId)
        {
            // Ищем максимальный sort_order среди уже добавленных машин с такими же brand, model, generation
            int maxOrder = 0;

            foreach (var car in cars)
            {
                if (car.Brand == brand && car.Model == model && car.Generation == generation)
                {
                    if (car.SortOrder > maxOrder)
                    {
                        maxOrder = car.SortOrder;
                    }
                }
            }

            // Новый sort_order = maxOrder + 1
            int newSortOrder = maxOrder + 1;

            // ОБНОВЛЯЕМ ЗНАЧЕНИЕ В БАЗЕ ДАННЫХ
            try
            {
                string connectionString = GetConnectionString();
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE reflash_cars SET sort_order = @sort_order WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@sort_order", newSortOrder);
                        command.Parameters.AddWithValue("@id", carId);
                        command.ExecuteNonQuery();
                    }
                }
                Console.WriteLine($"✅ Обновлён sort_order для машины #{carId}: {newSortOrder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при обновлении sort_order для машины #{carId}: {ex.Message}");
            }

            return newSortOrder;
        }

        private ReflashCarModel GetCarById(int id)
        {
            ReflashCarModel car = null;
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT id, brand, model, generation, engine, image, 
                                    about_ru, result_ru, engine_control_ru, price_ru, grafic,
                                    additional_price_ru, old_url, sort_order
                             FROM reflash_cars WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                car = new ReflashCarModel
                                {
                                    Id = reader.GetInt32("id"),
                                    Brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? "" : reader.GetString("brand"),
                                    Model = reader.IsDBNull(reader.GetOrdinal("model")) ? "" : reader.GetString("model"),
                                    Generation = reader.IsDBNull(reader.GetOrdinal("generation")) ? "" : reader.GetString("generation"),
                                    Engine = reader.IsDBNull(reader.GetOrdinal("engine")) ? "" : reader.GetString("engine"),
                                    Image = reader.IsDBNull(reader.GetOrdinal("image")) ? "" : reader.GetString("image"),
                                    AboutRu = reader.IsDBNull(reader.GetOrdinal("about_ru")) ? "" : reader.GetString("about_ru"),
                                    ResultRu = reader.IsDBNull(reader.GetOrdinal("result_ru")) ? "" : reader.GetString("result_ru"),
                                    EngineControlRu = reader.IsDBNull(reader.GetOrdinal("engine_control_ru")) ? "" : reader.GetString("engine_control_ru"),
                                    PriceRu = reader.IsDBNull(reader.GetOrdinal("price_ru")) ? "" : reader.GetString("price_ru"),
                                    grafic = reader.IsDBNull(reader.GetOrdinal("grafic")) ? "" : reader.GetString("grafic"),
                                    additional_price_ru = reader.IsDBNull(reader.GetOrdinal("additional_price_ru")) ? "" : reader.GetString("additional_price_ru"),
                                    old_url = reader.IsDBNull(reader.GetOrdinal("old_url")) ? "" : reader.GetString("old_url"),
                                    SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order")
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при загрузке автомобиля: {ex.Message}";
            }

            return car;
        }

        // ============================================
        // ПАРТНЁРЫ
        // ============================================

        public IActionResult Partners()
        {
            List<PartnersModel> partners = GetPartnersFromDatabase();
            return View(partners);
        }

        public IActionResult EditPartner(int id)
        {
            PartnersModel partner = GetPartnerById(id);
            if (partner == null)
            {
                TempData["Error"] = "Партнер не найден";
                return RedirectToAction("Partners");
            }
            return View(partner);
        }

        public IActionResult CreatePartner()
        {
            return View("CreatePartner", new PartnersModel());
        }

        private PartnersModel GetPartnerById(int id)
        {
            PartnersModel partner = null;
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT id, name, phone, photo_url, vk_url, website_url, 
                                    city, street, house, longitude, latitude,
                                    vk_group_url, telegram, whatsapp, email, point_name 
                             FROM partners WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var photoUrl = reader.IsDBNull(reader.GetOrdinal("photo_url")) ? "" : reader.GetString("photo_url");
                                string photoFileName = photoUrl;
                                if (!string.IsNullOrEmpty(photoUrl) && photoUrl.Contains("/"))
                                {
                                    photoFileName = Path.GetFileName(photoUrl);
                                }

                                partner = new PartnersModel
                                {
                                    Id = reader.GetInt32("id"),
                                    name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                    phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "" : reader.GetString("phone"),
                                    photo = photoFileName,
                                    vk = reader.IsDBNull(reader.GetOrdinal("vk_url")) ? "" : reader.GetString("vk_url"),
                                    website = reader.IsDBNull(reader.GetOrdinal("website_url")) ? "" : reader.GetString("website_url"),
                                    city = reader.IsDBNull(reader.GetOrdinal("city")) ? "" : reader.GetString("city"),
                                    street = reader.IsDBNull(reader.GetOrdinal("street")) ? "" : reader.GetString("street"),
                                    house = reader.IsDBNull(reader.GetOrdinal("house")) ? "" : reader.GetString("house"),
                                    longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? "" : reader.GetString("longitude"),
                                    latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? "" : reader.GetString("latitude"),
                                    vk_group = reader.IsDBNull(reader.GetOrdinal("vk_group_url")) ? "" : reader.GetString("vk_group_url"),
                                    telegram = reader.IsDBNull(reader.GetOrdinal("telegram")) ? "" : reader.GetString("telegram"),
                                    whatsapp = reader.IsDBNull(reader.GetOrdinal("whatsapp")) ? "" : reader.GetString("whatsapp"),
                                    email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email"),
                                    point_name = reader.IsDBNull(reader.GetOrdinal("point_name")) ? "" : reader.GetString("point_name")
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при загрузке партнера: {ex.Message}";
            }
            return partner;
        }

        private List<PartnersModel> GetPartnersFromDatabase()
        {
            List<PartnersModel> partners = new List<PartnersModel>();
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT id, name, phone, photo_url, vk_url, website_url, 
                                    city, street, house, longitude, latitude,
                                    vk_group_url, telegram, whatsapp, email, point_name 
                             FROM partners ORDER BY name";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var photoUrl = reader.IsDBNull(reader.GetOrdinal("photo_url")) ? "" : reader.GetString("photo_url");
                                string photoFileName = photoUrl;
                                if (!string.IsNullOrEmpty(photoUrl) && photoUrl.Contains("/"))
                                {
                                    photoFileName = Path.GetFileName(photoUrl);
                                }

                                partners.Add(new PartnersModel
                                {
                                    Id = reader.GetInt32("id"),
                                    name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                    phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "" : reader.GetString("phone"),
                                    photo = photoFileName,
                                    vk = reader.IsDBNull(reader.GetOrdinal("vk_url")) ? "" : reader.GetString("vk_url"),
                                    website = reader.IsDBNull(reader.GetOrdinal("website_url")) ? "" : reader.GetString("website_url"),
                                    city = reader.IsDBNull(reader.GetOrdinal("city")) ? "" : reader.GetString("city"),
                                    street = reader.IsDBNull(reader.GetOrdinal("street")) ? "" : reader.GetString("street"),
                                    house = reader.IsDBNull(reader.GetOrdinal("house")) ? "" : reader.GetString("house"),
                                    longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? "" : reader.GetString("longitude"),
                                    latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? "" : reader.GetString("latitude"),
                                    vk_group = reader.IsDBNull(reader.GetOrdinal("vk_group_url")) ? "" : reader.GetString("vk_group_url"),
                                    telegram = reader.IsDBNull(reader.GetOrdinal("telegram")) ? "" : reader.GetString("telegram"),
                                    whatsapp = reader.IsDBNull(reader.GetOrdinal("whatsapp")) ? "" : reader.GetString("whatsapp"),
                                    email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email"),
                                    point_name = reader.IsDBNull(reader.GetOrdinal("point_name")) ? "" : reader.GetString("point_name")
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при загрузке партнеров: {ex.Message}";
            }
            return partners;
        }

        [HttpPost]
        public async Task<IActionResult> AddPartner(
            string name,
            string phone,
            string vk,
            string website,
            IFormFile photoFile,
            string photo,
            string city,
            string street,
            string house,
            string longitude,
            string latitude,
            string vk_group,
            string telegram,
            string whatsapp,
            string email,
            string point_name)
        {
            string connectionString = GetConnectionString();

            try
            {
                string fileName = null;

                if (photoFile != null && photoFile.Length > 0)
                {
                    var partnersPath = Path.Combine(_sharedUploadsPath, "partners");

                    if (!Directory.Exists(partnersPath))
                    {
                        Directory.CreateDirectory(partnersPath);
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(photoFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Разрешены только файлы изображений (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("Partners");
                    }

                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Файл слишком большой (максимум 5MB)";
                        return RedirectToAction("Partners");
                    }

                    fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(partnersPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO partners 
                        (name, phone, photo_url, vk_url, website_url, 
                        city, street, house, longitude, latitude,
                        vk_group_url, telegram, whatsapp, email, point_name) 
                    VALUES 
                        (@name, @phone, @photo_url, @vk_url, @website_url, 
                        @city, @street, @house, @longitude, @latitude,
                        @vk_group, @telegram, @whatsapp, @email, @point_name)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@phone", phone ?? "");
                        command.Parameters.AddWithValue("@photo_url", fileName ?? "");
                        command.Parameters.AddWithValue("@vk_url", vk ?? "");
                        command.Parameters.AddWithValue("@website_url", website ?? "");
                        command.Parameters.AddWithValue("@city", city ?? "");
                        command.Parameters.AddWithValue("@street", street ?? "");
                        command.Parameters.AddWithValue("@house", house ?? "");
                        command.Parameters.AddWithValue("@longitude", longitude ?? "");
                        command.Parameters.AddWithValue("@latitude", latitude ?? "");
                        command.Parameters.AddWithValue("@vk_group", vk_group ?? "");
                        command.Parameters.AddWithValue("@telegram", telegram ?? "");
                        command.Parameters.AddWithValue("@whatsapp", whatsapp ?? "");
                        command.Parameters.AddWithValue("@email", email ?? "");
                        command.Parameters.AddWithValue("@point_name", point_name ?? "");

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Партнер {name} успешно добавлен";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Не удалось добавить партнера";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при добавлении: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при загрузке файла: {ex.Message}";
            }

            return RedirectToAction("Partners");
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePartner(
            int id,
            string name,
            string phone,
            string vk,
            string website,
            IFormFile photoFile,
            string photo,
            string city,
            string street,
            string house,
            string longitude,
            string latitude,
            string vk_group,
            string telegram,
            string whatsapp,
            string email,
            string point_name)
        {
            string connectionString = GetConnectionString();

            try
            {
                string fileName = photo;

                if (photoFile != null && photoFile.Length > 0)
                {
                    var partnersPath = Path.Combine(_sharedUploadsPath, "partners");

                    if (!Directory.Exists(partnersPath))
                    {
                        Directory.CreateDirectory(partnersPath);
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(photoFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Разрешены только файлы изображений (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("Partners");
                    }

                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Файл слишком большой (максимум 5MB)";
                        return RedirectToAction("Partners");
                    }

                    fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(partnersPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(photo))
                    {
                        var oldFilePath = Path.Combine(partnersPath, photo);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try { System.IO.File.Delete(oldFilePath); } catch { }
                        }
                    }
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"UPDATE partners SET 
                        name = @name, 
                        phone = @phone, 
                        photo_url = @photo_url, 
                        vk_url = @vk_url, 
                        website_url = @website_url,
                        city = @city,
                        street = @street,
                        house = @house,
                        longitude = @longitude,
                        latitude = @latitude,
                        vk_group_url = @vk_group,
                        telegram = @telegram,
                        whatsapp = @whatsapp,
                        email = @email,
                        point_name = @point_name
                        WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@phone", phone ?? "");
                        command.Parameters.AddWithValue("@photo_url", fileName ?? "");
                        command.Parameters.AddWithValue("@vk_url", vk ?? "");
                        command.Parameters.AddWithValue("@website_url", website ?? "");
                        command.Parameters.AddWithValue("@city", city ?? "");
                        command.Parameters.AddWithValue("@street", street ?? "");
                        command.Parameters.AddWithValue("@house", house ?? "");
                        command.Parameters.AddWithValue("@longitude", longitude ?? "");
                        command.Parameters.AddWithValue("@latitude", latitude ?? "");
                        command.Parameters.AddWithValue("@vk_group", vk_group ?? "");
                        command.Parameters.AddWithValue("@telegram", telegram ?? "");
                        command.Parameters.AddWithValue("@whatsapp", whatsapp ?? "");
                        command.Parameters.AddWithValue("@email", email ?? "");
                        command.Parameters.AddWithValue("@point_name", point_name ?? "");

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Партнер {name} успешно обновлен";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Партнер не найден";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при обновлении: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при обработке файла: {ex.Message}";
            }

            return RedirectToAction("Partners");
        }

        [HttpPost]
        public async Task<IActionResult> DeletePartner(int id)
        {
            string connectionString = GetConnectionString();

            try
            {
                string fileName = "";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT photo_url FROM partners WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        var result = await command.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            fileName = result.ToString();
                        }
                    }
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM partners WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                var filePath = Path.Combine(_sharedUploadsPath, "partners", fileName);
                                if (System.IO.File.Exists(filePath))
                                {
                                    try { System.IO.File.Delete(filePath); } catch { }
                                }
                            }

                            TempData["Message"] = "Партнер успешно удален";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Партнер не найден";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при удалении: {ex.Message}";
            }

            return RedirectToAction("Partners");
        }

        // ============================================
        // НОВОСТИ
        // ============================================

        public IActionResult News()
        {
            List<NewsModel> news = GetNewsFromDatabase();
            return View(news);
        }

        private List<NewsModel> GetNewsFromDatabase()
        {
            List<NewsModel> news = new List<NewsModel>();
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM news ORDER BY id DESC";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var photoUrl = reader.IsDBNull(reader.GetOrdinal("news_url")) ? "" : reader.GetString("news_url");
                                string photoFileName = photoUrl;
                                if (!string.IsNullOrEmpty(photoUrl) && photoUrl.Contains("/"))
                                {
                                    photoFileName = Path.GetFileName(photoUrl);
                                }

                                news.Add(new NewsModel
                                {
                                    id = reader.GetInt32("id"),
                                    news_name = reader.IsDBNull(reader.GetOrdinal("news_name")) ? "" : reader.GetString("news_name"),
                                    news_text = reader.IsDBNull(reader.GetOrdinal("news_text")) ? "" : reader.GetString("news_text"),
                                    news_date = reader.IsDBNull(reader.GetOrdinal("news_date")) ? "" : reader.GetString("news_date"),
                                    news_url = photoFileName
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при загрузке новостей: {ex.Message}";
            }
            return news;
        }

        [HttpPost]
        public async Task<IActionResult> AddNews(string news_name, string news_text, string news_date, IFormFile photoFile)
        {
            string connectionString = GetConnectionString();

            try
            {
                string fileName = null;

                if (photoFile != null && photoFile.Length > 0)
                {
                    var newsPath = Path.Combine(_sharedUploadsPath, "news");

                    if (!Directory.Exists(newsPath))
                    {
                        Directory.CreateDirectory(newsPath);
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(photoFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Разрешены только файлы изображений (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("News");
                    }

                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Файл слишком большой (максимум 5MB)";
                        return RedirectToAction("News");
                    }

                    fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(newsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO news 
                        (news_name, news_text, news_url, news_date) 
                    VALUES 
                        (@news_name, @news_text, @news_url, @news_date)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@news_name", news_name ?? "");
                        command.Parameters.AddWithValue("@news_text", news_text ?? "");
                        command.Parameters.AddWithValue("@news_url", fileName ?? "");
                        command.Parameters.AddWithValue("@news_date", news_date ?? "");

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Новость '{news_name}' успешно добавлена";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Не удалось добавить новость";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при добавлении: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при загрузке файла: {ex.Message}";
            }

            return RedirectToAction("News");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNews(int id, string news_name, string news_text, string news_date, IFormFile photoFile, string photo)
        {
            string connectionString = GetConnectionString();

            try
            {
                string fileName = photo;

                if (photoFile != null && photoFile.Length > 0)
                {
                    var newsPath = Path.Combine(_sharedUploadsPath, "news");

                    if (!Directory.Exists(newsPath))
                    {
                        Directory.CreateDirectory(newsPath);
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(photoFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Разрешены только файлы изображений (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("News");
                    }

                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Файл слишком большой (максимум 5MB)";
                        return RedirectToAction("News");
                    }

                    fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(newsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(photo))
                    {
                        var oldFilePath = Path.Combine(newsPath, photo);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try { System.IO.File.Delete(oldFilePath); } catch { }
                        }
                    }
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"UPDATE news SET 
                        news_name = @news_name, 
                        news_text = @news_text, 
                        news_url = @news_url, 
                        news_date = @news_date 
                        WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@news_name", news_name ?? "");
                        command.Parameters.AddWithValue("@news_text", news_text ?? "");
                        command.Parameters.AddWithValue("@news_url", fileName ?? "");
                        command.Parameters.AddWithValue("@news_date", news_date ?? "");

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Новость '{news_name}' успешно обновлена";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Новость не найдена";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при обновлении: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при обработке файла: {ex.Message}";
            }

            return RedirectToAction("News");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNews(int id)
        {
            string connectionString = GetConnectionString();

            try
            {
                string fileName = "";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT news_url FROM news WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        var result = await command.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            fileName = result.ToString();
                        }
                    }
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "DELETE FROM news WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                var filePath = Path.Combine(_sharedUploadsPath, "news", fileName);
                                if (System.IO.File.Exists(filePath))
                                {
                                    try { System.IO.File.Delete(filePath); } catch { }
                                }
                            }

                            TempData["Message"] = "Новость успешно удалена";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Новость не найдена";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при удалении: {ex.Message}";
            }

            return RedirectToAction("News");
        }

        // ============================================
        // ГЛАВНАЯ СТРАНИЦА
        // ============================================

        public IActionResult FurstPage()
        {
            FurstPageModel first_page = GetFurstPageFromDatabase();
            return View(first_page);
        }

        private FurstPageModel GetFurstPageFromDatabase()
        {
            FurstPageModel furst_page = null;
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM first_page_content LIMIT 1";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                furst_page = new FurstPageModel
                                {
                                    image_1 = reader.IsDBNull(reader.GetOrdinal("image_1")) ? "" : reader.GetString("image_1"),
                                    image_2 = reader.IsDBNull(reader.GetOrdinal("image_2")) ? "" : reader.GetString("image_2"),
                                    image_3 = reader.IsDBNull(reader.GetOrdinal("image_3")) ? "" : reader.GetString("image_3"),
                                    image_4 = reader.IsDBNull(reader.GetOrdinal("image_4")) ? "" : reader.GetString("image_4"),
                                    head_1_ru = reader.IsDBNull(reader.GetOrdinal("head_1_ru")) ? "" : reader.GetString("head_1_ru"),
                                    head_1_eng = reader.IsDBNull(reader.GetOrdinal("head_1_eng")) ? "" : reader.GetString("head_1_eng"),
                                    head_1_ger = reader.IsDBNull(reader.GetOrdinal("head_1_ger")) ? "" : reader.GetString("head_1_ger"),
                                    head_2_ru = reader.IsDBNull(reader.GetOrdinal("head_2_ru")) ? "" : reader.GetString("head_2_ru"),
                                    head_2_eng = reader.IsDBNull(reader.GetOrdinal("head_2_eng")) ? "" : reader.GetString("head_2_eng"),
                                    head_2_ger = reader.IsDBNull(reader.GetOrdinal("head_2_ger")) ? "" : reader.GetString("head_2_ger"),
                                    text_ru = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                                    text_eng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                                    text_ger = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger"),
                                    block_1_ru = reader.IsDBNull(reader.GetOrdinal("block_1_ru")) ? "" : reader.GetString("block_1_ru"),
                                    block_1_eng = reader.IsDBNull(reader.GetOrdinal("block_1_eng")) ? "" : reader.GetString("block_1_eng"),
                                    block_1_ger = reader.IsDBNull(reader.GetOrdinal("block_1_ger")) ? "" : reader.GetString("block_1_ger"),
                                    block_1_title_ru = reader.IsDBNull(reader.GetOrdinal("block_1_title_ru")) ? "" : reader.GetString("block_1_title_ru"),
                                    block_1_title_eng = reader.IsDBNull(reader.GetOrdinal("block_1_title_eng")) ? "" : reader.GetString("block_1_title_eng"),
                                    block_1_title_ger = reader.IsDBNull(reader.GetOrdinal("block_1_title_ger")) ? "" : reader.GetString("block_1_title_ger"),

                                    // Блок 2
                                    block_2_ru = reader.IsDBNull(reader.GetOrdinal("block_2_ru")) ? "" : reader.GetString("block_2_ru"),
                                    block_2_eng = reader.IsDBNull(reader.GetOrdinal("block_2_eng")) ? "" : reader.GetString("block_2_eng"),
                                    block_2_ger = reader.IsDBNull(reader.GetOrdinal("block_2_ger")) ? "" : reader.GetString("block_2_ger"),
                                    block_2_title_ru = reader.IsDBNull(reader.GetOrdinal("block_2_title_ru")) ? "" : reader.GetString("block_2_title_ru"),
                                    block_2_title_eng = reader.IsDBNull(reader.GetOrdinal("block_2_title_eng")) ? "" : reader.GetString("block_2_title_eng"),
                                    block_2_title_ger = reader.IsDBNull(reader.GetOrdinal("block_2_title_ger")) ? "" : reader.GetString("block_2_title_ger"),

                                    // Блок 3
                                    block_3_ru = reader.IsDBNull(reader.GetOrdinal("block_3_ru")) ? "" : reader.GetString("block_3_ru"),
                                    block_3_eng = reader.IsDBNull(reader.GetOrdinal("block_3_eng")) ? "" : reader.GetString("block_3_eng"),
                                    block_3_ger = reader.IsDBNull(reader.GetOrdinal("block_3_ger")) ? "" : reader.GetString("block_3_ger"),
                                    block_3_title_ru = reader.IsDBNull(reader.GetOrdinal("block_3_title_ru")) ? "" : reader.GetString("block_3_title_ru"),
                                    block_3_title_eng = reader.IsDBNull(reader.GetOrdinal("block_3_title_eng")) ? "" : reader.GetString("block_3_title_eng"),
                                    block_3_title_ger = reader.IsDBNull(reader.GetOrdinal("block_3_title_ger")) ? "" : reader.GetString("block_3_title_ger"),

                                    // Блок 4
                                    block_4_ru = reader.IsDBNull(reader.GetOrdinal("block_4_ru")) ? "" : reader.GetString("block_4_ru"),
                                    block_4_eng = reader.IsDBNull(reader.GetOrdinal("block_4_eng")) ? "" : reader.GetString("block_4_eng"),
                                    block_4_ger = reader.IsDBNull(reader.GetOrdinal("block_4_ger")) ? "" : reader.GetString("block_4_ger"),
                                    block_4_title_ru = reader.IsDBNull(reader.GetOrdinal("block_4_title_ru")) ? "" : reader.GetString("block_4_title_ru"),
                                    block_4_title_eng = reader.IsDBNull(reader.GetOrdinal("block_4_title_eng")) ? "" : reader.GetString("block_4_title_eng"),
                                    block_4_title_ger = reader.IsDBNull(reader.GetOrdinal("block_4_title_ger")) ? "" : reader.GetString("block_4_title_ger"),

                                    // Блок 5
                                    block_5_ru = reader.IsDBNull(reader.GetOrdinal("block_5_ru")) ? "" : reader.GetString("block_5_ru"),
                                    block_5_eng = reader.IsDBNull(reader.GetOrdinal("block_5_eng")) ? "" : reader.GetString("block_5_eng"),
                                    block_5_ger = reader.IsDBNull(reader.GetOrdinal("block_5_ger")) ? "" : reader.GetString("block_5_ger"),
                                    block_5_title_ru = reader.IsDBNull(reader.GetOrdinal("block_5_title_ru")) ? "" : reader.GetString("block_5_title_ru"),
                                    block_5_title_eng = reader.IsDBNull(reader.GetOrdinal("block_5_title_eng")) ? "" : reader.GetString("block_5_title_eng"),
                                    block_5_title_ger = reader.IsDBNull(reader.GetOrdinal("block_5_title_ger")) ? "" : reader.GetString("block_5_title_ger"),

                                    // Блок 6
                                    block_6_ru = reader.IsDBNull(reader.GetOrdinal("block_6_ru")) ? "" : reader.GetString("block_6_ru"),
                                    block_6_eng = reader.IsDBNull(reader.GetOrdinal("block_6_eng")) ? "" : reader.GetString("block_6_eng"),
                                    block_6_ger = reader.IsDBNull(reader.GetOrdinal("block_6_ger")) ? "" : reader.GetString("block_6_ger"),
                                    block_6_title_ru = reader.IsDBNull(reader.GetOrdinal("block_6_title_ru")) ? "" : reader.GetString("block_6_title_ru"),
                                    block_6_title_eng = reader.IsDBNull(reader.GetOrdinal("block_6_title_eng")) ? "" : reader.GetString("block_6_title_eng"),
                                    block_6_title_ger = reader.IsDBNull(reader.GetOrdinal("block_6_title_ger")) ? "" : reader.GetString("block_6_title_ger")

                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Ошибка MySQL при загрузке данных главной страницы: {ex.Message}");
            }

            return furst_page;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFurstPage(FurstPageModel page,
            IFormFile imageFile_1, IFormFile imageFile_2, IFormFile imageFile_3, IFormFile imageFile_4,
            bool remove_image_1 = false, bool remove_image_2 = false, bool remove_image_3 = false, bool remove_image_4 = false)
        {
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"UPDATE first_page_content SET 
                        image_1 = @image_1,
                        image_2 = @image_2,
                        image_3 = @image_3,
                        image_4 = @image_4,
                        head_1_ru = @head_1_ru,
                        head_1_eng = @head_1_eng,
                        head_1_ger = @head_1_ger,
                        head_2_ru = @head_2_ru,
                        head_2_eng = @head_2_eng,
                        head_2_ger = @head_2_ger,
                        text_ru = @text_ru,
                        text_eng = @text_eng,
                        text_ger = @text_ger,
                        block_1_ru = @block_1_ru,
                block_1_eng = @block_1_eng,
                block_1_ger = @block_1_ger,
                block_1_title_ru = @block_1_title_ru,
                block_1_title_eng = @block_1_title_eng,
                block_1_title_ger = @block_1_title_ger,
                
                block_2_ru = @block_2_ru,
                block_2_eng = @block_2_eng,
                block_2_ger = @block_2_ger,
                block_2_title_ru = @block_2_title_ru,
                block_2_title_eng = @block_2_title_eng,
                block_2_title_ger = @block_2_title_ger,
                
                block_3_ru = @block_3_ru,
                block_3_eng = @block_3_eng,
                block_3_ger = @block_3_ger,
                block_3_title_ru = @block_3_title_ru,
                block_3_title_eng = @block_3_title_eng,
                block_3_title_ger = @block_3_title_ger,
                
                block_4_ru = @block_4_ru,
                block_4_eng = @block_4_eng,
                block_4_ger = @block_4_ger,
                block_4_title_ru = @block_4_title_ru,
                block_4_title_eng = @block_4_title_eng,
                block_4_title_ger = @block_4_title_ger,
                
                block_5_ru = @block_5_ru,
                block_5_eng = @block_5_eng,
                block_5_ger = @block_5_ger,
                block_5_title_ru = @block_5_title_ru,
                block_5_title_eng = @block_5_title_eng,
                block_5_title_ger = @block_5_title_ger,
                
                block_6_ru = @block_6_ru,
                block_6_eng = @block_6_eng,
                block_6_ger = @block_6_ger,
                block_6_title_ru = @block_6_title_ru,
                block_6_title_eng = @block_6_title_eng,
                block_6_title_ger = @block_6_title_ger
                        WHERE id = 1";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Сохраняем изображения
                        if (!remove_image_1 && imageFile_1 != null)
                        {
                            var fileName = await SaveFurstImage(imageFile_1);
                            page.image_1 = fileName;
                        }
                        else if (remove_image_1)
                        {
                            page.image_1 = "";
                        }

                        if (!remove_image_2 && imageFile_2 != null)
                        {
                            var fileName = await SaveFurstImage(imageFile_2);
                            page.image_2 = fileName;
                        }
                        else if (remove_image_2)
                        {
                            page.image_2 = "";
                        }

                        if (!remove_image_3 && imageFile_3 != null)
                        {
                            var fileName = await SaveFurstImage(imageFile_3);
                            page.image_3 = fileName;
                        }
                        else if (remove_image_3)
                        {
                            page.image_3 = "";
                        }

                        if (!remove_image_4 && imageFile_4 != null)
                        {
                            var fileName = await SaveFurstImage(imageFile_4);
                            page.image_4 = fileName;
                        }
                        else if (remove_image_4)
                        {
                            page.image_4 = "";
                        }

                        command.Parameters.AddWithValue("@image_1", page.image_1 ?? "");
                        command.Parameters.AddWithValue("@image_2", page.image_2 ?? "");
                        command.Parameters.AddWithValue("@image_3", page.image_3 ?? "");
                        command.Parameters.AddWithValue("@image_4", page.image_4 ?? "");
                        command.Parameters.AddWithValue("@head_1_ru", page.head_1_ru ?? "");
                        command.Parameters.AddWithValue("@head_1_eng", page.head_1_eng ?? "");
                        command.Parameters.AddWithValue("@head_1_ger", page.head_1_ger ?? "");
                        command.Parameters.AddWithValue("@head_2_ru", page.head_2_ru ?? "");
                        command.Parameters.AddWithValue("@head_2_eng", page.head_2_eng ?? "");
                        command.Parameters.AddWithValue("@head_2_ger", page.head_2_ger ?? "");
                        command.Parameters.AddWithValue("@text_ru", page.text_ru ?? "");
                        command.Parameters.AddWithValue("@text_eng", page.text_eng ?? "");
                        command.Parameters.AddWithValue("@text_ger", page.text_ger ?? "");
                        command.Parameters.AddWithValue("@block_1_ru", page.block_1_ru ?? "");
                        command.Parameters.AddWithValue("@block_1_eng", page.block_1_eng ?? "");
                        command.Parameters.AddWithValue("@block_1_ger", page.block_1_ger ?? "");
                        command.Parameters.AddWithValue("@block_2_ru", page.block_2_ru ?? "");
                        command.Parameters.AddWithValue("@block_2_eng", page.block_2_eng ?? "");
                        command.Parameters.AddWithValue("@block_2_ger", page.block_2_ger ?? "");
                        command.Parameters.AddWithValue("@block_3_ru", page.block_3_ru ?? "");
                        command.Parameters.AddWithValue("@block_3_eng", page.block_3_eng ?? "");
                        command.Parameters.AddWithValue("@block_3_ger", page.block_3_ger ?? "");
                        command.Parameters.AddWithValue("@block_4_ru", page.block_4_ru ?? "");
                        command.Parameters.AddWithValue("@block_4_eng", page.block_4_eng ?? "");
                        command.Parameters.AddWithValue("@block_4_ger", page.block_4_ger ?? "");
                        command.Parameters.AddWithValue("@block_5_ru", page.block_5_ru ?? "");
                        command.Parameters.AddWithValue("@block_5_eng", page.block_5_eng ?? "");
                        command.Parameters.AddWithValue("@block_5_ger", page.block_5_ger ?? "");
                        command.Parameters.AddWithValue("@block_6_ru", page.block_6_ru ?? "");
                        command.Parameters.AddWithValue("@block_6_eng", page.block_6_eng ?? "");
                        command.Parameters.AddWithValue("@block_6_ger", page.block_6_ger ?? "");

                        command.Parameters.AddWithValue("@block_1_title_ru", page.block_1_title_ru ?? "");
                        command.Parameters.AddWithValue("@block_1_title_eng", page.block_1_title_eng ?? "");
                        command.Parameters.AddWithValue("@block_1_title_ger", page.block_1_title_ger ?? "");

                        command.Parameters.AddWithValue("@block_2_title_ru", page.block_2_title_ru ?? "");
                        command.Parameters.AddWithValue("@block_2_title_eng", page.block_2_title_eng ?? "");
                        command.Parameters.AddWithValue("@block_2_title_ger", page.block_2_title_ger ?? "");

                        command.Parameters.AddWithValue("@block_3_title_ru", page.block_3_title_ru ?? "");
                        command.Parameters.AddWithValue("@block_3_title_eng", page.block_3_title_eng ?? "");
                        command.Parameters.AddWithValue("@block_3_title_ger", page.block_3_title_ger ?? "");

                        command.Parameters.AddWithValue("@block_4_title_ru", page.block_4_title_ru ?? "");
                        command.Parameters.AddWithValue("@block_4_title_eng", page.block_4_title_eng ?? "");
                        command.Parameters.AddWithValue("@block_4_title_ger", page.block_4_title_ger ?? "");

                        command.Parameters.AddWithValue("@block_5_title_ru", page.block_5_title_ru ?? "");
                        command.Parameters.AddWithValue("@block_5_title_eng", page.block_5_title_eng ?? "");
                        command.Parameters.AddWithValue("@block_5_title_ger", page.block_5_title_ger ?? "");

                        command.Parameters.AddWithValue("@block_6_title_ru", page.block_6_title_ru ?? "");
                        command.Parameters.AddWithValue("@block_6_title_eng", page.block_6_title_eng ?? "");
                        command.Parameters.AddWithValue("@block_6_title_ger", page.block_6_title_ger ?? "");

                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["Message"] = "Главная страница обновлена";
                await NotifyReaderSite();
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
            }

            return RedirectToAction("FurstPage");
        }

        private async Task<string> SaveFurstImage(IFormFile imageFile)
        {
            var furstPath = Path.Combine(_sharedUploadsPath, "furst");
            if (!Directory.Exists(furstPath))
            {
                Directory.CreateDirectory(furstPath);
            }

            var ext = Path.GetExtension(imageFile.FileName).ToLower();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowed.Contains(ext))
            {
                throw new Exception("Неподдерживаемый формат файла");
            }

            if (imageFile.Length > 5 * 1024 * 1024)
            {
                throw new Exception("Файл слишком большой (макс. 5MB)");
            }

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(furstPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }

        // ============================================
        // ЦЕНЫ (PRICE)
        // ============================================

        public IActionResult Prices()
        {
            List<PriceModel> prices = GetPricesFromDatabase();
            return View(prices);
        }

        public IActionResult CreatePrice()
        {
            return View("CreatePrice", new PriceModel());
        }

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

        private List<PriceModel> GetPricesFromDatabase()
        {
            List<PriceModel> prices = new List<PriceModel>();
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT id, name_ru, name_eng, name_ger, 
                            base_price, pro_price,
                            base_price_eng, pro_price_eng,
                            base_price_ger, pro_price_ger,
                            info_ru, info_eng, info_ger
                     FROM price ORDER BY id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                prices.Add(new PriceModel
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
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }
            return prices;
        }

        private PriceModel GetPriceById(int id)
        {
            PriceModel price = null;
            string connectionString = GetConnectionString();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT id, name_ru, name_eng, name_ger, 
                            base_price, pro_price,
                            base_price_eng, pro_price_eng,
                            base_price_ger, pro_price_ger,
                            info_ru, info_eng, info_ger 
                     FROM price WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                price = new PriceModel
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
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }
            return price;
        }

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult TemplatePrices()
        {
            List<TemplatePriceModel> templates = GetTemplatePricesFromDatabase();
            return View(templates);
        }

        public IActionResult CreateTemplatePrice()
        {
            return View("CreateTemplatePrice", new TemplatePriceModel());
        }

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

        private List<TemplatePriceModel> GetTemplatePricesFromDatabase()
        {
            List<TemplatePriceModel> templates = new List<TemplatePriceModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_price ORDER BY id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            templates.Add(new TemplatePriceModel
                            {
                                Id = reader.GetInt32("id"),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                Prices = reader.IsDBNull(reader.GetOrdinal("prices")) ? "" : reader.GetString("prices")
                            });
                        }
                    }
                }
            }
            return templates;
        }

        private TemplatePriceModel GetTemplatePriceById(int id)
        {
            TemplatePriceModel template = null;
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_price WHERE id = @id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            template = new TemplatePriceModel
                            {
                                Id = reader.GetInt32("id"),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                Prices = reader.IsDBNull(reader.GetOrdinal("prices")) ? "" : reader.GetString("prices")
                            };
                        }
                    }
                }
            }
            return template;
        }

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult Abouts()
        {
            List<AboutModel> abouts = GetAboutsFromDatabase();
            return View(abouts);
        }

        public IActionResult CreateAbout()
        {
            return View("CreateAbout", new AboutModel());
        }

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

        private List<AboutModel> GetAboutsFromDatabase()
        {
            List<AboutModel> abouts = new List<AboutModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM about ORDER BY id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            abouts.Add(new AboutModel
                            {
                                Id = reader.GetInt32("id"),
                                TextRu = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                                TextEng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                                TextGer = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger")
                            });
                        }
                    }
                }
            }
            return abouts;
        }

        private AboutModel GetAboutById(int id)
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult TemplateAbouts()
        {
            List<TemplateAboutModel> templates = GetTemplateAboutsFromDatabase();
            return View(templates);
        }

        private List<TemplateAboutModel> GetTemplateAboutsFromDatabase()
        {
            List<TemplateAboutModel> templates = new List<TemplateAboutModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_about ORDER BY sort_order ASC, id ASC";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            templates.Add(new TemplateAboutModel
                            {
                                Id = reader.GetInt32("id"),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                Ids = reader.IsDBNull(reader.GetOrdinal("ids")) ? "" : reader.GetString("ids"),
                                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order")
                            });
                        }
                    }
                }
            }
            return templates;
        }

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult Results()
        {
            List<ResultModel> results = GetResultsFromDatabase();
            return View(results);
        }

        private List<ResultModel> GetResultsFromDatabase()
        {
            List<ResultModel> results = new List<ResultModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM result ORDER BY id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ResultModel
                            {
                                Id = reader.GetInt32("id"),
                                TextRu = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                                TextEng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                                TextGer = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger")
                            });
                        }
                    }
                }
            }
            return results;
        }

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult TemplateResults()
        {
            List<TemplateResultModel> templates = GetTemplateResultsFromDatabase();
            return View(templates);
        }

        private List<TemplateResultModel> GetTemplateResultsFromDatabase()
        {
            List<TemplateResultModel> templates = new List<TemplateResultModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_result ORDER BY id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            templates.Add(new TemplateResultModel
                            {
                                Id = reader.GetInt32("id"),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                Ids = reader.IsDBNull(reader.GetOrdinal("ids")) ? "" : reader.GetString("ids")
                            });
                        }
                    }
                }
            }
            return templates;
        }

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult EngineControls()
        {
            List<EngineControlModel> controls = GetEngineControlsFromDatabase();
            return View(controls);
        }

        private List<EngineControlModel> GetEngineControlsFromDatabase()
        {
            List<EngineControlModel> controls = new List<EngineControlModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM engine_control ORDER BY id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            controls.Add(new EngineControlModel
                            {
                                Id = reader.GetInt32("id"),
                                TextRu = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                                TextEng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                                TextGer = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger")
                            });
                        }
                    }
                }
            }
            return controls;
        }

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult TemplateEngineControls()
        {
            List<TemplateEngineControlModel> templates = GetTemplateEngineControlsFromDatabase();
            return View(templates);
        }

        private List<TemplateEngineControlModel> GetTemplateEngineControlsFromDatabase()
        {
            List<TemplateEngineControlModel> templates = new List<TemplateEngineControlModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_engine_control ORDER BY id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            templates.Add(new TemplateEngineControlModel
                            {
                                Id = reader.GetInt32("id"),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                Ids = reader.IsDBNull(reader.GetOrdinal("ids")) ? "" : reader.GetString("ids")
                            });
                        }
                    }
                }
            }
            return templates;
        }

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult Grafics()
        {
            List<GraficModel> grafics = GetGraficsFromDatabase();
            return View(grafics);
        }

        public IActionResult CreateGrafic()
        {
            return View("CreateGrafic", new GraficModel());
        }

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

        private List<GraficModel> GetGraficsFromDatabase()
        {
            List<GraficModel> grafics = new List<GraficModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM grafic ORDER BY id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            grafics.Add(new GraficModel
                            {
                                Id = reader.GetInt32("id"),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                NameEng = reader.IsDBNull(reader.GetOrdinal("name_eng")) ? "" : reader.GetString("name_eng"),
                                NameGer = reader.IsDBNull(reader.GetOrdinal("name_ger")) ? "" : reader.GetString("name_ger"),
                                Image = reader.IsDBNull(reader.GetOrdinal("image")) ? "" : reader.GetString("image"),
                                DescriptionRu = reader.IsDBNull(reader.GetOrdinal("description_ru")) ? "" : reader.GetString("description_ru"),
                                DescriptionEng = reader.IsDBNull(reader.GetOrdinal("description_eng")) ? "" : reader.GetString("description_eng"),
                                DescriptionGer = reader.IsDBNull(reader.GetOrdinal("description_ger")) ? "" : reader.GetString("description_ger")
                            });
                        }
                    }
                }
            }
            return grafics;
        }

        private GraficModel GetGraficById(int id)
        {
            GraficModel grafic = null;
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM grafic WHERE id = @id";
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

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult TemplateGrafics()
        {
            List<TemplateGraficModel> templates = GetTemplateGraficsFromDatabase();
            return View(templates);
        }

        private List<TemplateGraficModel> GetTemplateGraficsFromDatabase()
        {
            List<TemplateGraficModel> templates = new List<TemplateGraficModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_grafic ORDER BY id";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            templates.Add(new TemplateGraficModel
                            {
                                Id = reader.GetInt32("id"),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                Ids = reader.IsDBNull(reader.GetOrdinal("ids")) ? "" : reader.GetString("ids")
                            });
                        }
                    }
                }
            }
            return templates;
        }

        [HttpPost]
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

        [HttpPost]
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

        public IActionResult AdditionalPrices()
        {
            List<AdditionalPriceModel> prices = GetAdditionalPricesFromDatabase();
            return View(prices);
        }

        public IActionResult CreateAdditionalPrice()
        {
            return View("CreateAdditionalPrice", new AdditionalPriceModel());
        }

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

        private List<AdditionalPriceModel> GetAdditionalPricesFromDatabase()
        {
            List<AdditionalPriceModel> prices = new List<AdditionalPriceModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT id, name_ru, name_eng, name_ger, 
                        price_rubl, price_dolar, price_euro,
                        info_ru, info_eng, info_ger, sort_order, price_controler,
                        free_price_ids, base_price_ids, pro_price_ids, unselected_price_mode
                 FROM additional_prices ORDER BY sort_order, id";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            prices.Add(new AdditionalPriceModel
                            {
                                Id = reader.GetInt32("id"),
                                NameRu = reader.IsDBNull(reader.GetOrdinal("name_ru")) ? "" : reader.GetString("name_ru"),
                                NameEng = reader.IsDBNull(reader.GetOrdinal("name_eng")) ? "" : reader.GetString("name_eng"),
                                NameGer = reader.IsDBNull(reader.GetOrdinal("name_ger")) ? "" : reader.GetString("name_ger"),
                                PriceRubl = reader.IsDBNull(reader.GetOrdinal("price_rubl")) ? "" : reader.GetString("price_rubl"),
                                PriceDolar = reader.IsDBNull(reader.GetOrdinal("price_dolar")) ? "" : reader.GetString("price_dolar"),
                                PriceEuro = reader.IsDBNull(reader.GetOrdinal("price_euro")) ? "" : reader.GetString("price_euro"),
                                InfoRu = reader.IsDBNull(reader.GetOrdinal("info_ru")) ? "" : reader.GetString("info_ru"),
                                InfoEng = reader.IsDBNull(reader.GetOrdinal("info_eng")) ? "" : reader.GetString("info_eng"),
                                InfoGer = reader.IsDBNull(reader.GetOrdinal("info_ger")) ? "" : reader.GetString("info_ger"),
                                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order"),
                                PriceControler = reader.IsDBNull(reader.GetOrdinal("price_controler")) ? 0 : reader.GetInt32("price_controler"),
                                // ===== НОВЫЕ ПОЛЯ =====
                                FreePriceIds = reader.IsDBNull(reader.GetOrdinal("free_price_ids")) ? "" : reader.GetString("free_price_ids"),
                                BasePriceIds = reader.IsDBNull(reader.GetOrdinal("base_price_ids")) ? "" : reader.GetString("base_price_ids"),
                                ProPriceIds = reader.IsDBNull(reader.GetOrdinal("pro_price_ids")) ? "" : reader.GetString("pro_price_ids"),

                                UnselectedPriceMode = reader.IsDBNull(reader.GetOrdinal("unselected_price_mode")) ? 0 : reader.GetInt32("unselected_price_mode")
                            });
                        }
                    }
                }
            }
            return prices;
        }

        private AdditionalPriceModel GetAdditionalPriceById(int id)
        {
            AdditionalPriceModel price = null;
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"SELECT id, name_ru, name_eng, name_ger, 
                        price_rubl, price_dolar, price_euro,
                        info_ru, info_eng, info_ger, sort_order, price_controler,
                        free_price_ids, base_price_ids, pro_price_ids, unselected_price_mode
                 FROM additional_prices WHERE id = @id";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            price = new AdditionalPriceModel
                            {
                                Id = reader.GetInt32("id"),
                                NameRu = reader.IsDBNull(reader.GetOrdinal("name_ru")) ? "" : reader.GetString("name_ru"),
                                NameEng = reader.IsDBNull(reader.GetOrdinal("name_eng")) ? "" : reader.GetString("name_eng"),
                                NameGer = reader.IsDBNull(reader.GetOrdinal("name_ger")) ? "" : reader.GetString("name_ger"),
                                PriceRubl = reader.IsDBNull(reader.GetOrdinal("price_rubl")) ? "" : reader.GetString("price_rubl"),
                                PriceDolar = reader.IsDBNull(reader.GetOrdinal("price_dolar")) ? "" : reader.GetString("price_dolar"),
                                PriceEuro = reader.IsDBNull(reader.GetOrdinal("price_euro")) ? "" : reader.GetString("price_euro"),
                                InfoRu = reader.IsDBNull(reader.GetOrdinal("info_ru")) ? "" : reader.GetString("info_ru"),
                                InfoEng = reader.IsDBNull(reader.GetOrdinal("info_eng")) ? "" : reader.GetString("info_eng"),
                                InfoGer = reader.IsDBNull(reader.GetOrdinal("info_ger")) ? "" : reader.GetString("info_ger"),
                                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order"),
                                PriceControler = reader.IsDBNull(reader.GetOrdinal("price_controler")) ? 0 : reader.GetInt32("price_controler"),
                                // ===== НОВЫЕ ПОЛЯ =====
                                FreePriceIds = reader.IsDBNull(reader.GetOrdinal("free_price_ids")) ? "" : reader.GetString("free_price_ids"),
                                BasePriceIds = reader.IsDBNull(reader.GetOrdinal("base_price_ids")) ? "" : reader.GetString("base_price_ids"),
                                ProPriceIds = reader.IsDBNull(reader.GetOrdinal("pro_price_ids")) ? "" : reader.GetString("pro_price_ids"),
                                UnselectedPriceMode = reader.IsDBNull(reader.GetOrdinal("unselected_price_mode")) ? 0 : reader.GetInt32("unselected_price_mode")
                            };
                        }
                    }
                }
            }
            return price;
        }

        [HttpPost]
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

        [HttpPost]
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

        [HttpPost]
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

        private List<TemplateAdditionalPriceModel> GetTemplateAdditionalPricesFromDatabase()
        {
            List<TemplateAdditionalPriceModel> templates = new List<TemplateAdditionalPriceModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT id, name, price_ids, used_in_cars FROM template_additional_prices ORDER BY id";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            templates.Add(new TemplateAdditionalPriceModel
                            {
                                Id = reader.GetInt32("id"),
                                Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                PriceIds = reader.IsDBNull(reader.GetOrdinal("price_ids")) ? "" : reader.GetString("price_ids"),
                                UsedInCars = reader.IsDBNull(reader.GetOrdinal("used_in_cars")) ? "" : reader.GetString("used_in_cars")
                            });
                        }
                    }
                }
            }
            return templates;
        }

        // ============================================
        // ПОЛУЧЕНИЕ ВСЕХ ЗАПИСЕЙ ПО ТИПУ
        // ============================================

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
        [HttpPost]
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
        [HttpPost]
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

        [HttpGet]
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
        [HttpGet]
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

        [HttpPost]
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
                TempData["Message"] = "Старый URL успешно обновлён";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при обновлении старого URL: {ex.Message}";
            }

            return RedirectToAction("EditCar", new { id = id });
        }

        // ==========================================
        // ПОЛУЧЕНИЕ ОПЦИЙ ИЗ ШАБЛОНОВ ЦЕН
        // ==========================================

        [HttpGet]
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

        [HttpGet]
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

        public class TemplatePriceOptionDto
        {
            public int TemplateId { get; set; }
            public string TemplateName { get; set; }
            public List<PriceOptionItemDto> Prices { get; set; } = new();
        }

        public class PriceOptionItemDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string BasePrice { get; set; }
            public string ProPrice { get; set; }
        }

        // ==========================================
        // ОБНОВЛЕНИЕ ЗАПИСИ (из модального окна)
        // ==========================================
        [HttpPost]
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

        [HttpGet]
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

        [HttpGet]
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

        // ============================================
        // КЛАССЫ ДЛЯ ЗАПРОСОВ
        // ============================================
        public class DeleteItemRequest
        {
            public string Type { get; set; }
            public int Id { get; set; }
            public string ReturnTab { get; set; }
        }

        public class DeleteUnusedRequest
        {
            public string Type { get; set; }
            public string ReturnTab { get; set; }
        }



        // ============================================
        // ОШИБКИ
        // ============================================

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}