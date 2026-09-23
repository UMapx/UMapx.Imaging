<p align="center"><img width="25%" src="https://raw.githubusercontent.com/UMapx/UMapx/master/docs/umapxnet_big.png" /></p>
<p align="center">UMapx sub-library for image processing, filtering and analysis</p>

# Installation

The current source tree uses a local [UMapx](https://github.com/UMapx/UMapx)
project for its mathematical operations and color spaces. Place both repositories
side by side as described in [Build and test](#build-and-test).

Add a reference from your application to `sources/UMapx.Imaging.csproj`.
For example, from an application directory beside `UMapx.Imaging`:

```shell
dotnet add MyApp.csproj reference ../UMapx.Imaging/sources/UMapx.Imaging.csproj
```

Replace `MyApp.csproj` with your project name. The UMapx dependency is included
through the project reference.

# Quick start

Load an image, convert it to 32-bit ARGB, apply a blur and save the result:

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using UMapx.Imaging;

using var source = new Bitmap("input.jpg");
using var image = source.To32bpp();

new GaussianBlur(3, 3).Apply(image);
image.Save("output.png", ImageFormat.Png);
```

Replace `input.jpg` with the path to your image. The snippet uses C# 9 or later.
`To32bpp()` creates a separate bitmap; filtering changes `image` in place and
leaves `source` unchanged. Dispose both bitmaps when finished.

# Image processing

| Area | Examples |
| --- | --- |
| Color and tone correction | `BrightnessCorrection`, `ContrastCorrection`, `GammaCorrection`, `LevelsCorrection`, `SaturationCorrection` |
| Filtering and noise reduction | `Convolution`, `BoxBlur`, `GaussianBlur`, `Median`, `Wiener` |
| Morphology and edge detection | `Erosion`, `Dilatation`, `Opening`, `Closing`, `CannyEdgeDetector`, `FreiChen` |
| Local contrast and exposure | `CLAHE`, `LocalHistogramEqualization`, `SingleScaleRetinex`, `ShadowsHighlightsCorrection`, `ExposureFusion` |
| Geometry and composition | `Resize`, `Rotate`, `Crop`, `PerspectiveWarp`, `Merge`, `Chromakey` |
| Motion and stereo | `MotionDetector`, `MotionEventDetector`, `StereoDisparity`, `StereoAnaglyph` |
| Matrices, tensors and depth maps | `BitmapMatrix`, `TensorMatrix`, `TensorTransform`, `DepthMatrix`, `DepthTransform` |

The public API is in the `UMapx.Imaging` namespace. Filters also use shared types
from `UMapx.Core`, such as `SizeInt`, `RangeFloat` and `InterpolationMode`.

# Platform support

The library targets **.NET Standard 2.0** and builds as **AnyCPU**. Its bitmap
APIs use `System.Drawing.Common` and require Windows; targeting .NET Standard
does not make bitmap processing portable to Linux or macOS.

The regression suite has been run on Windows with .NET 8 in an x64 process.
Building and running the tests requires the .NET 8 SDK, or a newer SDK with
the .NET 8 runtime installed.

# Working with images

`IBitmapFilter.Apply(bitmap)` modifies the supplied bitmap in place.
`IBitmapFilter2.Apply(destination, source)` writes into the first bitmap and
reads from the second. Use separate bitmap instances for these arguments.
Most two-image filters require matching dimensions; geometric filters such as
`Resize` and `Crop` use a destination sized for their output.

Filters operating on pixel buffers expect `PixelFormat.Format32bppArgb`.
Use `To32bpp()` to prepare an input image. The `Bitmap` overloads manage pixel
buffer locking internally. When calling a `BitmapData` overload, the caller
owns the lock and must release it, including when processing throws.

Matrix conversions use `[height, width]` arrays. `ToRGB()` returns normalized
`float[,]` planes in **B, G, R** order; `ToRGB(alpha: true)` appends an alpha
plane. `FromRGB()` expects the same order.

Tensor conversions use three flattened channel arrays. `ToByteTensor()` and
`ToFloatTensor()` default to **B, G, R** order; pass `rgb: true` for **R, G, B**.
Each channel uses row-major indexing (`y * width + x`), and bitmap-to-float
conversion retains the **0–255** range. Depth maps use `ushort[height, width]`.

# Build and test

Keep the repositories in this layout:

```text
UMapx/
  sources/UMapx.csproj
UMapx.Imaging/
  sources/UMapx.Imaging.csproj
  tests/UMapx.Imaging.Tests.csproj
  UMapx.Imaging.sln
```

Use the split UMapx sources, version 8.0.0.3, which no longer contain the
`UMapx.Imaging` types. Earlier monolithic UMapx packages define those types
themselves and conflict with this separate library.

Run from the `UMapx.Imaging` repository root on Windows:

```shell
dotnet build UMapx.Imaging.sln -c Release
dotnet test tests/UMapx.Imaging.Tests.csproj -c Release --no-build --no-restore
```

The tests cover pixel operations, filter composition, color and tensor
conversions, depth processing, geometry and bitmap resource handling.
They are also discoverable in Visual Studio.

The library and XML API documentation are written to
`sources/bin/Release/netstandard2.0/`. The build also creates a
`UMapx.Imaging.*.nupkg` package in `sources/bin/Release/`. To install it from a
local NuGet feed, include the matching `UMapx.*.nupkg` built in
`../UMapx/sources/bin/Release/` in that feed.

# License

MIT
