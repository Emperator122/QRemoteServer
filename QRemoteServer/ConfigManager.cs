using System;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;

namespace QRemoteServer
{
    static class ConfigManager
    {
        /// <summary>
        /// Путь к конфигу
        /// </summary>
        public static string ConfigPath = Application.StartupPath + "\\config.xml";

        /// <summary>
        /// Получение и десериализация данных конфигурации из файла
        /// </summary>
        /// <returns>Класс с настройками</returns>
        public static ConfigData GetConfigData()
        {
            ConfigData settings;
            try
            {
                using (Stream stream = new FileStream(ConfigPath, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ConfigData));
                    settings  = (ConfigData)serializer.Deserialize(stream);
                    return settings;
                }
            }
            catch
            {
                settings = new ConfigData();
                settings.IP = "auto";
                settings.Port = 11000;
                settings.AutoRun = true;
                try
                {
                    SaveConfigData(settings);
                }
                catch
                {
                    return settings;
                }
                return settings;
            }
        }

        /// <summary>
        /// Сериализация и сохранение данных конфигурации
        /// </summary>
        /// <param name="data">Файл с записанными настройками</param>
        public static void SaveConfigData(ConfigData data)
        {
            using (Stream writer = new FileStream(ConfigPath, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigData));
                serializer.Serialize(writer, data);
            }
        }
    }
    public class ConfigData
    {
        public string IP;
        public int Port;
        public bool AutoRun;
    }
}
