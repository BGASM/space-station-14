using System.Linq;
using Content.Shared.PDA;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.PDA.Messenger;

public sealed class PdaMessengerUiState : BoundUserInterfaceState
{
    public List<PdaConversation> PdaConversations;
    public List<KnownPda> CurrentRecipients;
    public string OutgoingMessage;
    public List<CheckBox> checkList = new List<CheckBox>();

    public PdaMessengerUiState()
    {
        PdaConversations = new List<PdaConversation>();
        CurrentRecipients = new List<KnownPda>();
        OutgoingMessage = "";
    }
}
