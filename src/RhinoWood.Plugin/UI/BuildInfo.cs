using System;
using System.IO;
using System.Reflection;

namespace RhinoWood.Plugin.UI
{
    /// <summary>Which build is loaded and from where - the first thing to check when the panel does not look like the latest version.</summary>
    public static class BuildInfo
    {
        public const string Version = "1.1.0";
        public static string Path => Assembly.GetExecutingAssembly().Location;
        public static string Text
        {
            get
            {
                string built = "";
                try { built = " · build " + File.GetLastWriteTime(Path).ToString("dd.MM.yyyy HH:mm"); } catch { }
                return "v" + Version + built;
            }
        }
    }
}
