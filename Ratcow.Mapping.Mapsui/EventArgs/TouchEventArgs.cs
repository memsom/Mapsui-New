using System;

namespace Ratcow.Mapping
{
    public enum TouchEventType { shortPress, longPress }
    public class TouchEventArgs : EventArgs
    {
        public TouchEventType TouchEventType { get; set; }
    }
}