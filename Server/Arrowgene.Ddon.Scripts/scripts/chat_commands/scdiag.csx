using System.Text;

public class ChatCommand : IChatCommand
{
    public override AccountStateType AccountState => AccountStateType.User;
    public override string CommandName            => "scdiag";
    public override string HelpText               => "usage: `/scdiag` server-side hints. `/scverify` PASS/FAIL verification. `/scdiag events` timeline. `/scdiag log` snapshot.";

    public override void Execute(DdonGameServer server, string[] command, GameClient client, ChatMessage message, List<ChatResponse> responses)
    {
        if (command.Length > 1 && command[1].Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            client.SupplyCacheDiagnostics.Clear();
            responses.Add(ChatResponse.ServerChat(client, "Supply cache diagnostics cleared for this session."));
            return;
        }

        if (command.Length > 1 && command[1].Equals("events", StringComparison.OrdinalIgnoreCase))
        {
            IReadOnlyList<string> events = SupplyCacheEventLog.GetRecentEvents(client.Character.CharacterId, 8);
            if (events.Count == 0)
            {
                responses.Add(ChatResponse.ServerChat(client, "No supply cache events yet this session."));
                return;
            }

            StringBuilder sb = new();
            sb.Append($"Supply cache events (build {SupplyCacheEventLog.BuildStamp}):");
            foreach (string line in events)
            {
                if (sb.Length >= SupplyCacheSessionDiagnostics.MaxChatReportLength)
                {
                    break;
                }

                int evtStart = line.IndexOf(" evt=", StringComparison.Ordinal);
                sb.Append('\n');
                sb.Append(evtStart >= 0 ? line[evtStart..] : line);
            }

            responses.Add(ChatResponse.ServerChat(client, sb.ToString()));
            return;
        }

        if (command.Length > 1 && command[1].Equals("log", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                SupplyCacheHealthReport report = client.SupplyCacheDiagnostics.BuildReport(server, client);
                string path = SupplyCacheEventLog.WriteSessionSnapshot(
                    server,
                    client,
                    report,
                    client.SupplyCacheDiagnostics.LastDrop);
                responses.Add(ChatResponse.ServerChat(client, $"Supply cache snapshot written: {path.Replace('\\', '/')}"));
            }
            catch (Exception ex)
            {
                responses.Add(ChatResponse.ServerChat(client, $"scdiag log failed: {ex.Message}"));
            }

            return;
        }

        try
        {
            SupplyCacheHealthReport report = client.SupplyCacheDiagnostics.BuildReport(server, client);
            string text = SupplyCacheSessionDiagnostics.FormatReportForChat(report, client.SupplyCacheDiagnostics.LastDrop);
            if (text.Length < SupplyCacheSessionDiagnostics.MaxChatReportLength - 64)
            {
                text += $"\nbuild={SupplyCacheEventLog.BuildStamp} (/scverify for PASS/FAIL)";
            }

            responses.Add(ChatResponse.ServerChat(client, text));

            if (server.GameSettings.GameServerSettings.SupplyCacheDiagnosticsEnabled)
            {
                client.SupplyCacheDiagnostics.LogDropEvent(server, client, client.Character.CharacterId, "scdiag");
            }
        }
        catch (Exception ex)
        {
            responses.Add(ChatResponse.ServerChat(client, $"scdiag failed: {ex.Message}"));
        }
    }
}

return new ChatCommand();
