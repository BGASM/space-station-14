using System.Linq;
using Content.Shared.PDA;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.PDA.Messenger;

public sealed class PdaMessengerUiState : BoundUserInterfaceState
{
    public List<PdaConversation> PdaConversations;
    public List<Recipient> CurrentRecipients;
    public string OutgoingMessage;
    public List<CheckBox> checkList = new List<CheckBox>();

    public PdaMessengerUiState()
    {
        PdaConversations = new List<PdaConversation>();
        CurrentRecipients = new List<Recipient>();
        OutgoingMessage = "";
    }
    public sealed class Recipient
    {
        public string Name;
        public string Address;

        public Recipient(string name, string address)
        {
            Name = name;
            Address = address;
        }
    }
}
