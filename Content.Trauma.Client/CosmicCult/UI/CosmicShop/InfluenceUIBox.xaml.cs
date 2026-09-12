// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;
using Content.Trauma.Shared.CosmicCult.Prototypes;

namespace Content.Trauma.Client.CosmicCult.UI.CosmicShop;

[GenerateTypedNameReferences]
public sealed partial class InfluenceUIBox : BoxContainer
{
    public Action? OnGainButtonPressed;

    private SpriteSystem _sprite;
    private InfluenceUIBoxState? _state;
    public InfluencePrototype? Proto;

    public InfluenceUIBox(SpriteSystem sprite)
    {
        RobustXamlLoader.Load(this);

        _sprite = sprite;

        GainButton.OnPressed += _ => OnGainButtonPressed?.Invoke();
    }

    public void SetProto(InfluencePrototype proto)
    {
        Proto = proto;

        InfluenceIcon.Texture = _sprite.Frame0(proto.Icon);
        NameLabel.SetMessage(Loc.GetString(proto.Name), Color.FromHex("#4CA7AD"));

        var type = proto.Passive ? "passive" : "active";
        Type.Text = Loc.GetString($"influence-type-{type}");
        Cost.Text = proto.Cost.ToString();
        Description.SetMessage(Loc.GetString(proto.Description));
        if (proto.EmpoweredDescription is { } desc)
        {
            UpgradeDescription.SetMessage(Loc.GetString(desc), Color.FromHex("#4CA7AD"));
            UpgradeDescription.Visible = true;
        }
        else
        {
            UpgradeDescription.Visible = false;
        }

        _state = null;
    }

    public void Update(CosmicCultComponent comp)
    {
        if (Proto is not { } proto)
            return;

        var state = GetState(proto, comp);
        if (state == _state)
            return;

        _state = state;
        switch (state)
        {
            case InfluenceUIBoxState.Owned:
                Status.Text = Loc.GetString("cosmic-shop-interface-influences-owned");

                GainButton.Disabled = true;
                GainButton.Modulate = Color.Green;
                GainButton.Text = Loc.GetString("cosmic-shop-interface-influences-purchased");
                GainButton.ToolTip = Loc.GetString("cosmic-shop-interface-influences-owned-tooltip");

                break;

            case InfluenceUIBoxState.UnlockedAndEnoughEntropy:
                Status.Text = Loc.GetString("cosmic-shop-interface-influences-unlocked");

                GainButton.Disabled = false;
                GainButton.Modulate = Color.White;
                GainButton.Text = Loc.GetString("cosmic-shop-interface-influences-button-gain");
                GainButton.ToolTip = null;

                break;

            case InfluenceUIBoxState.UnlockedAndNotEnoughEntropy:
                Status.Text = Loc.GetString("cosmic-shop-interface-influences-unlocked");

                GainButton.Disabled = false;
                GainButton.Modulate = Color.Gray;
                GainButton.Text = Loc.GetString("cosmic-shop-interface-influences-locked");
                GainButton.ToolTip = Loc.GetString("cosmic-shop-interface-influences-unlocked-not-enough-entropy-tooltip", ("entropy", proto.Cost));
                break;

            case InfluenceUIBoxState.Locked:
                // TODO: check for dependencies. If some are missing, replace the text accordingly.
                Status.Text = Loc.GetString("cosmic-shop-interface-influences-locked");

                GainButton.Disabled = true;
                GainButton.Modulate = Color.Gray;
                GainButton.Text = Loc.GetString("cosmic-shop-interface-influences-locked");
                GainButton.ToolTip = Loc.GetString("cosmic-shop-interface-influences-locked-tooltip");

                break;
        }
    }

    public static InfluenceUIBoxState GetState(InfluencePrototype proto, CosmicCultComponent comp)
    {
        var unlocked = comp.UnlockedInfluences.Contains(proto.ID);
        var owned = comp.OwnedInfluences.Contains(proto.ID);

        // more verbose than it needs to be, but it reads nicer
        if (owned)
            return InfluenceUIBoxState.Owned;

        // TODO: dependency check when skill trees are real

        // if it's unlocked, do we have enough entropy to buy it?
        if (unlocked)
            return proto.Cost > comp.EntropyBudget
                ? InfluenceUIBoxState.UnlockedAndNotEnoughEntropy
                : InfluenceUIBoxState.UnlockedAndEnoughEntropy;

        return InfluenceUIBoxState.Locked;
    }
}

public enum InfluenceUIBoxState : byte
{
    UnlockedAndEnoughEntropy = 0,
    UnlockedAndNotEnoughEntropy = 1,
    Owned = 2,
    Locked = 3,
}
