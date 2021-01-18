using System;

namespace Ratcow.Mapping
{
    public enum MapMobileDisplayMode { Default, Pinned, Focused }

    public class MapMobileDisplayModeEventArgs: EventArgs
    {
        public MapMobileDisplayMode Mode { get; set; }        
    }
}