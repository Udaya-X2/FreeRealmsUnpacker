using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UnpackerGui.ViewModels;

/// <summary>
/// Represents a filter based on the image extension of the specified type.
/// </summary>
/// <typeparam name="T">The type of the item to filter.</typeparam>
public class ImageOptionsViewModel<T> : FilterViewModel<T>
{
    private static readonly FrozenSet<string> s_imageFormats = ((HashSet<string>)
    [
        "3FR",
        "AFPHOTO",
        "AI",
        "APNG",
        "ARI",
        "ARW",
        "ASTC",
        "AVIF",
        "BAY",
        "BMP",
        "BPG",
        "BRAW",
        "CAP",
        "CD5",
        "CDR",
        "CLIP",
        "CPT",
        "CR2",
        "CR3",
        "CRI",
        "CRW",
        "DCR",
        "DCS",
        "DDS",
        "DIB",
        "DNG",
        "DRF",
        "DXF",
        "EIP",
        "EMF",
        "EPS",
        "ERF",
        "FFF",
        "FITS",
        "FLIF",
        "GIF",
        "GPR",
        "HDR",
        "HEIC",
        "HEIF",
        "ICO",
        "IIQ",
        "IND",
        "INDD",
        "INDT",
        "J2K",
        "JFI",
        "JFIF",
        "JIF",
        "JP2",
        "JPE",
        "JPEG",
        "JPF",
        "JPG",
        "JPM",
        "JPX",
        "JXL",
        "K25",
        "KDC",
        "KRA",
        "KTX",
        "MDC",
        "MDP",
        "MEF",
        "MJ2",
        "MOS",
        "MRW",
        "NEF",
        "NRW",
        "ORF",
        "PDF",
        "PDN",
        "PEF",
        "PKM",
        "PLD",
        "PNG",
        "PSD",
        "PSP",
        "PTX",
        "PXN",
        "R3D",
        "RAF",
        "RAW",
        "RW2",
        "RWL",
        "RWZ",
        "SAI",
        "SR2",
        "SRF",
        "SRW",
        "SVG",
        "SVGZ",
        "TCO",
        "TGA",
        "TIF",
        "TIFF",
        "WBMP",
        "WEBP",
        "WMF",
        "X3F",
        "XCF"
    ]).ToFrozenSet();

    private readonly Func<T, string> _converter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageOptionsViewModel{T}"/> class.
    /// </summary>
    /// <param name="converter">The converter from <typeparamref name="T"/> to an image extension.</param>
    /// <exception cref="ArgumentNullException"/>
    public ImageOptionsViewModel(Func<T, string> converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        IsMatch = x => s_imageFormats.Contains(_converter(x));
    }

    /// <summary>
    /// Returns <see langword="true"/> if the search options always produce a match; otherwise, <see langword="false"/>.
    /// </summary>
    public override bool IsAlwaysMatch => false;

    /// <summary>
    /// Updates the match predicate according to the current search options.
    /// </summary>
    /// <exception cref="ValidationException">If the regular expression is invalid.</exception>
    protected override void UpdateMatchPredicate() { }
}
