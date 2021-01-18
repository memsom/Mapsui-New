using Ratcow.Mapping.Mapsui;
using Ratcow.Mapping.Support;
using Mapsui.UI.Forms;
using Mapsui.UI.Objects;

namespace Ratcow.Mapping.Interfaces
{
    public static class IHeatMapPointHelper
    {
        public static Drawable ToMapsuiDrawable(this IHeatMapPoint point)
        {
            var position = new Position(point.Lat, point.Lon);
            var circle = new global::Mapsui.UI.Forms.Circle
            {
                FillColor = point.Background,
                StrokeColor = point.Border,
                Center = position,
                Quality = 180,
                Radius = Distance.FromMeters(10)
            };

            return circle;
        }

        public static Pin ToMapsuiPin(this IHeatMapPoint point, IMapView mapView)
        {
            var position = new Position(point.Lat, point.Lon);

            var svg = DeviceSvgHelper.GetHeatMapFor(point.Background, point.Border);

            var pin = new Pin(mapView)
            {
                Position = position,
                Type = PinType.Svg,
                Svg = svg,
                Label = position.ToString(),
                Scale = 1f,                
            };

            return pin;
        }

        public static HeatMapMarker ToHeatMapMarker(this IHeatMapPoint point, IMapView mapView)
        {
            var position = new Position(point.Lat, point.Lon);

            var marker = new HeatMapMarker(mapView, point.Background, point.Border)
            {
                Position = position,
                Label = position.ToString(),
                Scale = 1f,
            };

            return marker;
        }

        public static HeatMapMarker ToHeatMapMarker(this HeatMapPoint point, IMapView mapView)
        {
            if(point is IHeatMapPoint ipoint)
            {
                return ipoint.ToHeatMapMarker(mapView);
            }

            return default;
        }
    }
}