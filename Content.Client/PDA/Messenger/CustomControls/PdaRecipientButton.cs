using Robust.Client.UserInterface.Controls;

namespace Content.Client.PDA.Messenger.CustomControls;

public class PdaRecipientButton : Button
{
    [ViewVariables]
    public string? Idx { get; set; }

    [ViewVariables]
    public bool isSelected = false;
}
