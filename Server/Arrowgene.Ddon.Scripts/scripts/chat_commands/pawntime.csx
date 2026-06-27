using System.Collections.Generic;
using System.Text;

public class ChatCommand : IChatCommand
{
    public override AccountStateType AccountState => AccountStateType.User;
    public override string CommandName => "pawntime";
    public override string HelpText => "usage: `/pawntime` - Check rental pawn adventure timers.";

    public override void Execute(DdonGameServer server, string[] command, GameClient client, ChatMessage message, List<ChatResponse> responses)
    {
        responses.Add(ChatResponse.ServerChat(client, $"Pawn Adventure Timers: "));
        foreach (var member in client.Party.Members)
        {
            if (member is PawnPartyMember pawnMember
                && pawnMember.Pawn is RentalPawn rentalPawn
                && rentalPawn.CharacterId == client.Character.CharacterId)
            {
                var (isRunning, timeLeft) = server.RentalPawnManager.GetAdventureTimeRemaining(pawnMember);
                if (isRunning)
                {
                    responses.Add(ChatResponse.ServerChat(client, $"  {rentalPawn.Name} : {timeLeft} minutes."));
                }
                else
                {
                    responses.Add(ChatResponse.ServerChat(client, $"  {rentalPawn.Name} : -Elapsed-"));
                }
            }
        }
    }
}

return new ChatCommand();
