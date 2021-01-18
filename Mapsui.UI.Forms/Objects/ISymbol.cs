using Mapsui.Providers;
using Mapsui.UI.Objects;
using System.ComponentModel;

namespace Mapsui.UI.Forms
{
    public interface ISymbol : IFeatureProvider, INotifyPropertyChanged
    {
        IMapView MapView { get; set; }
        Position Position { get; set; }

        float Scale { get; set; }

        string Label { get; set; }

        float Rotation { get; set; }

        bool IsVisible { get; set; }

        double MinVisible { get; set; }

        double MaxVisible { get; set; }

        double Width { get; }

        double Height { get; }

        float Transparency { get; set; }
    }
}
