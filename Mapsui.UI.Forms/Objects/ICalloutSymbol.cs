namespace Mapsui.UI.Forms
{
    public interface ICalloutSymbol: ISymbol
    {
        bool IsCalloutVisible();
        void HideCallout();

        void ShowCallout();

        Callout Callout { get; }
    }
}
