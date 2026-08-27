using System.Windows.Input;

namespace BetterSR.Models;

public class HotkeyDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public ModifierKeys Modifiers { get; set; }
    public Key Key { get; set; }
    public bool IsGlobal { get; set; }
}
