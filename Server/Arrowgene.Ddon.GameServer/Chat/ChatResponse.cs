using System.Collections.Generic;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.GameServer.Chat
{
    public class ChatResponse
    {
        public ChatResponse() { }

        public ChatResponse(GameClient client, string message, LobbyChatMsgType type)
        {
            Message = message;
            Type = type;
            Recipients = [ client ];
        }

        public static ChatResponse CommandError(GameClient client, string message)
        {
            return new ChatResponse(client, message, LobbyChatMsgType.ManagementAlertC);
        }

        public static ChatResponse ServerMessage(GameClient client, string message)
        {
            return new ChatResponse(client, message, LobbyChatMsgType.System);
        }

        public static ChatResponse ServerChat(GameClient client, string message)
        {
            return new ChatResponse(client, message, LobbyChatMsgType.ManagementGuideC);
        }

        public static ChatResponse FromMessage(GameClient client, ChatMessage message)
        {
            return new ChatResponse()
            {
                HandleId = message.HandleId,
                Message = message.Message,
                FirstName = client.Character.FirstName,
                LastName = client.Character.LastName,
                CharacterId = client.Character.CharacterId,
                Type = message.Type,
                MessageFlavor = message.MessageFlavor,
                PhrasesCategory = message.PhrasesCategory,
                PhrasesIndex = message.PhrasesIndex,
                Recipients = { client }
            };
        }

        public uint HandleId { get; set; }
        public List<GameClient> Recipients { get; } = [];
        public bool Deliver { get; set; } = true;
        public LobbyChatMsgType Type { get; set; } = LobbyChatMsgType.Say;
        public byte MessageFlavor { get; set; }
        public uint PhrasesCategory { get; set; }
        public uint PhrasesIndex { get; set; }
        public string Message { get; set; } = string.Empty;
        public uint CharacterId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ClanName { get; set; } = string.Empty;
    }
}
