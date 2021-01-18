using Ratcow.Mapping.Interfaces;
using Ratcow.Mapping.Support;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Mapsui.Samples.Forms.Shared
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DebugPage : ContentPage
    {
        public DebugPage()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            TheMap.PanTo(58.414682939712385, 15.620379953586431, 3);
        }

        private void Button_Clicked_1(object sender, EventArgs e)
        {
            TheMap.Zoom(ZoomDirection.In);
        }

        private void Button_Clicked_2(object sender, EventArgs e)
        {
            TheMap.Zoom(ZoomDirection.Out);
        }

        IMappedDevice rd  = null;

        private void Button_Clicked_3(object sender, EventArgs e)
        {
            var device = new CommanderUnitData
            {
                Fore = Color.Indigo,
                Back = Color.OrangeRed,
                DeviceId = "1234567890",
                Identifier = "BASE-1",
                Lat = 58.43252561197848,
                Lon = 15.587319219670876,
                Name = "Ops#1",
                IsVisible = true
            };

            TheMap.AddDevice(device);

            rd = new RemoteDeviceData
            {
                Fore = Color.Cyan,
                Back = Color.LawnGreen,
                DeviceId = "1234567895",
                Identifier = "MOB-2",
                Lat = 58.43,
                Lon = 15.58,
                Name = "Target#1",
                IsVisible = true
            };

            TheMap.AddDevice(rd);

            //set the location to the centre of the RD
            TheMap.SetFocusedLocation(58.43, 15.58, true);

            TheMap.FocusedDevice = rd;
        }

        async void Button_Clicked_4(object sender, EventArgs e)
        {
            if(rd is IMappedDevice device)
            {
                if (rd is IHeatMappedDevice hmd)
                {
                    hmd.HeatMap.Add(new HeatMapPoint
                    {
                        Background = Color.GreenYellow,
                        Border = Color.Black,
                        Lat = device.Lat,
                        Lon = device.Lon,
                        DeviceId = rd.DeviceId
                    });
                }

                var lat = device.Lat + 0.0025;
                device.TrySetPosition(lat, device.Lon);

                TheMap.SetFocusedLocation(device.Lat, device.Lon, true);

                if (show)
                {
                    await TheMap.UpdateHeatMapFor(rd as IHeatMappedDevice, true);
                }
            }
        }

        bool show = false;

        async void Button_Clicked_5(object sender, EventArgs e)
        {
            show = !show;
            await TheMap.UpdateHeatMapFor(rd as IHeatMappedDevice, show);
        }
    }
}