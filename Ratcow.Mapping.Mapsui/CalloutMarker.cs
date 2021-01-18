using Mapsui.Rendering.Skia;
using Mapsui.UI.Forms;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace Ratcow.Mapping.Mapsui
{
    public enum CalloutPosition { Left, Top, Right, Bottom, Custom }

    /// <summary>
    /// A base class for markers with Callouts.
    /// </summary>
    public abstract class CalloutMarker: Marker, ICalloutSymbol
    {
        protected CalloutMarker(IMapView mapView) : base(mapView)
        {
        }

        protected CalloutMarker()
        {
        }

        public static readonly BindableProperty CalloutPositionProperty =
            BindableProperty.Create(nameof(CalloutPosition), typeof(CalloutPosition), typeof(CalloutMarker), CalloutPosition.Left);


        // this auto aligns the callout on the boundaries of the rendered image
        // set to custom to override that behaviour.
        public CalloutPosition CalloutPosition
        {
            get => (CalloutPosition)GetValue(CalloutPositionProperty);
            set => SetValue(CalloutPositionProperty, value);
        }

        public bool IsCalloutVisible()
        {
            return mapView != null ? mapView.IsCalloutVisible(callout) : false;
        }

        public void HideCallout()
        {
            mapView.RemoveCallout(callout);
        }

        public void ShowCallout()
        {
            callout.Update();
            mapView.AddCallout(callout);
        }

        protected Callout callout;

        /// <summary>
        /// Gets the callout
        /// </summary>
        /// <value>Callout for this pin</value>
        public Callout Callout
        {
            get
            {
                // Show a new Callout
                if (callout == null)
                {
                    // Create a default callout
                    callout = new Callout(this);

                    // set the default callout style for a device
                    var size = Device.GetNamedSize(NamedSize.Micro, typeof(Label));
                    callout.TitleFontSize = size;
                    callout.SubtitleFontSize = size;

                    UpdateCalloutText();
                }

                return callout;
            }
            internal set
            {
                if (value != null && callout != value)
                {
                    callout = value;
                }
            }
        }

        protected abstract void UpdateCalloutText();

        public override void BitmapLoaded()
        {
            SetCalloutAnchor();
        }

        private void SetCalloutAnchor()
        {
            switch (CalloutPosition)
            {
                case CalloutPosition.Left:
                    Callout.ArrowAlignment = ArrowAlignment.Right;
                    Callout.Anchor = new Point((Height / 2), 0);
                    break;

                case CalloutPosition.Right:
                    Callout.ArrowAlignment = ArrowAlignment.Left;
                    Callout.Anchor = new Point(-(Height / 2), 0);
                    break;

                case CalloutPosition.Top:
                    Callout.ArrowAlignment = ArrowAlignment.Bottom;
                    Callout.Anchor = new Point(0, (Width / 2));
                    break;

                case CalloutPosition.Bottom:
                    Callout.ArrowAlignment = ArrowAlignment.Top;
                    Callout.Anchor = new Point(0, -(Width / 2));
                    break;

            }

        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            switch (propertyName)
            {
                case nameof(Anchor):
                case nameof(CalloutPosition):
                    SetCalloutAnchor();
                    break;

                case nameof(Position):
                    if (Feature != null)
                    {
                        //Feature.Geometry = Position.ToMapsui();
                        Callout.Feature.Geometry = Feature.Geometry;
                    }
                    break;

                case nameof(IsVisible):
                    if (!IsVisible)
                        HideCallout();
                    break;

                case nameof(Label):
                    UpdateCalloutText();
                    break;
            }
        }
    }
}
