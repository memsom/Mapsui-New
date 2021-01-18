using System.IO;
using SkiaSharp;

namespace Mapsui.UI.Forms
{
    public class SvgTouchButton: BaseSvgButton
    {
        public SvgTouchButton(Stream stream) : base(stream)
        {
            CommonConstructor();
        }

        public SvgTouchButton(SKPicture picture) : base(picture)
        {
            CommonConstructor();
        }

        private void CommonConstructor()
        {
            EnableTouchEvents = true;
            Touch += (s, e) => _command?.Execute(e);
        }
    }
}
