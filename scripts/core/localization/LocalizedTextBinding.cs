using Godot;

namespace GFrameworkGodotTemplate.scripts.core.localization;

/// <summary>
///     Describes one localized text assignment from a content table to a node property.
/// </summary>
[GlobalClass]
public partial class LocalizedTextBinding : Resource
{
    [Export]
    public string TargetPath { get; set; } = string.Empty;

    [Export]
    public string TargetProperty { get; set; } = "Text";

    [Export]
    public LocalizedTextCatalogSource Source { get; set; } = LocalizedTextCatalogSource.Menu;

    [Export]
    public string Key { get; set; } = string.Empty;
}
