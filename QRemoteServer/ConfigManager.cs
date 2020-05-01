using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace QRemoteServer
{
    static class ConfigManager
    {
        /// <summary>
        /// Путь к конфигу
        /// </summary>
        public static string ConfigPath = "config.cfg";

        /// <summary>
        /// IP из конфига
        /// </summary>
        public static string IP
        {
            get
            {
                if (ip == String.Empty)
                    GetData();
                return ip;
            }
        }

        /// <summary>
        /// Порт из конфига
        /// </summary>
        public static int Port
        {
            get
            {
                if (port == String.Empty)
                    GetData();
                return Convert.ToInt32(port);
            }
        }

        // Поля для ip и порта
        private static string ip = String.Empty;
        private static string port = String.Empty;

        /// <summary>
        /// Получение данных из конфига
        /// </summary>
        public static void GetData()
        {
            try
            {
                string[] arr = File.ReadAllLines(ConfigPath);
                if (arr.Length > 2)
                    throw new Exception("config is broken");
                ip = arr[0].Split('=')[1].Trim().ToLowerInvariant();
                port = arr[1].Split('=')[1].Trim().ToLowerInvariant();
            }
            catch(Exception e)
            {
                File.WriteAllLines(ConfigPath,  new string[] { "ip=Auto", "port=11000" });
                ip = "auto";
                port = "11000";
                Console.WriteLine(e.ToString());
            }
        }
    }
}
