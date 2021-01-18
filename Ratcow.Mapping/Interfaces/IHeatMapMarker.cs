using Xamarin.Forms;

namespace Ratcow.Mapping.Interfaces
{
    public interface IHeatMapMarker
    {
        Color BorderColor { get; set; }
        Color RssiColor { get; set; }

        float Scale { get; set; }
        string Label { get; set; }
        float Rotation { get; set; }
        bool IsVisible { get; set; }
        double MinVisible { get; set; }
        double MaxVisible { get; set; }
        double Width { get; }
        double Height { get; }
        float Transparency { get; set; }

        double Lat { get; }
        double Lon { get; }
    }
}