using System.Text;

public class ChatCommand : IChatCommand
{
    public override AccountStateType AccountState => AccountStateType.User;
    public override string CommandName            => "scverify";
    public override string HelpText               => "usage: `/scverify` run supply cache PASS/FAIL verification. `/scverify log` write verification snapshot.";

    public override void Execute(DdonGameServer server, string[] command, GameClient client, ChatMessage message, List<ChatResponse> responses)
    {
        SupplyCacheVerificationReport report = SupplyCacheSessionVerifier.Verify(server, client);

        if (command.Length > 1 && command[1].Equals("log", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                SupplyCacheHealthReport diag = client.SupplyCacheDiagnostics.BuildReport(server, client);
                string path = SupplyCacheEventLog.WriteSessionSnapshot(
                    server,
                    client,
                    diag,
                    client.SupplyCacheDiagnostics.LastDrop);
                SupplyCacheEventLog.Record(
                    client,
                    "verify",
                    $"verdict={report.Verdict} snapshot={path.Replace('\\', '/')}");
                responses.Add(ChatResponse.ServerChat(
                    client,
                    $"Verification {report.Verdict}. Snapshot: {path.Replace('\\', '/')}"));
            }
            catch (Exception ex)
            {
                responses.Add(ChatResponse.ServerChat(client, $"scverify log failed: {ex.Message}"));
            }

            return;
        }

        string text = SupplyCacheSessionVerifier.FormatForChat(report);
        responses.Add(ChatResponse.ServerChat(client, text));
        SupplyCacheEventLog.Record(client, "verify", $"verdict={report.Verdict}");
    }
}

return new ChatCommand();
