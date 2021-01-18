using Ratcow.Mapping.Interfaces;
using Ratcow.Mapping.Mapsui;
using Ratcow.Mapping.Support;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Rendering.Skia;
using Mapsui.UI.Forms;
using Mapsui.Widgets;
using Mapsui.Widgets.ScaleBar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Ratcow.Mapping.Controls
{
    public enum MapFocusStyle { Host, Device }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomMapView : ContentView, ICustomMapView
    {
        TileLayer baseLayer;
        Map currentMap;

        const string HeatMapName = "HeatMap";
        const string BaseName = "BaseDeviceLayer";
        const string MobileName = "MobileDeviceLayer";

        public MapViewEx Internal => mapView;

        public CustomMapView()
        {
            InitializeComponent();

            InitializeMap();

            mapView.RotationLock = false;
            mapView.UnSnapRotationDegrees = 30;
            mapView.ReSnapRotationDegrees = 5;

            mapView.PinClicked += OnPinClicked;
            mapView.MarkerClicked += OnMarkerClicked;
            mapView.MapClicked += OnMapClicked;

            var index = mapView.Map.Layers.ToList().FindIndex(x => x.Name == "Callouts");

            mapView.AddCustomLayer<BaseMarker>(BaseName, index - 1);
            mapView.AddCustomLayer<MobileMarker>(MobileName, index - 1);
            mapView.AddCustomLayer<HeatMapMarker>(HeatMapName, index - 1);
            mapView.MyLocationLayer.ShowMarker = false;
            mapView.MobileDisplayModeChanged += MapView_MapModeChanged;
            mapView.DeviceToFocusRequest += MapView_DeviceToFocusRequest;
        }

        private void MapView_DeviceToFocusRequest(object sender, TouchEventArgs e)
        {   
        }

        void MapView_MapModeChanged(object sender, MapMobileDisplayModeEventArgs e)
        {
            SetMapMode(e.Mode);
        }

        void SetMapMode(MapMobileDisplayMode mode)
        {
            foreach (var device in Devices)
            {
                if (device is IMobileMarkerProvider provider)
                {
                    SetMapModeFor(provider, mode, false);
                }
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                mapView.Refresh();
            });
        }

        void SetMapModeFor(IMobileMarkerProvider provider, MapMobileDisplayMode mode, bool refresh = true)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                switch (mode)
                {
                    case MapMobileDisplayMode.Default:
                        {

                            provider.Marker.IsVisible = false;
                        }
                        break;
                    case MapMobileDisplayMode.Pinned:
                        {

                            if (provider.HasPosition)
                            {
                                provider.Marker.IsVisible = true;
                            }

                        }
                        break;
                    case MapMobileDisplayMode.Focused:
                        {
                            // yeah, this is going around the houses, but this was originally one big loop over Devices so it made more sense.
                            if (provider.HasPosition && provider is IMappedDevice device)
                            {
                                provider.Marker.IsVisible = device.IsFocused;
                            }
                        }
                        break;
                }

                if (refresh)
                {
                    mapView.Refresh();
                }
            });
        }

        public static readonly BindableProperty DefaultZoomLevelProperty =
            BindableProperty.Create(nameof(DefaultZoomLevel), typeof(double), typeof(CustomMapView), 9.0, defaultBindingMode: BindingMode.TwoWay);

        public double DefaultZoomLevel
        {
            get => (double)GetValue(DefaultZoomLevelProperty);
            set => SetValue(DefaultZoomLevelProperty, value);
        }

        public static readonly BindableProperty FocusStyleProperty =
            BindableProperty.Create(nameof(FocusStyle), typeof(MapFocusStyle), typeof(CustomMapView), MapFocusStyle.Host, defaultBindingMode: BindingMode.TwoWay);

        public MapFocusStyle FocusStyle
        {
            get => (MapFocusStyle)GetValue(FocusStyleProperty);
            set => SetValue(FocusStyleProperty, value);
        }

        Position homePosition = new Position(0, 0);

        public void SetHome(Position position)
        {
            homePosition = position;

            Home(mapView.Navigator);
        }

        public static readonly BindableProperty DevicesProperty =
           BindableProperty.Create(nameof(Devices), typeof(List<IMappedDevice>), typeof(CustomMapView), new List<IMappedDevice>(), defaultBindingMode: BindingMode.TwoWay);

        public IList<IMappedDevice> Devices
        {
            get => (List<IMappedDevice>)GetValue(DevicesProperty);
            set => SetValue(DevicesProperty, value);
        }

        public static readonly BindableProperty FocusedDeviceProperty =
           BindableProperty.Create(nameof(Device), typeof(IMappedDevice), typeof(CustomMapView), default(IMappedDevice), defaultBindingMode: BindingMode.TwoWay);

        public IMappedDevice FocusedDevice
        {
            get => (IMappedDevice)GetValue(FocusedDeviceProperty);
            set => SetValue(FocusedDeviceProperty, value);
        }

        public void RemoveDevice(IMappedDevice device)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (Devices.Any(x => x.DeviceId == device.DeviceId))
                {
                    if (device is IBaseMarkerProvider basemarker)
                    {
                        device.PositionUpdated -= Device_PositionUpdated;
                        Devices.Remove(device);

                        mapView[BaseName].RemoveSymbol(basemarker.Marker);
                        basemarker.Marker = null;
                    }
                    else if (device is IMobileMarkerProvider mobilemarker)
                    {
                        device.PositionUpdated -= Device_PositionUpdated;
                        Devices.Remove(device);


                        mapView[MobileName].RemoveSymbol(mobilemarker.Marker);
                        mobilemarker.Marker = null;
                    }
                }
            });
        }
        public void AddDevice(IMappedDevice device)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (!Devices.Any(x => x.DeviceId == device.DeviceId))
                {
                    switch (device)
                    {
                        case IBaseMarkerProvider basemarker:
                            {
                                device.PositionUpdated += Device_PositionUpdated;
                                Devices.Add(device);

                                var position = new Position(device.Lat, device.Lon);

                                var basedevice = new BaseMarker(mapView, device.Fore, device.Back)
                                {
                                    Position = position,
                                    Label = device.Identifier,
                                    DeviceIdentifier = device.Identifier,
                                    Name = device.Name,
                                    RotateWithMap = true,
                                    Scale = 1f,
                                };

                                basemarker.Marker = basedevice;

                                mapView[BaseName].AddSymbol(basedevice);
                            }
                            break;

                        case IMobileMarkerProvider mobilemarker:

                            {
                                device.PositionUpdated += Device_PositionUpdated;
                                Devices.Add(device);

                                var position = new Position(device.Lat, device.Lon);

                                var mobiledevice = new MobileMarker(mapView, device.Fore, device.Back)
                                {
                                    Position = position,
                                    Label = device.Identifier,
                                    DeviceIdentifier = device.Identifier,
                                    Name = device.Name,
                                    RotateWithMap = true,
                                    Scale = 1f,
                                };

                                mobilemarker.Marker = mobiledevice;

                                mapView[MobileName].AddSymbol(mobiledevice);

                                SetMapModeFor(mobilemarker, mapView?.MobileDisplayMode ?? MapMobileDisplayMode.Default, false);
                            }
                            break;
                    }
                }

                // only pan to the lat long if the device is visible, otherwise the map will pan for no reason.
                if (device?.IsVisible ?? false)
                {
                    PanTo(device.Lat, device.Lon);
                }
            });
        }

        void Device_PositionUpdated(object sender, PositionEventArgs e)
        {
            if (sender is IBaseMarkerProvider basedevice)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    //find the pin 
                    basedevice.Marker.Position = new Position(e.Lat, e.Lon);
                    mapView.Refresh();
                });
            }

            else if (sender is IMobileMarkerProvider mobiledevice)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    //find the pin 
                    mobiledevice.Marker.Position = new Position(e.Lat, e.Lon);
                    SetMapModeFor(mobiledevice, mapView.MobileDisplayMode);
                    mapView.Refresh();
                });
            }
        }

        private void OnMarkerClicked(object sender, MarkerClickedEventArgs e)
        {
            if (e.Marker != null)
            {
                if (e.NumOfTaps == 2)
                {
                    e.Marker.IsVisible = false;
                }
                if (e.NumOfTaps == 1 && e.Marker is ICalloutSymbol cmarker)
                {
                    if (cmarker.Callout.IsVisible)
                    {
                        cmarker.HideCallout();
                    }
                    else
                    {
                        cmarker.ShowCallout();
                    }
                }
            }

            e.Handled = true;
        }

        void OnPinClicked(object sender, PinClickedEventArgs e)
        {
            if (e.Pin != null)
            {
                if (e.NumOfTaps == 2)
                {
                    e.Pin.IsVisible = false;
                }
                if (e.NumOfTaps == 1)
                    if (e.Pin.Callout.IsVisible)
                        e.Pin.HideCallout();
                    else
                        e.Pin.ShowCallout();
            }

            e.Handled = true;
        }

        void OnMapClicked(object sender, MapClickedEventArgs e)
        {
        }

        // here we create the base map - in the future, this will be more complicated
        public void InitializeMap()
        {
            var factory = new OsmMapViewFactory();
            currentMap = factory.CreateMap();
            baseLayer = factory.CreateBaseLayer();
            currentMap.Layers.Add(baseLayer);

            currentMap.Home = n => Home(n);

            currentMap.Widgets.Add(
                new ScaleBarWidget(currentMap)
                {
                    TextAlignment = Alignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top
                });

            mapView.Map = currentMap;
        }

        public void PanTo(double lat, double lon, double level)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(lon, lat);
                mapView.Navigator.NavigateTo(sphericalMercatorCoordinate, level);
            });
        }

        public void PanTo(double lat, double lon)
        {
            PanTo(lat, lon, DefaultZoomLevel);
        }

        public void Zoom(ZoomDirection direction)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                switch (direction)
                {
                    case ZoomDirection.In:
                        mapView.Navigator.ZoomIn();
                        break;
                    case ZoomDirection.Out:
                        mapView.Navigator.ZoomOut();
                        break;
                    case ZoomDirection.Home:
                    default:
                        mapView.Navigator.ZoomTo(DefaultZoomLevel);
                        break;
                }
            });
        }

        public void ZoomToLevel(double level)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                mapView.Navigator.ZoomTo(level);
            });
        }

        public void CentreOn(double lat, double lon)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(lon, lat);
                mapView.Navigator.CenterOn(sphericalMercatorCoordinate);
            });
        }

        void Home(INavigator navigator)
        {
            if (navigator != null)
            {
                if (FocusStyle == MapFocusStyle.Device && FocusedDevice != null)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(FocusedDevice.Lon, FocusedDevice.Lat);
                        navigator.CenterOn(sphericalMercatorCoordinate);
                    });
                }
                else if (FocusStyle == MapFocusStyle.Host)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(homePosition.Longitude, homePosition.Latitude);
                        navigator.NavigateTo(sphericalMercatorCoordinate, DefaultZoomLevel);
                    });
                }
            }
        }

        public void SetFocusedLocation(double lat, double lon, bool isDevice)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var position = new Position(lat, lon);
                mapView.MyLocationLayer.UpdateMyLocation(position, isDevice, false);

                homePosition = position; // if we don't set this, weird stuff happens when we call "Home(..)"

                if (mapView.MyLocationFollow)
                {
                    CentreOn(lat, lon);
                }

                mapView.Refresh();
            });
        }

        public bool AddHeatMapPoint(IHeatMapMarker marker)
        {
            if (marker is HeatMapMarker hmarker)
            {
                return mapView[HeatMapName].AddSymbol(hmarker);
            }

            return false;
        }

        public async Task<bool> UpdateHeatMapFor(IHeatMappedDevice device, bool show)
        {
            return await Task.Run(() =>
            {
                var result = false;

                if (show)
                {
                    if (device.HeatMap?.Count > 0)
                    {
                        var heatmap = device.HeatMap
                                            .ToArray()
                                            .Select(x => x.ToHeatMapMarker(mapView));

                        mapView[HeatMapName].AddSymbols(heatmap);
                    }
                }
                else
                {
                    mapView[HeatMapName].ClearSymbols();
                }


                return result;
            });
        }
    }
}