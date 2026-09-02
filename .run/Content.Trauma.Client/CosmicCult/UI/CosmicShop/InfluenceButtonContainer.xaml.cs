// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;
using Content.Trauma.Shared.CosmicCult.Prototypes;

namespace Content.Trauma.Client.CosmicCult.UI.CosmicShop;

[GenerateTypedNameReferences]
public sealed partial class InfluenceButtonContainer : BoxContainer
{
    public Action? OnDetailButtonPressed;

    public readonly InfluencePrototype Proto;

    public InfluenceButtonContainer(SpriteSystem sprite, InfluencePrototype proto)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        Proto = proto;

        DetailButton.TextureNormal = sprite.Frame0(proto.Icon);
        DetailButton.ToolTip = Loc.GetString(proto.Name);
        DetailButton.OnPressed += _ => OnDetailButtonPressed?.Invoke();
    }

    public void Update(CosmicCultComponent comp)
    {
        var state = InfluenceUIBox.GetState(Proto, comp);
        DetailButton.Modulate = state switch
        {
            InfluenceUIBoxState.Owned => Color.Green,
            InfluenceUIBoxState.UnlockedAndEnoughEntropy => Color.White,
            _ => Color.Gray
        };
    }
}
