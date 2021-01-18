using Mapsui.Providers;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using Mapsui.UI.Objects;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;
using SKSvg = Svg.Skia.SKSvg;

namespace Mapsui.UI.Forms
{
    public abstract class Marker : BindableObject, IFeatureProvider, ISymbol
    {
        private int bitmapId = -1;
        protected IMapView mapView;
        
        public static readonly BindableProperty PositionProperty = BindableProperty.Create(nameof(Position), typeof(Position), typeof(Marker), default(Position));
        public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(Marker), default(string));                
        public static readonly BindableProperty SvgProperty = BindableProperty.Create(nameof(Svg), typeof(string), typeof(Marker), default(string));
        public static readonly BindableProperty ScaleProperty = BindableProperty.Create(nameof(Scale), typeof(float), typeof(Marker), 1.0f);
        public static readonly BindableProperty RotationProperty = BindableProperty.Create(nameof(Rotation), typeof(float), typeof(Marker), 0f);
        public static readonly BindableProperty RotateWithMapProperty = BindableProperty.Create(nameof(RotateWithMap), typeof(bool), typeof(Marker), false);
        public static readonly BindableProperty IsVisibleProperty = BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(Marker), true);
        public static readonly BindableProperty MinVisibleProperty = BindableProperty.Create(nameof(MinVisible), typeof(double), typeof(Marker), 0.0);
        public static readonly BindableProperty MaxVisibleProperty = BindableProperty.Create(nameof(MaxVisible), typeof(double), typeof(Marker), double.MaxValue);
        public static readonly BindableProperty WidthProperty = BindableProperty.Create(nameof(Width), typeof(double), typeof(Marker), -1.0, BindingMode.OneWayToSource);
        public static readonly BindableProperty HeightProperty = BindableProperty.Create(nameof(Height), typeof(double), typeof(Marker), -1.0);
        public static readonly BindableProperty AnchorProperty = BindableProperty.Create(nameof(Anchor), typeof(Point), typeof(Marker), new Point(0, 0));
        public static readonly BindableProperty TransparencyProperty = BindableProperty.Create(nameof(Transparency), typeof(float), typeof(Marker), 0f);

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Mapsui.UI.Forms.Marker"/> class
        /// </summary>
        /// <param name="mapView">MapView to which this pin belongs</param>
        public Marker(IMapView mapView)
        {
            this.mapView = mapView;

            CreateFeature();
        }

        protected abstract void LoadSvgData();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Mapsui.UI.Forms.Marker"/> class
        /// </summary>
        /// <param name="mapView">MapView to which this pin belongs</param>
        public Marker()
        {
        }

        // this allows stuff that needs to happen as the mapview changes to still happen
        protected virtual void MapviewChanging(IMapView current, IMapView next)
        {

        }

        protected virtual void MapviewChanged(IMapView current)
        {

        }

        /// <summary>
        /// Internal MapView for refreshing of screen
        /// </summary>
        IMapView ISymbol.MapView
        {
            get
            {
                return mapView;
            }
            set
            {
                if (mapView != value)
                {
                    MapviewChanging(mapView, value);

                    Feature = null;
                    mapView = value;

                    CreateFeature();

                    MapviewChanged(mapView);
                }
            }
        }

        /// <summary>
        /// Position of marker, place where anchor is
        /// </summary>
        public Position Position
        {
            get { return (Position)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        /// <summary>
        /// Scaling of marker
        /// </summary>
        public float Scale
        {
            get { return (float)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        /// <summary>
        /// Label of marker
        /// </summary>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }


        /// <summary>
        /// String holding the Svg image informations
        /// </summary>
        public string Svg
        {
            get { return (string)GetValue(SvgProperty); }
            set { SetValue(SvgProperty, value); }
        }

        /// <summary>
        /// Rotation in degrees around the anchor point
        /// </summary>
        public float Rotation
        {
            get { return (float)GetValue(RotationProperty); }
            set { SetValue(RotationProperty, value); }
        }

        /// <summary>
        /// When true a symbol will rotate along with the rotation of the map.
        /// The default is false.
        /// </summary>
        public bool RotateWithMap
        {
            get { return (bool)GetValue(RotateWithMapProperty); }
            set { SetValue(RotateWithMapProperty, value); }
        }

        /// <summary>
        /// Determins, if the pin is drawn on map
        /// </summary>
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        /// <summary>
        /// MinVisible for pin in resolution of Mapsui (smaller values are higher zoom levels)
        /// </summary>
        public double MinVisible
        {
            get { return (double)GetValue(MinVisibleProperty); }
            set { SetValue(MinVisibleProperty, value); }
        }

        /// <summary>
        /// MaxVisible for pin in resolution of Mapsui (smaller values are higher zoom levels)
        /// </summary>
        public double MaxVisible
        {
            get { return (double)GetValue(MaxVisibleProperty); }
            set { SetValue(MaxVisibleProperty, value); }
        }

        /// <summary>
        /// Width of the bitmap after scaling, if there is one, if not, than -1
        /// </summary>
        public double Width
        {
            get { return (double)GetValue(WidthProperty); }
            private set { SetValue(WidthProperty, value); }
        }

        /// <summary>
        /// Height of the bitmap after scaling, if there is one, if not, than -1
        /// </summary>
        public double Height
        {
            get { return (double)GetValue(HeightProperty); }
            private set { SetValue(HeightProperty, value); }
        }

        /// <summary>
        /// Anchor of bitmap in pixel
        /// </summary>
        public Point Anchor
        {
            get { return (Point)GetValue(AnchorProperty); }
            set { SetValue(AnchorProperty, value); }
        }

        /// <summary>
        /// Transparency of pin
        /// </summary>
        public float Transparency
        {
            get { return (float)GetValue(TransparencyProperty); }
            set { SetValue(TransparencyProperty, value); }
        }

        /// <summary>
        /// Tag holding free data
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Mapsui feature for this pin
        /// </summary>
        /// <value>Mapsui feature</value>
        public Feature Feature { get; private set; }
        
        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="T:Mapsui.UI.Forms.Pin"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="T:Mapsui.UI.Forms.Pin"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="object"/> is equal to the current
        /// <see cref="T:Mapsui.UI.Forms.Pin"/>; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((Marker)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Label?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ Position.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)PinType.Svg;
                return hashCode;
            }
        }

        public static bool operator ==(Marker left, Marker right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Marker left, Marker right)
        {
            return !Equals(left, right);
        }

        protected virtual bool Equals(Marker other)
        {
            return string.Equals(Label, other.Label) && Equals(Position, other.Position);
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            switch (propertyName)
            {
                case nameof(Position):
                    if (Feature != null)
                    {
                        Feature.Geometry = Position.ToMapsui();
                    }
                    break;
                case nameof(Label):
                    if (Feature != null)
                        Feature["Label"] = Label;
                    break;
                case nameof(Transparency):
                    ((SymbolStyle)Feature.Styles.First()).Opacity = 1 - Transparency;
                    break;
                case nameof(Anchor):
                    ((SymbolStyle)Feature.Styles.First()).SymbolOffset = new Offset(Anchor.X, Anchor.Y);
                    break;
                case nameof(Rotation):
                    ((SymbolStyle)Feature.Styles.First()).SymbolRotation = Rotation;
                    break;
                case nameof(RotateWithMap):
                    ((SymbolStyle)Feature.Styles.First()).RotateWithMap = RotateWithMap;
                    break;
                case nameof(IsVisible):
                    ((SymbolStyle)Feature.Styles.First()).Enabled = IsVisible;
                    break;
                case nameof(MinVisible):
                    // TODO: Update callout MinVisble too
                    ((SymbolStyle)Feature.Styles.First()).MinVisible = MinVisible;
                    break;
                case nameof(MaxVisible):
                    // TODO: Update callout MaxVisble too
                    ((SymbolStyle)Feature.Styles.First()).MaxVisible = MaxVisible;
                    break;
                case nameof(Scale):
                    ((SymbolStyle)Feature.Styles.First()).SymbolScale = Scale;
                    break;

                case nameof(Svg):
                    CreateFeature();
                    break;
            }
        }

        readonly object _sync = new object();

        protected virtual void BeforeCreateFeature(Feature feature)
        {

        }

        protected virtual void AfterCreateFeature(Feature feature)
        {

        }

        public virtual void BitmapLoaded() { }


        void CreateFeature()
        {
            lock (_sync)
            {
                BeforeCreateFeature(Feature);

                if (Feature == null)
                {
                    // Create a new one
                    Feature = new Feature
                    {
                        Geometry = Position.ToMapsui(),
                        ["Label"] = Label,
                    };
                }

                AfterCreateFeature(Feature);

                // Check for bitmapId
                if (bitmapId != -1)
                {
                    // There is already a registered bitmap, so delete it
                    BitmapRegistry.Instance.Unregister(bitmapId);
                    // We don't have any bitmap up to now
                    bitmapId = -1;
                }

                Stream stream = null;


                // if the Svg is null, load the data from the embedded resource
                if(string.IsNullOrEmpty(Svg))
                {
                    LoadSvgData(); // this could still fail!
                }

                // Load the SVG document
                if (!string.IsNullOrEmpty(Svg))
                {
                    stream = new MemoryStream(Encoding.UTF8.GetBytes(Svg));
                }

                if (stream == null)
                {
                    return;
                }

                var svg = new SKSvg();
                svg.Load(stream);
                Width = svg.Picture.CullRect.Width * Scale;
                Height = svg.Picture.CullRect.Height * Scale;

                // Create bitmap to hold canvas
                var info = new SKImageInfo((int)svg.Picture.CullRect.Width, (int)svg.Picture.CullRect.Height) { AlphaType = SKAlphaType.Premul };
                var bitmap = new SKBitmap(info);
                var canvas = new SKCanvas(bitmap);
                // Now draw Svg image to bitmap
                using (var paint = new SKPaint() { IsAntialias = true })
                {                   
                    canvas.Clear();
                    canvas.DrawPicture(svg.Picture, paint);
                }

                var bitmapData = default(byte[]);
                // Now convert canvas to bitmap
                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    bitmapData = data.ToArray();
                }
                bitmapId = BitmapRegistry.Instance.Register(new MemoryStream(bitmapData));

                //bitmapId = BitmapRegistry.Instance.Register(stream);

                // If we have a bitmapId (and we should have one), than draw bitmap, otherwise nothing
                if (bitmapId != -1)
                {
                    // We only want to have one style
                    Feature.Styles.Clear();
                    Feature.Styles.Add(new SymbolStyle
                    {
                        BitmapId = bitmapId,
                        SymbolScale = Scale,
                        SymbolRotation = Rotation,
                        RotateWithMap = RotateWithMap,
                        SymbolOffset = new Offset(Anchor.X, Anchor.Y),
                        Opacity = 1 - Transparency,
                        Enabled = IsVisible,
                    });
                }

                BitmapLoaded();
            }
        }
    }
}
