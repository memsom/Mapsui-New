using Ratcow.Mapping.Support;
using Mapsui.UI.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Ratcow.Mapping.Interfaces;

namespace Ratcow.Mapping.Mapsui
{
    public class HeatMapMarker : Marker, IHeatMapMarker
    {
        public static readonly BindableProperty BorderColorProperty =
            BindableProperty.Create(nameof(BorderColor), typeof(Xamarin.Forms.Color), typeof(Pin), SKColors.Red.ToFormsColor(), BindingMode.TwoWay);

        public static readonly BindableProperty RssiColorProperty =
            BindableProperty.Create(nameof(RssiColor), typeof(Xamarin.Forms.Color), typeof(Pin), SKColors.Blue.ToFormsColor());

        public HeatMapMarker(IMapView mapView, Xamarin.Forms.Color rssi, Xamarin.Forms.Color border) : base(mapView)
        {
            BorderColor = border;
            RssiColor = rssi;

            LoadSvgData();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Mapsui.UI.Forms.Marker"/> class
        /// </summary>
        /// <param name="mapView">MapView to which this pin belongs</param>
        public HeatMapMarker() : base()
        {
            LoadSvgData();
        }

        /// <summary>
        /// Color of pin
        /// </summary>
        public Xamarin.Forms.Color BorderColor
        {
            get { return (Xamarin.Forms.Color)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }

        /// <summary>
        /// Color of pin
        /// </summary>
        public Xamarin.Forms.Color RssiColor
        {
            get { return (Xamarin.Forms.Color)GetValue(RssiColorProperty); }
            set { SetValue(RssiColorProperty, value); }
        }

        double IHeatMapMarker.Lat => Position.Latitude;

        double IHeatMapMarker.Lon => Position.Longitude;

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            switch (propertyName)
            {
                case nameof(BorderColor):
                case nameof(RssiColor):
                    LoadSvgData();
                    break;
            }
        }

        protected override void LoadSvgData()
        {
            Svg = DeviceSvgHelper.GetHeatMapFor(RssiColor, BorderColor);
        }
    }
}
