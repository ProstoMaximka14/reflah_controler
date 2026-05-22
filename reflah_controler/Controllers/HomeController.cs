using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using reflah_controler.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.AspNetCore.SignalR;
using reflah_controler.Hubs;

namespace reflah_controler.Controllers
{
    public class HomeController : Controller
    {

        private readonly IConfiguration _configuration;
        private readonly IHubContext<DatabaseHub> _hubContext;


        public HomeController(IConfiguration configuration, IHubContext<DatabaseHub> hubContext) 
        {
            _configuration = configuration;
            _hubContext = hubContext;
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
        // АВТОМОБИЛИ 
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

        // GET: Страница Создание нового автомобиля
        public IActionResult CreateCar()
        {
            return View("CreateCar", new ReflashCarModel());
        }

        // POST: Обновить автомобиль
        [HttpPost]
        public async Task<IActionResult> UpdateCar(ReflashCarModel car)
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
                TempData["Error"] = $"Ошибка MySQL при обновлении: {ex.Message}";
            }

            return RedirectToAction("EditCar", new { id = car.Id });
        }

        // POST: Удалить автомобиль
        [HttpPost]
        public async Task<IActionResult> DeleteCar(int id)
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

        // POST: Добавить автомобиль
        [HttpPost]
        public async Task<IActionResult> AddCar(ReflashCarModel car)
        {
            

            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash;user=root;password=QaZmLp2414;CharSet=utf8;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();


                    string query = @"
                INSERT INTO reflash_cars (
                    brand, 
                    model, 
                    generation, 
                    engine, 
                    image,
                    about_ru, 
                    about_eng, 
                    about_ger,
                    result_ru, 
                    result_eng, 
                    result_ger,
                    engine_control_ru, 
                    engine_control_eng, 
                    engine_control_ger,
                    options_ru, 
                    options_eng, 
                    options_ger,
                    price_ru, 
                    price_eng, 
                    price_ger
                ) VALUES (
                    @brand, 
                    @model, 
                    @generation, 
                    @engine, 
                    @image,
                    @about_ru, 
                    @about_eng, 
                    @about_ger,
                    @result_ru, 
                    @result_eng, 
                    @result_ger,
                    @engine_control_ru, 
                    @engine_control_eng, 
                    @engine_control_ger,
                    @options_ru, 
                    @options_eng, 
                    @options_ger,
                    @price_ru, 
                    @price_eng, 
                    @price_ger
                )";

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

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                       

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Автомобиль {car.Brand} {car.Model} успешно добавлен";
                            return RedirectToAction("Cars");
                        }
                        else
                        {
                            System.IO.File.AppendAllText(@"C:\temp\addcar_log.txt", "ERROR: rowsAffected == 0\n");
                            TempData["Error"] = "Не удалось добавить автомобиль";
                            return RedirectToAction("Cars");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                
                TempData["Error"] = $"Ошибка: {ex.Message}";
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

        private readonly string sharedUploadsPath = @"C:\fotos";

        // GET: Страница партнеров
        public IActionResult Partners()
        {
            List<PartnersModel> partners = GetPartnersFromDatabase();
            return View(partners);
        }

        // GET: Страница редактирования конкретного автомобиля
        public IActionResult EditPartner(int id)
        {
            PartnersModel partner = GetPartnerById(id);
            if (partner == null)
            {
                TempData["Error"] = "Автомобиль не найден";
                return RedirectToAction("Cars");
            }
            return View(partner);
        }

        // Метод для получения конкретного автомобиля по ID
        private PartnersModel GetPartnerById(int id)
        {
            PartnersModel partner = null;
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM partners WHERE id = @id";

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
                                    photo = photoFileName, // Только имя файла
                                    vk = reader.IsDBNull(reader.GetOrdinal("vk_url")) ? "" : reader.GetString("vk_url"),
                                    website = reader.IsDBNull(reader.GetOrdinal("website_url")) ? "" : reader.GetString("website_url"),
                                    city = reader.IsDBNull(reader.GetOrdinal("city")) ? "" : reader.GetString("city"),
                                    street = reader.IsDBNull(reader.GetOrdinal("street")) ? "" : reader.GetString("street"),
                                    house = reader.IsDBNull(reader.GetOrdinal("house")) ? "" : reader.GetString("house"),
                                    longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? "" : reader.GetString("longitude"),
                                    latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? "" : reader.GetString("latitude")
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

            return partner;
        }

        // GET: Страница Создание нового автомобиля
        public IActionResult CreatePartner()
        {
            return View("CreatePartner", new PartnersModel());
        }

        // POST: Добавить партнера
        [HttpPost]
        public async Task<IActionResult> AddPartner(string name, string phone, string vk, string website,
            IFormFile photoFile, string photo, string city, string street, string house, string longitude, string latitude)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                string fileName = null;

                // Обработка загрузки фото
                if (photoFile != null && photoFile.Length > 0)
                {
                    var partnersPath = Path.Combine(sharedUploadsPath, "partners");

                    // Создаем папку, если не существует
                    if (!Directory.Exists(partnersPath))
                    {
                        Directory.CreateDirectory(partnersPath);
                    }

                    // Проверка типа файла
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(photoFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Разрешены только файлы изображений (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("Partners");
                    }

                    // Проверка размера (макс. 5MB)
                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Файл слишком большой (максимум 5MB)";
                        return RedirectToAction("Partners");
                    }

                    // Генерируем уникальное имя файла
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
                    (name, phone, photo_url, vk_url, website_url, city, street, house, longitude, latitude) 
                    VALUES 
                    (@name, @phone, @photo_url, @vk_url, @website_url, @city, @street, @house, @longitude, @latitude)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@phone", phone ?? "");
                        command.Parameters.AddWithValue("@photo_url", fileName ?? ""); //  Сохраняем только имя файла
                        command.Parameters.AddWithValue("@vk_url", vk ?? "");
                        command.Parameters.AddWithValue("@website_url", website ?? "");
                        command.Parameters.AddWithValue("@city", city ?? "");
                        command.Parameters.AddWithValue("@street", street ?? "");
                        command.Parameters.AddWithValue("@house", house ?? "");
                        command.Parameters.AddWithValue("@longitude", longitude ?? "");
                        command.Parameters.AddWithValue("@latitude", latitude ?? "");

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

        // POST: Обновить партнера
        [HttpPost]
        public async Task<IActionResult> UpdatePartner(int id, string name, string phone, string vk,
            string website, IFormFile photoFile, string photo, string city, string street, string house, string longitude, string latitude)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                string fileName = photo; // Изначально старое имя файла

                // Обработка загрузки нового фото
                if (photoFile != null && photoFile.Length > 0)
                {
                    var partnersPath = Path.Combine(sharedUploadsPath, "partners");

                    // Создаем папку, если не существует
                    if (!Directory.Exists(partnersPath))
                    {
                        Directory.CreateDirectory(partnersPath);
                    }

                    // Проверка типа файла
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(photoFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Разрешены только файлы изображений (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("Partners");
                    }

                    // Проверка размера (макс. 5MB)
                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Файл слишком большой (максимум 5MB)";
                        return RedirectToAction("Partners");
                    }

                    // Генерируем уникальное имя файла
                    fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(partnersPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }

                    // Удаляем старое фото, если оно существует
                    if (!string.IsNullOrEmpty(photo))
                    {
                        var oldFilePath = Path.Combine(partnersPath, photo);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                // Логируем, но не прерываем
                                Console.WriteLine($"Не удалось удалить старый файл: {ex.Message}");
                            }
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
                    latitude = @latitude

                    WHERE id = @id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@name", name ?? "");
                        command.Parameters.AddWithValue("@phone", phone ?? "");
                        command.Parameters.AddWithValue("@photo_url", fileName ?? ""); // Сохраняем только имя файла
                        command.Parameters.AddWithValue("@vk_url", vk ?? "");
                        command.Parameters.AddWithValue("@website_url", website ?? "");
                        command.Parameters.AddWithValue("@city", city ?? "");
                        command.Parameters.AddWithValue("@street", street ?? "");
                        command.Parameters.AddWithValue("@house", house ?? "");
                        command.Parameters.AddWithValue("@longitude", longitude ?? "");
                        command.Parameters.AddWithValue("@latitude", latitude ?? "");
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Партнер {name} успешно обновлен";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Партнер не найден";

                            // Если создали новый файл, но запись не обновилась - удаляем файл
                            if (photoFile != null && fileName != photo)
                            {
                                var newFilePath = Path.Combine(sharedUploadsPath, "partners", fileName);
                                if (System.IO.File.Exists(newFilePath))
                                {
                                    System.IO.File.Delete(newFilePath);
                                }
                            }
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
                string fileName = "";

                // Сначала получаем имя файла фото
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
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                var filePath = Path.Combine(sharedUploadsPath, "partners", fileName);

                                if (System.IO.File.Exists(filePath))
                                {
                                    try
                                    {
                                        System.IO.File.Delete(filePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        // Логируем, но не прерываем
                                        Console.WriteLine($"Не удалось удалить файл: {ex.Message}");
                                    }
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
                                var photoUrl = reader.IsDBNull(reader.GetOrdinal("photo_url")) ? "" : reader.GetString("photo_url");

                                // Если в БД сохранен полный путь, оставляем только имя файла
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
                                    photo = photoFileName, // Только имя файла
                                    vk = reader.IsDBNull(reader.GetOrdinal("vk_url")) ? "" : reader.GetString("vk_url"),
                                    website = reader.IsDBNull(reader.GetOrdinal("website_url")) ? "" : reader.GetString("website_url"),
                                    city = reader.IsDBNull(reader.GetOrdinal("city")) ? "" : reader.GetString("city"),
                                    street = reader.IsDBNull(reader.GetOrdinal("street")) ? "" : reader.GetString("street"),
                                    house = reader.IsDBNull(reader.GetOrdinal("house")) ? "" : reader.GetString("house"),
                                    longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? "" : reader.GetString("longitude"),
                                    latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? "" : reader.GetString("latitude")
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
        // НОВООСТИ
        // ============================================

        // GET: Страница новоостей
        public IActionResult News()
        {
            List<NewsModel> news = Get_News_from_data();
            return View(news);
        }

        // POST: Добавить новость
        [HttpPost]
        public async Task<IActionResult> AddNews(string news_name, string news_text, string news_date, IFormFile photoFile, string photo)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                string fileName = null;

                // Обработка загрузки фото
                if (photoFile != null && photoFile.Length > 0)
                {
                    var newsPath = Path.Combine(sharedUploadsPath, "news");

                    // Создаем папку, если не существует
                    if (!Directory.Exists(newsPath))
                    {
                        Directory.CreateDirectory(newsPath);
                    }

                    // Проверка типа файла
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(photoFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Разрешены только файлы изображений (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("News");
                    }

                    // Проверка размера (макс. 5MB)
                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Файл слишком большой (максимум 5MB)";
                        return RedirectToAction("News");
                    }

                    // Генерируем уникальное имя файла
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
                        command.Parameters.AddWithValue("@news_url", fileName ?? ""); //  Сохраняем только имя файла
                        command.Parameters.AddWithValue("news_date", news_date ?? "");
                        

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Новость {news_name} успешно добавлен";
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

        // POST: Обновить новость
        [HttpPost]
        public async Task<IActionResult> UpdateNews(int id, string news_name, string news_text, string news_date, IFormFile photoFile, string photo)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                string fileName = photo; // Изначально старое имя файла

                // Обработка загрузки нового фото
                if (photoFile != null && photoFile.Length > 0)
                {
                    var newsPath = Path.Combine(sharedUploadsPath, "news");

                    // Создаем папку, если не существует
                    if (!Directory.Exists(newsPath))
                    {
                        Directory.CreateDirectory(newsPath);
                    }

                    // Проверка типа файла
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(photoFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Разрешены только файлы изображений (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("Partners");
                    }

                    // Проверка размера (макс. 5MB)
                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Файл слишком большой (максимум 5MB)";
                        return RedirectToAction("Partners");
                    }

                    // Генерируем уникальное имя файла
                    fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(newsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }

                    // Удаляем старое фото, если оно существует
                    if (!string.IsNullOrEmpty(photo))
                    {
                        var oldFilePath = Path.Combine(newsPath, photo);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                // Логируем, но не прерываем
                                Console.WriteLine($"Не удалось удалить старый файл: {ex.Message}");
                            }
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
                        command.Parameters.AddWithValue("@news_url", fileName ?? ""); // Сохраняем только имя файла
                        command.Parameters.AddWithValue("@news_date", news_date ?? "");
                       

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"Новость {news_name} успешно обновлен";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Нвость не найден";

                            // Если создали новый файл, но запись не обновилась - удаляем файл
                            if (photoFile != null && fileName != photo)
                            {
                                var newFilePath = Path.Combine(sharedUploadsPath, "news", fileName);
                                if (System.IO.File.Exists(newFilePath))
                                {
                                    System.IO.File.Delete(newFilePath);
                                }
                            }
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

        // POST: Удалить новость
        [HttpPost]
        public async Task<IActionResult> DeleteNews(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                string fileName = "";

                // Сначала получаем имя файла фото
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

                // Удаляем запись из БД
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
                            // Удаляем файл фото, если он существует
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                var filePath = Path.Combine(sharedUploadsPath, "news", fileName);

                                if (System.IO.File.Exists(filePath))
                                {
                                    try
                                    {
                                        System.IO.File.Delete(filePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        // Логируем, но не прерываем
                                        Console.WriteLine($"Не удалось удалить файл: {ex.Message}");
                                    }
                                }
                            }

                            TempData["Message"] = "Новость успешно удален";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Новсть не найден";
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

        // Метод для получения списка нвостей из БД
        private List<NewsModel> Get_News_from_data()
        {
            List<NewsModel> news = new List<NewsModel>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT * FROM news ORDER BY news_name";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var photoUrl = reader.IsDBNull(reader.GetOrdinal("news_url")) ? "" : reader.GetString("news_url");

                                // ВАЖНО: Если в БД сохранен полный путь, оставляем только имя файла
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
                TempData["Error"] = $"Ошибка MySQL при загрузке партнеров: {ex.Message}";
            }
            return news;
        }

        // ============================================
        // Главная страница
        // ============================================

        // GET: Страница для редактироования основной страницы
        public IActionResult FurstPage()
        {
            FurstPageModel first_page = GetFurstPageFromDatabase();   
            
            return View(first_page);
        }

        //Полуение данных главной странцицы из бд
        private FurstPageModel GetFurstPageFromDatabase()
        {
            FurstPageModel furst_page = null;
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=first_page_content;user=root;password=;";

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
                                    // Изображения
                                    image_1 = reader.IsDBNull(reader.GetOrdinal("image_1")) ? "" : reader.GetString("image_1"),
                                    image_2 = reader.IsDBNull(reader.GetOrdinal("image_2")) ? "" : reader.GetString("image_2"),
                                    image_3 = reader.IsDBNull(reader.GetOrdinal("image_3")) ? "" : reader.GetString("image_3"),
                                    image_4 = reader.IsDBNull(reader.GetOrdinal("image_4")) ? "" : reader.GetString("image_4"),

                                    // Заголовки (первый блок)
                                    head_1_ru = reader.IsDBNull(reader.GetOrdinal("head_1_ru")) ? "" : reader.GetString("head_1_ru"),
                                    head_1_eng = reader.IsDBNull(reader.GetOrdinal("head_1_eng")) ? "" : reader.GetString("head_1_eng"),
                                    head_1_ger = reader.IsDBNull(reader.GetOrdinal("head_1_ger")) ? "" : reader.GetString("head_1_ger"),

                                    // Заголовки (второй блок)
                                    head_2_ru = reader.IsDBNull(reader.GetOrdinal("head_2_ru")) ? "" : reader.GetString("head_2_ru"),
                                    head_2_eng = reader.IsDBNull(reader.GetOrdinal("head_2_eng")) ? "" : reader.GetString("head_2_eng"),
                                    head_2_ger = reader.IsDBNull(reader.GetOrdinal("head_2_ger")) ? "" : reader.GetString("head_2_ger"),

                                    // Основной текст
                                    text_ru = reader.IsDBNull(reader.GetOrdinal("text_ru")) ? "" : reader.GetString("text_ru"),
                                    text_eng = reader.IsDBNull(reader.GetOrdinal("text_eng")) ? "" : reader.GetString("text_eng"),
                                    text_ger = reader.IsDBNull(reader.GetOrdinal("text_ger")) ? "" : reader.GetString("text_ger"),

                                    // Блок 1
                                    block_1_ru = reader.IsDBNull(reader.GetOrdinal("block_1_ru")) ? "" : reader.GetString("block_1_ru"),
                                    block_1_eng = reader.IsDBNull(reader.GetOrdinal("block_1_eng")) ? "" : reader.GetString("block_1_eng"),
                                    block_1_ger = reader.IsDBNull(reader.GetOrdinal("block_1_ger")) ? "" : reader.GetString("block_1_ger"),

                                    // Блок 2
                                    block_2_ru = reader.IsDBNull(reader.GetOrdinal("block_2_ru")) ? "" : reader.GetString("block_2_ru"),
                                    block_2_eng = reader.IsDBNull(reader.GetOrdinal("block_2_eng")) ? "" : reader.GetString("block_2_eng"),
                                    block_2_ger = reader.IsDBNull(reader.GetOrdinal("block_2_ger")) ? "" : reader.GetString("block_2_ger"),

                                    // Блок 3
                                    block_3_ru = reader.IsDBNull(reader.GetOrdinal("block_3_ru")) ? "" : reader.GetString("block_3_ru"),
                                    block_3_eng = reader.IsDBNull(reader.GetOrdinal("block_3_eng")) ? "" : reader.GetString("block_3_eng"),
                                    block_3_ger = reader.IsDBNull(reader.GetOrdinal("block_3_ger")) ? "" : reader.GetString("block_3_ger"),

                                    // Блок 4
                                    block_4_ru = reader.IsDBNull(reader.GetOrdinal("block_4_ru")) ? "" : reader.GetString("block_4_ru"),
                                    block_4_eng = reader.IsDBNull(reader.GetOrdinal("block_4_eng")) ? "" : reader.GetString("block_4_eng"),
                                    block_4_ger = reader.IsDBNull(reader.GetOrdinal("block_4_ger")) ? "" : reader.GetString("block_4_ger"),

                                    // Блок 5
                                    block_5_ru = reader.IsDBNull(reader.GetOrdinal("block_5_ru")) ? "" : reader.GetString("block_5_ru"),
                                    block_5_eng = reader.IsDBNull(reader.GetOrdinal("block_5_eng")) ? "" : reader.GetString("block_5_eng"),
                                    block_5_ger = reader.IsDBNull(reader.GetOrdinal("block_5_ger")) ? "" : reader.GetString("block_5_ger"),

                                    // Блок 6
                                    block_6_ru = reader.IsDBNull(reader.GetOrdinal("block_6_ru")) ? "" : reader.GetString("block_6_ru"),
                                    block_6_eng = reader.IsDBNull(reader.GetOrdinal("block_6_eng")) ? "" : reader.GetString("block_6_eng"),
                                    block_6_ger = reader.IsDBNull(reader.GetOrdinal("block_6_ger")) ? "" : reader.GetString("block_6_ger")
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка MySQL при загрузке данных главной страницы: {ex.Message}");
                // Если используете TempData в контроллере
                // TempData["Error"] = $"Ошибка MySQL: {ex.Message}";
            }

            return furst_page;
        }

        //Редактирование основнй страницы
        [HttpPost]
        public async Task<IActionResult> UpdateFurstPage(FurstPageModel page)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflah_controler_db;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Проверяем, существует ли запись
                    string checkQuery = "SELECT COUNT(*) FROM first_page_content";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    string query;
                    if (count == 0)
                    {
                        // INSERT - если записи нет
                        query = @"INSERT INTO first_page_content (
                                    image_1, image_2, image_3, image_4,
                                    head_1_ru, head_1_eng, head_1_ger,
                                    head_2_ru, head_2_eng, head_2_ger,
                                    text_ru, text_eng, text_ger,
                                    block_1_ru, block_1_eng, block_1_ger,
                                    block_2_ru, block_2_eng, block_2_ger,
                                    block_3_ru, block_3_eng, block_3_ger,
                                    block_4_ru, block_4_eng, block_4_ger,
                                    block_5_ru, block_5_eng, block_5_ger,
                                    block_6_ru, block_6_eng, block_6_ger
                                ) VALUES (
                                    @image_1, @image_2, @image_3, @image_4,
                                    @head_1_ru, @head_1_eng, @head_1_ger,
                                    @head_2_ru, @head_2_eng, @head_2_ger,
                                    @text_ru, @text_eng, @text_ger,
                                    @block_1_ru, @block_1_eng, @block_1_ger,
                                    @block_2_ru, @block_2_eng, @block_2_ger,
                                    @block_3_ru, @block_3_eng, @block_3_ger,
                                    @block_4_ru, @block_4_eng, @block_4_ger,
                                    @block_5_ru, @block_5_eng, @block_5_ger,
                                    @block_6_ru, @block_6_eng, @block_6_ger
                                )";
                    }
                    else
                    {
                        // UPDATE - если запись существует
                        query = @"UPDATE first_page_content SET 
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
                                    block_2_ru = @block_2_ru,
                                    block_2_eng = @block_2_eng,
                                    block_2_ger = @block_2_ger,
                                    block_3_ru = @block_3_ru,
                                    block_3_eng = @block_3_eng,
                                    block_3_ger = @block_3_ger,
                                    block_4_ru = @block_4_ru,
                                    block_4_eng = @block_4_eng,
                                    block_4_ger = @block_4_ger,
                                    block_5_ru = @block_5_ru,
                                    block_5_eng = @block_5_eng,
                                    block_5_ger = @block_5_ger,
                                    block_6_ru = @block_6_ru,
                                    block_6_eng = @block_6_eng,
                                    block_6_ger = @block_6_ger
                                WHERE id = 1";
                    }

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // Изображения
                        command.Parameters.AddWithValue("@image_1", page.image_1 ?? "");
                        command.Parameters.AddWithValue("@image_2", page.image_2 ?? "");
                        command.Parameters.AddWithValue("@image_3", page.image_3 ?? "");
                        command.Parameters.AddWithValue("@image_4", page.image_4 ?? "");

                        // Заголовки (первый блок)
                        command.Parameters.AddWithValue("@head_1_ru", page.head_1_ru ?? "");
                        command.Parameters.AddWithValue("@head_1_eng", page.head_1_eng ?? "");
                        command.Parameters.AddWithValue("@head_1_ger", page.head_1_ger ?? "");

                        // Заголовки (второй блок)
                        command.Parameters.AddWithValue("@head_2_ru", page.head_2_ru ?? "");
                        command.Parameters.AddWithValue("@head_2_eng", page.head_2_eng ?? "");
                        command.Parameters.AddWithValue("@head_2_ger", page.head_2_ger ?? "");

                        // Основной текст
                        command.Parameters.AddWithValue("@text_ru", page.text_ru ?? "");
                        command.Parameters.AddWithValue("@text_eng", page.text_eng ?? "");
                        command.Parameters.AddWithValue("@text_ger", page.text_ger ?? "");

                        // Блок 1
                        command.Parameters.AddWithValue("@block_1_ru", page.block_1_ru ?? "");
                        command.Parameters.AddWithValue("@block_1_eng", page.block_1_eng ?? "");
                        command.Parameters.AddWithValue("@block_1_ger", page.block_1_ger ?? "");

                        // Блок 2
                        command.Parameters.AddWithValue("@block_2_ru", page.block_2_ru ?? "");
                        command.Parameters.AddWithValue("@block_2_eng", page.block_2_eng ?? "");
                        command.Parameters.AddWithValue("@block_2_ger", page.block_2_ger ?? "");

                        // Блок 3
                        command.Parameters.AddWithValue("@block_3_ru", page.block_3_ru ?? "");
                        command.Parameters.AddWithValue("@block_3_eng", page.block_3_eng ?? "");
                        command.Parameters.AddWithValue("@block_3_ger", page.block_3_ger ?? "");

                        // Блок 4
                        command.Parameters.AddWithValue("@block_4_ru", page.block_4_ru ?? "");
                        command.Parameters.AddWithValue("@block_4_eng", page.block_4_eng ?? "");
                        command.Parameters.AddWithValue("@block_4_ger", page.block_4_ger ?? "");

                        // Блок 5
                        command.Parameters.AddWithValue("@block_5_ru", page.block_5_ru ?? "");
                        command.Parameters.AddWithValue("@block_5_eng", page.block_5_eng ?? "");
                        command.Parameters.AddWithValue("@block_5_ger", page.block_5_ger ?? "");

                        // Блок 6
                        command.Parameters.AddWithValue("@block_6_ru", page.block_6_ru ?? "");
                        command.Parameters.AddWithValue("@block_6_eng", page.block_6_eng ?? "");
                        command.Parameters.AddWithValue("@block_6_ger", page.block_6_ger ?? "");

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = "Данные главной страницы успешно обновлены";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "Данные не найдены";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"Ошибка MySQL при обновлении: {ex.Message}";
            }

            return RedirectToAction("FurstPage");
        }

        // ============================================
        // ОБЩИЕ МЕТОДЫ
        // ============================================

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task NotifyReaderSite()
        {
            try
            {
                string readerSiteUrl = "http://localhost:80";

                // Создаем handler с отключенной проверкой SSL
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using var client = new HttpClient(handler);
                await client.PostAsync($"{readerSiteUrl}/api/db-notify", null);

                Console.WriteLine("✅ Уведомление отправлено читающему сайту");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки: {ex.Message}");
            }
        }
    }
}