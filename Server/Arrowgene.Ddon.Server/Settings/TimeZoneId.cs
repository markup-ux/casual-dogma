using System;

namespace Arrowgene.Ddon.Server.Settings
{
    /// <summary>
    /// Named timezone constants for use with ServerTimeZone.
    /// Each constant returns a TimeZoneInfo that covers both DST-observing and fixed-offset
    /// timezones - no manual update is needed when clocks change.
    ///
    /// If your timezone is not listed, use FindSystemTimeZoneById with any IANA ID:
    ///   settings.ServerTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Indiana/Knox");
    ///
    /// For a fully custom offset with no IANA ID:
    ///   settings.ServerTimeZone = TimeZoneInfo.CreateCustomTimeZone("custom", TimeSpan.FromHours(5.5), "UTC+5:30", "UTC+5:30");
    ///
    /// A full list of IANA timezone IDs: https://en.wikipedia.org/wiki/List_of_tz_database_time_zones
    /// </summary>
    public static class TimeZoneId
    {
        // ── Universal ─────────────────────────────────────────────────────────
        public static TimeZoneInfo UTC              => TimeZoneInfo.FindSystemTimeZoneById("UTC");

        // ── Europe - DST-observing ────────────────────────────────────────────
        public static TimeZoneInfo UKIreland        => TimeZoneInfo.FindSystemTimeZoneById("Europe/London");    // UK, Ireland              (GMT/BST)
        public static TimeZoneInfo PortugalMainland => TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");    // Portugal mainland         (WET/WEST)
        public static TimeZoneInfo CentralEurope    => TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");    // Germany, France, Spain, Italy, Netherlands,
                                                                                                                // Belgium, Austria, Switzerland, Poland, Czech Republic,
                                                                                                                // Slovakia, Hungary, Croatia, Slovenia, Denmark,
                                                                                                                // Norway, Sweden, Luxembourg, and more (CET/CEST)
        public static TimeZoneInfo EasternEurope    => TimeZoneInfo.FindSystemTimeZoneById("Europe/Helsinki"); // Finland, Estonia, Latvia, Lithuania (EET/EEST)
        public static TimeZoneInfo SoutheastEurope  => TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");   // Greece, Romania, Bulgaria, Cyprus  (EET/EEST)
        public static TimeZoneInfo Ukraine          => TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");     // Ukraine                   (EET/EEST)

        // ── Europe - fixed offset (no DST) ───────────────────────────────────
        public static TimeZoneInfo Iceland          => TimeZoneInfo.FindSystemTimeZoneById("Atlantic/Reykjavik"); // Iceland                (UTC+0)
        public static TimeZoneInfo Moscow           => TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");   // Russia (Moscow zone)       (UTC+3, no DST since 2014)
        public static TimeZoneInfo Turkey           => TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); // Turkey                    (UTC+3, no DST since 2016)

        // ── Americas - DST-observing ──────────────────────────────────────────
        public static TimeZoneInfo Eastern          => TimeZoneInfo.FindSystemTimeZoneById("America/New_York");    // US/Canada Eastern      (EST/EDT)
        public static TimeZoneInfo Central          => TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");     // US/Canada Central      (CST/CDT)
        public static TimeZoneInfo Mountain         => TimeZoneInfo.FindSystemTimeZoneById("America/Denver");      // US Mountain            (MST/MDT)
        public static TimeZoneInfo Pacific          => TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles"); // US/Canada Pacific      (PST/PDT)
        public static TimeZoneInfo Alaska           => TimeZoneInfo.FindSystemTimeZoneById("America/Anchorage");   // Alaska                 (AKST/AKDT)
        public static TimeZoneInfo Brazil           => TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");   // Brazil (Brasília)      (BRT/BRST)

        // ── Americas - fixed offset (no DST) ─────────────────────────────────
        public static TimeZoneInfo Arizona          => TimeZoneInfo.FindSystemTimeZoneById("America/Phoenix");     // Arizona                (MST, no DST)
        public static TimeZoneInfo Hawaii           => TimeZoneInfo.FindSystemTimeZoneById("Pacific/Honolulu");    // Hawaii                 (HST, no DST)

        // ── Asia - fixed offset (no DST) ─────────────────────────────────────
        public static TimeZoneInfo Japan            => TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");          // Japan                  (JST, UTC+9)
        public static TimeZoneInfo Korea            => TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul");          // South Korea            (KST, UTC+9)
        public static TimeZoneInfo China            => TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");       // China                  (CST, UTC+8)
        public static TimeZoneInfo Singapore        => TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");      // Singapore              (SGT, UTC+8)
        public static TimeZoneInfo HongKong         => TimeZoneInfo.FindSystemTimeZoneById("Asia/Hong_Kong");      // Hong Kong              (HKT, UTC+8)
        public static TimeZoneInfo Indochina        => TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");        // Thailand, Vietnam, etc.(ICT, UTC+7)
        public static TimeZoneInfo WesternIndonesia => TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");        // Western Indonesia      (WIB, UTC+7)
        public static TimeZoneInfo India            => TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");        // India                  (IST, UTC+5:30)
        public static TimeZoneInfo Nepal            => TimeZoneInfo.FindSystemTimeZoneById("Asia/Kathmandu");      // Nepal                  (NPT, UTC+5:45)
        public static TimeZoneInfo Pakistan         => TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");        // Pakistan               (PKT, UTC+5)
        public static TimeZoneInfo Iran             => TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");         // Iran                   (IRST/IRDT, observes DST)

        // ── Pacific / Oceania ─────────────────────────────────────────────────
        public static TimeZoneInfo AustraliaCentral => TimeZoneInfo.FindSystemTimeZoneById("Australia/Darwin");    // Australia Central      (ACST, UTC+9:30, no DST)
        public static TimeZoneInfo AustraliaEastern => TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");    // Australia Eastern      (AEST/AEDT, observes DST)
        public static TimeZoneInfo NewZealand       => TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");    // New Zealand            (NZST/NZDT, observes DST)
    }
}
