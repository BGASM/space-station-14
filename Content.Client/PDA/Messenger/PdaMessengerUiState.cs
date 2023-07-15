using System.Linq;

namespace Content.Client.PDA.Messenger;

public sealed class PdaMessengerUiState : BoundUserInterfaceState
{
    public List<PdaConversation> PdaConversations;

    public PdaMessengerUiState(List<PdaConversation> pdaConversations)
    {
        PdaConversations = pdaConversations;
    }

    public PdaConversation? getConversation(string name)
    {
        return PdaConversations.First(f => f.Name == name);
    }

    public sealed class PdaMessage
    {
        public string Name;
        public string Message;

        public PdaMessage(string name, string message)
        {
            Name = name;
            Message = message;
        }
    }

    public sealed class PdaConversation
    {
        public string Name;
        public List<string> Messages;
        public string LastMessage { get; set; }

        public PdaConversation(string name, string message)
        {
            Name = name;
            Messages = new List<string>() { message };
            LastMessage = message;
        }

        public PdaConversation(PdaMessage message)
        {
            Name = message.Name;
            Messages = new List<string>() { message.Message };
            LastMessage = message.Message;
        }
    }

}
