using Avalonia.Controls;
using System;
using UnpackerGui.Enums;

namespace UnpackerGui.Views;

public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        colorThemeComboBox.ItemsSource = Enum.GetValues<ColorTheme>();
    }
}
