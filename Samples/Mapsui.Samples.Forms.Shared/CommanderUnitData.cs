using Ratcow.Mapping.Interfaces;
using Ratcow.Mapping.Mapsui;
using System;
using Xamarin.Forms;

namespace Ratcow.Mapping.Support
{
    public class CommanderUnitData : IMappedDevice, IBaseMarkerProvider
    {
        private double lat;
        private double lon;

        public string DeviceId
        {
            get => deviceId;
            set
            {
                deviceId = value;
                HeatMap.DeviceId = this.DeviceId;
            }
        }

        public Color Fore { get; set; }
        public Color Back { get; set; }
        public string Name { get; set; }
        public string Identifier { get; set; }
        public double Lat
        {
            get => lat;
            set
            {
                lat = value;
                InvokePositionUpdated();
            }
        }

        public double Lon
        {
            get => lon;
            set
            {
                lon = value;
                InvokePositionUpdated();
            }
        }

        public bool HeatMapIsVisible { get; set; } = false;

        IHeatMapList heatMap = null;
        private string deviceId;

        public IHeatMapList HeatMap => heatMap ??= new HeatMapList { DeviceId = deviceId };

        public object RawPin => Marker;

        public bool IsVisible { get; set; }

        public bool IsRemote => false;

        public bool IsFocused { get; set; }

        public bool HasPosition { get; set; }
        public IBaseMarker Marker { get; set; }        

        void InvokePositionUpdated()
        {
            PositionUpdated?.Invoke(
                this,
                new PositionEventArgs
                {
                    Lat = Lat,
                    Lon = Lon
                });
        }

        public bool TrySetPosition(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;

            return true;
        }

        public event EventHandler<PositionEventArgs> PositionUpdated;
    }

    
}