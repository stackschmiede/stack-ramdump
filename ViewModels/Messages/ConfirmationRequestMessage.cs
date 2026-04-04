using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RamDump.ViewModels.Messages;

public class ConfirmationRequestMessage(string message) : AsyncRequestMessage<bool>
{
    public string Message { get; } = message;
}
