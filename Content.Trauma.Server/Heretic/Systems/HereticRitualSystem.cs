// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Content.Server.Chat.Managers;
using Content.Server.Polymorph.Systems;
using Content.Server.Revolutionary.Components;
using Content.Shared.Chat;
using Content.Shared.Mind;
using Content.Trauma.Server.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Rituals;
using Robust.Server.Player;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed partial class HereticRitualSystem : SharedHereticRitualSystem
{
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _rand = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IChatManager _chat = default!;

    [Dependency] private EntityQuery<CommandStaffComponent> _commandQuery = default!;
    [Dependency] private EntityQuery<SecurityStaffComponent> _secQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualComponent, HereticRitualEffectEvent<PolymorphRitualEffect>>(OnPolymorph);

        SubscribeLocalEvent<HereticKnowledgeRitualComponent, MapInitEvent>(OnKnowledgeInit);
        SubscribeLocalEvent<HereticKnowledgeRitualComponent, HereticRitualOwnerSetEvent>(OnSetOwner);
    }

    private void OnPolymorph(Entity<HereticRitualComponent> ent,
        ref HereticRitualEffectEvent<PolymorphRitualEffect> args)
    {
        if (args.Effect.ApplyOn == string.Empty)
            return;

        HashSet<EntityUid> result = new();
        foreach (var uid in args.Ritual.Comp.Raiser.GetTargets<EntityUid>(args.Effect.ApplyOn))
        {
            if (_polymorph.PolymorphEntity(uid, args.Effect.Polymorph) is { } newUid)
                result.Add(newUid);
        }

        if (result.Count > 0)
            args.Ritual.Comp.Blackboard[args.Effect.Result] = result;
    }

    protected override (bool isCommand, bool isSec) IsCommandOrSec(EntityUid uid)
    {
        return (_commandQuery.HasComp(uid), _secQuery.HasComp(uid));
    }

    private void OnKnowledgeInit(Entity<HereticKnowledgeRitualComponent> ent, ref MapInitEvent args)
    {
        SelectKnowledgeIngredients(ent);
    }

    private bool SelectKnowledgeIngredients(Entity<HereticKnowledgeRitualComponent> ent)
    {
        if (ent.Comp.Ingredients.Count > 0)
            return true;

        foreach (var (id, amount) in ent.Comp.Datasets)
        {
            var dataset = _proto.Index(id);
            for (var i = 0; i < amount; i++)
            {
                ent.Comp.Ingredients.Add(_rand.Pick(dataset.Ingredients));
            }

            Dirty(ent);
        }

        return ent.Comp.Ingredients.Count > 0;
    }

    private void OnSetOwner(Entity<HereticKnowledgeRitualComponent> ent, ref HereticRitualOwnerSetEvent args)
    {
        if (!SelectKnowledgeIngredients(ent))
            return;

        var userId = CompOrNull<MindComponent>(args.Owner)?.UserId;
        if (!_player.TryGetSessionById(userId, out var session))
            return;

        var sb = new StringBuilder();
        foreach (var ingredient in ent.Comp.Ingredients)
        {
            sb.Append($"{Loc.GetString(ingredient.Name)} x{ingredient.Amount} ");
        }

        sb.Remove(sb.Length - 1, 1);

        var str = Loc.GetString("heretic-ritual-knowledge-items", ("itemlist", sb.ToString()));
        _chat.ChatMessageToOne(ChatChannel.Server, str, str, default, false, session.Channel, Color.Green);
    }
}
