using Ratcow.Mapping.Support;
using Mapsui.UI.Forms;
using Xamarin.Forms;

namespace Ratcow.Mapping.Mapsui
{
    public interface IBaseMarker: ISymbol
    {

    }

    public class BaseMarker : DeviceMarker, IBaseMarker
    {
        public BaseMarker()
        {
        }

        public BaseMarker(IMapView mapView, Color fore, Color back) : base(mapView, fore, back)
        {
        }

        protected override void LoadSvgData()
        {
            Svg = DeviceSvgHelper.GetBaseFor(BackColor, ForeColor);
        }

    }
}
