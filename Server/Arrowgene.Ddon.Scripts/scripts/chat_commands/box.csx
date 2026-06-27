using Arrowgene.Ddon.GameServer.Characters;

public class ChatCommand : IChatCommand
{
    public override AccountStateType AccountState => AccountStateType.User;
    public override string CommandName => "box";
    public override string HelpText => "usage: `/box` - Open your storage box from anywhere.";

    public override void Execute(DdonGameServer server, string[] command, GameClient client, ChatMessage message, List<ChatResponse> responses)
    {
        if (client.GameMode != GameMode.Normal)
        {
            responses.Add(ChatResponse.CommandError(client, "Storage box is not available in this game mode."));
            return;
        }

        RemoteStorageManager.OpenStorageBox(client);
    }
}

return new ChatCommand();
