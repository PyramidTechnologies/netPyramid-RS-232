[![NuGet](https://img.shields.io/nuget/v/PyramidNETRS232.svg)](https://www.nuget.org/packages/PyramidNETRS232/)

Pyramid C# .NET RS-232 API
=========================

Overview
--------

This library is for OEMs and software developers looking to quickly and easily integrate an RS-232 bill validator
into their system. Get up and running quickly without having to worry about the low-level bit twiddling.

PyramidNETRS232 is available on NuGet

    Install-Package PyramidNETRS232

## Important
If you are using an Apex 7000 or Spectra, please make sure that all of your dip switches are in the off position. The unit msust be in RS-232 mode to use this library. For more information about configuration, please see our [faq](http://pyramidacceptors.com/support/faq/).

## C# .NET Pyramid Device API

* Windows .NET 3.5 Runtime
* Supports Escrow Mode
* Highly Configurable
* Very lightweight library (small dll and low memory consumption)

### Dependencies

* [log4net](https://www.nuget.org/packages/log4net/2.0.5)
    
### Troubleshooting

Mono runtime is untested and folks on Unity will have trouble. We've experimented with a variety of mods on the Unity experiment branch but we just can't seem to make it work. If you have any ideas feel free to reach out to us!

### License ###

[This library is available under the MIT license](https://opensource.org/licenses/MIT)

### Contribution guidelines ###

We warmly welcome pull requests, feature requests, and any other feedback you are willing to offer.
