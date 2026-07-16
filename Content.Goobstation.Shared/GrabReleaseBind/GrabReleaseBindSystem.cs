// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Trauma.Common.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Goobstation.Shared.GrabReleaseBind;

/// <summary>
/// This handle binding the resist grab key
/// </summary>
public sealed partial class GrabReleaseBindSystem : EntitySystem
{
    [Dependency] private PullingSystem _pulling = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(TraumaKeyFunctions.ResistGrab,
                InputCmdHandler.FromDelegate(HandleResistGrab, handle: false, outsidePrediction: false))
            .Register<GrabReleaseBindSystem>();
    }

    private void HandleResistGrab(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } uid || !TryComp<PullableComponent>(uid, out var pullable))
            return;

        _pulling.TryStopPull(uid, pullable, user: uid);
    }
}
