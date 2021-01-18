using System.Collections.Generic;

namespace Ratcow.Mapping.Interfaces
{

    public interface IHeatMapList: IList<IHeatMapPoint>
    {
        string DeviceId { get; set; }
    }

    public class HeatMapList : List<IHeatMapPoint>, IHeatMapList
    {
        public string DeviceId { get; set; }
    }

    public interface IHeatMappedDevice
    {
        bool HeatMapIsVisible { get; set; }
        IHeatMapList HeatMap { get;  }
    }
}