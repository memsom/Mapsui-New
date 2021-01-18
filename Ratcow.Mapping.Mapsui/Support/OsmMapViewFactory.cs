using Ratcow.Mapping.Interfaces;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Utilities;

namespace Ratcow.Mapping.Support
{
    public class OsmMapViewFactory : ICustomMapViewFactory
    {
        public TileLayer CreateBaseLayer()
        {
            return OpenStreetMap.CreateTileLayer();
        }

        public Map CreateMap()
        {
            return new Map
            {
                CRS = "EPSG:3857",
                Transformation = new MinimalTransformation()
            };
        }
    }
}