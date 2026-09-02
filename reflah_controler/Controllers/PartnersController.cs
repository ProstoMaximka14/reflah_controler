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
    public class PartnersController : AppController
    {
        public PartnersController(IConfiguration configuration, IHubContext<DatabaseHub> hubContext)
            : base(configuration, hubContext)
        {
        }
        // ============================================
        // РџРђР РўРќРЃР Р«
        // ============================================

        [HttpGet("Partners")]
        public IActionResult Partners()
        {
            List<PartnersModel> partners = GetPartnersFromDatabase();
            return View(partners);
        }

        [HttpGet("EditPartner/{id?}")]
        public IActionResult EditPartner(int id)
        {
            PartnersModel partner = GetPartnerById(id);
            if (partner == null)
            {
                TempData["Error"] = "РџР°СЂС‚РЅРµСЂ РЅРµ РЅР°Р№РґРµРЅ";
                return RedirectToAction("Partners");
            }
            return View(partner);
        }

        [HttpGet("CreatePartner")]
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
                                    vk_group_url, telegram, whatsapp, email, point_name, info
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
                                    info = reader.IsDBNull(reader.GetOrdinal("info")) ? "" : reader.GetString("info"),
                                    point_name = reader.IsDBNull(reader.GetOrdinal("point_name")) ? "" : reader.GetString("point_name")
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL РїСЂРё Р·Р°РіСЂСѓР·РєРµ РїР°СЂС‚РЅРµСЂР°: {ex.Message}";
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
                                    vk_group_url, telegram, whatsapp, email, point_name, info
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
                                    info = reader.IsDBNull(reader.GetOrdinal("info")) ? "" : reader.GetString("info"),
                                    point_name = reader.IsDBNull(reader.GetOrdinal("point_name")) ? "" : reader.GetString("point_name")
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL РїСЂРё Р·Р°РіСЂСѓР·РєРµ РїР°СЂС‚РЅРµСЂРѕРІ: {ex.Message}";
            }
            return partners;
        }

        [HttpPost("AddPartner")]
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
            string info,
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
                        TempData["Error"] = "Р Р°Р·СЂРµС€РµРЅС‹ С‚РѕР»СЊРєРѕ С„Р°Р№Р»С‹ РёР·РѕР±СЂР°Р¶РµРЅРёР№ (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("Partners");
                    }

                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Р¤Р°Р№Р» СЃР»РёС€РєРѕРј Р±РѕР»СЊС€РѕР№ (РјР°РєСЃРёРјСѓРј 5MB)";
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
                        vk_group_url, telegram, whatsapp, email, point_name, info) 
                    VALUES 
                        (@name, @phone, @photo_url, @vk_url, @website_url, 
                        @city, @street, @house, @longitude, @latitude,
                        @vk_group, @telegram, @whatsapp, @email, @point_name, @info)";

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
                        command.Parameters.AddWithValue("@info", info ?? "");
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"РџР°СЂС‚РЅРµСЂ {name} СѓСЃРїРµС€РЅРѕ РґРѕР±Р°РІР»РµРЅ";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "РќРµ СѓРґР°Р»РѕСЃСЊ РґРѕР±Р°РІРёС‚СЊ РїР°СЂС‚РЅРµСЂР°";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL РїСЂРё РґРѕР±Р°РІР»РµРЅРёРё: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° РїСЂРё Р·Р°РіСЂСѓР·РєРµ С„Р°Р№Р»Р°: {ex.Message}";
            }

            return RedirectToAction("Partners");
        }

        [HttpPost("UpdatePartner")]
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
            string info,
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
                        TempData["Error"] = "Р Р°Р·СЂРµС€РµРЅС‹ С‚РѕР»СЊРєРѕ С„Р°Р№Р»С‹ РёР·РѕР±СЂР°Р¶РµРЅРёР№ (JPG, PNG, GIF, WebP)";
                        return RedirectToAction("Partners");
                    }

                    if (photoFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Р¤Р°Р№Р» СЃР»РёС€РєРѕРј Р±РѕР»СЊС€РѕР№ (РјР°РєСЃРёРјСѓРј 5MB)";
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
                        point_name = @point_name,
                        info = @info
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
                        command.Parameters.AddWithValue("@info", info ?? "");

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            TempData["Message"] = $"РџР°СЂС‚РЅРµСЂ {name} СѓСЃРїРµС€РЅРѕ РѕР±РЅРѕРІР»РµРЅ";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "РџР°СЂС‚РЅРµСЂ РЅРµ РЅР°Р№РґРµРЅ";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL РїСЂРё РѕР±РЅРѕРІР»РµРЅРёРё: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° РїСЂРё РѕР±СЂР°Р±РѕС‚РєРµ С„Р°Р№Р»Р°: {ex.Message}";
            }

            return RedirectToAction("Partners");
        }

        [HttpPost("DeletePartner")]
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

                            TempData["Message"] = "РџР°СЂС‚РЅРµСЂ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅ";
                            await NotifyReaderSite();
                        }
                        else
                        {
                            TempData["Error"] = "РџР°СЂС‚РЅРµСЂ РЅРµ РЅР°Р№РґРµРЅ";
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["Error"] = $"РћС€РёР±РєР° MySQL РїСЂРё СѓРґР°Р»РµРЅРёРё: {ex.Message}";
            }

            return RedirectToAction("Partners");
        }
    }
}
