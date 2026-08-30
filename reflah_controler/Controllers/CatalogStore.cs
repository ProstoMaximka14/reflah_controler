using MySql.Data.MySqlClient;
using reflah_controler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace reflah_controler.Controllers
{
    public class CatalogStore
    {
        private readonly string _connectionString;
        public Action<string> OnError { get; set; }

        public CatalogStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }


        public List<ReflashCarModel> GetCarsFromDatabase()
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
                            additional_price_ru, old_url, sort_order, SortOrder2
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
                                    SortOrder = sortOrder,
                                    SortOrder2 = reader.IsDBNull(reader.GetOrdinal("SortOrder2")) ? 0 : reader.GetInt32("SortOrder2")
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                OnError?.Invoke($"РћС€РёР±РєР° MySQL РїСЂРё Р·Р°РіСЂСѓР·РєРµ Р°РІС‚РѕРјРѕР±РёР»РµР№: {ex.Message}");
            }

            return cars;
        }


        private int GetNextSortOrderAndUpdate(List<ReflashCarModel> cars, string brand, string model, string generation, int carId)
        {
            // РС‰РµРј РјР°РєСЃРёРјР°Р»СЊРЅС‹Р№ sort_order СЃСЂРµРґРё СѓР¶Рµ РґРѕР±Р°РІР»РµРЅРЅС‹С… РјР°С€РёРЅ СЃ С‚Р°РєРёРјРё Р¶Рµ brand, model, generation
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

            // РќРѕРІС‹Р№ sort_order = maxOrder + 1
            int newSortOrder = maxOrder + 1;

            // РћР‘РќРћР’Р›РЇР•Рњ Р—РќРђР§Р•РќРР• Р’ Р‘РђР—Р• Р”РђРќРќР«РҐ
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
                Console.WriteLine($"вњ… РћР±РЅРѕРІР»С‘РЅ sort_order РґР»СЏ РјР°С€РёРЅС‹ #{carId}: {newSortOrder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"вќЊ РћС€РёР±РєР° РїСЂРё РѕР±РЅРѕРІР»РµРЅРёРё sort_order РґР»СЏ РјР°С€РёРЅС‹ #{carId}: {ex.Message}");
            }

            return newSortOrder;
        }


        public ReflashCarModel GetCarById(int id)
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
                                    additional_price_ru, old_url, sort_order, SortOrder2
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
                                    SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order"),
                                    SortOrder2 = reader.IsDBNull(reader.GetOrdinal("SortOrder2")) ? 0 : reader.GetInt32("SortOrder2")
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                OnError?.Invoke($"РћС€РёР±РєР° MySQL РїСЂРё Р·Р°РіСЂСѓР·РєРµ Р°РІС‚РѕРјРѕР±РёР»СЏ: {ex.Message}");
            }

            return car;
        }


        public List<PriceModel> GetPricesFromDatabase()
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
                OnError?.Invoke($"РћС€РёР±РєР° MySQL: {ex.Message}");
            }
            return prices;
        }


        public PriceModel GetPriceById(int id)
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
                OnError?.Invoke($"РћС€РёР±РєР° MySQL: {ex.Message}");
            }
            return price;
        }


        public List<TemplatePriceModel> GetTemplatePricesFromDatabase()
        {
            List<TemplatePriceModel> templates = new List<TemplatePriceModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_price ORDER BY sort_order ASC, id ASC";  
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
                                Prices = reader.IsDBNull(reader.GetOrdinal("prices")) ? "" : reader.GetString("prices"),
                                UsedInCars = reader.IsDBNull(reader.GetOrdinal("used_in_cars")) ? "" : reader.GetString("used_in_cars"),
                                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order")  
                            });
                        }
                    }
                }
            }
            return templates;
        }


        public TemplatePriceModel GetTemplatePriceById(int id)
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


        public List<AboutModel> GetAboutsFromDatabase()
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


        public AboutModel GetAboutById(int id)
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


        public ResultModel GetResultById(int id)
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


        public EngineControlModel GetEngineControlById(int id)
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


        public List<TemplateAboutModel> GetTemplateAboutsFromDatabase()
        {
            List<TemplateAboutModel> templates = new List<TemplateAboutModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_about ORDER BY sort_order ASC, id ASC";  // ИЗМЕНЕНО
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
                                UsedInCars = reader.IsDBNull(reader.GetOrdinal("used_in_cars")) ? "" : reader.GetString("used_in_cars"),
                                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order")  // ДОБАВИТЬ
                            });
                        }
                    }
                }
            }
            return templates;
        }


        public List<ResultModel> GetResultsFromDatabase()
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


        public List<TemplateResultModel> GetTemplateResultsFromDatabase()
        {
            List<TemplateResultModel> templates = new List<TemplateResultModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_result ORDER BY sort_order ASC, id ASC";  
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
                                Ids = reader.IsDBNull(reader.GetOrdinal("ids")) ? "" : reader.GetString("ids"),
                                UsedInCars = reader.IsDBNull(reader.GetOrdinal("used_in_cars")) ? "" : reader.GetString("used_in_cars"),
                                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order")  
                            });
                        }
                    }
                }
            }
            return templates;
        }


        public List<EngineControlModel> GetEngineControlsFromDatabase()
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


        public List<TemplateEngineControlModel> GetTemplateEngineControlsFromDatabase()
        {
            List<TemplateEngineControlModel> templates = new List<TemplateEngineControlModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_engine_control ORDER BY sort_order ASC, id ASC";  
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
                                Ids = reader.IsDBNull(reader.GetOrdinal("ids")) ? "" : reader.GetString("ids"),
                                UsedInCars = reader.IsDBNull(reader.GetOrdinal("used_in_cars")) ? "" : reader.GetString("used_in_cars"),
                                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order") 
                            });
                        }
                    }
                }
            }
            return templates;
        }


        public List<GraficModel> GetGraficsFromDatabase()
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


        public GraficModel GetGraficById(int id)
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


        public List<TemplateGraficModel> GetTemplateGraficsFromDatabase()
        {
            List<TemplateGraficModel> templates = new List<TemplateGraficModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM template_grafic ORDER BY sort_order ASC, id ASC";
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
                                Ids = reader.IsDBNull(reader.GetOrdinal("ids")) ? "" : reader.GetString("ids"),
                                UsedInCars = reader.IsDBNull(reader.GetOrdinal("used_in_cars")) ? "" : reader.GetString("used_in_cars"),
                                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order")  
                            });
                        }
                    }
                }
            }
            return templates;
        }


        public List<AdditionalPriceModel> GetAdditionalPricesFromDatabase()
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
                                // ===== РќРћР’Р«Р• РџРћР›РЇ =====
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


        public AdditionalPriceModel GetAdditionalPriceById(int id)
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
                                // ===== РќРћР’Р«Р• РџРћР›РЇ =====
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


        public List<TemplateAdditionalPriceModel> GetTemplateAdditionalPricesFromDatabase()
        {
            List<TemplateAdditionalPriceModel> templates = new List<TemplateAdditionalPriceModel>();
            string connectionString = GetConnectionString();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT id, name, price_ids, used_in_cars, sort_order FROM template_additional_prices ORDER BY sort_order ASC, id ASC";  
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
                                UsedInCars = reader.IsDBNull(reader.GetOrdinal("used_in_cars")) ? "" : reader.GetString("used_in_cars"),
                                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order")  
                            });
                        }
                    }
                }
            }
            return templates;
        }


        public static string FirstNonEmpty(params string[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Length > 120 ? v.Substring(0, 120) + "вЂ¦" : v;
            }
            return "(РїСѓСЃС‚Рѕ)";
        }


        public static List<int> ParseIdList(string idsString)
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


        public static string AppendIdToList(string existing, int id)
        {
            var list = ParseIdList(existing);
            if (!list.Contains(id))
                list.Add(id);
            return string.Join(",", list);
        }


        public static string RemoveIdFromList(string existing, int id)
        {
            var list = ParseIdList(existing).Where(x => x != id).ToList();
            return string.Join(",", list);
        }


        public async Task<AboutModel> GetAboutByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand("SELECT id, text_ru, text_eng, text_ger FROM about WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
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


        public async Task<ResultModel> GetResultByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand("SELECT id, text_ru, text_eng, text_ger FROM result WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
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


        public async Task<EngineControlModel> GetEngineControlByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand("SELECT id, text_ru, text_eng, text_ger FROM engine_control WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
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


        public async Task<PriceModel> GetPriceByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand(
                "SELECT id, name_ru, name_eng, name_ger, base_price, pro_price, " +
                "base_price_eng, pro_price_eng, base_price_ger, pro_price_ger, " +
                "info_ru, info_eng, info_ger FROM price WHERE id = @id",
                connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
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


        public async Task<GraficModel> GetGraficByIdAsync(MySqlConnection connection, int id)
        {
            using (var cmd = new MySqlCommand("SELECT id, name, name_eng, name_ger, image, " +
                "description_ru, description_eng, description_ger FROM grafic WHERE id = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
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


        public string GetTemplateTable(string type)
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


        public string GetRecordTable(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return ""; // в†ђ Р’РѕР·РІСЂР°С‰Р°РµРј РїСѓСЃС‚СѓСЋ СЃС‚СЂРѕРєСѓ РІРјРµСЃС‚Рѕ РёСЃРєР»СЋС‡РµРЅРёСЏ
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


        // HomeController.EditCarBlocks.cs вЂ” РґРѕР±Р°РІРёС‚СЊ РјРµС‚РѕРґС‹

        /// <summary>
        /// РћР±РЅРѕРІР»СЏРµС‚ РїРѕСЂСЏРґРѕРє ID РІ СЃС‚СЂРѕРєРµ
        /// </summary>
        public string ReorderIdsString(string idsString, List<int> newOrder)
        {
            if (string.IsNullOrEmpty(idsString))
                return "";

            var existingIds = ParseIdList(idsString);
            var ordered = newOrder.Where(id => existingIds.Contains(id)).ToList();

            // Р”РѕР±Р°РІР»СЏРµРј ID, РєРѕС‚РѕСЂС‹Рµ РµСЃС‚СЊ РІ СЃСѓС‰РµСЃС‚РІСѓСЋС‰РµРј СЃРїРёСЃРєРµ, РЅРѕ РЅРµ Р±С‹Р»Рё РїРµСЂРµРґР°РЅС‹
            foreach (var id in existingIds)
            {
                if (!ordered.Contains(id))
                    ordered.Add(id);
            }

            return string.Join(",", ordered);
        }

    }
}
