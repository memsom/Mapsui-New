using Mapsui.Layers;
using Mapsui.UI.Objects;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Mapsui.UI.Forms
{
    // this interface breaks out the specific stuff needed to make a generic interface we can use for creating new MapViews
    public interface IMapView: IAnimatable
    {
        bool MyLocationFollow { get; set; }
        bool MyLocationEnabled { get; set; }

        MyLocationLayer MyLocationLayer { get; }

        IMapControl MapControl { get; }

        Map Map { get; }

        INavigator Navigator { get; set; }

        IReadOnlyViewport Viewport { get; }

        IList<Drawable> Drawables { get; }

        IList<Pin> Pins { get; }

        Pin SelectedPin { get; set; }

        bool UseDoubleTap { get; set; }
        bool UniqueCallout { get; set; }

        void Refresh(ChangeType changeType = ChangeType.Discrete);

        void RemoveCallout(Callout callout);

        void AddCallout(Callout callout);

        bool IsCalloutVisible(Callout callout);

        byte[] GetSnapshot(IEnumerable<ILayer> layers = null);
    }
}
