using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Rituals;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Shitcode.Heretic.UI;

[UsedImplicitly]
public sealed class EldritchIdBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out EldritchIdCardComponent? id))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var buttonModels = ConvertToButtons(id.Configs.ToList());
        _menu.SetButtons(buttonModels);

        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption<EldritchIdConfiguration>> ConvertToButtons(
        IReadOnlyList<EldritchIdConfiguration> configs)
    {
        var models = new RadialMenuActionOption<EldritchIdConfiguration>[configs.Count];
        for (var i = 0; i < configs.Count; i++)
        {
            var config = configs[i];
            var proto = _proto.Index(config.CardPrototype);

            var jobSuffix = string.IsNullOrWhiteSpace(config.JobTitle) ? string.Empty : $" ({config.JobTitle})";

            var val = string.IsNullOrWhiteSpace(config.FullName)
                ? Loc.GetString("access-id-card-component-owner-name-job-title-text",
                    ("jobSuffix", jobSuffix))
                : Loc.GetString("access-id-card-component-owner-full-name-job-title-text",
                    ("fullName", config.FullName),
                    ("jobSuffix", jobSuffix));

            models[i] = new RadialMenuActionOption<EldritchIdConfiguration>(HandleRadialMenuClick, config)
            {
                IconSpecifier = new RadialMenuEntityPrototypeIconSpecifier(proto),
                ToolTip = val,
            };
        }

        return models;
    }

    private void HandleRadialMenuClick(EldritchIdConfiguration config)
    {
        SendPredictedMessage(new EldritchIdMessage(config));
    }
}
