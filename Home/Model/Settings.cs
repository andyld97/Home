using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public class Settings
    {
        private static readonly string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Home", "Settings.xml");

        public static Settings Instance = Settings.Load();

        static Settings()
        {
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            }
            catch { }
        }

        public string Host { get; set; }

        #region Theme-Settings

        public bool UseDarkMode { get; set; } = true;

        public string Theme { get; set; } = "Cobalt"; // Cobalt is the default one used in Home

        /// <summary>
        /// Determines if the glowing brush of the mainwindow is disabled or not
        /// </summary>
        public bool ActivateGlowingBrush { get; set; } = true;

        #endregion

        public void Save()
        {
            try
            {
                Serialization.Serialization.Save<Settings>(path, this);
            }
            catch
            {

            }
        }

        public static Settings Load()
        {

            try
            {
                var settings = Serialization.Serialization.Read<Settings>(path);
                if (settings != null)
                    return settings;
            }
            catch
            {

            }

            return new Settings();
        }
    }
}