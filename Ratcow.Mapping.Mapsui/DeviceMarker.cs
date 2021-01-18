using Mapsui.Providers;
using Mapsui.Rendering.Skia;
using Mapsui.UI.Forms;
using Ratcow.Mapping.Interfaces;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace Ratcow.Mapping.Mapsui
{

    public abstract class DeviceMarker : CalloutMarker
    {
        public static readonly BindableProperty BackColorProperty =
           BindableProperty.Create(nameof(BackColor), typeof(Xamarin.Forms.Color), typeof(DeviceMarker), SKColors.Red.ToFormsColor(), BindingMode.TwoWay);

        public static readonly BindableProperty ForeColorProperty =
            BindableProperty.Create(nameof(ForeColor), typeof(Xamarin.Forms.Color), typeof(DeviceMarker), SKColors.Blue.ToFormsColor());

        public static readonly BindableProperty DeviceIdentifierProperty =
            BindableProperty.Create(nameof(DeviceIdentifier), typeof(string), typeof(DeviceMarker), default(string));

        public static readonly BindableProperty NameProperty =
            BindableProperty.Create(nameof(Name), typeof(string), typeof(DeviceMarker), default(string));

  
        public DeviceMarker(IMapView mapView, Xamarin.Forms.Color fore, Xamarin.Forms.Color back) : base(mapView)
        {
            BackColor = back;
            ForeColor = fore;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Mapsui.UI.Forms.Marker"/> class
        /// </summary>
        /// <param name="mapView">MapView to which this pin belongs</param>
        public DeviceMarker() : base()
        {
            LoadSvgData();
        }

        /// <summary>
        /// Color of pin
        /// </summary>
        public Xamarin.Forms.Color BackColor
        {
            get { return (Xamarin.Forms.Color)GetValue(BackColorProperty); }
            set { SetValue(BackColorProperty, value); }
        }

        /// <summary>
        /// Color of pin
        /// </summary>
        public Xamarin.Forms.Color ForeColor
        {
            get { return (Xamarin.Forms.Color)GetValue(ForeColorProperty); }
            set { SetValue(ForeColorProperty, value); }
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            switch (propertyName)
            {
                case nameof(BackColor):
                case nameof(ForeColor):
                    LoadSvgData();
                    break;

                case nameof(Name):
                case nameof(DeviceIdentifier):
                    UpdateCalloutText();
                    break;
            }
        }



        

        /// <summary>
        /// ModelSerial of device
        /// </summary>
        public string DeviceIdentifier
        {
            get { return (string)GetValue(DeviceIdentifierProperty); }
            set { SetValue(DeviceIdentifierProperty, value); }
        }

        /// <summary>
        /// Label of marker
        /// </summary>
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        protected override void MapviewChanged(IMapView current)
        {
            if (callout != null)
            {
                mapView?.RemoveCallout(callout);
            }
        }
      
       protected override void UpdateCalloutText()
        {
            var label = !string.IsNullOrEmpty(DeviceIdentifier) ? DeviceIdentifier : Label;

            if (!string.IsNullOrEmpty(Name))
            {
                callout.Type = CalloutType.Detail;
                callout.Title = Name;
                callout.Subtitle = label;
            }
            else
            {
                callout.Type = CalloutType.Single;
                callout.Title = label;
            }
        }

        protected override void AfterCreateFeature(Feature feature)
        {
            if (callout != null)
            {
                callout.Feature.Geometry = Position.ToMapsui();
            }
        }
    }
}
