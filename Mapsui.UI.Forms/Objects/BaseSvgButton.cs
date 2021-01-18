using System;
using System.IO;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using SKSvg = Svg.Skia.SKSvg;
using Xamarin.Forms;

namespace Mapsui.UI.Forms
{
    public class BaseSvgButton : SKCanvasView
    {
        protected Command _command;
        protected SKPicture _picture;
        protected double _rotation = 0;

        public Command Command
        {
            get
            {
                return _command;
            }
            set
            {
                _command = value;
            }
        }

        public SKPicture Picture
        {
            get => _picture;
            set
            {
                if (_picture != value)
                {
                    _picture = value;
                    InvalidateSurface();
                }
            }
        }

        new public double Rotation
        {
            get => _rotation;
            set
            {
                if (_rotation != value)
                {
                    _rotation = value;
                    InvalidateSurface();
                }
            }
        }

        public BaseSvgButton(Stream stream) : this(new SKSvg().Load(stream))
        {
        }

        public BaseSvgButton(SKPicture picture) : base()
        {
            _picture = picture;
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            // get the size of the canvas
            double canvasMin = Math.Min(CanvasSize.Width, CanvasSize.Height);

            // get the size of the picture
            double svgMax = Math.Max(_picture.CullRect.Width, _picture.CullRect.Height);

            // get the scale to fill the screen
            float scale = (float)(canvasMin / svgMax);

            // Rotate picture
            var matrix = SKMatrix.CreateRotationDegrees((float)_rotation, _picture.CullRect.Width / 2f, _picture.CullRect.Height / 2f);

            // create a scale matrix
            matrix = matrix.PostConcat(SKMatrix.CreateScale(scale, scale));

            e.Surface.Canvas.DrawPicture(_picture, ref matrix, new SKPaint() { IsAntialias = true });
        }
    }
}
