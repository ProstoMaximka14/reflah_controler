using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using reflah_controler.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace reflah_controler.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ============================================
        // АДМИНИСТРАТОРЫ
        // ============================================

        // GET: Показать список администраторов
        public IActionResult Index()
        {
            List<AdminsModel> admins = GetAdminsFromDatabase();
            return View(admins);
        }

        [HttpPost]
        public IActionResult UpdateAdmin(int id, string login, string password)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

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
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

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
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

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
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

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
        // АВТОМОБИЛИ (ЧИП-ТЮНИНГ)
        // ============================================

        // GET: Страница выбора автомобиля
        public IActionResult Cars()
        {
            List<ReflashCarModel> cars = GetCarsFromDatabase();
            return View(cars);
        }

        // GET: Страница редактирования конкретного автомобиля
        public IActionResult EditCar(int id)
        {
            ReflashCarModel car = GetCarById(id);
            if (car == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("Cars");
            }
            return View(car);
        }

        // POST: Обновить автомобиль
        [HttpPost]
        public IActionResult UpdateCar(ReflashCarModel car)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"UPDATE reflash_cars SET 
                                    brand = @brand, 
                                    model = @model, 
                                    generation = @generation, 
                                    engine = @engine, 
                                    image = @image, 
                                    about_ru = @about_ru, 
                                    about_eng = @about_eng, 
                                    about_ger = @about_ger, 
                                    result_ru = @result_ru, 
                                    result_eng = @result_eng, 
                                    result_ger = @result_ger, 
                                    engine_control_ru = @engine_control_ru, 
                                    engine_control_eng = @engine_control_eng, 
                                    engine_control_ger = @engine_control_ger, 
                                    options_ru = @options_ru, 
                                    options_eng = @options_eng, 
                                    options_ger = @options_ger, 
                                    price_ru = @price_ru, 
                                    price_eng = @price_eng, 
                                    price_ger = @price_ger 
                                    WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", car.Id);
                        command.Parameters.AddWithValue("@brand", car.Brand);
                        command.Parameters.AddWithValue("@model", car.Model);
                        command.Parameters.AddWithValue("@generation", car.Generation);
                        command.Parameters.AddWithValue("@engine", car.Engine);
                        command.Parameters.AddWithValue("@image", car.Image);

                        command.Parameters.AddWithValue("@about_ru", car.AboutRu ?? "");
                        command.Parameters.AddWithValue("@result_ru", car.ResultRu ?? "");
                        command.Parameters.AddWithValue("@engine_control_ru", car.EngineControlRu ?? "");
                        command.Parameters.AddWithValue("@options_ru", car.OptionsRu ?? "");
                        command.Parameters.AddWithValue("@price_ru", car.PriceRu ?? "");

                        command.Parameters.AddWithValue("@about_eng", car.AboutEng ?? "");
                        command.Parameters.AddWithValue("@result_eng", car.ResultEng ?? "");
                        command.Parameters.AddWithValue("@engine_control_eng", car.EngineControlEng ?? "");
                        command.Parameters.AddWithValue("@options_eng", car.OptionsEng ?? "");
                        command.Parameters.AddWithValue("@price_eng", car.PriceEng ?? "");

                        command.Parameters.AddWithValue("@about_ger", car.AboutGer ?? "");
                        command.Parameters.AddWithValue("@result_ger", car.ResultGer ?? "");
                        command.Parameters.AddWithValue("@engine_control_ger", car.EngineControlGer ?? "");
                        command.Parameters.AddWithValue("@options_ger", car.OptionsGer ?? "");
                        command.Parameters.AddWithValue("@price_ger", car.PriceGer ?? "");

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Автомобиль {car.Brand} {car.Model} успешно обновлен";
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
                TempData["Error"] = $"Ошибка MySQL при обновлении: {ex.Message}";
            }

            return RedirectToAction("EditCar", new { id = car.Id });
        }

        // POST: Удалить автомобиль
        [HttpPost]
        public IActionResult DeleteCar(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM reflash_cars WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = "Автомобиль успешно удален";
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

        // POST: Добавить автомобиль
        [HttpPost]
        public IActionResult AddCar(ReflashCarModel car)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO reflash_cars 
                                    (brand, model, generation, engine, image, 
                                     about_ru, about_eng, about_ger,
                                     result_ru, result_eng, result_ger,
                                     engine_control_ru, engine_control_eng, engine_control_ger,
                                     options_ru, options_eng, options_ger,
                                     price_ru, price_eng, price_ger) 
                                    VALUES 
                                    (@brand, @model, @generation, @engine, @image,
                                     @about_ru, @about_eng, @about_ger,
                                     @result_ru, @result_eng, @result_ger,
                                     @engine_control_ru, @engine_control_eng, @engine_control_ger,
                                     @options_ru, @options_eng, @options_ger,
                                     @price_ru, @price_eng, @price_ger)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@brand", car.Brand);
                        command.Parameters.AddWithValue("@model", car.Model);
                        command.Parameters.AddWithValue("@generation", car.Generation);
                        command.Parameters.AddWithValue("@engine", car.Engine);
                        command.Parameters.AddWithValue("@image", car.Image);

                        command.Parameters.AddWithValue("@about_ru", car.AboutRu ?? "");
                        command.Parameters.AddWithValue("@result_ru", car.ResultRu ?? "");
                        command.Parameters.AddWithValue("@engine_control_ru", car.EngineControlRu ?? "");
                        command.Parameters.AddWithValue("@options_ru", car.OptionsRu ?? "");
                        command.Parameters.AddWithValue("@price_ru", car.PriceRu ?? "");

                        command.Parameters.AddWithValue("@about_eng", car.AboutEng ?? "");
                        command.Parameters.AddWithValue("@result_eng", car.ResultEng ?? "");
                        command.Parameters.AddWithValue("@engine_control_eng", car.EngineControlEng ?? "");
                        command.Parameters.AddWithValue("@options_eng", car.OptionsEng ?? "");
                        command.Parameters.AddWithValue("@price_eng", car.PriceEng ?? "");

                        command.Parameters.AddWithValue("@about_ger", car.AboutGer ?? "");
                        command.Parameters.AddWithValue("@result_ger", car.ResultGer ?? "");
                        command.Parameters.AddWithValue("@engine_control_ger", car.EngineControlGer ?? "");
                        command.Parameters.AddWithValue("@options_ger", car.OptionsGer ?? "");
                        command.Parameters.AddWithValue("@price_ger", car.PriceGer ?? "");

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Автомобиль {car.Brand} {car.Model} успешно добавлен";
                            // Получаем ID нового автомобиля
                            int newId = (int)command.LastInsertedId;
                            return RedirectToAction("EditCar", new { id = newId });
                        }
                        else
                        {
                            TempData["Error"] = "Не удалось добавить автомобиль";
                            return RedirectToAction("Cars");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при добавлении: {ex.Message}";
                return RedirectToAction("Cars");
            }
        }

        // Метод для получения списка автомобилей
        private List<ReflashCarModel> GetCarsFromDatabase()
        {
            List<ReflashCarModel> cars = new List<ReflashCarModel>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM reflash_cars ORDER BY brand, model, generation";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cars.Add(new ReflashCarModel
                                {
                                    Id = reader.GetInt32("id"),
                                    Brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? "" : reader.GetString("brand"),
                                    Model = reader.IsDBNull(reader.GetOrdinal("model")) ? "" : reader.GetString("model"),
                                    Generation = reader.IsDBNull(reader.GetOrdinal("generation")) ? "" : reader.GetString("generation"),
                                    Engine = reader.IsDBNull(reader.GetOrdinal("engine")) ? "" : reader.GetString("engine"),
                                    Image = reader.IsDBNull(reader.GetOrdinal("image")) ? "" : reader.GetString("image")
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

        // Метод для получения конкретного автомобиля по ID
        private ReflashCarModel GetCarById(int id)
        {
            ReflashCarModel car = null;
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM reflash_cars WHERE id = @id";

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
                                    OptionsRu = reader.IsDBNull(reader.GetOrdinal("options_ru")) ? "" : reader.GetString("options_ru"),
                                    PriceRu = reader.IsDBNull(reader.GetOrdinal("price_ru")) ? "" : reader.GetString("price_ru"),

                                    AboutEng = reader.IsDBNull(reader.GetOrdinal("about_eng")) ? "" : reader.GetString("about_eng"),
                                    ResultEng = reader.IsDBNull(reader.GetOrdinal("result_eng")) ? "" : reader.GetString("result_eng"),
                                    EngineControlEng = reader.IsDBNull(reader.GetOrdinal("engine_control_eng")) ? "" : reader.GetString("engine_control_eng"),
                                    OptionsEng = reader.IsDBNull(reader.GetOrdinal("options_eng")) ? "" : reader.GetString("options_eng"),
                                    PriceEng = reader.IsDBNull(reader.GetOrdinal("price_eng")) ? "" : reader.GetString("price_eng"),

                                    AboutGer = reader.IsDBNull(reader.GetOrdinal("about_ger")) ? "" : reader.GetString("about_ger"),
                                    ResultGer = reader.IsDBNull(reader.GetOrdinal("result_ger")) ? "" : reader.GetString("result_ger"),
                                    EngineControlGer = reader.IsDBNull(reader.GetOrdinal("engine_control_ger")) ? "" : reader.GetString("engine_control_ger"),
                                    OptionsGer = reader.IsDBNull(reader.GetOrdinal("options_ger")) ? "" : reader.GetString("options_ger"),
                                    PriceGer = reader.IsDBNull(reader.GetOrdinal("price_ger")) ? "" : reader.GetString("price_ger")
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

        

        // GET: Страница партнеров
        public IActionResult Partners()
        {
            List<PartnersModel> partners = GetPartnersFromDatabase();
            return View(partners);
        }

        // POST: Добавить партнера
        [HttpPost]
        public async Task<IActionResult> AddPartner(string name, string phone, string vk, string website, IFormFile photoFile)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                string photoUrl = "";

                // Обработка загрузки фото
                if (photoFile != null && photoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "partners");

                    // Создаем папку, если не существует
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Генерируем уникальное имя файла
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }

                    photoUrl = $"/uploads/partners/{fileName}";
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO partners 
                            (name, phone, photo_url, vk_url, website_url) 
                            VALUES 
                            (@name, @phone, @photo_url, @vk_url, @website_url)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@phone", phone ?? "");
                        command.Parameters.AddWithValue("@photo_url", photoUrl);
                        command.Parameters.AddWithValue("@vk_url", vk ?? "");
                        command.Parameters.AddWithValue("@website_url", website ?? "");

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Партнер {name} успешно добавлен";
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

        // POST: Обновить партнера
        [HttpPost]
        public async Task<IActionResult> UpdatePartner(int id, string name, string phone, string vk, string website, IFormFile photoFile, string photo)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                string photoUrl = photo;

                // Обработка загрузки нового фото
                if (photoFile != null && photoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "partners");

                    // Создаем папку, если не существует
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Генерируем уникальное имя файла
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }

                    photoUrl = $"/uploads/partners/{fileName}";

                    // Удаляем старое фото, если оно существует и это не дефолтное
                    if (!string.IsNullOrEmpty(photo) && photo.StartsWith("/uploads/partners/"))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photo.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
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
                            website_url = @website_url 
                            WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@phone", phone ?? "");
                        command.Parameters.AddWithValue("@photo_url", photoUrl);
                        command.Parameters.AddWithValue("@vk_url", vk ?? "");
                        command.Parameters.AddWithValue("@website_url", website ?? "");

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Партнер {name} успешно обновлен";
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

        // POST: Удалить партнера
        [HttpPost]
        public async Task<IActionResult> DeletePartner(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                // Сначала получаем информацию о фото
                string photoUrl = "";
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
                            photoUrl = result.ToString();
                        }
                    }
                }

                // Удаляем запись из БД
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
                            // Удаляем файл фото, если он существует
                            if (!string.IsNullOrEmpty(photoUrl) && photoUrl.StartsWith("/uploads/partners/"))
                            {
                                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photoUrl.TrimStart('/'));
                                if (System.IO.File.Exists(filePath))
                                {
                                    System.IO.File.Delete(filePath);
                                }
                            }

                            TempData["Message"] = "Партнер успешно удален";
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

        // Метод для получения списка партнеров из БД
        private List<PartnersModel> GetPartnersFromDatabase()
        {
            List<PartnersModel> partners = new List<PartnersModel>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM partners ORDER BY name";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                partners.Add(new PartnersModel
                                {
                                    Id = reader.GetInt32("id"),
                                    name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString("name"),
                                    phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "" : reader.GetString("phone"),
                                    photo = reader.IsDBNull(reader.GetOrdinal("photo_url")) ? "" : reader.GetString("photo_url"),
                                    vk = reader.IsDBNull(reader.GetOrdinal("vk_url")) ? "" : reader.GetString("vk_url"),
                                    website = reader.IsDBNull(reader.GetOrdinal("website_url")) ? "" : reader.GetString("website_url")
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

        // ============================================
        // ОБЩИЕ МЕТОДЫ
        // ============================================

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}