using AssetIO;
using System.Collections.Generic;

namespace UnpackerGui.Models;

/// <summary>
/// Represents an asset file and the assets selected from it.
/// </summary>
/// <param name="File">The asset file.</param>
/// <param name="SelectedAssets">The assets selected from the asset file.</param>
public record AssetFileSelection(AssetFile File, IEnumerable<AssetInfo> SelectedAssets);
