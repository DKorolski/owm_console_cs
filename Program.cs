using Newtonsoft.Json;
using System.Data;
using Microsoft.Data.Sqlite;
namespace ConsoleWeatherAPI
{
    class Program
    {

        static async Task Main(string[] args)
        {
            string apiKey = "600e7808c4d14fe07520213ad9926375";
            string target_folder = System.IO.Directory.GetCurrentDirectory();
            string DB_NAME = "weatherdata.db";
            string db_path = Path.Combine(target_folder, Path.GetFileName(DB_NAME));
            Console.WriteLine(db_path);
            if (File.Exists(db_path))
            {
                Console.WriteLine("DB file exists");
            }
            else
            {
                using (var connection = new SqliteConnection("Data Source=" + DB_NAME))

                {
                    connection.Open();
                    SqliteCommand command1 = new SqliteCommand();
                    command1.Connection = connection;
                    command1.CommandText = @"CREATE TABLE IF NOT EXISTS city (
                                                city_sk INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                                owm_city_id INTEGER NOT NULL,
                                                name TEXT NOT NULL,
                                                country TEXT NOT NULL,
                                                state TEXT,
                                                lat REAL NOT NULL,
                                                lon REAL NOT NULL
                                               ); ";
                    command1.ExecuteNonQuery();
                    command1.CommandText = @"CREATE TABLE IF NOT EXISTS forecast (
                                                weather_sk INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                                city_sk INTEGER NOT NULL,
                                                owm_city_id INTEGER NOT NULL,
                                                dt_txt TEXT NOT NULL,
                                                main_temp REAL NOT NULL,
                                                wind_speed REAL,
                                                wind_deg REAL NOT NULL,
                                                humidity REAL NOT NULL,
                                                description TEXT,
                                                FOREIGN KEY(city_sk) REFERENCES city(city_sk)
                                            );";
                    command1.ExecuteNonQuery();
                    command1.CommandText = @"INSERT INTO 
                                                city (owm_city_id, name, country, state, lat, lon)
                                            VALUES
                                                (3245, 'Taglag', 'IR', '', '', ''),
                                                (833, 'Heşār-e Sefīd', 'IR', '', '', ''),
                                                (2960, 'Ayn Halāqīm', 'SY', '', '', '')
                                               ";
                    int number = command1.ExecuteNonQuery();
                    Console.WriteLine("Таблицы city, forecast созданы");
                    Console.WriteLine($"В таблицу city добавлено объектов: {number}");
                    connection.Close();
                }
            }
            if (File.Exists(db_path))
            {
                string sql_query = "SELECT * FROM city";
                var counter = 0;
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://api.openweathermap.org");
                using (var connection = new SqliteConnection("Data Source=" + DB_NAME))

                {
                    connection.Open();
                    SqliteCommand command2 = new SqliteCommand(sql_query, connection);
                    command2.Connection = connection;
                    using (SqliteDataReader reader = command2.ExecuteReader())
                    {
                        if (reader.HasRows) // если есть данные
                        {
                            while (reader.Read())   // построчно считываем данные
                            {
                                var sk = reader.GetValue(0);
                                var id = reader.GetValue(1);
                                var name = reader.GetValue(2);
                                string sql_stored_check = @"SELECT 
                                                                    (case when (substr(dt_txt,0,11) = date('now')) then 1  else 0 end) res 
                                                                FROM
                                                                    forecast
                                                                WHERE
                                                                    owm_city_id="+ id;
                                SqliteCommand command3 = new SqliteCommand(sql_stored_check, connection);
                                command3.Connection = connection;
                                DataTable dt = new DataTable();
                                dt.Load(command3.ExecuteReader()); // если есть данные по городу и дате 
                                if (dt.Rows.Count > 0)
                                {
                                    Console.WriteLine($"Forecasts already stored for city: {name}");
                                }
                                else
                                {
                                    Console.WriteLine($"New forecasts for city: {name}");
                                    Console.WriteLine($"{sk} \t {id} \t {name}");
                                    var choose_id = id;

                                    var response = await client.GetAsync($"/data/2.5/forecast?id={choose_id}&units=metric&lang=en&appid={apiKey}");
                                    var stringResult = await response.Content.ReadAsStringAsync();
                                    var obj = JsonConvert.DeserializeObject<dynamic>(stringResult);
                                    foreach (var kvp in obj.list)
                                    {
                                        var tmpDegrees = Math.Round(((float)kvp.main.temp));
                                        var dt_txt = kvp.dt_txt;
                                        var humidity = kvp.main.humidity;
                                        var wind_speed = kvp.wind.speed;
                                        var wind_deg = kvp.wind.deg;
                                        var w_description = kvp.weather[0].description;
                                        SqliteCommand command1 = new SqliteCommand();
                                        command1.Connection = connection;
                                        command1.CommandText = @"INSERT INTO 
                                                forecast (city_sk, owm_city_id, dt_txt, main_temp, wind_speed, wind_deg, humidity, description)
                                            VALUES
                                               (" + sk + ", " + id + ",'" + dt_txt + "','" + tmpDegrees + "', '" + wind_speed + "', '" + wind_deg + "','" + humidity + "','" + w_description + "')";
                                        int number = command1.ExecuteNonQuery();
                                        counter += number;
                                    }
                                }
                            }
                            Console.WriteLine($"В таблицу weather добавлено строк: {counter}");
                        }
                    }
                }
            }
        }
    }
}





