using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test_console_c
{
    internal class sqlite
    {
        SqliteConnection? connection = null;


        ~sqlite()
        {
            if (connection != null) 
            { 
                connection.Close(); 
            }
        }

        public enum sqlite_open_state
        {
            exist,
            created,
            error
        }

        void db_create_city()
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = connection;
            command.CommandText = @"CREATE TABLE IF NOT EXISTS city (
                                            city_sk INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                            owm_city_id INTEGER NOT NULL,
                                            name TEXT NOT NULL,
                                            country TEXT NOT NULL,
                                            state TEXT,
                                            lat REAL NOT NULL,
                                            lon REAL NOT NULL
                                            ); ";
            command.ExecuteNonQuery();
        }
        int db_init_city() {
            SqliteCommand command = new SqliteCommand();
            command.Connection = connection;
            command.CommandText = @"INSERT INTO 
                                            city (owm_city_id, name, country, state, lat, lon)
                                        VALUES
                                            (3245, 'Taglag', 'IR', '', '', ''),
                                            (833, 'Heşār-e Sefīd', 'IR', '', '', ''),
                                            (2960, 'Ayn Halāqīm', 'SY', '', '', '')
                                            ";
            return command.ExecuteNonQuery();
        }

        void db_create_forecast()
        {
            SqliteCommand command = new SqliteCommand();
            command.Connection = connection;
            command.CommandText = @"CREATE TABLE IF NOT EXISTS forecast (
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
            command.ExecuteNonQuery();
        }

        public (sqlite_open_state, int) open_or_create_db()
        {
            try
            { 
                string target_folder = System.IO.Directory.GetCurrentDirectory();
                string db_path = Path.Combine(target_folder, Path.GetFileName("weatherdata.db"));
                Console.WriteLine(db_path);

                if (File.Exists(db_path))
                {
                    connection = new SqliteConnection("Data Source=" + db_path);
                    connection.Open();
                    return (sqlite_open_state.exist, 0);
                }
                else
                {
                    connection = new SqliteConnection("Data Source=" + db_path);
                    connection.Open();

                    db_create_city();
                    db_create_forecast();
                    var added_objects = db_init_city();

                    return (sqlite_open_state.created, added_objects);
                } 
            }
            catch (Exception)
            {
                return (sqlite_open_state.error, 0);
            }
        }

        public SqliteDataReader get_all_cities()
        {
            string sql_query = "SELECT * FROM city";
            SqliteCommand command = new SqliteCommand(sql_query, connection);
            command.Connection = connection;
            return command.ExecuteReader();

        }

        public bool is_today_forecast_for_citie_exist(object id)
        {
            string sql = @"SELECT 
                                (case 
                                    when 
                                        (substr(dt_txt,0,11) = date('now')) 
                                    then 
                                        1  
                                    else 
                                        0 
                                end) res 
                            FROM
                                forecast
                            WHERE
                                owm_city_id=" + id;
            SqliteCommand command = new SqliteCommand(sql, connection);
            DataTable data = new DataTable();
            data.Load(command.ExecuteReader()); // если есть данные по городу и дате 
            if (data.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }
        public int add_today_forecast_for_citie(object city_sk, object city_id, object dt_txt, object main_temp, object wind_speed, object wind_deg, object humidity, object description)
        {
            string sql = @"INSERT INTO forecast 
                            (
                                city_sk, 
                                owm_city_id, 
                                dt_txt, 
                                main_temp,
                                wind_speed,
                                wind_deg,
                                humidity,
                                description
                            )
                        VALUES
                            ("  + city_sk + ", "
                                + city_id + ",'" 
                                + dt_txt + "','" 
                                + main_temp + "', '" 
                                + wind_speed + "', '" 
                                + wind_deg + "','" 
                                + humidity + "','" 
                                + description + "'" 
                            + ")";
            SqliteCommand command = new SqliteCommand(sql, connection);
            return command.ExecuteNonQuery();
        }
    }
}
