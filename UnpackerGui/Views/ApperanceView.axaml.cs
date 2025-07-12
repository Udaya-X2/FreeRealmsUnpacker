using Avalonia.Controls;
using FluentIcons.Common;
using System;
using UnpackerGui.Enums;

namespace UnpackerGui.Views;

public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        colorThemeComboBox.ItemsSource = Enum.GetValues<ColorTheme>();
        iconTypeComboBox.ItemsSource = new IconVariant[] { IconVariant.Regular, IconVariant.Filled };
    }
}
