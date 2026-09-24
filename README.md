<p align="center"><img width="25%" src="https://raw.githubusercontent.com/UMapx/UMapx.Imaging/main/docs/umapxnet_big.png" /></p>
<p align="center">UMapx sub-library for image processing, filtering and analysis on Windows</p>

# Installation

Install **UMapx.Imaging** using [NuGet](https://www.nuget.org/packages/UMapx.Imaging/):

```shell
dotnet add package UMapx.Imaging
```

NuGet restores the **UMapx** and **System.Drawing.Common** dependencies
automatically. The public API is in the `UMapx.Imaging` namespace.

# Quick start

Load an image, apply a blur and save the result:

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using UMapx.Imaging;

using var image = new Bitmap("input.jpg");
new GaussianBlur(16, 16).Apply(image);
image.Save("output.png", ImageFormat.Png);
```

Replace `input.jpg` with the path to your image. This example uses C# 9 or later. 
The filter modifies that bitmap in place, and the `using` declarations dispose both images
at the end of the scope.

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

Filters use shared types from `UMapx.Core`, including `SizeInt`, `RangeFloat`
and `InterpolationMode`. Add `using UMapx.Core;` when working with these types.

# Platform support

The library targets **.NET Standard 2.0** and builds as **AnyCPU**. Bitmap
processing requires Windows because it uses `System.Drawing.Common`.
The bitmap APIs are not supported on Linux or macOS.

Regression tests cover Windows with .NET 8 in an x64 process. Building and
running the tests requires the .NET 8 SDK, or a newer SDK with the .NET 8
runtime installed.

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
conversion retains the **0-255** range. Depth maps use `ushort[height, width]`.

# Build and test

Run from the repository root on Windows:

```shell
dotnet build UMapx.Imaging.sln -c Release
dotnet test tests/UMapx.Imaging.Tests.csproj -c Release --no-build --no-restore
```

The solution contains the library and its tests. Dependencies are restored
from NuGet during the build; no separate UMapx checkout is required.

Tests cover pixel operations, filter composition, color and tensor conversions,
depth processing, geometry and bitmap resource handling. They are also
discoverable in Visual Studio.

The library and XML API documentation are written to
`sources/bin/Release/netstandard2.0/`. The build also creates a
`UMapx.Imaging.*.nupkg` package in `sources/bin/Release/`.

# License

MIT
