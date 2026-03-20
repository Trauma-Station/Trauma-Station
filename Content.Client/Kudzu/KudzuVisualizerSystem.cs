// SPDX-FileCopyrightText: 2022 Jessica M <jessica@jessicamaybe.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Spreader;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Content.Shared.DrawDepth;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Client.Kudzu;

public sealed class KudzuVisualsSystem : VisualizerSystem<KudzuVisualsComponent>
{
    private static readonly ProtoId<SpeciesPrototype> DionaSpecies = "Diona";

    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(_ => RefreshAllKudzuDrawDepths());
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(_ => RefreshAllKudzuDrawDepths());
    }

    protected override void OnAppearanceChange(EntityUid uid, KudzuVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<int>(uid, KudzuVisuals.Variant, out var var, args.Component)
            && AppearanceSystem.TryGetData<int>(uid, KudzuVisuals.GrowthLevel, out var level, args.Component))
        {
            var index = SpriteSystem.LayerMapReserve((uid, args.Sprite), $"{component.Layer}");
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), index, $"kudzu_{level}{var}");
        }

        UpdateKudzuDrawDepth(uid, args.Sprite);
    }

    private void RefreshAllKudzuDrawDepths()
    {
        var query = EntityQueryEnumerator<KudzuVisualsComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            UpdateKudzuDrawDepth(uid, sprite);
        }
    }

    private void UpdateKudzuDrawDepth(EntityUid uid, SpriteComponent sprite)
    {
        var drawDepth = LocalPlayerIsDiona()
            ? (int) Content.Shared.DrawDepth.DrawDepth.HighFloorObjects
            : (int) Content.Shared.DrawDepth.DrawDepth.Overdoors;

        SpriteSystem.SetDrawDepth((uid, sprite), drawDepth);
    }

    private bool LocalPlayerIsDiona()
    {
        var attached = _player.LocalSession?.AttachedEntity;
        if (attached == null)
            return false;

        return TryComp(attached.Value, out HumanoidProfileComponent? profile) &&
               profile.Species == DionaSpecies;
    }
}
