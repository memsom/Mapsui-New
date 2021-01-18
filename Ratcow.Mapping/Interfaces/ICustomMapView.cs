using Ratcow.Mapping.Support;
using System.Threading.Tasks;

namespace Ratcow.Mapping.Interfaces
{
    public interface ICustomMapView
    {
        void PanTo(double lat, double lon, double resolution);
        void PanTo(double lat, double lon);
        void CentreOn(double lat, double lon);
        void ZoomToLevel(double resolution);

        void Zoom(ZoomDirection direction);

        void AddDevice(IMappedDevice device);

        Task<bool> UpdateHeatMapFor(IHeatMappedDevice device, bool show);

        bool AddHeatMapPoint(IHeatMapMarker marker);
    }
}