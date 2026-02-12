// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Disease;
using Content.Goobstation.Shared.Disease.Components;
using Content.Medical.Common.Body;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Wounds;
using Content.Shared._Shitmed.Medical.HealthAnalyzer;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.FixedPoint;
using Content.Shared.MedicalScanner;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerControl
{
    public event Action<ProtoId<OrganCategoryPrototype>?, EntityUid>? OnBodyPartSelected;
    public event Action<HealthAnalyzerMode, EntityUid>? OnModeChanged;

    private WoundSystem _wound = default!;
    private EntityUid? _target;
    private EntityUid? _spriteViewEntity;
    private Dictionary<ProtoId<OrganCategoryPrototype>, TextureButton> _bodyPartControls = default!;

    private static readonly EntProtoId _bodyView = "AlertSpriteView";

    private void InitializeTrauma()
    {
        _wound = _entityManager.System<WoundSystem>();

        _bodyPartControls = new Dictionary<ProtoId<OrganCategoryPrototype>, TextureButton>
        {
            { "Head", HeadButton },
            { "Torso", TorsoButton },
            { "ArmLeft", LeftArmButton },
            { "HandLeft", LeftHandButton },
            { "ArmRight", RightArmButton },
            { "HandRight", RightHandButton },
            { "LegLeft", LeftLegButton },
            { "FootLeft", LeftFootButton },
            { "LegRight", RightLegButton },
            { "FootRight", RightFootButton },
        };

        foreach (var (part, button) in _bodyPartControls)
        {
            button.MouseFilter = MouseFilterMode.Stop;
            button.OnPressed += _ => SetActiveBodyPart(part);
        }
        ReturnButton.OnPressed += _ => ResetBodyPart();
        BodyButton.OnPressed += _ => SetMode(HealthAnalyzerMode.Body);
        OrgansButton.OnPressed += _ => SetMode(HealthAnalyzerMode.Organs);
        ChemicalsButton.OnPressed += _ => SetMode(HealthAnalyzerMode.Chemicals);
    }

    public void SetActiveBodyPart(ProtoId<OrganCategoryPrototype> part)
    {
        if (_target is {} target)
            OnBodyPartSelected?.Invoke(part, target);
    }

    public void SetMode(HealthAnalyzerMode mode)
    {
        if (_target is {} target)
            OnModeChanged?.Invoke(mode, target);
    }

    public void ResetBodyPart()
    {
        if (_target is {} target)
            OnBodyPartSelected?.Invoke(null, target);
    }

    public void SetActiveButtons(bool isHumanoid)
    {
        foreach (var button in _bodyPartControls.Values)
            button.Visible = isHumanoid;
    }

    public void PopulateTrauma(EntityUid target, HealthAnalyzerUiState state)
    {
        _target = target;
        var humanoid = _entityManager.HasComponent<HumanoidProfileComponent>(target);
        SetActiveButtons(humanoid);

        // Patient Information

        DamageLabelVital.Text = state.VitalDamage.ToString();

        if (humanoid)
            SpriteView.SetEntity(SetupIcon(state.Body, state.Bleeding));

        PartView.Visible = SpriteView.Visible;

        switch (state.ScanState)
        {
            case HealthAnalyzerBodyState body:
                PopulateBody(target, state, body);
                break;
            case HealthAnalyzerOrgansState organs:
                PopulateOrgans(organs);
                break;
            case HealthAnalyzerChemicalsState chemicals:
                PopulateChemicals(chemicals);
                break;
        }

        if (ConditionsListContainer.ChildCount == 0)
        {
            ConditionsListContainer.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("condition-none"),
                Margin = new Thickness(0, 4),
            });
        }
    }

    #region Scan state populate methods

    public void PopulateBody(EntityUid target, HealthAnalyzerUiState state, HealthAnalyzerBodyState body)
    {
        var part = _entityManager.GetEntity(state.Part);
        if (part != null)
            target = part.Value;
        var isPart = part != null;

        if (!_entityManager.TryGetComponent<DamageableComponent>(target, out var damageable))
            return;

        ReturnButton.Visible = isPart;
        PartNameLabel.Visible = isPart;
        DamageLabelHeading.Visible = true;
        DamageLabel.Visible = true;
        DamageLabel.Text = damageable.TotalDamage.ToString();

        var identity = Identity.Name(target, _entityManager);
        if (isPart)
        {
            PartNameLabel.Text = _entityManager.HasComponent<MetaDataComponent>(target)
                ? identity
                : Loc.GetString("health-analyzer-window-entity-unknown-value-text");
        }

        var damageSortedGroups =
            damageable.DamagePerGroup.OrderByDescending(damage => damage.Value)
                .ToDictionary(x => x.Key, x => x.Value);

        var damagePerType = damageable.Damage.DamageDict;

        DrawDiagnosticGroups(damageSortedGroups, damagePerType);

        if (_entityManager.TryGetComponent<DiseaseCarrierComponent>(target, out var carrier))
        {
            DrawDiseases(carrier.Diseases.ContainedEntities);
        }

        ConditionsListContainer.RemoveAllChildren();

        if (state.Unrevivable == true)
            ConditionsListContainer.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("condition-body-unrevivable", ("entity", identity)),
                Margin = new Thickness(0, 4),
            });

        foreach (var bleeding in state.Bleeding)
        {
            var name = _prototypes.Index(bleeding).Name.ToLowerInvariant();
            var locString = Loc.GetString("condition-body-part-bleeding", ("entity", identity), ("part", name));

            ConditionsListContainer.AddChild(new RichTextLabel
            {
                Text = locString,
                Margin = new Thickness(0, 4),
            });
        }

        foreach (var (woundableTrauma, traumas) in body.Traumas)
        {
            if (!TryGetEntityName(woundableTrauma, out var woundableName)
                || isPart
                && woundableTrauma != state.Part)
                continue;

            foreach (var trauma in traumas)
            {
                // TODO: Once these string conditionals are better defined, rewrite to use a switch case based on trauma types.
                string locString;
                if (trauma.TargetType is {} targetType)
                    locString = Loc.GetString($"condition-body-trauma-{trauma.TraumaType}",
                        ("targetSymmetry", targetType.Item2 != BodyPartSymmetry.None
                            ? $"{targetType.Item2.ToString().ToLower()} " // This is so fucking ugly.
                            : ""),
                        ("targetType", targetType.Item1.ToString().ToLower()));
                else
                    locString = trauma.SeverityString is {} severity
                        ? Loc.GetString($"condition-body-trauma-{trauma.TraumaType}-{severity}", ("woundable", woundableName))
                        : Loc.GetString($"condition-body-trauma-{trauma.TraumaType}", ("woundable", woundableName));

                ConditionsListContainer.AddChild(new RichTextLabel
                {
                    Text = locString,
                    Margin = new Thickness(0, 4),
                });
            }
        }

        /*foreach (var (woundablePain, pain) in body.NervePainFeels)
        {
            if (pain == 1.0
                || !TryGetEntityName(woundablePain, out var woundableName)
                || isPart
                && woundablePain != state.Part)
                continue;

            var painString = pain > 1.0 ? "increased" : "decreased";
            var locString = Loc.GetString($"condition-body-pain-{painString}", ("woundable", woundableName));

            ConditionsListContainer.AddChild(new RichTextLabel
            {
                Text = locString,
                Margin = new Thickness(0, 4),
            });
        }*/
    }

    public void PopulateOrgans(HealthAnalyzerOrgansState state)
    {
        ReturnButton.Visible = false;
        PartNameLabel.Visible = false;
        DamageLabelHeading.Visible = false;
        DamageLabel.Visible = false;

        ConditionsListContainer.RemoveAllChildren();
        GroupsContainer.RemoveAllChildren();
        foreach (var (organ, data) in state.Organs)
        {
            var organEnt = _entityManager.GetEntity(organ);

            if (!TryGetEntityName(organEnt, out var organName)
                || data.IntegrityCap == 0) // avoid division by zero
                continue;

            DrawOrganDiagnostics(organEnt, organName, data.Integrity / data.IntegrityCap * 100);

            if (_entityManager.HasComponent<RottingComponent>(organEnt))
            {
                ConditionsListContainer.AddChild(new RichTextLabel
                {
                    Text = Loc.GetString("condition-organ-rotting", ("organ", organName)),
                    Margin = new Thickness(0, 4),
                });
            }

            /*if (data.Integrity > data.IntegrityCap * 0.90) // Organs without at LEAST some significant damage wont be shown.
                return;
            */
            ConditionsListContainer.AddChild(new RichTextLabel
            {
                Text = Loc.GetString($"condition-organ-damage-{data.Severity.ToString()}", ("organ", organName)),
                Margin = new Thickness(0, 4),
            });
        }

        if (ConditionsListContainer.ChildCount == 0)
        {
            ConditionsListContainer.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("condition-none"),
                Margin = new Thickness(0, 4),
            });
        }
    }

    public void PopulateChemicals(HealthAnalyzerChemicalsState state)
    {
        ReturnButton.Visible = false;
        PartNameLabel.Visible = false;
        DamageLabelHeading.Visible = false;
        DamageLabel.Visible = false;

        ConditionsListContainer.RemoveAllChildren();
        GroupsContainer.RemoveAllChildren();

        DrawSolutionDiagnostics(state.Solutions);

        ConditionsListContainer.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("condition-none"),
            Margin = new Thickness(0, 4),
        });
    }

    #endregion

    private bool TryGetEntityName(NetEntity ent, out string name)
    {
        name = Loc.GetString("health-analyzer-window-entity-unknown-value-text");
        var targetedEnt = _entityManager.GetEntity(ent);

        if (!_entityManager.HasComponent<MetaDataComponent>(targetedEnt))
            return false;

        name = Identity.Name(targetedEnt, _entityManager);
        return true;
    }

    private bool TryGetEntityName(EntityUid ent, out string name)
    {
        name = Loc.GetString("health-analyzer-window-entity-unknown-value-text");

        if (!_entityManager.HasComponent<MetaDataComponent>(ent))
            return false;

        name = Identity.Name(ent, _entityManager);
        return true;
    }

    /// <summary>
    /// Sets up the Body Doll using Alert Entity to use in Health Analyzer.
    /// </summary>
    private EntityUid? SetupIcon(Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity>? body,
        HashSet<ProtoId<OrganCategoryPrototype>> bleeding)
    {
        if (body is null)
            return null;

        if (!_entityManager.Deleted(_spriteViewEntity))
            _entityManager.QueueDeleteEntity(_spriteViewEntity);

        _spriteViewEntity = _entityManager.Spawn(_bodyView);

        if (!_entityManager.TryGetComponent<SpriteComponent>(_spriteViewEntity, out var sprite))
            return null;

        int layer = 0;
        foreach (var (part, integrity) in body)
        {
            // TODO: PartStatusUIController and make it use layers instead of TextureRects when EE refactors alerts.
            var name = part.ToString().ToLowerInvariant();
            int enumValue = (int) integrity;
            var baseRsiPath = new ResPath($"/Textures/_Shitmed/Interface/Targeting/Status/{name}.rsi");
            var rsi = new SpriteSpecifier.Rsi(baseRsiPath, $"{enumValue}");
            // Shitcode with love from Russia :)
            // fuck you mocho
            CreateOrAddToLayer(sprite, rsi, layer);
            layer++;

            if (bleeding.Contains(part))
            {
                var bleedRsi = new SpriteSpecifier.Rsi(baseRsiPath, "bleed");
                CreateOrAddToLayer(sprite, bleedRsi, layer);
                layer++;
            }
        }

        return _spriteViewEntity;
    }

    private void CreateOrAddToLayer(SpriteComponent sprite, SpriteSpecifier rsi, int layer)
    {
        if (!sprite.TryGetLayer(layer, out _))
            sprite.AddLayer(_spriteSystem.Frame0(rsi));
        else
            sprite.LayerSetTexture(layer, _spriteSystem.Frame0(rsi));

        sprite.LayerSetScale(layer, new Vector2(3f, 3f));
    }

    #region Drawing

    private void DrawOrganDiagnostics(EntityUid ent, string name, FixedPoint2 damage)
    {
        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        var groupTitleText = Loc.GetString("group-organ-status",
            ("organ", textInfo.ToTitleCase(name)),
            ("capacity", damage));

        var groupContainer = new BoxContainer
        {
            Align = BoxContainer.AlignMode.Begin,
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };

        groupContainer.AddChild(CreateDiagnosticGroupTitle(groupTitleText, ent));

        GroupsContainer.AddChild(groupContainer);
    }

    private void DrawSolutionDiagnostics(Dictionary<NetEntity, Solution> solutions)
    {
        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        foreach (var (ent, data) in solutions)
        {
            // TODO SHITMED: get SolutionComponent off ent??? it should be networked
            var groupTitleText = Loc.GetString("group-solution-name",
                ("solution", data.Name ?? Loc.GetString("group-solution-unknown")));

            var groupContainer = new BoxContainer
            {
                Align = BoxContainer.AlignMode.Begin,
                Orientation = BoxContainer.LayoutOrientation.Vertical,
            };

            groupContainer.AddChild(CreateDiagnosticGroupTitle(textInfo.ToTitleCase(groupTitleText), "metaphysical"));

            GroupsContainer.AddChild(groupContainer);

            foreach (var reagent in data.Contents)
            {
                if (reagent.Quantity == 0)
                    continue;

                var reagentName = Loc.GetString("chem-master-window-unknown-reagent-text");
                if (_prototypes.Resolve<ReagentPrototype>(reagent.Reagent.Prototype, out var proto))
                    reagentName = proto.LocalizedName;

                var reagentString = $"{Loc.GetString(
                    "group-solution-contents",
                    ("reagent", textInfo.ToTitleCase(reagentName)),
                    ("quantity", reagent.Quantity)
                )}";

                groupContainer.AddChild(CreateDiagnosticItemLabel(reagentString.Insert(0, " · ")));
            }
        }
    }

    private void DrawDiseases(IReadOnlyList<EntityUid> diseases)
    {
        DiseasesContainer.RemoveAllChildren();

        if (diseases.Count == 0)
        {
            DiseasesDivider.Visible = false;
            DiseasesContainer.Visible = false;
            return;
        }
        DiseasesDivider.Visible = true;
        DiseasesContainer.Visible = true;

        DiseasesContainer.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("health-analyzer-window-diseases"),
        });

        foreach (var diseaseUid in diseases)
        {
            if (!_entityManager.TryGetComponent<DiseaseComponent>(diseaseUid, out var disease))
                continue;

            var diseaseInfoContainer = new BoxContainer
            {
                Align = BoxContainer.AlignMode.Begin,
                Orientation = BoxContainer.LayoutOrientation.Vertical,
            };
            diseaseInfoContainer.AddChild(CreateDiagnosticItemLabel(Loc.GetString("health-analyzer-window-disease-type-text", ("type", disease.Genotype))));
            diseaseInfoContainer.AddChild(CreateDiagnosticItemLabel(" · " + Loc.GetString(
                "health-analyzer-window-disease-progress-text",
                ("progress", disease.InfectionProgress)
            )));
            diseaseInfoContainer.AddChild(CreateDiagnosticItemLabel(" · " + Loc.GetString(
                "health-analyzer-window-immunity-progress-text",
                ("progress", disease.ImmunityProgress)
            )));

            DiseasesContainer.AddChild(diseaseInfoContainer);
        }
    }

    #endregion

    private BoxContainer CreateDiagnosticGroupTitle(string text, EntityUid ent, string? textureOverride = null)
    {
        var rootContainer = new BoxContainer
        {
            Margin = new Thickness(0, 6, 0, 0),
            VerticalAlignment = VAlignment.Bottom,
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
        };

        if (textureOverride != null)
        {
            rootContainer.AddChild(new TextureRect
            {
                SetSize = new Vector2(30, 30),
                Texture = GetTexture(textureOverride.ToLower())
            });
        }
        else
        {
            var spriteView = new SpriteView
            {
                SetSize = new Vector2(30, 30),
                OverrideDirection = Direction.South,
            };

            spriteView.SetEntity(ent);

            rootContainer.AddChild(spriteView);
        }

        rootContainer.AddChild(CreateDiagnosticItemLabel(text));

        return rootContainer;
    }
}
