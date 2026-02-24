using System.Linq;
using Content.Shared.Flash;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Shared.Effects;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._DV.CosmicCult.Abilities;

public sealed class CosmicGlareSystem : EntitySystem
{
    [Dependency] private readonly SharedCosmicCultSystem _cult = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;

    private HashSet<Entity<MobStateComponent>> _mobs = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicGlare>(OnCosmicGlare);
    }

    private void OnCosmicGlare(Entity<CosmicCultComponent> ent, ref EventCosmicGlare args)
    {
        _audio.PlayPredicted(ent.Comp.GlareSFX, ent, ent);
        if (_net.IsServer) // Predicted spawn looks bad with animations
            PredictedSpawnAtPosition(ent.Comp.GlareVFX, Transform(ent).Coordinates);
        _cult.MalignEcho(ent);
        args.Handled = true;

        _mobs.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.CosmicGlareRange, _mobs);
        _mobs.RemoveWhere(target =>
        {
            if (_cult.EntityIsCultist(target)) return true;

            var evt = new CosmicAbilityAttemptEvent(target, true);
            RaiseLocalEvent(ref evt);
            return evt.Cancelled;
        });

        foreach (var target in _mobs)
            _flash.Flash(target, ent, args.Action, ent.Comp.CosmicGlareDuration, ent.Comp.CosmicGlarePenalty, stunDuration: (ent.Comp.CosmicGlareStun == TimeSpan.FromSeconds(0) ? null : ent.Comp.CosmicGlareStun));
    }
}
