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
    public class NewsController : AppController
    {
        public NewsController(IConfiguration configuration, IHubContext<DatabaseHub> hubContext)
            : base(configuration, hubContext)
        {
        }
        // ============================================
        // НОВОСТИ
        // ============================================

        [HttpGet("News")]
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

        [HttpPost("AddNews")]
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

        [HttpPost("UpdateNews")]
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

        [HttpPost("DeleteNews")]
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
    }
}
