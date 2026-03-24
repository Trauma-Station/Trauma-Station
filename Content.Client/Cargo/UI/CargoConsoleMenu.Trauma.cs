using Robust.Client.UserInterface.Controls;

namespace Content.Client.Cargo.UI;

/// <summary>
/// Trauma - order destination setting tab
/// </summary>
public sealed partial class CargoConsoleMenu
{
    public event Action<NetEntity>? OnSetDestination;

    private RadioOptions<NetEntity> _options = default!;

    private void InitializeTrauma()
    {
        TabContainer.SetTabTitle(2, Loc.GetString("cargo-console-menu-tab-title-destination"));

        _options = new(RadioOptionsLayout.Vertical);
        _options.OnItemSelected += args =>
        {
            _options.Select(args.Id);
            OnSetDestination?.Invoke(_options.SelectedValue);
        };
        Destinations.AddChild(_options);
    }

    private void UpdateDestination(NetEntity? dest)
    {
        if (dest is {} ent)
            _options.SelectByValue(ent);
    }

    public void UpdateDestinations(List<(string, NetEntity)> dests)
    {
        _options.Clear();
        foreach (var (name, ent) in dests)
        {
            _options.AddItem(name, ent);
        }

        if (_orderConsoleQuery.CompOrNull(_owner)?.Destination is {} dest)
            _options.SelectByValue(dest);

        NoDestinations.Visible = dests.Count == 0;
        Destinations.Visible = dests.Count > 0;
    }
}
