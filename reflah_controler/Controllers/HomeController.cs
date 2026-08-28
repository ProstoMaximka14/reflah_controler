using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using reflah_controler.Hubs;
using reflah_controler.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace reflah_controler.Controllers
{
    public class HomeController : AppController
    {
        public HomeController(IConfiguration configuration, IHubContext<DatabaseHub> hubContext)
            : base(configuration, hubContext)
        {
        }

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
