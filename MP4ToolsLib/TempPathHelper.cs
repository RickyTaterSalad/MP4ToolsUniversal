using System;
using System.IO;

namespace MP4ToolsLib
{
    public static class TempPathHelper
    {
        private static readonly string _customTempPath = GetTempPathInternal();

        public static string GetTempPath() => _customTempPath;

        public static string GetTempFileName()
        {
            string fileName = Path.Combine(_customTempPath, Guid.NewGuid().ToString() + ".tmp");
            using (File.Create(fileName)) { }
            return fileName;
        }

        private static string GetTempPathInternal()
        {
            // Check for custom environment variable first
            string? customTemp =  "/opt/Encodes/tmp"; //Environment.GetEnvironmentVariable("MP4TOOLS_TEMP");
            if (!string.IsNullOrEmpty(customTemp) && Directory.Exists(customTemp))
            {
                return customTemp;
            }

            // Fall back to system temp path
            return Path.GetTempPath();
        }
    }
}
