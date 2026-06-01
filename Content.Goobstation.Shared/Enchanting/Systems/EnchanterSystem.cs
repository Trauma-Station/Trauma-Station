// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Enchanting.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Enchanting.Systems;

/// <summary>
/// Handles using enchanters on altars to enchant items.
/// </summary>
public sealed partial class EnchanterSystem : EntitySystem
{
    [Dependency] private EnchantingSystem _enchanting = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private CommonKnowledgeSystem _knowledge = default!;

    private List<EntProtoId<EnchantComponent>> _pool = new();

    private static readonly EntProtoId MagicalLiteracy = "MagicalLiteracyKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnchanterComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<EnchantingToolComponent, ExaminedEvent>(OnToolExamined);
        SubscribeLocalEvent<EnchantingToolComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
    }

    private void OnExamined(Entity<EnchanterComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("enchanter-examine"));
    }

    private void OnToolExamined(Entity<EnchantingToolComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("enchanting-tool-examine"));
    }

    private void OnBeforeInteract(Entity<EnchantingToolComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (!args.CanReach || args.Target is not {} item)
            return;

        // do nothing if used without an altar
        if (_enchanting.FindTable(item) == null)
            return;

        args.Handled = true;

        // need an enchanter on the altar as well as the target
        var user = args.User;
        if (_enchanting.FindEnchanter(item) is not {} enchanter)
        {
            _popup.PopupClient(Loc.GetString("enchanting-tool-no-enchanter"), user, user);
            return;
        }

        TryEnchant(enchanter, item, user);
    }

    private void GetPossibleEnchants(Entity<EnchanterComponent> ent, EntityUid item)
    {
        _pool.Clear();
        foreach (var id in ent.Comp.Enchants)
        {
            if (_enchanting.CanEnchant(item, id))
                _pool.Add(id);
        }
    }

    /// <summary>
    /// Try to use an enchanter to add random enchant(s) to an item, deleting it if successful.
    /// </summary>
    public bool TryEnchant(Entity<EnchanterComponent> ent, Entity<EnchantedComponent?> item, EntityUid user)
    {
        GetPossibleEnchants(ent, item);
        if (_pool.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("enchanter-cant-enchant"), item, user);
            return false;
        }

        if (_knowledge.GetKnowledge(user, MagicalLiteracy) is not { } skill || _knowledge.GetMastery(skill.Comp) < 1)
        {
            _popup.PopupClient(Loc.GetString("enchanter-no-skill"), item, user);
            return false;
        }

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent), GetNetEntity(user));
        var picking = random.NextFloat(ent.Comp.MinCount, ent.Comp.MaxCount);
        var total = 0f;
        for (int i = 0; i < 20 && total < picking; i++)
        {
            var id = random.Pick(_pool);
            // TODO: Integrate with skills 2
            var level = (int) random.NextFloat(ent.Comp.MinLevel, _knowledge.GetMastery(skill.Comp) + ent.Comp.AdjustLevel);
            if (_enchanting.Enchant(item, id, level))
                total += 1f;
        }

        _audio.PlayPredicted(ent.Comp.Sound, item, user);
        _popup.PopupPredicted(Loc.GetString("enchanter-enchanted", ("item", item)), item, user, PopupType.Large);

        _adminLogger.Add(LogType.EntityDelete, LogImpact.Low, $"{ToPrettyString(user):player} enchanted {ToPrettyString(item):item} using {ToPrettyString(ent):enchanter}");

        if (!TryComp<StackComponent>(ent, out var stack) || !_stack.TryUse((ent, stack), 1))
        {
            ent.Comp.Enchants = new(); // prevent double enchanting by malf client
            PredictedQueueDel(ent);
        }
        return true;
    }
}
