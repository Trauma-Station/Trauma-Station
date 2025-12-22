using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.Disease;
using Content.Goobstation.Shared.Disease.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Numerics;

namespace Content.Goobstation.Server.Disease;

public sealed partial class DiseaseSystem
{
    // cache for field setters for DiseaseGenericEffectComponent
    private readonly Dictionary<(Type, string), Action<Component, float>> _setterCache = new();

    protected override void InitializeEffects()
    {
        base.InitializeEffects();

        SubscribeLocalEvent<DiseaseGenericEffectComponent, DiseaseEffectEvent>(OnGenericEffect);
        SubscribeLocalEvent<DiseaseGenericEffectComponent, DiseaseEffectFailedEvent>(OnGenericEffectFail);
    }

    private void OnGenericEffect(Entity<DiseaseGenericEffectComponent> ent, ref DiseaseEffectEvent args)
    {
        ApplyGenericEffect(ent, GetScale(args, ent.Comp));
    }

    private void OnGenericEffectFail(Entity<DiseaseGenericEffectComponent> ent, ref DiseaseEffectFailedEvent args)
    {
        if (ent.Comp.ZeroOnFail)
            ApplyGenericEffect(ent, 0f);
    }

    public void ApplyGenericEffect(Entity<DiseaseGenericEffectComponent> ent, float mul)
    {
        if (!Factory.TryGetRegistration(ent.Comp.Component, out var registration))
        {
            Log.Error($"Unknown target component '{ent.Comp.Component}' on {ToPrettyString(ent)}");
            return;
        }

        var targetType = registration.Type;
        if (!EntityManager.TryGetComponent(ent, Factory.GetRegistration(targetType), out var comp))
            return;

        foreach (var (field, baseValue) in ent.Comp.Defaults)
        {
            var key = (targetType, field);

            if (!_setterCache.TryGetValue(key, out var setter))
            {
                setter = CompileSetter(targetType, field);
                _setterCache[key] = setter;
            }

            setter?.Invoke((Component)comp, baseValue * mul);
        }
    }

    /// <summary>
    /// Compiles a lambda: (Component target, float value) => ((TargetType)target).FieldName = value;
    /// </summary>
    private Action<Component, float> CompileSetter(Type targetType, string fieldName)
    {
        var targetParam = Expression.Parameter(typeof(Component), "target");
        var valueParam = Expression.Parameter(typeof(float), "value");
        var castParam = Expression.Convert(targetParam, targetType);

        var memberAccess = Expression.PropertyOrField(castParam, fieldName);

        DebugTools.Assert(memberAccess.Type == typeof(float));

        var assign = Expression.Assign(memberAccess, valueParam);

        return Expression.Lambda<Action<Component, float>>(assign, targetParam, valueParam).Compile();
    }
}
