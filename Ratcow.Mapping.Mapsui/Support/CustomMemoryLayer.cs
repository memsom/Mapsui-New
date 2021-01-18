using Ratcow.Mapping.Mapsui;
using Mapsui;
using Mapsui.Layers;
using Mapsui.UI.Forms;
using Mapsui.UI.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Ratcow.Mapping.Support
{
    public abstract class CustomMemoryLayer
    {
        public string Name { get; set; }
        public MemoryLayer Layer { get; protected set; }

        public int ZIndexRequest { get; set; } = 0;

        public abstract IList<ISymbol> RawData { get; }

        public abstract bool AddSymbol<Symbol>(Symbol item) where Symbol : ISymbol;

        /// <summary>
        /// returns number added (non zero is same as "true" I guess...)
        /// </summary>
        public abstract int AddSymbols<Symbol>(IEnumerable<Symbol> item) where Symbol : ISymbol;

        public abstract bool RemoveSymbol<Symbol>(Symbol marker) where Symbol : ISymbol;

        public abstract void ClearSymbols();

        public abstract void AttachLayer();
        public abstract bool DetatchLayer();
    }

    /// <summary>
    /// This is a more concrete version of the custom layer.
    /// </summary>
    public class CustomMemoryLayer<Symbol> : CustomMemoryLayer where Symbol : ISymbol
    {
        IMapView owner;

        readonly ObservableRangeCollection<Symbol> data = new ObservableRangeCollection<Symbol>();

        public CustomMemoryLayer(IMapView owner, string name)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Name = name ?? throw new ArgumentNullException(nameof(name));

            Layer = new MemoryLayer() { Name = Name, IsMapInfoLayer = true };

            data.CollectionChanged += OnCollectionChanged;

            Layer.DataSource = new ObservableCollectionProvider<Symbol>(data);
            Layer.Style = null;  // We don't want a global style for this layer

        }

        // override this if you want to do something extra, such as hide callouts etc.
        protected virtual void ItemRemoved(Symbol item)
        {
            // implement
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Cast<Symbol>().Any(s => s.Label == null))
                throw new ArgumentException("Marker must have a Label to be added to a map");

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    // Remove old pins from layer
                    if (item is Symbol symbol)
                    {
                        symbol.PropertyChanged -= HandlerLayerPropertyChanged;

                        ItemRemoved(symbol);
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is Symbol symbol)
                    {
                        // Add new pins to layer, so set MapView
                        symbol.MapView = owner;
                        symbol.PropertyChanged += HandlerLayerPropertyChanged;
                    }
                }
            }

            owner.Refresh();
        }

        void HandlerLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            owner.Map.RefreshData(
                owner.MapControl.Viewport.Extent,
                owner.MapControl.Viewport.Resolution, ChangeType.Continuous);

            // Repaint map, because something could have changed
            owner.MapControl.RefreshGraphics();
        }

        public IList<Symbol> Data => data;
        public override IList<ISymbol> RawData => Data.Cast<ISymbol>().ToList();

        public override void AttachLayer()
        {
            var zindex = -1;
            if (ZIndexRequest > 0 && ZIndexRequest < owner.MapControl.Map.Layers.Count)
            {
                zindex = ZIndexRequest;
            }

            if (zindex > -1)
            {
                owner.MapControl.Map.Layers.Insert(zindex, Layer);
            }
            else
            {
                owner.MapControl.Map.Layers.Add(Layer);
            }
        }

        public override bool DetatchLayer()
        {
            return owner.MapControl.Map.Layers.Remove(Layer);
        }

        public override bool AddSymbol<SymbolValue>(SymbolValue item)
        {
            var result = false;

            if (item is Symbol symbol && !data.Any(x => x.Equals(symbol)))
            {
                data.Add(symbol);

                result = true;
            }
            return result;
        }

        public override int AddSymbols<SymbolValue>(IEnumerable<SymbolValue> items)
        {
            var result = 0;
            foreach (var item in items)
            {
                if (AddSymbol(item))
                {
                    result++;
                }
            }

            return result;
        }

        public override bool RemoveSymbol<SymbolValue>(SymbolValue item)
        {
            var result = false;

            if (item is Symbol symbol && data.Any(x => x.Equals(symbol)))
            {
                data.Remove(symbol);
                result = true;
            }
            return result;
        }

        public override void ClearSymbols()
        {
            data?.Clear();
        }
    }
}
