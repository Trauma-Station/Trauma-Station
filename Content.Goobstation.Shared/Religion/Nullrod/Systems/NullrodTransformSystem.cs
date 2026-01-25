// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Religion.Nullrod;

public sealed class NullrodTransformSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public static readonly ProtoId<TagPrototype> NullrodTag = "Nullrod";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AltarSourceComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, AltarSourceComponent component, InteractUsingEvent args)
    {
        // Checks used entity for the tag we need.
        if (args.Handled || !_tag.HasTag(args.Used, NullrodTag))
            return;

        // *flaaavor*
        PredictedSpawnAtPosition(component.EffectProto, Transform(uid).Coordinates);
        _audio.PlayPredicted(component.SoundPath, uid, args.User, AudioParams.Default.WithVolume(-4f));

        // Spawn proto associated with the altar.
        PredictedSpawnAtPosition(component.RodProto, args.ClickLocation.SnapToGrid(EntityManager));

        // Remove the nullrod
        PredictedQueueDel(args.Used);
        args.Handled = true;
    }
}
