namespace Ratcow.Mapping.Interfaces
{

    public interface IMarkerObject
    {
        object RawPin { get; }

        bool HasPosition { get; }
    }

    public interface IMarkerObject<T>: IMarkerObject
    {
        T Marker { get; set; }
    }
}