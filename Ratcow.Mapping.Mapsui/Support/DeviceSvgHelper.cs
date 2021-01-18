using System;
using System.IO;
using System.Reflection;
using Xamarin.Forms;

namespace Ratcow.Mapping.Support
{
    static class DeviceSvgHelper
    {
        static string HexConverter(Color c)
        {
            return "#"+ ((int)(c.R * 255)).ToString("X2") + ((int)(c.G * 255)).ToString("X2") + ((int)(c.B * 255)).ToString("X2");
        }

        static string GetResourceFor(string name, Color back, Color fore)
        {
            var result = default(string);

            var assembly = typeof(DeviceSvgHelper).GetTypeInfo().Assembly;
            using var image = assembly.GetManifestResourceStream(name);

            if (image == null) throw new ArgumentException("EmbeddedResource not found");
            else
            {
                using var sr = new StreamReader(image);
                var s = sr.ReadToEnd();

                var fors = HexConverter(fore);
                var bacs = HexConverter(back);

                result = s.Replace("#feed17", bacs)  // these are magic values...
                          .Replace("#1ea717", fors);
            }

            return result;
        }

        public static string GetBaseFor(Color back, Color fore)
        {
            return GetResourceFor("Ratcow.Mapping.Mapsui.Images.Base.svg", back, fore);
        }

        public static string GetMobileFor(Color back, Color fore)
        {
            return GetResourceFor("Ratcow.Mapping.Mapsui.Images.Mobile.svg", back, fore);
        }

        public static string GetHeatMapFor(Color back, Color fore)
        {
            return GetResourceFor("Ratcow.Mapping.Mapsui.Images.HeatMap.svg", back, fore);
        }
    }
}