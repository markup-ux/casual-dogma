using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arrowgene.Ddon.Server;

namespace Arrowgene.Ddon.GameServer.Characters;

public sealed class SupplyCacheVerificationReport
{
    public string Verdict { get; init; } = "INCONCLUSIVE";
    public IReadOnlyList<SupplyCacheHealthCheck> Checks { get; init; } = [];
    public IReadOnlyList<string> Hints { get; init; } = [];
}

/// <summary>
/// Session-level verification with explicit PASS/FAIL suitable for /scverify and automated review.
/// Unlike /scdiag, this fails when server-side state looks healthy but registration contracts broke.
/// </summary>
public static class SupplyCacheSessionVerifier
{
    public const string AutomatedTestCommand =
        "dotnet test D:\\DDON\\Server\\Arrowgene.Ddon.Test\\Arrowgene.Ddon.Test.csproj -c Release --filter FullyQualifiedName~SupplyCache";

    public static SupplyCacheVerificationReport Verify(DdonGameServer server, GameClient client)
    {
        List<SupplyCacheHealthCheck> checks = [];
        List<string> hints = [];

        SupplyCacheDropRecord? lastDrop = client.SupplyCacheDiagnostics.LastDrop;
        SupplyCacheRegistrationAudit.Entry? lastReg = client.RegistrationAudit.LastEntry;
        SupplyCacheDropListRecord? latestList = null;
        SupplyCacheRegistrationAudit.Entry? lastRegAfterDrop = null;
        bool regAfterInteract = false;
        bool interactAfterLastReg = false;

        if (lastDrop != null && lastDrop.SkipReason == null)
        {
            latestList = client.SupplyCacheDiagnostics.RecentDropLists
                .Where(record => record.UtcTime >= lastDrop.UtcTime && record.SetId == lastDrop.WireSetId)
                .LastOrDefault();

            lastRegAfterDrop = client.RegistrationAudit.Entries
                .Where(entry => entry.UtcTime >= lastDrop.UtcTime)
                .LastOrDefault();

            if (latestList != null && lastRegAfterDrop != null)
            {
                interactAfterLastReg = latestList.UtcTime >= lastRegAfterDrop.UtcTime;
                regAfterInteract = lastRegAfterDrop.UtcTime > latestList.UtcTime;
            }
        }

        if (lastDrop != null && lastDrop.SkipReason == null)
        {
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Discard PopDrop sent",
                Passed = lastDrop.PopDropSent,
                Detail = $"cache={lastDrop.CacheId} wire={lastDrop.WireSetId}",
            });

            bool regAfterDrop = client.RegistrationAudit.Entries
                .Any(entry => entry.UtcTime >= lastDrop.UtcTime
                    && entry.PayloadWireIds.Contains(lastDrop.WireSetId));
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Discard wire registered",
                Passed = regAfterDrop,
                Detail = regAfterDrop
                    ? $"wire {lastDrop.WireSetId} present in a post-drop registration payload"
                    : $"no registration payload included wire {lastDrop.WireSetId} after discard",
            });

            if (latestList == null)
            {
                checks.Add(new SupplyCacheHealthCheck
                {
                    Name = "Bag click reached server",
                    Passed = false,
                    Detail = "No GetDropItemList for this discard yet — click the bag to finish verification",
                });
                hints.Add("Discard done. Click the bag once, then run /scverify again.");
            }
            else
            {
                bool lootable = latestList.Outcome == SupplyCacheDropListOutcome.SupplyCacheList
                    && latestList.ItemCount > 0;
                checks.Add(new SupplyCacheHealthCheck
                {
                    Name = "Bag click returned items",
                    Passed = lootable,
                    Detail = lootable
                        ? $"items={latestList.ItemCount} path={latestList.ResponsePath}"
                        : $"outcome={latestList.Outcome} items={latestList.ItemCount}",
                });

                if (lastRegAfterDrop != null)
                {
                    checks.Add(new SupplyCacheHealthCheck
                    {
                        Name = "Click after last registration",
                        Passed = interactAfterLastReg,
                        Detail = interactAfterLastReg
                            ? $"click at {latestList.UtcTime:HH:mm:ss}Z after {lastRegAfterDrop.Path} at {lastRegAfterDrop.UtcTime:HH:mm:ss}Z"
                            : $"last registration {lastRegAfterDrop.Path} at {lastRegAfterDrop.UtcTime:HH:mm:ss}Z is newer than the bag click",
                    });
                }

                if (regAfterInteract)
                {
                    checks.Add(new SupplyCacheHealthCheck
                    {
                        Name = "Registration stable after click",
                        Passed = false,
                        Detail = $"{lastRegAfterDrop!.Path} at {lastRegAfterDrop.UtcTime:HH:mm:ss}Z ran after the bag click — client state was refreshed",
                    });
                    hints.Add("A proximity sync re-registered drop sets after your click. Click the bag again, then /scverify.");
                }
            }
        }
        else
        {
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Gameplay smoke test",
                Passed = false,
                Detail = "No discard recorded — drop one item, click the bag, then /scverify",
            });
            hints.Add("Full verification: zone in, discard an item, click the bag, run /scverify.");
        }

        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "Startup self-test",
            Passed = SupplyCacheSelfTest.LastRunPassed,
            Detail = SupplyCacheSelfTest.LastRunPassed
                ? "Core registration checks passed at server start"
                : string.Join("; ", SupplyCacheSelfTest.LastFailures),
        });

        if (!SupplyCacheSelfTest.LastRunPassed)
        {
            hints.Add("Restart the server after fixing code — startup self-test failed.");
            hints.Add($"Run automated tests: {AutomatedTestCommand}");
        }

        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "Feature enabled",
            Passed = server.GameSettings.GameServerSettings.SupplyCachesEnabled,
            Detail = $"SupplyCachesEnabled={server.GameSettings.GameServerSettings.SupplyCachesEnabled}",
        });

        int regFailures = client.RegistrationAudit.FailCount;
        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "Registration payloads complete",
            Passed = regFailures == 0,
            Detail = regFailures == 0
                ? $"audit entries={client.RegistrationAudit.Entries.Count}, failures=0"
                : $"{regFailures} incomplete registration(s) this session",
        });

        if (lastReg != null)
        {
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Last registration audit",
                Passed = lastReg.Passed,
                Detail = lastReg.Passed
                    ? $"{lastReg.Path} wires=[{string.Join(',', lastReg.PayloadWireIds)}]"
                    : $"{lastReg.Path} missing=[{string.Join(',', lastReg.MissingWireIds)}]",
            });
        }

        bool anyIncompleteReg = client.RegistrationAudit.Entries.Any(entry => !entry.Passed);
        bool startupOk = SupplyCacheSelfTest.LastRunPassed
            && server.GameSettings.GameServerSettings.SupplyCachesEnabled;
        bool gameplayComplete = lastDrop != null
            && lastDrop.SkipReason == null
            && lastDrop.PopDropSent
            && client.RegistrationAudit.Entries.Any(entry =>
                entry.UtcTime >= lastDrop.UtcTime && entry.PayloadWireIds.Contains(lastDrop.WireSetId))
            && latestList != null
            && latestList.Outcome == SupplyCacheDropListOutcome.SupplyCacheList
            && latestList.ItemCount > 0
            && interactAfterLastReg
            && !regAfterInteract;

        string verdict;
        if (!startupOk || anyIncompleteReg)
        {
            verdict = "FAIL";
        }
        else if (gameplayComplete && regFailures == 0)
        {
            verdict = "PASS";
        }
        else if (lastDrop != null && lastDrop.PopDropSent && regAfterInteract)
        {
            verdict = "FAIL";
        }
        else if (lastDrop != null && lastDrop.PopDropSent)
        {
            verdict = "PENDING";
            if (latestList == null)
            {
                hints.Add("Server path looks OK so far — complete the bag click step.");
            }
            else if (!interactAfterLastReg)
            {
                hints.Add("Registration changed after your discard. Click the bag again, then /scverify.");
            }
        }
        else
        {
            verdict = "INCONCLUSIVE";
        }

        if (verdict != "PASS")
        {
            hints.Add($"Automated regression tests: {AutomatedTestCommand}");
        }

        return new SupplyCacheVerificationReport
        {
            Verdict = verdict,
            Checks = checks,
            Hints = hints.Distinct().ToList(),
        };
    }

    public static string FormatForChat(SupplyCacheVerificationReport report)
    {
        StringBuilder sb = new();
        sb.Append($"Supply cache verification: {report.Verdict}");
        foreach (SupplyCacheHealthCheck check in report.Checks)
        {
            if (sb.Length >= SupplyCacheSessionDiagnostics.MaxChatReportLength - 40)
            {
                break;
            }

            sb.Append('\n');
            sb.Append(check.Passed ? "[OK] " : "[!!] ");
            sb.Append(check.Name);
            sb.Append(": ");
            sb.Append(check.Detail);
        }

        foreach (string hint in report.Hints)
        {
            if (sb.Length >= SupplyCacheSessionDiagnostics.MaxChatReportLength - 8)
            {
                break;
            }

            sb.Append('\n');
            sb.Append("-> ");
            sb.Append(hint);
        }

        return sb.ToString();
    }
}
