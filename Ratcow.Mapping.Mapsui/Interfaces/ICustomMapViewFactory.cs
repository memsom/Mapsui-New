using Mapsui;
using Mapsui.Layers;

namespace Ratcow.Mapping.Interfaces
{
    public interface ICustomMapViewFactory
    {
        TileLayer CreateBaseLayer();

        Map CreateMap();
    }
}