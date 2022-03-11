using Newtonsoft.Json;
using System.Data;
using Microsoft.Data.Sqlite;
using test_console_c;

namespace ConsoleWeatherAPI
{
    class Program
    {

        static async Task Main(string[] args)
        {
            string apiKey = "600e7808c4d14fe07520213ad9926375";

            var database = new sqlite();

            var (state, number_of_added_objects) = database.open_or_create_db();
            
            if (state == sqlite.sqlite_open_state.exist)
            {
                Console.WriteLine("DB file exists");
            } else if (state == sqlite.sqlite_open_state.created)
            {
                Console.WriteLine("Таблицы city, forecast созданы");
                Console.WriteLine($"В таблицу city добавлено объектов: {number_of_added_objects}");
            }


            if (state != sqlite.sqlite_open_state.error)
            {

                var cities = database.get_all_cities();


                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://api.openweathermap.org");
                var counter = 0;

                if (cities.HasRows) // если есть данные
                {
                    while (cities.Read())   // построчно считываем данные
                    {
                        var city_sk = cities.GetValue(0);
                        var city_id = cities.GetValue(1);
                        var name = cities.GetValue(2);


                        if (database.is_today_forecast_for_citie_exist(city_id))
                        {
                            Console.WriteLine($"Forecasts already stored for city: {name}");
                        }
                        else
                        {
                            Console.WriteLine($"New forecasts for city: {name}");
                            Console.WriteLine($"{city_sk} \t {city_id} \t {name}");

                            var choose_id = city_id;

                            var response = await client.GetAsync($"/data/2.5/forecast?id={choose_id}&units=metric&lang=en&appid={apiKey}");

                            var stringResult = await response.Content.ReadAsStringAsync();
                            var obj = JsonConvert.DeserializeObject<dynamic>(stringResult);
                            if (obj != null)
                            {
                                foreach (var item in obj.list)
                                {
                                    var main_temp = Math.Round(((float)item.main.temp));
                                    var dt_txt = item.dt_txt;
                                    var humidity = item.main.humidity;
                                    var wind_speed = item.wind.speed;
                                    var wind_deg = item.wind.deg;
                                    var description = item.weather[0].description;

                                    counter += database.add_today_forecast_for_citie(
                                        city_sk,
                                        city_id,
                                        dt_txt,
                                        main_temp,
                                        wind_speed,
                                        wind_deg,
                                        humidity,
                                        description
                                    );
                                }
                            }
                        }
                    }
                    Console.WriteLine($"В таблицу weather добавлено строк: {counter}");
                }
            }
        }
    }
}





