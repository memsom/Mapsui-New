using Ratcow.Mapping.Support;
using Mapsui.UI.Forms;
using Xamarin.Forms;

namespace Ratcow.Mapping.Mapsui
{
    public interface IMobileMarker : ISymbol
    {

    }

    public class MobileMarker : DeviceMarker, IMobileMarker
    {
        public MobileMarker()
        {
        }

        public MobileMarker(IMapView mapView, Color fore, Color back) : base(mapView, fore, back)
        {
        }

        protected override void LoadSvgData()
        {
            Svg = DeviceSvgHelper.GetMobileFor(BackColor, ForeColor);
        }

    }
}
