using Ratcow.Mapping.Support;
using Mapsui.Layers;
using Mapsui.UI;
using Mapsui.UI.Forms;
using Mapsui.UI.Forms.Extensions;
using Mapsui.Utilities;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xamarin.Forms;
using SKSvg = Svg.Skia.SKSvg;

namespace Ratcow.Mapping.Controls
{
    public class MapViewEx : MapView
    {
        public ISymbol SelectedMarker { get; set; }

        protected readonly SvgButton displayModeButton;
        protected readonly SvgTouchButton focusOnDeviceButton;
        protected readonly Image mapSpacingButton3;
        private MapMobileDisplayMode mobileDisplayMode = MapMobileDisplayMode.Pinned;
        private SvgTouchButton pressedButton;
        private long pressedButtonTicks;
        protected readonly SKPicture focusedPicture;
        protected readonly SKPicture pinnedPicture;
        protected readonly SKPicture nonePicture;

        protected readonly SvgButton mapZoomInButton;
        protected readonly SvgButton mapZoomOutButton;
        protected readonly Image mapSpacingButton1;
        protected readonly SvgButton mapMyLocationButton;
        protected readonly Image mapSpacingButton2;
        protected readonly SvgButton mapNorthingButton;
        protected readonly SKPicture pictMyLocationNoCenter;
        protected readonly SKPicture pictMyLocationCenter;

        protected readonly StackLayout _mapButtons;

        private readonly object syncObject = new object();
        private const int duration = 1000;
        //timer to track long press
        private Timer timer;

        private volatile bool isReleased;

        public bool FocusedDevice
        {
            get => focusOnDeviceButton.Picture == pictMyLocationCenter;
            set
            {
                focusOnDeviceButton.Picture = value ? pictMyLocationCenter : pictMyLocationNoCenter;
            }
        }

        public MapMobileDisplayMode MobileDisplayMode
        {
            get => mobileDisplayMode;
            set
            {
                mobileDisplayMode = value;
                OnPropertyChanged();
            }
        }

        public event EventHandler<MapMobileDisplayModeEventArgs> MobileDisplayModeChanged;
        public event EventHandler<TouchEventArgs> DeviceToFocusRequest;

        public SKPicture GetMobileDisplayModeImage() => mobileDisplayMode switch
        {
            MapMobileDisplayMode.Default => nonePicture,
            MapMobileDisplayMode.Pinned => pinnedPicture,
            MapMobileDisplayMode.Focused => focusedPicture,
            _ => nonePicture
        };

        public MapViewEx() : base()
        {
            AbsoluteLayout.SetLayoutBounds(_mapControl, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(_mapControl, AbsoluteLayoutFlags.All);

            pictMyLocationNoCenter = new SKSvg().Load(EmbeddedResourceLoader.Load("Images.LocationNoCenter.svg", typeof(MapViewEx)));
            pictMyLocationCenter = new SKSvg().Load(EmbeddedResourceLoader.Load("Images.LocationCenter.svg", typeof(MapViewEx)));

            mapZoomInButton = new SvgButton(EmbeddedResourceLoader.Load("Images.ZoomIn.svg", typeof(MapViewEx)))
            {
                BackgroundColor = Color.Transparent,
                WidthRequest = 40,
                HeightRequest = 40,
                Command = new Command(obj =>
                {
                    _mapControl.Navigator.ZoomIn();
                })
            };

            mapZoomOutButton = new SvgButton(EmbeddedResourceLoader.Load("Images.ZoomOut.svg", typeof(MapViewEx)))
            {
                BackgroundColor = Color.Transparent,
                WidthRequest = 40,
                HeightRequest = 40,
                Command = new Command(obj =>
                {
                    _mapControl.Navigator.ZoomOut();
                }),
            };

            mapSpacingButton1 = new Image { BackgroundColor = Color.Transparent, WidthRequest = 40, HeightRequest = 8, InputTransparent = true };

            focusOnDeviceButton = new SvgTouchButton(pictMyLocationNoCenter)
            {
                BackgroundColor = Color.Transparent,
                WidthRequest = 40,
                HeightRequest = 40,
                Command = new Command(obj =>
                {
                    if (obj is SKTouchEventArgs skTouchEventArgs)
                    {
                        HandledTouchButtonEvent(focusOnDeviceButton, skTouchEventArgs);
                    }
                }),
            };

            mapSpacingButton2 = new Image { BackgroundColor = Color.Transparent, WidthRequest = 40, HeightRequest = 8, InputTransparent = true };

            mapNorthingButton = new SvgButton(EmbeddedResourceLoader.Load("Images.RotationZero.svg", typeof(MapViewEx)))
            {
                BackgroundColor = Color.Transparent,
                WidthRequest = 40,
                HeightRequest = 40,
                Command = new Command(obj =>
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        _mapControl.Navigator.RotateTo(0);
                    });
                }),
            };

            nonePicture = new SKSvg().Load(EmbeddedResourceLoader.Load("Images.ShowNone.svg", typeof(MapViewEx)));
            pinnedPicture = new SKSvg().Load(EmbeddedResourceLoader.Load("Images.ShowPinned.svg", typeof(MapViewEx)));
            focusedPicture = new SKSvg().Load(EmbeddedResourceLoader.Load("Images.ShowFocused.svg", typeof(MapViewEx)));

            displayModeButton = new SvgButton(GetMobileDisplayModeImage())
            {
                BackgroundColor = Color.Transparent,
                WidthRequest = 40,
                HeightRequest = 40,
                Command = new Command(obj =>
                {
                    MobileDisplayMode = mobileDisplayMode switch
                    {
                        MapMobileDisplayMode.Default => MapMobileDisplayMode.Pinned,
                        MapMobileDisplayMode.Pinned => MapMobileDisplayMode.Focused,
                        MapMobileDisplayMode.Focused => MapMobileDisplayMode.Default,
                        _ => MapMobileDisplayMode.Default
                    };

                    displayModeButton.Picture = GetMobileDisplayModeImage();

                    MobileDisplayModeChanged?.Invoke(this, new MapMobileDisplayModeEventArgs { Mode = MobileDisplayMode });
                }),
            };

            mapSpacingButton3 = new Image { BackgroundColor = Color.Transparent, WidthRequest = 40, HeightRequest = 8, InputTransparent = true };

            _mapButtons = new StackLayout { BackgroundColor = Color.Transparent, Spacing = 0, IsVisible = true, InputTransparent = true, CascadeInputTransparent = false };

            _mapButtons.Children.Add(mapZoomInButton);
            _mapButtons.Children.Add(mapZoomOutButton);
            _mapButtons.Children.Add(mapSpacingButton1);
            _mapButtons.Children.Add(focusOnDeviceButton);
            _mapButtons.Children.Add(mapSpacingButton2);
            _mapButtons.Children.Add(mapNorthingButton);
            _mapButtons.Children.Add(mapSpacingButton3);
            _mapButtons.Children.Add(displayModeButton);

            if (Content is AbsoluteLayout layout)
            {
                layout.Children.Add(_mapButtons);
            }
            else
            {
                Content = new AbsoluteLayout
                {
                    Children = {
                    _mapControl,
                    _mapButtons
                }
                };
            }


            var height = _mapButtons.Children.Sum(x => x.HeightRequest); // was 176 + 8 + 40
            AbsoluteLayout.SetLayoutBounds(_mapButtons, new Rectangle(0.95, 0.03, 40, height));
            AbsoluteLayout.SetLayoutFlags(_mapButtons, AbsoluteLayoutFlags.PositionProportional);
        }

        List<CustomMemoryLayer> CustomLayers { get; } = new List<CustomMemoryLayer>();

        public void AddCustomLayer<Symbol>(string name, int zindex = -1) where Symbol : ISymbol
        {
            var layer = new CustomMemoryLayer<Symbol>(this, name)
            {
                ZIndexRequest = zindex,
            };
            CustomLayers.Add(layer);
            layer.AttachLayer();
        }

        public bool HasLayer(string name)
        {
            return CustomLayers.Any(x => x.Name == name);
        }

        protected override bool IsExcludedFromHandlerLayerChanged(ILayer layer)
        {
            var result = base.IsExcludedFromHandlerLayerChanged(layer);
            if (!result)
            {
                result = CustomLayers.Any(x => x.Layer == layer);
            }
            return result;
        }

        protected override void RemoveLayers()
        {
            base.RemoveLayers();

            foreach (var layer in CustomLayers.Select(x => x.Layer))
            {
                _mapControl.Map.Layers.Remove(layer);
            }
        }

        public CustomMemoryLayer this[string name]
        {
            get
            {
                return CustomLayers.FirstOrDefault(x => x.Name == name);
            }
        }

        public event EventHandler<MarkerClickedEventArgs> MarkerClicked;

        /// <summary>
        /// Occurs when selected pin changed
        /// </summary>
        public event EventHandler<SelectedMarkerChangedEventArgs> SelectedMarkerChanged;

        protected override void HandlerInfo(object sender, MapInfoEventArgs e)
        {
            base.HandlerInfo(sender, e);

            if (CustomLayers.FirstOrDefault(x => x.Name == e.MapInfo?.Layer?.Name) is CustomMemoryLayer layer)
            {
                ISymbol clickedMarker = null;
                var symbols = layer.RawData.ToList();

                foreach (var symbol in symbols)
                {
                    if (symbol.IsVisible && symbol.Feature.Equals(e.MapInfo.Feature))
                    {
                        clickedMarker = symbol;
                        break;
                    }
                }

                if (clickedMarker != null)
                {
                    SelectedMarker = clickedMarker;

                    SelectedMarkerChanged?.Invoke(this, new SelectedMarkerChangedEventArgs(SelectedMarker));

                    var args = new MarkerClickedEventArgs(clickedMarker, _mapControl.Viewport.ScreenToWorld(e.MapInfo.ScreenPosition).ToForms(), e.NumTaps);

                    MarkerClicked?.Invoke(this, args);

                    if (args.Handled)
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName.Equals(nameof(MyLocationFollowProperty)) || propertyName.Equals(nameof(MyLocationFollow)))
            {
                if (MyLocationFollow)
                {
                    mapMyLocationButton.Picture = pictMyLocationCenter;
                    _mapControl.Navigator.CenterOn(MyLocationLayer.MyLocation.ToMapsui());
                }
                else
                {
                    mapMyLocationButton.Picture = pictMyLocationNoCenter;
                }

                Refresh();
            }

            if (propertyName.Equals(nameof(IsZoomButtonVisibleProperty)) || propertyName.Equals(nameof(IsZoomButtonVisible)))
            {
                mapZoomInButton.IsVisible = IsZoomButtonVisible;
                mapZoomOutButton.IsVisible = IsZoomButtonVisible;
                mapSpacingButton1.IsVisible = IsZoomButtonVisible && IsMyLocationButtonVisible;
                _mapButtons.IsVisible = IsZoomButtonVisible || IsMyLocationButtonVisible || IsNorthingButtonVisible;
            }

            if (propertyName.Equals(nameof(IsMyLocationButtonVisibleProperty)) || propertyName.Equals(nameof(IsMyLocationButtonVisible)))
            {
                mapMyLocationButton.IsVisible = IsMyLocationButtonVisible;
                mapSpacingButton1.IsVisible = IsZoomButtonVisible && IsMyLocationButtonVisible;
                _mapButtons.IsVisible = IsZoomButtonVisible || IsMyLocationButtonVisible || IsNorthingButtonVisible;
            }

            if (propertyName.Equals(nameof(IsNorthingButtonVisibleProperty)) || propertyName.Equals(nameof(IsNorthingButtonVisible)))
            {
                mapNorthingButton.IsVisible = IsNorthingButtonVisible;
                mapSpacingButton2.IsVisible = (IsMyLocationButtonVisible || IsZoomButtonVisible) && IsNorthingButtonVisible;
                _mapButtons.IsVisible = IsZoomButtonVisible || IsMyLocationButtonVisible || IsNorthingButtonVisible;
            }
        }

        /// <summary>
        /// Viewport of map has changed
        /// </summary>
        /// <param name="sender">Viewport of this event</param>
        /// <param name="e">Event arguments containing what changed</param>
        protected override void HandlerViewportChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Viewport.Rotation)))
            {
                MyLocationLayer.UpdateMyDirection(MyLocationLayer.Direction, _mapControl.Viewport.Rotation);

                // Update rotationButton
                mapNorthingButton.Rotation = _mapControl.Viewport.Rotation;
            }

            if (e.PropertyName.Equals(nameof(Viewport.Center)))
            {
                if (MyLocationFollow && !_mapControl.Viewport.Center.Equals(MyLocationLayer.MyLocation.ToMapsui()))
                {
                    //_mapControl.Map.NavigateTo(_mapMyLocationLayer.MyLocation.ToMapsui());
                }
            }
        }

        void HandledTouchButtonEvent(SvgTouchButton button, SKTouchEventArgs e)
        {
            if (e.ActionType == SKTouchAction.Pressed)
            {
                pressedButton = button;
                pressedButtonTicks = DateTime.Now.Ticks;
                e.Handled = true;
                isReleased = false;
                InitializeTimer();
            }
            else if (e.ActionType == SKTouchAction.Released)
            {
                if (button == pressedButton && pressedButton == focusOnDeviceButton && (DateTime.Now.Ticks - pressedButtonTicks) < duration * 10000)
                {
                    DeInitializeTimer();

                    if (!e.Handled)
                    {
                        e.Handled = true;
                        var eventArgs = new TouchEventArgs() { TouchEventType = TouchEventType.shortPress };
                        DeviceToFocusRequest?.Invoke(this, eventArgs);
                    }
                }
            }
        }

        void DeInitializeTimer()
        {
            lock (syncObject)
            {
                if (timer == null)
                {
                    return;
                }
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
                timer = null;
            }
        }

        void InitializeTimer()
        {
            lock (syncObject)
            {
                timer = new Timer(Timer_Elapsed, null, duration, Timeout.Infinite);
            }
        }

        private void Timer_Elapsed(object state)
        {
            DeInitializeTimer();
            if (isReleased)
            {
                return;
            }

            var eventArgs = new TouchEventArgs() { TouchEventType = TouchEventType.longPress };

            Device.BeginInvokeOnMainThread(() => DeviceToFocusRequest?.Invoke(this, eventArgs));
        }
    }
}
