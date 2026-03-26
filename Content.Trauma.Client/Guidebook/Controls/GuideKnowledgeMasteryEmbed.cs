// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Client.Guidebook.Richtext;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Guidebook.Controls;

/// <summary>
/// Gets a list of knowledge masteries and puts them in a table.
/// </summary>
public sealed partial class GuideKnowledgeMasteryEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public GuideKnowledgeMasteryEmbed()
    {
        IoCManager.InjectDependencies(this);

        Orientation = LayoutOrientation.Vertical;

        var knowledge = _entMan.System<CommonKnowledgeSystem>();

        var table = new GridContainer
        {
            Columns = 2,
            HorizontalExpand = true,
            VerticalExpand = true
        };

        table.AddChild(new Label { Text = "Level", FontColorOverride = Color.Gray });
        table.AddChild(new Label { Text = "Mastery", FontColorOverride = Color.Gray });

        for (int i = 0; i < 6; i++)
        {
            var levelLabel = new RichTextLabel
            {
                Text = knowledge.GetInverseMastery(i).ToString()
            };

            var masteryLabel = new RichTextLabel
            {
                Text = knowledge.GetMasteryString(i)
            };

            table.AddChild(levelLabel);
            table.AddChild(masteryLabel);
        }
        AddChild(table);
    }

    bool IDocumentTag.TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = this;
        return true;
    }
}
