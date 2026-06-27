public class ChatCommand : IChatCommand
{
    public override AccountStateType AccountState => AccountStateType.GameMaster;
    public override string CommandName => "chain";
    public override string HelpText => "usage: `/chain [chainNumber]` - Updates the chain dungeon UI";

    public override void Execute(DdonGameServer server, string[] command, GameClient client, ChatMessage message, List<ChatResponse> responses)
    {
        uint chainNumber = 0;

        if (command.Length == 0)
        {
            responses.Add(ChatResponse.CommandError(client, "No arguments provided."));
            return;
        }

        if (command.Length >= 1)
        {
            if (UInt32.TryParse(command[0], out uint parsedChain) && parsedChain >= 0 && parsedChain <= 15)
            {
                chainNumber = parsedChain;
            }
            else
            {
                responses.Add(ChatResponse.CommandError(client, $"Invalid chain number"));
                return;
            }
        }

        uint chestNumber = 0;

        if (chainNumber >= 5 && chainNumber <= 9)
        {
            chestNumber = 1;
        }
        else if (chainNumber >= 10 && chainNumber <= 14)
        {
            chestNumber = 2;
        }
        else if (chainNumber == 15)
        {
            chestNumber = 3;
        }

        responses.Add(ChatResponse.ServerMessage(client, $"Chain number = {chainNumber}, unlocked chests = {chestNumber}"));

        S2CSituationDataUpdateObjectivesNtc ntc = new S2CSituationDataUpdateObjectivesNtc()
        {
            Unk0 = false, // mission complete
            Unk1 = 0, // final objective?
            Unk2 = chainNumber,
            Unk3 = 15, // max chains
            Unk4 = chestNumber,
            Unk5 = 3, // max chests
            ObjectiveList = new()
            {
                new CDataSituationObjective() { Unk0 = 63, Unk1 = 196610, Message = "A minor abnormality has occurred" },
                new CDataSituationObjective() { Unk0 = 65, Unk1 = 393225, Message = "A minor abnormality has occurred" },
                new CDataSituationObjective() { Unk0 = 68, Unk1 = 851973, Message = "A minor abnormality has occurred" },
                new CDataSituationObjective() { Unk0 = 68, Unk1 = 851974, Message = "A minor abnormality has occurred" }
            }
        };
        client.Send(ntc);
    }
}
return new ChatCommand();