using System;

namespace Ratcow.Mapping
{
    public class PositionEventArgs : EventArgs
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}