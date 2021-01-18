using Ratcow.Mapping.Interfaces;
using Xamarin.Forms;

namespace Ratcow.Mapping.Support
{
    public class HeatMapPoint: IHeatMapPoint
    {
        public string DeviceId { get; set; }
        public Color Border { get; set; } // border color
        public Color Background { get; set; } // Heat colour

        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}