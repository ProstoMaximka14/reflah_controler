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
    public class FurstPageController : AppController
    {
        public FurstPageController(IConfiguration configuration, IHubContext<DatabaseHub> hubContext)
            : base(configuration, hubContext)
        {
        }
        // ============================================
        // ГЛАВНАЯ СТРАНИЦА
        // ============================================

        [HttpGet("FurstPage")]
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

        [HttpPost("UpdateFurstPage")]
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
    }
}
