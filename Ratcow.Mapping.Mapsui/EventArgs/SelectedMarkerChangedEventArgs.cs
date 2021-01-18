using System;

namespace Mapsui.UI.Forms
{
    public sealed class SelectedMarkerChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Pin that was selected
        /// </summary>
        public ISymbol SelectedMarker { get; }

        internal SelectedMarkerChangedEventArgs(ISymbol selectedMarker)
        {
            SelectedMarker = selectedMarker;
        }
    }
}