using AssetIO;
using System;
using System.Collections.Generic;

namespace UnpackerGui.Models;

/// <summary>
/// Provides properties and a delegate method for the extraction of a Free Realms asset file.
/// </summary>
/// <param name="Info">The name of the asset file.</param>
/// <param name="OpenRead">Creates an <see cref="AssetReader"/> that reads from the asset file.</param>
/// <param name="Assets">The assets in the asset file.</param>
public record ExtractionAssetFile(string Name, Func<AssetReader> OpenRead, IEnumerable<AssetInfo> Assets);
