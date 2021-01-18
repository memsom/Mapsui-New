using System;
using Xamarin.Forms;

namespace Ratcow.Mapping.Interfaces
{

    public interface IMappedDevice
    {
        /// <summary>
        /// this is the key
        /// </summary>
        string DeviceId { get;}

        Color Fore { get;}
        Color Back { get;}

        /// <summary>
        /// this is whatever the name is meant to be
        /// </summary>
        string Name { get; }

        /// <summary>
        /// this would be the internal identifier
        /// </summary>
        string Identifier { get; }
        double Lat { get; }
        double Lon { get; }

        bool TrySetPosition(double lat, double lon);

        /// <summary>
        /// when we get the first fix, this is set to true.
        /// </summary>
        bool IsVisible { get; set; } 

        bool IsRemote { get; }

        bool IsFocused { get; }

        event EventHandler<PositionEventArgs> PositionUpdated;
    }
}