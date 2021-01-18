namespace Mapsui.UI.Forms
{
    class TouchEvent
    {
        public long Id { get; }
        public Geometries.Point Location { get; }
        public long Tick { get; }

        public TouchEvent(long id, Geometries.Point screenPosition, long tick)
        {
            Id = id;
            Location = screenPosition;
            Tick = tick;
        }
    }
}