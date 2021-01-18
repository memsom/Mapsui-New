using Xamarin.Forms;

namespace Ratcow.Mapping.Interfaces
{
    public interface IHeatMapPoint
    {
        string DeviceId { get; set; }
        /// <summary>
        /// Border colour
        /// </summary>
        Color Border { get; set; }
        /// <summary>
        /// Heat colour
        /// </summary>
        Color Background { get; set; } 

        double Lat { get; set; }
        double Lon { get; set; }        
    }
}