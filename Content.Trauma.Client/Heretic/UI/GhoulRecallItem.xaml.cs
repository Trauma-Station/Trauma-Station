// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Client.Heretic.UI;

[GenerateTypedNameReferences]
public sealed partial class GhoulRecallItem : BoxContainer
{
    public GhoulRecallItem(string name, float? distance)
    {
        RobustXamlLoader.Load(this);

        NameLabel.Text = name;

        var distStr = distance == null
            ? Loc.GetString("heretic-flesh-grasp-recall-distance-far")
            : ((int) MathF.Round(distance.Value)).ToString();

        DistanceLabel.Text = Loc.GetString("heretic-flesh-grasp-recall-distance", ("dist", distStr));
    }
}
