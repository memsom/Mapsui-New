using System.IO;
using SkiaSharp;
using Xamarin.Forms;

namespace Mapsui.UI.Forms
{
    public class SvgButton : BaseSvgButton
    {
        public SvgButton(Stream stream) : base(stream)
        {
            AddGestureRecognizers();
        }

        public SvgButton(SKPicture picture) : base(picture)
        {
            AddGestureRecognizers();
        }

        void AddGestureRecognizers()
        {
            GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(CommandHandler)
            });
        }

        void CommandHandler(object obj)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                _command?.Execute(obj);
            });
        }

    }
}
