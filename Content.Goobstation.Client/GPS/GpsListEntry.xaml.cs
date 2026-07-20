// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Client.GPS;

[GenerateTypedNameReferences]
public sealed partial class GpsListEntry : Button
{
    public NetEntity TrackedEntity;

    public GpsListEntry(string text, NetEntity trackedEntity, IRsiStateLike? icon, Color? iconColor)
    {
        RobustXamlLoader.Load(this);

        const int maxLength = 10;

        ToolTip = text;
        NameableLabel.Text = text.Length > maxLength ? text.Substring(0, maxLength) : text;
        EntryIcon.Texture = icon?.Default;
        TrackedEntity = trackedEntity;

        if (iconColor != null)
            EntryIcon.Modulate = iconColor.Value;
    }
}
