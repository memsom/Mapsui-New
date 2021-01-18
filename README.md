# Mapsui "New"

This repo is the base source code I have used in my recent projects using Mapsui as the mapping. The code here is intended for my own use, and use of my associates. Please feel free to look at it, but I am not currently accepting pull requests.

This code is mainly intended to be used for Xamarin Forms. There is a WPF and Android sample project. The iOS build was removed as I have not used it yet, and will be added back in when I get a chance.

Please see [The official Mapsui repo](https://github.com/Mapsui/Mapsui) for the a more complete, but incompatible version.

_This code is online to comply with the LGPL 2.1 license._

# Extensions over the source repo
We have simplified the `MapView` contained in the original repo. We have exposed a lot more of the internals to inheritance. We have improved the `SvgButton`. The `MapViewEx` is on a par with the original `MapView`. The `CustomMapView` pulls together the extra details we needed for our project. We have added in a new abstraction `Marker` as a better base for creating new Map objects. We have added in the ability to create arbitrary `CustomLayer`s, and a mechanism to allow these to be placed in the right logical Z-order.