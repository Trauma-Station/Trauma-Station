// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Transform;
using Content.Shared.Popups;

namespace Content.Trauma.Shared.EntityEffects;

// TODO: kill...
/// <summary>
/// Effect that shows a single predicted popup.
/// </summary>
public sealed partial class PopupPredicted : EntityEffectBase<PopupPredicted>
{
    /// <summary>
    /// The popup to show.
    /// </summary>
    [DataField(required: true)]
    public string Message = string.Empty;

    /// <summary>
    /// Whether to just the entity we're affecting, or everyone around them.
    /// </summary>
    [DataField]
    public PopupRecipients Type = PopupRecipients.Local;

    /// <summary>
    /// Which popup API method to use.
    /// Use PopupCoordinates in case the entity will be deleted while the popup is shown.
    /// </summary>
    [DataField]
    public PopupMethod Method = PopupMethod.PopupEntity;

    /// <summary>
    /// Size of the popup.
    /// </summary>
    [DataField]
    public PopupType VisualType = PopupType.Small;
}

public sealed partial class PopupPredictedEffectSystem : EntityEffectSystem<TransformComponent, PopupPredicted>
{
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<PopupPredicted> args)
    {
        var effect = args.Effect;

        var msg = effect.Message;
        var method = effect.Method;
        var type = effect.Type;

        switch ((method, type))
        {
            case (PopupMethod.PopupEntity, PopupRecipients.Local):
                _popup.PopupEntity(msg, ent, ent, args.Effect.VisualType);
                break;
            case (PopupMethod.PopupEntity, PopupRecipients.Pvs):
                _popup.PopupEntity(msg, ent, args.Effect.VisualType);
                break;
            case (PopupMethod.PopupCoordinates, PopupRecipients.Local):
                _popup.PopupCoordinates(msg, Transform(ent).Coordinates, ent, args.Effect.VisualType);
                break;
            case (PopupMethod.PopupCoordinates, PopupRecipients.Pvs):
                _popup.PopupCoordinates(msg, Transform(ent).Coordinates, args.Effect.VisualType);
                break;
        }
    }
}
