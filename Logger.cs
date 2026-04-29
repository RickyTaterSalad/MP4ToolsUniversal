using System;
using System.Collections.Generic;

namespace MP4Tools
{
    public static class Logger
    {
        public static event EventHandler<string> LogMessageReceived;

        public static void Log(string message)
        {
            LogMessageReceived?.Invoke(null, message);
        }

        public static void Log(Exception ex)
        {
            Log($"Error: {ex.Message}");
        }
    }
}