// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Goobstation.Common.Examine;
using Content.Shared.Chat;
using Content.Shared.Speech.Components;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.Side;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Systems.Side;

public abstract class SharedFearSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    [Dependency] private readonly EntityQuery<VocalComponent> _vocalQuery = default!;

    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private readonly Regex _pattern = new("[a-zA-Z0-9]");

    private readonly HashSet<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FearComponent, GetExamineRangeEvent>(OnGetExamineRange);
        SubscribeLocalEvent<FearComponent, UserExaminedEvent>(OnExamine);
        SubscribeLocalEvent<FearComponent, GetExamineNameEvent>(OnGetName);
    }

    private void OnGetName(Entity<FearComponent> ent, ref GetExamineNameEvent args)
    {
        if (ent.Comp.TotalFear < ent.Comp.HorrorThreshold)
            return;

        args.Result = TransformString(args.Ent.Comp.EntityName);
    }

    private void OnExamine(Entity<FearComponent> ent, ref UserExaminedEvent args)
    {
        if (ent.Comp.TotalFear < ent.Comp.HorrorThreshold)
            return;

        StringBuilder sb = new();
        foreach (var node in args.Message.Nodes)
        {
            sb.Append(ConvertMarkupNode(node));
        }

        args.Message = FormattedMessage.FromMarkupPermissive(sb.ToString());
    }

    private string TransformString(string str)
    {
        return _pattern.Replace(str, _ => Chars[_random.Next(Chars.Length)].ToString());
    }

    private string ConvertMarkupNode(MarkupNode node)
    {
        if (node.Value.StringValue is { } str)
        {
            var parameter = new MarkupParameter(TransformString(str), node.Value.LongValue, node.Value.ColorValue);
            return new MarkupNode(node.Name, parameter, node.Attributes, node.Closing).ToString();
        }

        return node.ToString();
    }

    private void OnGetExamineRange(Entity<FearComponent> ent, ref GetExamineRangeEvent args)
    {
        args.Range *= Math.Clamp(1f - ent.Comp.TotalFear / ent.Comp.MaxFear, ent.Comp.MinRadius, 1f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<FearComponent>();
        while (query.MoveNext(out var uid, out var fear))
        {
            if (now < fear.NextUpdate)
                continue;

            fear.NextUpdate = now + fear.UpdateDelay;

            _toRemove.Clear();
            var count = fear.FearData.Count;
            foreach (var (k, v) in fear.FearData)
            {
                if (v <= 0 || !Exists(k))
                    _toRemove.Add(k);
            }

            if (_toRemove.Count == count)
            {
                RemCompDeferred(uid, fear);
                continue;
            }

            foreach (var k in _toRemove)
            {
                fear.FearData.Remove(k);
            }

            if (now > fear.NextReduction)
                ReduceFear((uid, fear), fear.Modifier, null);
            else if (_toRemove.Count > 0)
                ReCalculateTargetFear((uid, fear));
            else
                Dirty(uid, fear);

            var modifier = Math.Clamp(fear.TargetFear - fear.TotalFear, -fear.Modifier, fear.Modifier);
            var lastFear = fear.TotalFear;
            fear.TotalFear += modifier;

            UpdateSoundVolume((uid, fear));

            if (lastFear >= fear.HorrorThreshold || fear.TotalFear < fear.HorrorThreshold || now < fear.NextScream ||
                !_vocalQuery.TryComp(uid, out var vocal))
                continue;

            fear.NextScream = now + fear.ScreamDelay;
            _chat.TryEmoteWithChat(uid, vocal.ScreamId);
        }
    }

    public void AdjustFear(EntityUid uid, EntityUid source, float amount)
    {
        switch (amount)
        {
            case > 0:
                AddFear(uid, amount, source);
                break;
            case < 0:
                ReduceFear(uid, -amount, source);
                break;
        }
    }

    public void ReCalculateTargetFear(Entity<FearComponent> ent, bool dirty = true)
    {
        var total = ent.Comp.FearData.Sum(x => x.Value);
        ent.Comp.TargetFear = total;
        if (dirty)
            Dirty(ent);
    }

    protected void AddFear(EntityUid uid, float amount, EntityUid source)
    {
        var fear = EnsureComp<FearComponent>(uid);
        amount = MathF.Min(amount, fear.MaxFear - fear.TargetFear);
        if (amount <= 0f)
            return;
        if (!fear.FearData.TryAdd(source, amount))
            fear.FearData[source] += amount;
        fear.NextReduction = Timing.CurTime + fear.ReductionDelay;
        ReCalculateTargetFear((uid, fear));
    }

    protected void ReduceFear(Entity<FearComponent?> ent, float amount, EntityUid? source)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (source is { } uid)
        {
            if (!ent.Comp.FearData.TryGetValue(uid, out var value))
                return;

            if (value <= amount)
            {
                ent.Comp.FearData.Remove(uid);

                if (ent.Comp.FearData.Count == 0)
                    RemCompDeferred(ent, ent.Comp);
                else
                    ReCalculateTargetFear(ent!);

                return;
            }

            ent.Comp.FearData[uid] = value - amount;
            ReCalculateTargetFear(ent!);
            return;
        }

        var reduction = amount / ent.Comp.FearData.Count;
        ent.Comp.FearData = ent.Comp.FearData.Where(x => x.Value > reduction)
            .ToDictionary(x => x.Key, x => x.Value - reduction);

        if (ent.Comp.FearData.Count == 0)
            RemCompDeferred(ent, ent.Comp);
        else
            ReCalculateTargetFear(ent!);
    }

    protected virtual void UpdateSoundVolume(Entity<FearComponent> ent) { }

    protected virtual void Scream(EntityUid uid) { }
}
