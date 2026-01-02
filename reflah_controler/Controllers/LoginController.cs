using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using reflah_controler.Models;
using System.Collections.Generic;

namespace reflah_controler.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

     
        public IActionResult Index()
        {
            return View();
        }

       
        [HttpPost]
        public IActionResult Check(string login, string password)
        {
            List<AdminsModel> admins = new List<AdminsModel>();

           
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? "server=localhost;port=3306;database=reflash_cars;user=root;password=;";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                   
                    string query = "SELECT * FROM admins";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                admins.Add(new AdminsModel
                                {
                                    Login = reader.GetString("login"),
                                    Password = reader.GetString("password")
                                });
                            }
                        }
                    }
                }

               
                bool isAuthenticated = false;
                foreach (var admin in admins)
                {
                    if (admin.Login == login && admin.Password == password)
                    {
                        isAuthenticated = true;
                        break;
                    }
                }

                if (isAuthenticated)
                {
                    
                    ViewBag.Message = "Вход выполнен успешно!";
                    
                }
                else
                {
                    ViewBag.Error = "Неверный логин или пароль";
                }
            }
            catch (MySqlException ex)
            {
                ViewBag.Error = $"Ошибка MySQL: {ex.Message}";
            }

            
            return View("Index");
        }
    }
}