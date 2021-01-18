using Mapsui.Geometries.Utilities;
using Mapsui.Rendering;
using Mapsui.UI.Utils;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Mapsui.Geometries;
using System.ComponentModel;
using System.Net;
using Mapsui.Fetcher;
using Mapsui.Layers;
using Mapsui.Logging;
using Mapsui.Rendering.Skia;
using Mapsui.Widgets;
using System.Runtime.CompilerServices;
using Point = Mapsui.Geometries.Point;

namespace Mapsui.UI.Forms
{
    /// <summary>
    /// Class, that uses the API of all other Mapsui MapControls
    /// </summary>
    public partial class MapControl : SKGLView, IMapControl, IDisposable, INotifyPropertyChanged
    {
        private Map _map;
        private double _unSnapRotationDegrees;

        // See http://grepcode.com/file/repository.grepcode.com/java/ext/com.google.android/android/4.0.4_r2.1/android/view/ViewConfiguration.java#ViewConfiguration.0PRESSED_STATE_DURATION for values
        private const int shortTap = 125;
        private const int shortClick = 250;
        private const int delayTap = 200;
        private const int longTap = 500;

        /// <summary>
        /// If a finger touches down and up it counts as a tap if the distance between the down and up location is smaller
        /// then the touch slob.
        /// The slob is initialized at 8. How did we get to 8? Well you could read the discussion here: https://github.com/Mapsui/Mapsui/issues/602
        /// We basically copied it from the Java source code: https://android.googlesource.com/platform/frameworks/base/+/master/core/java/android/view/ViewConfiguration.java#162
        /// </summary>
        private const int touchSlop = 8;

        private bool _initialized = false;
        private double _innerRotation;
        private ConcurrentDictionary<long, TouchEvent> _touches = new ConcurrentDictionary<long, TouchEvent>();
        private Geometries.Point _firstTouch;
        private bool _waitingForDoubleTap;
        private int _numOfTaps = 0;
        private readonly FlingTracker _flingTracker = new FlingTracker();
        private Geometries.Point _previousCenter;

        /// <summary>
        /// Saver for angle before last pinch movement
        /// </summary>
        private double _previousAngle;

        /// <summary>
        /// Saver for radius before last pinch movement
        /// </summary>
        private double _previousRadius = 1f;

        private TouchMode _mode;

        public MapControl()
        {
            Initialize();
        }

        public float ScreenWidth => (float)Width;

        public float ScreenHeight => (float)Height;

        private float ViewportWidth => ScreenWidth;

        private float ViewportHeight => ScreenHeight;

        public ISymbolCache SymbolCache => _renderer.SymbolCache;

        public bool UseDoubleTap = true;

        public void Initialize()
        {
            Map = new Map();
            BackgroundColor = Color.White;

            EnableTouchEvents = true;

            PaintSurface += OnPaintSurface;
            Touch += OnTouch;
            SizeChanged += OnSizeChanged;

            _initialized = true;
        }

        /// <summary>
        /// After how many degrees start rotation to take place
        /// </summary>
        public double UnSnapRotationDegrees
        {
            get { return _unSnapRotationDegrees; }
            set
            {
                if (_unSnapRotationDegrees != value)
                {
                    _unSnapRotationDegrees = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _reSnapRotationDegrees;

        /// <summary>
        /// With how many degrees from 0 should map snap to 0 degrees
        /// </summary>
        public double ReSnapRotationDegrees
        {
            get { return _reSnapRotationDegrees; }
            set
            {
                if (_reSnapRotationDegrees != value)
                {
                    _reSnapRotationDegrees = value;
                    OnPropertyChanged();
                }
            }
        }

        public float PixelDensity
        {
            get => GetPixelDensity();
        }

        private IRenderer _renderer = new MapRenderer();

        /// <summary>
        /// Renderer that is used from this MapControl
        /// </summary>
        public IRenderer Renderer
        {
            get { return _renderer; }
            set
            {
                if (_renderer != value)
                {
                    _renderer = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly LimitedViewport _viewport = new LimitedViewport();
        private INavigator _navigator;

        /// <summary>
        /// Viewport holding information about visible part of the map. Viewport can never be null.
        /// </summary>
        public IReadOnlyViewport Viewport => _viewport;

        /// <summary>
        /// Handles all manipulations of the map viewport
        /// </summary>
        public INavigator Navigator
        {
            get => _navigator;
            set
            {
                if (_navigator != null)
                {
                    _navigator.Navigated -= Navigated;
                }
                _navigator = value ?? throw new ArgumentException($"{nameof(Navigator)} can not be null");
                _navigator.Navigated += Navigated;
            }
        }

        private void Navigated(object sender, ChangeType changeType)
        {
            _map.Initialized = true;
            Refresh(changeType);
        }

        /// <summary>
        /// Called when the viewport is initialized
        /// </summary>
        public event EventHandler ViewportInitialized; //todo: Consider to use the Viewport PropertyChanged

        /// <summary>
        /// Called whenever a feature in one of the layers in InfoLayers is hitten by a click 
        /// </summary>
        public event EventHandler<MapInfoEventArgs> Info;


        private void OnSizeChanged(object sender, EventArgs e)
        {
            _touches.Clear();
            SetViewportSize();
        }

        /// <summary>
        /// Called whenever a property is changed
        /// </summary>
        public new event PropertyChangedEventHandler PropertyChanged;

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Unsubscribe from map events 
        /// </summary>
        public void Unsubscribe()
        {
            UnsubscribeFromMapEvents(_map);
        }

        /// <summary>
        /// Subscribe to map events
        /// </summary>
        /// <param name="map">Map, to which events to subscribe</param>
        private void SubscribeToMapEvents(Map map)
        {
            map.DataChanged += MapDataChanged;
            map.PropertyChanged += MapPropertyChanged;
        }

        /// <summary>
        /// Unsubcribe from map events
        /// </summary>
        /// <param name="map">Map, to which events to unsubscribe</param>
        private void UnsubscribeFromMapEvents(Map map)
        {
            var temp = map;
            if (temp != null)
            {
                temp.DataChanged -= MapDataChanged;
                temp.PropertyChanged -= MapPropertyChanged;
                temp.AbortFetch();
            }
        }

        /// <summary>
        /// Refresh data of the map and than repaint it
        /// </summary>
        public void Refresh(ChangeType changeType = ChangeType.Discrete)
        {
            RefreshData(changeType);
            RefreshGraphics();
        }

        private void MapDataChanged(object sender, DataChangedEventArgs e)
        {
            RunOnUIThread(() =>
            {
                try
                {
                    if (e == null)
                    {
                        Logger.Log(LogLevel.Warning, "Unexpected error: DataChangedEventArgs can not be null");
                    }
                    else if (e.Cancelled)
                    {
                        Logger.Log(LogLevel.Warning, "Fetching data was cancelled", e.Error);
                    }
                    else if (e.Error is WebException)
                    {
                        Logger.Log(LogLevel.Warning, "A WebException occurred. Do you have internet?", e.Error);
                    }
                    else if (e.Error != null)
                    {
                        Logger.Log(LogLevel.Warning, "An error occurred while fetching data", e.Error);
                    }
                    else // no problems
                    {
                        RefreshGraphics();
                    }
                }
                catch (Exception exception)
                {
                    Logger.Log(LogLevel.Warning, $"Unexpected exception in {nameof(MapDataChanged)}", exception);
                }
            });
        }
        // ReSharper disable RedundantNameQualifier - needed for iOS for disambiguation

        private void MapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Layers.Layer.Enabled))
            {
                RefreshGraphics();
            }
            else if (e.PropertyName == nameof(Layers.Layer.Opacity))
            {
                RefreshGraphics();
            }
            else if (e.PropertyName == nameof(Map.BackColor))
            {
                RefreshGraphics();
            }
            else if (e.PropertyName == nameof(Layers.Layer.DataSource))
            {
                Refresh(); // There is a new DataSource so let's fetch the new data.
            }
            else if (e.PropertyName == nameof(Map.Envelope))
            {
                CallHomeIfNeeded();
                Refresh();
            }
            else if (e.PropertyName == nameof(Map.Layers))
            {
                CallHomeIfNeeded();
                Refresh();
            }
            if (e.PropertyName.Equals(nameof(Map.Limiter)))
            {
                _viewport.Limiter = Map.Limiter;
            }
        }
        // ReSharper restore RedundantNameQualifier

        public void CallHomeIfNeeded()
        {
            if (_map != null && !_map.Initialized && _viewport.HasSize && _map?.Envelope != null)
            {
                _map.Home?.Invoke(Navigator);
                _map.Initialized = true;
            }
        }

        /// <summary>
        /// Map holding data for which is shown in this MapControl
        /// </summary>
        public Map Map
        {
            get => _map;
            set
            {
                if (_map != null)
                {
                    UnsubscribeFromMapEvents(_map);
                    _map = null;
                }

                _map = value;

                if (_map != null)
                {
                    SubscribeToMapEvents(_map);
                    Navigator = new Navigator(_map, _viewport);
                    _viewport.Map = Map;
                    _viewport.Limiter = Map.Limiter;
                    CallHomeIfNeeded();
                }

                Refresh();
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public Point ToPixels(Point coordinateInDeviceIndependentUnits)
        {
            return new Point(
                coordinateInDeviceIndependentUnits.X * PixelDensity,
                coordinateInDeviceIndependentUnits.Y * PixelDensity);
        }

        /// <inheritdoc />
        public Point ToDeviceIndependentUnits(Point coordinateInPixels)
        {
            return new Point(coordinateInPixels.X / PixelDensity, coordinateInPixels.Y / PixelDensity);
        }

        private void OnViewportSizeInitialized()
        {
            ViewportInitialized?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Refresh data of Map, but don't paint it
        /// </summary>
        public void RefreshData(ChangeType changeType = ChangeType.Discrete)
        {
            _map?.RefreshData(Viewport.Extent, Viewport.Resolution, changeType);
        }

        private void OnInfo(MapInfoEventArgs mapInfoEventArgs)
        {
            if (mapInfoEventArgs == null) return;

            Info?.Invoke(this, mapInfoEventArgs);
        }

        private bool WidgetTouched(IWidget widget, Point screenPosition)
        {
            var result = widget.HandleWidgetTouched(Navigator, screenPosition);

            if (!result && widget is Hyperlink hyperlink && !string.IsNullOrWhiteSpace(hyperlink.Url))
            {
                OpenBrowser(hyperlink.Url);

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public MapInfo GetMapInfo(Point screenPosition, int margin = 0)
        {
            return Renderer.GetMapInfo(screenPosition.X, screenPosition.Y, Viewport, Map.Layers, margin);
        }

        /// <inheritdoc />
        public byte[] GetSnapshot(IEnumerable<ILayer> layers = null)
        {
            byte[] result = null;

            using (var stream = Renderer.RenderToBitmapStream(Viewport, layers ?? Map.Layers, pixelDensity: PixelDensity))
            {
                if (stream != null)
                    result = stream.ToArray();
            }

            return result;
        }

        /// <summary>
        /// Check if a widget or feature at a given screen position is clicked/tapped
        /// </summary>
        /// <param name="screenPosition">Screen position to check for widgets and features</param>
        /// <param name="startScreenPosition">Screen position of Viewport/MapControl</param>
        /// <param name="numTaps">Number of clickes/taps</param>
        /// <returns>True, if something done </returns>
        private MapInfoEventArgs InvokeInfo(Point screenPosition, Point startScreenPosition, int numTaps)
        {
            return InvokeInfo(
                Map.GetWidgetsOfMapAndLayers(),
                screenPosition,
                startScreenPosition,
                WidgetTouched,
                numTaps);
        }

        /// <summary>
        /// Check if a widget or feature at a given screen position is clicked/tapped
        /// </summary>
        /// <param name="widgets">The Map widgets</param>
        /// <param name="screenPosition">Screen position to check for widgets and features</param>
        /// <param name="startScreenPosition">Screen position of Viewport/MapControl</param>
        /// <param name="widgetCallback">Callback, which is called when Widget is hit</param>
        /// <param name="numTaps">Number of clickes/taps</param>
        /// <returns>True, if something done </returns>
        private MapInfoEventArgs InvokeInfo(IEnumerable<IWidget> widgets, Point screenPosition,
            Point startScreenPosition, Func<IWidget, Point, bool> widgetCallback, int numTaps)
        {
            // Check if a Widget is tapped. In the current design they are always on top of the map.
            var touchedWidgets = WidgetTouch.GetTouchedWidget(screenPosition, startScreenPosition, widgets);

            foreach (var widget in touchedWidgets)
            {
                var result = widgetCallback(widget, screenPosition);

                if (result)
                {
                    return new MapInfoEventArgs
                    {
                        Handled = true
                    };
                }
            }

            // Check which features in the map were tapped.
            var mapInfo = Renderer.GetMapInfo(screenPosition.X, screenPosition.Y, Viewport, Map.Layers);

            if (mapInfo != null)
            {
                return new MapInfoEventArgs
                {
                    MapInfo = mapInfo,
                    NumTaps = numTaps,
                    Handled = false
                };
            }

            return null;
        }

        private void SetViewportSize()
        {
            var hadSize = Viewport.HasSize;
            _viewport.SetSize(ViewportWidth, ViewportHeight);
            if (!hadSize && Viewport.HasSize) OnViewportSizeInitialized();
            CallHomeIfNeeded();
            Refresh();
        }

        /// <summary>
        /// Clear cache and repaint map
        /// </summary>
        public void Clear()
        {
            // not sure if we need this method
            _map?.ClearCache();
            RefreshGraphics();
        }

        private async void OnTouch(object sender, SKTouchEventArgs e)
        {
            // Save time, when the event occures
            long ticks = DateTime.Now.Ticks;

            var location = GetScreenPosition(e.Location);

            if (e.ActionType == SKTouchAction.Pressed)
            {
                _firstTouch = location;

                _touches[e.Id] = new TouchEvent(e.Id, location, ticks);

                _flingTracker.Clear();

                // Do we have a doubleTapTestTimer running?
                // If yes, stop it and increment _numOfTaps
                if (_waitingForDoubleTap)
                {
                    _waitingForDoubleTap = false;
                    _numOfTaps++;
                }
                else
                    _numOfTaps = 1;

                e.Handled = OnTouchStart(_touches.Select(t => t.Value.Location).ToList());
            }
            // Delete e.Id from _touches, because finger is released
            else if (e.ActionType == SKTouchAction.Released && _touches.TryRemove(e.Id, out var releasedTouch))
            {
                // Is this a fling or swipe?
                if (_touches.Count == 0)
                {
                    double velocityX;
                    double velocityY;

                    (velocityX, velocityY) = _flingTracker.CalcVelocity(e.Id, ticks);

                    if (Math.Abs(velocityX) > 200 || Math.Abs(velocityY) > 200)
                    {
                        // This was the last finger on screen, so this is a fling
                        e.Handled = OnFlinged(velocityX, velocityY);
                    }

                    // Do we have a tap event
                    if (releasedTouch == null)
                    {
                        e.Handled = false;
                        return;
                    }

                    // While tapping on screen, there could be a small movement of the finger
                    // (especially on Samsung). So check, if touch start location isn't more 
                    // than a number of pixels away from touch end location.
                    bool isAround = IsAround(releasedTouch);

                    // If touch start and end is in the same area and the touch time is shorter
                    // than longTap, than we have a tap.
                    if (isAround && (ticks - releasedTouch.Tick) < (e.DeviceType == SKTouchDeviceType.Mouse ? shortClick : longTap) * 10000)
                    {
                        _waitingForDoubleTap = true;
                        if (UseDoubleTap) { await Task.Delay(delayTap); }

                        if (_numOfTaps > 1)
                        {
                            if (!e.Handled)
                                e.Handled = OnDoubleTapped(location, _numOfTaps);
                        }
                        else
                        {
                            if (!e.Handled)
                            {
                                e.Handled = OnSingleTapped(location);
                            }
                        }
                        _numOfTaps = 1;
                        if (_waitingForDoubleTap)
                        {
                            _waitingForDoubleTap = false; ;
                        }
                    }
                    else if (isAround && (ticks - releasedTouch.Tick) >= longTap * 10000)
                    {
                        if (!e.Handled)
                            e.Handled = OnLongTapped(location);
                    }
                }

                _flingTracker.RemoveId(e.Id);

                if (_touches.Count == 1)
                {
                    e.Handled = OnTouchStart(_touches.Select(t => t.Value.Location).ToList());
                }

                if (!e.Handled)
                    e.Handled = OnTouchEnd(_touches.Select(t => t.Value.Location).ToList(), releasedTouch.Location);
            }
            else if (e.ActionType == SKTouchAction.Moved)
            {
                _touches[e.Id] = new TouchEvent(e.Id, location, ticks);

                if (e.InContact)
                    _flingTracker.AddEvent(e.Id, location, ticks);

                if (e.InContact && !e.Handled)
                    e.Handled = OnTouchMove(_touches.Select(t => t.Value.Location).ToList());
                else
                    e.Handled = OnHovered(_touches.Select(t => t.Value.Location).FirstOrDefault());
            }
            else if (e.ActionType == SKTouchAction.Cancelled)
            {
                // This gesture is cancelled, so clear all touches
                _touches.Clear();
            }
            else if (e.ActionType == SKTouchAction.Exited && _touches.TryRemove(e.Id, out var exitedTouch))
            {
                e.Handled = OnTouchExited(_touches.Select(t => t.Value.Location).ToList(), exitedTouch.Location);
            }
            else if (e.ActionType == SKTouchAction.Entered)
            {
                e.Handled = OnTouchEntered(_touches.Select(t => t.Value.Location).ToList());
            }
            else if (e.ActionType == SKTouchAction.WheelChanged)
            {
                if (e.WheelDelta > 0)
                {
                    OnZoomIn(location);
                }
                else
                {
                    OnZoomOut(location);
                }
            }
        }

        private bool IsAround(TouchEvent releasedTouch)
        {
            if (_firstTouch == null) { return false; }
            if (releasedTouch.Location == null) { return false; }
            return _firstTouch == null ? false : Algorithms.Distance(releasedTouch.Location, _firstTouch) < touchSlop;
        }

        void OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs skPaintSurfaceEventArgs)
        {
            if (PixelDensity <= 0) return;

            Navigator.UpdateAnimations();

            skPaintSurfaceEventArgs.Surface.Canvas.Scale(PixelDensity, PixelDensity);

            var canvas = skPaintSurfaceEventArgs.Surface.Canvas;

            _renderer.Render(canvas, new Viewport(Viewport), _map.Layers, _map.Widgets, _map.BackColor);
        }

        private Geometries.Point GetScreenPosition(SKPoint point)
        {
            return new Geometries.Point(point.X / PixelDensity, point.Y / PixelDensity);
        }

        public void RefreshGraphics()
        {
            if (!_initialized && GRContext == null)
            {
                // Could this be null before Home is called? If so we should change the logic.
                Logging.Logger.Log(Logging.LogLevel.Warning, "Refresh can not be called because GRContext is null");
                return;
            }

            RunOnUIThread(InvalidateSurface);
        }

        /// <summary>
        /// Event handlers
        /// </summary>

        /// <summary>
        /// TouchStart is called, when user press a mouse button or touch the display
        /// </summary>
        public event EventHandler<TouchedEventArgs> TouchStarted;

        /// <summary>
        /// TouchEnd is called, when user release a mouse button or doesn't touch display anymore
        /// </summary>
        public event EventHandler<TouchedEventArgs> TouchEnded;

        /// <summary>
        /// TouchEntered is called, when user moves an active touch onto the view
        /// </summary>
        public event EventHandler<TouchedEventArgs> TouchEntered;

        /// <summary>
        /// TouchExited is called, when user moves an active touch off the view
        /// </summary>
        public event EventHandler<TouchedEventArgs> TouchExited;

        /// <summary>
        /// TouchMove is called, when user move mouse over map (independent from mouse button state) or move finger on display
        /// </summary>
#if __WPF__
        public new event EventHandler<TouchedEventArgs> TouchMove;
#else
        public event EventHandler<TouchedEventArgs> TouchMove;
#endif

        /// <summary>
        /// Hover is called, when user move mouse over map without pressing mouse button
        /// </summary>
#if __ANDROID__
        public new event EventHandler<HoveredEventArgs> Hovered;
#else
        public event EventHandler<HoveredEventArgs> Hovered;
#endif

        /// <summary>
        /// Swipe is called, when user release mouse button or lift finger while moving with a certain speed 
        /// </summary>
        public event EventHandler<SwipedEventArgs> Swipe;

        /// <summary>
        /// Fling is called, when user release mouse button or lift finger while moving with a certain speed, higher than speed of swipe 
        /// </summary>
        public event EventHandler<SwipedEventArgs> Fling;

        /// <summary>
        /// SingleTap is called, when user clicks with a mouse button or tap with a finger on map 
        /// </summary>
        public event EventHandler<TappedEventArgs> SingleTap;

        /// <summary>
        /// LongTap is called, when user clicks with a mouse button or tap with a finger on map for 500 ms
        /// </summary>
        public event EventHandler<TappedEventArgs> LongTap;

        /// <summary>
        /// DoubleTap is called, when user clicks with a mouse button or tap with a finger two or more times on map
        /// </summary>
        public event EventHandler<TappedEventArgs> DoubleTap;

        /// <summary>
        /// Zoom is called, when map should be zoomed
        /// </summary>
        public event EventHandler<ZoomedEventArgs> Zoomed;

        /// <summary>
        /// Called, when map should zoom out
        /// </summary>
        /// <param name="screenPosition">Center of zoom out event</param>
        private bool OnZoomOut(Geometries.Point screenPosition)
        {
            if (Map.ZoomLock)
            {
                return true;
            }

            var args = new ZoomedEventArgs(screenPosition, ZoomDirection.ZoomOut);

            Zoomed?.Invoke(this, args);

            if (args.Handled)
                return true;

            // Perform standard behavior
            Navigator.ZoomOut(screenPosition);

            return true;
        }

        /// <summary>
        /// Called, when map should zoom in
        /// </summary>
        /// <param name="screenPosition">Center of zoom in event</param>
        private bool OnZoomIn(Geometries.Point screenPosition)
        {
            if (Map.ZoomLock)
            {
                return true;
            }

            var args = new ZoomedEventArgs(screenPosition, ZoomDirection.ZoomIn);

            Zoomed?.Invoke(this, args);

            if (args.Handled)
                return true;

            // Perform standard behavior
            Navigator.ZoomIn(screenPosition);

            return true;
        }

        /// <summary>
        /// Called, when mouse/finger/pen hovers around
        /// </summary>
        /// <param name="screenPosition">Actual position of mouse/finger/pen</param>
        private bool OnHovered(Geometries.Point screenPosition)
        {
            var args = new HoveredEventArgs(screenPosition);

            Hovered?.Invoke(this, args);

            return args.Handled;
        }

        /// <summary>
        /// Called, when mouse/finger/pen swiped over map
        /// </summary>
        /// <param name="velocityX">Velocity in x direction in pixel/second</param>
        /// <param name="velocityY">Velocity in y direction in pixel/second</param>
        private bool OnSwiped(double velocityX, double velocityY)
        {
            var args = new SwipedEventArgs(velocityX, velocityY);

            Swipe?.Invoke(this, args);

            // TODO
            // Perform standard behavior

            return args.Handled;
        }

        /// <summary>
        /// Called, when mouse/finger/pen flinged over map
        /// </summary>
        /// <param name="velocityX">Velocity in x direction in pixel/second</param>
        /// <param name="velocityY">Velocity in y direction in pixel/second</param>
        private bool OnFlinged(double velocityX, double velocityY)
        {
            var args = new SwipedEventArgs(velocityX, velocityY);

            Fling?.Invoke(this, args);

            // TODO
            // Perform standard behavior

            if (args.Handled)
                return true;

            Navigator.FlingWith(velocityX, velocityY, 1000);

            return true;
        }

        /// <summary>
        /// Called, when mouse/finger/pen click/touch map
        /// </summary>
        /// <param name="touchPoints">List of all touched points</param>
        private bool OnTouchStart(List<Geometries.Point> touchPoints)
        {
            // Sanity check
            if (touchPoints.Count == 0)
                return false;

            // We have a new interaction with the screen, so stop all navigator animations
            Navigator.StopRunningAnimation();

            var args = new TouchedEventArgs(touchPoints);

            TouchStarted?.Invoke(this, args);

            if (args.Handled)
                return true;

            if (touchPoints.Count == 2)
            {
                (_previousCenter, _previousRadius, _previousAngle) = GetPinchValues(touchPoints);
                _mode = TouchMode.Zooming;
                _innerRotation = Viewport.Rotation;
            }
            else
            {
                _mode = TouchMode.Dragging;
                _previousCenter = touchPoints.First();
            }

            return true;
        }

        /// <summary>
        /// Called, when mouse/finger/pen anymore click/touch map
        /// </summary>
        /// <param name="touchPoints">List of all touched points</param>
        /// <param name="releasedPoint">Released point, which was touched before</param>
        private bool OnTouchEnd(List<Geometries.Point> touchPoints, Geometries.Point releasedPoint)
        {
            var args = new TouchedEventArgs(touchPoints);

            TouchEnded?.Invoke(this, args);

            // Last touch released
            if (touchPoints.Count == 0)
            {
                _mode = TouchMode.None;
                _map.RefreshData(_viewport.Extent, _viewport.Resolution, ChangeType.Discrete);
            }

            return args.Handled;
        }

        /// <summary>
        /// Called when touch enters map
        /// </summary>
        /// <param name="touchPoints">List of all touched points</param>
        private bool OnTouchEntered(List<Geometries.Point> touchPoints)
        {
            // Sanity check
            if (touchPoints.Count == 0)
                return false;

            var args = new TouchedEventArgs(touchPoints);

            TouchEntered?.Invoke(this, args);

            if (args.Handled)
                return true;

            // We have an interaction with the screen, so stop all animations
            Navigator.StopRunningAnimation();

            return true;
        }

        /// <summary>
        /// Called when touch exits map
        /// </summary>
        /// <param name="touchPoints">List of all touched points</param>
        /// <param name="releasedPoint">Released point, which was touched before</param>
        private bool OnTouchExited(List<Geometries.Point> touchPoints, Geometries.Point releasedPoint)
        {
            var args = new TouchedEventArgs(touchPoints);

            TouchExited?.Invoke(this, args);

            // Last touch released
            if (touchPoints.Count == 0)
            {
                _mode = TouchMode.None;
                _map.RefreshData(_viewport.Extent, _viewport.Resolution, ChangeType.Discrete);
            }

            return args.Handled;
        }

        /// <summary>
        /// Called, when mouse/finger/pen moves over map
        /// </summary>
        /// <param name="touchPoints">List of all touched points</param>
        private bool OnTouchMove(List<Geometries.Point> touchPoints)
        {
            var args = new TouchedEventArgs(touchPoints);

            TouchMove?.Invoke(this, args);

            if (args.Handled)
                return true;

            switch (_mode)
            {
                case TouchMode.Dragging:
                    {
                        if (touchPoints.Count != 1)
                            return false;

                        var touchPosition = touchPoints.First();

                        if (!Map.PanLock && _previousCenter != null && !_previousCenter.IsEmpty())
                        {
                            _viewport.Transform(touchPosition, _previousCenter);

                            RefreshGraphics();
                        }

                        _previousCenter = touchPosition;
                    }
                    break;
                case TouchMode.Zooming:
                    {
                        if (touchPoints.Count != 2)
                            return false;

                        var (prevCenter, prevRadius, prevAngle) = (_previousCenter, _previousRadius, _previousAngle);
                        var (center, radius, angle) = GetPinchValues(touchPoints);

                        double rotationDelta = 0;

                        if (!Map.RotationLock)
                        {
                            _innerRotation += angle - prevAngle;
                            _innerRotation %= 360;

                            if (_innerRotation > 180)
                                _innerRotation -= 360;
                            else if (_innerRotation < -180)
                                _innerRotation += 360;

                            if (Viewport.Rotation == 0 && Math.Abs(_innerRotation) >= Math.Abs(UnSnapRotationDegrees))
                                rotationDelta = _innerRotation;
                            else if (Viewport.Rotation != 0)
                            {
                                if (Math.Abs(_innerRotation) <= Math.Abs(ReSnapRotationDegrees))
                                    rotationDelta = -Viewport.Rotation;
                                else
                                    rotationDelta = _innerRotation - Viewport.Rotation;
                            }
                        }

                        _viewport.Transform(center, prevCenter, Map.ZoomLock ? 1 : radius / prevRadius, rotationDelta);

                        (_previousCenter, _previousRadius, _previousAngle) = (center, radius, angle);

                        RefreshGraphics();
                    }
                    break;
            }

            return true;
        }

        /// <summary>
        /// Called, when mouse/finger/pen tapped on map 2 or more times
        /// </summary>
        /// <param name="screenPosition">First clicked/touched position on screen</param>
        /// <param name="numOfTaps">Number of taps on map (2 is a double click/tap)</param>
        /// <returns>True, if the event is handled</returns>
        private bool OnDoubleTapped(Geometries.Point screenPosition, int numOfTaps)
        {
            var args = new TappedEventArgs(screenPosition, numOfTaps);

            DoubleTap?.Invoke(this, args);

            if (args.Handled)
                return true;

            var eventReturn = InvokeInfo(screenPosition, screenPosition, numOfTaps);

            if (eventReturn?.Handled == true)
                return true;

            // Double tap as zoom
            return OnZoomIn(screenPosition);
        }

        /// <summary>
        /// Called, when mouse/finger/pen tapped on map one time
        /// </summary>
        /// <param name="screenPosition">Clicked/touched position on screen</param>
        /// <returns>True, if the event is handled</returns>
        private bool OnSingleTapped(Geometries.Point screenPosition)
        {
            var args = new TappedEventArgs(screenPosition, 1);

            SingleTap?.Invoke(this, args);

            if (args.Handled)
                return true;

            var infoToInvoke = InvokeInfo(screenPosition, screenPosition, 1);

            if (infoToInvoke?.Handled == true)
                return true;

            OnInfo(infoToInvoke);
            return infoToInvoke?.Handled ?? false;
        }

        /// <summary>
        /// Called, when mouse/finger/pen tapped long on map
        /// </summary>
        /// <param name="screenPosition">Clicked/touched position on screen</param>
        /// <returns>True, if the event is handled</returns>
        private bool OnLongTapped(Geometries.Point screenPosition)
        {
            var args = new TappedEventArgs(screenPosition, 1);

            LongTap?.Invoke(this, args);

            return args.Handled;
        }

        private static (Geometries.Point centre, double radius, double angle) GetPinchValues(List<Geometries.Point> locations)
        {
            if (locations.Count < 2)
                throw new ArgumentException();

            double centerX = 0;
            double centerY = 0;

            foreach (var location in locations)
            {
                centerX += location.X;
                centerY += location.Y;
            }

            centerX = centerX / locations.Count;
            centerY = centerY / locations.Count;

            var radius = Algorithms.Distance(centerX, centerY, locations[0].X, locations[0].Y);

            var angle = Math.Atan2(locations[1].Y - locations[0].Y, locations[1].X - locations[0].X) * 180.0 / Math.PI;

            return (new Geometries.Point(centerX, centerY), radius, angle);
        }

        /// <summary>
        /// Public functions
        /// </summary>

        public void OpenBrowser(string url)
        {
            Device.OpenUri(new Uri(url));
        }

        private void RunOnUIThread(Action action)
        {
            Device.BeginInvokeOnMainThread(action);
        }

        public void Dispose()
        {
            Unsubscribe();
        }

        protected void Dispose(bool disposing)
        {
            Unsubscribe();
        }

        private float GetPixelDensity()
        {
            if (Width <= 0) return 0;
            return (float)(CanvasSize.Width / Width);
        }
    }
}