using Mapsui.UI.Forms;
using System;

namespace Ratcow.Mapping
{
    public sealed class MarkerClickedEventArgs : EventArgs
    {
        /// <summary>
        /// Pin that was clicked
        /// </summary>
        public ISymbol Marker { get; }

        /// <summary>
        /// Point of click in EPSG:4326 coordinates
        /// </summary>
        public Position Point { get; }

        /// <summary>
        /// Number of taps
        /// </summary>
        public int NumOfTaps { get; }

        /// <summary>
        /// Flag, if this event was handled
        /// </summary>
        /// <value><c>true</c> if handled; otherwise, <c>false</c>.</value>
        public bool Handled { get; set; } = false;

        internal MarkerClickedEventArgs(ISymbol marker, Position point, int numOfTaps)
        {
            Marker = marker;
            Point = point;
            NumOfTaps = numOfTaps;
        }
    }
}