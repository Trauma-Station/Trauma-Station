// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.CosmicCult;
using Content.Trauma.Shared.CosmicCult.Components;
using Content.Trauma.Shared.CosmicCult.Prototypes;
using Robust.Client.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Trauma.Client.CosmicCult.UI.CosmicShop;

[GenerateTypedNameReferences]
public sealed partial class CosmicShopMenu : FancyWindow
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private SpriteSystem _sprite = default!;
    private EntityQuery<CosmicCultComponent> _cultistQuery = default!;

    public Action<ProtoId<InfluencePrototype>>? OnGainButtonPressed;
    public Action? OnLevelUpConfirmed;
    public Action? OnRespecConfirmed;

    private CosmicCultComponent? _comp;
    private List<InfluenceButtonContainer> _influenceButtons = new();
    private InfluenceUIBox _selectedInfo;
    private int _progressPercent = -1;
    private int _respecs = -1;
    private int _influenceCount;

    public CosmicShopMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _sprite = _ent.System<SpriteSystem>();
        _cultistQuery = _ent.GetEntityQuery<CosmicCultComponent>();

        _selectedInfo = new InfluenceUIBox(_sprite);
        _selectedInfo.Visible = false;
        _selectedInfo.OnGainButtonPressed += () =>
        {
            OnGainButtonPressed?.Invoke(_selectedInfo.Proto.ID);
        };
        InfluenceDetails.AddChild(_selectedInfo);
        SetupInfluences();

        LevelUpConfirm.OnPressed += _ => OnLevelUpConfirmed?.Invoke();
        RespecButton.OnPressed += _ => OnRespecConfirmed?.Invoke();

        CultProgressBar.BackgroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = new Color(15, 17, 30) };
        CultProgressBar.ForegroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = new Color(91, 62, 124) };

        Update();
    }

    private void Update()
    {
        if (_cultistQuery.TryComp(_player.LocalEntity, out _comp))
            UpdateState(_comp);
    }

    public void UpdateState(CosmicCultComponent comp)
    {
        UpdateBar(comp);
        UpdateEntropy(comp);
        UpdateInfluences(comp);
        UpdateLevelupConfirmation(comp);

        _selectedInfo?.Update(comp);
    }

    private void SetupInfluences()
    {
        foreach (var proto in _proto.EnumeratePrototypes<InfluencePrototype>())
        {
            var button = new InfluenceButtonContainer(_sprite, proto);
            button.OnDetailButtonPressed += () => SelectInfluence(proto);
            _influenceButtons.Add(button);
        }

        Control[] containers = [Level0, Level1, Level2, Level3];
        foreach (var box in _influenceButtons.OrderBy(box => box.Proto.Cost))
        {
            containers[box.Proto.Tier].AddChild(box);
        }
    }

    /// <summary>
    ///     Updates the progress bar
    /// </summary>
    private void UpdateBar(CosmicCultComponent comp)
    {
        var percentComplete = 100f * ((float) (comp.TotalEntropy - comp.EntropyRequirementOffset) / comp.EntropyForNextLevel);

        percentComplete = Math.Min(percentComplete, 100f);

        if (comp.EntropyLocked)
            percentComplete = 100f;

        CultProgressBar.Value = percentComplete;

        var percent = (int) percentComplete;
        if (_progressPercent == percent)
            return;

        _progressPercent = percent;
        ProgressBarPercentage.Text = Loc.GetString("cosmic-shop-interface-progress-bar", ("percentage", percent));
    }

    /// <summary>
    ///     Updates the entropy fields
    /// </summary>
    private void UpdateEntropy(CosmicCultComponent comp)
    {
        var entropyToNextStage = Math.Max(comp.EntropyForNextLevel - (comp.TotalEntropy - comp.EntropyRequirementOffset), 0);

        if (comp.EntropyLocked)
            entropyToNextStage = 0;

        // TODO: make these more reactive
        AvailableEntropy.Text = Loc.GetString("cosmic-shop-interface-entropy-value", ("infused", comp.EntropyBudget));
        EntropyUntilNextStage.Text = Loc.GetString("cosmic-shop-interface-entropy-value", ("infused", entropyToNextStage));
        CultistsUntilNextCultStage.Text = comp.CultistsForNextLevel.ToString();

        var respecDirty = false;
        if (_respecs != comp.RespecsAvailable)
        {
            _respecs = comp.RespecsAvailable;
            respecDirty = true;
        }
        if (_influenceCount != comp.OwnedInfluences.Count)
        {
            _influenceCount = comp.OwnedInfluences.Count;
            respecDirty = true;
        }

        if (!respecDirty)
            return;

        RespecButton.Disabled = comp.RespecsAvailable <= 0 || comp.OwnedInfluences.Count <= 0;
        if (RespecButton.Disabled)
        {
            RespecText.Text = Loc.GetString(comp.RespecsAvailable <= 0 ? "cosmic-shop-interface-respec-no-rift" : "cosmic-shop-interface-respec-no-influence");
            RespecButton.Modulate = Color.Gray;
        }
        else
        {
            RespecText.Text = Loc.GetString("cosmic-shop-interface-respec-amount", ("count", comp.RespecsAvailable));
            RespecButton.Modulate = Color.White;
        }
    }

    /// <summary>
    ///    Update all the influence thingies
    /// </summary>
    private void UpdateInfluences(CosmicCultComponent comp)
    {
        foreach (var box in _influenceButtons)
        {
            box.Update(comp);
        }
    }

    private void UpdateLevelupConfirmation(CosmicCultComponent comp)
    {
        LevelUpConfirmation.Visible = comp.LevelUpAwaitingConfirmation;
        if (!comp.LevelUpAwaitingConfirmation)
            return;

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("cosmic-shop-interface-consequences"));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("cosmic-shop-interface-consequence-level" + (comp.CurrentLevel + 1).ToString()));
        // It's hardcoded to check if next tier is tier 2, because tier 2 no longer has any effects, and I'm too lazy to make a generic check for that
        // This will be discarded in rework part 2 anyway. (lol)
        if (comp.CultistsForNextLevel <= 1 && comp.CurrentLevel == comp.CultTier && comp.CultTier != 1) // Tierup on normal conditions
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("cosmic-shop-interface-consequence-tier" + (comp.CultTier + 1).ToString()));
        }
        if (comp.CurrentLevel > comp.CultTier && comp.CultTier != 1) // Speedrun tierup
        {
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("cosmic-shop-interface-consequence-tier" + (comp.CurrentLevel).ToString()));
        }
        ConsequenceLabel.SetMessage(msg, Color.FromHex("#4CA7AD"));
    }

    /// <summary>
    ///    Show all the details of the selected influence
    /// </summary>
    private void SelectInfluence(InfluencePrototype proto)
    {
        _selectedInfo.Visible = true;
        _selectedInfo.SetProto(proto);
        if (_comp is { } comp)
            _selectedInfo.Update(comp);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (VisibleInTree)
            Update();
    }
}
