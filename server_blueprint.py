"""Casual Dogma server blueprint.

Pure data describing the roles, categories, and channels for the Discord
server. Kept separate from the bot logic so it's easy to tweak the layout
without touching the creation code.

Access levels (used per category, can be overridden per channel via "send"):
    info      -> everyone can VIEW, only staff can post (read-only boards)
    members   -> only verified players (Arisen) and staff can view/post
    staff     -> only Moderators, Pawn Engineers, and Arisen Council
    dev       -> only Pawn Engineers and Arisen Council

Per-channel "send" overrides:
    "everyone" -> anyone who can view may also send (e.g. support, bug reports)
    "staff"    -> force read-only for non-staff even inside a members category
"""

# ---------------------------------------------------------------------------
# Roles, ordered TOP (highest) -> BOTTOM (lowest).
# `perms` is a keyword the bot maps to a discord.Permissions preset.
# ---------------------------------------------------------------------------
ROLES = [
    {"name": "Arisen Council", "color": 0xE74C3C, "hoist": True, "perms": "admin"},
    {"name": "Pawn Engineers", "color": 0xE67E22, "hoist": True, "perms": "manage"},
    {"name": "Moderators", "color": 0xF1C40F, "hoist": True, "perms": "mod"},
    {"name": "Support Pawns", "color": 0x2ECC71, "hoist": True, "perms": "helper"},
    {"name": "Veteran Arisen", "color": 0x9B59B6, "hoist": True, "perms": "member"},
    {"name": "Booster", "color": 0xE91E63, "hoist": False, "perms": "member"},
    {"name": "Arisen", "color": 0x3498DB, "hoist": False, "perms": "member"},
    {"name": "Newcomer", "color": 0x95A5A6, "hoist": False, "perms": "newcomer"},
    {"name": "Bots", "color": 0x607D8B, "hoist": False, "perms": "member"},
]

# Cosmetic, self-assignable vocation roles (no special permissions).
VOCATION_ROLES = [
    "Fighter", "Warrior", "Strider", "Hunter", "Seeker", "Sorcerer",
    "Element Archer", "Spirit Lancer", "Shield Sage", "Alchemist",
    "Mystic Spearhand", "High Scepter",
]

# Self-assignable notification ping roles.
PING_ROLES = ["Boss Hunts", "Patch Notes", "Events", "LFG"]

# ---------------------------------------------------------------------------
# Categories and their channels.
# Channel type defaults to "text"; use "voice" or "forum" where noted.
# ---------------------------------------------------------------------------
CATEGORIES = [
    {
        "name": "📜 WELCOME & INFO",
        "access": "info",
        "channels": [
            {"name": "welcome", "topic": "Welcome to Casual Dogma! Start here."},
            {"name": "rules", "topic": "Read and react to verify. Be a good Arisen."},
            {"name": "announcements", "topic": "Major server news and updates."},
            {"name": "patch-notes", "topic": "Server changelog and game updates."},
            {"name": "server-status", "topic": "🟢 Online / 🔴 Down / 🟡 Maintenance."},
            {"name": "roles", "topic": "Pick your vocation flair and notification pings."},
            {"name": "faq", "topic": "Frequently asked questions."},
        ],
    },
    {
        "name": "🛠️ GET STARTED",
        "access": "info",
        "channels": [
            {"name": "installation-guide", "topic": "Step-by-step client download and patching."},
            {"name": "account-registration", "topic": "How to register a game account."},
            {"name": "launcher-troubleshooting", "topic": "Common install and connection fixes."},
            {"name": "tech-support", "topic": "Ask for setup/connection help.", "send": "everyone"},
            {"name": "bug-reports", "topic": "Report bugs. Include steps to reproduce.", "type": "forum", "send": "everyone"},
        ],
    },
    {
        "name": "💬 COMMUNITY",
        "access": "members",
        "channels": [
            {"name": "general", "topic": "Main chat."},
            {"name": "introductions", "topic": "Say hi and share your Arisen."},
            {"name": "off-topic", "topic": "Non-game chatter."},
            {"name": "screenshots-clips", "topic": "Media only — share your moments."},
            {"name": "pawn-showcase", "topic": "Show off your pawn builds and looks."},
            {"name": "memes", "topic": "Keep it fun and SFW."},
        ],
    },
    {
        "name": "⚔️ GAMEPLAY",
        "access": "members",
        "channels": [
            {"name": "lfg", "topic": "Looking for group — find a party."},
            {"name": "boss-hunts", "topic": "Coordinate world and area bosses."},
            {"name": "clans-recruitment", "topic": "Advertise and recruit for clans."},
            {"name": "builds-vocations", "topic": "Theorycrafting and vocation discussion."},
            {"name": "trading-market", "topic": "Player-to-player trading."},
            {"name": "guides", "topic": "Community guides and resources.", "type": "forum"},
            {"name": "events", "topic": "Server events, contests, and bonus weekends."},
        ],
    },
    {
        "name": "🎙️ VOICE",
        "access": "members",
        "channels": [
            {"name": "General VC", "type": "voice"},
            {"name": "Party 1", "type": "voice", "user_limit": 4},
            {"name": "Party 2", "type": "voice", "user_limit": 4},
            {"name": "Party 3", "type": "voice", "user_limit": 4},
            {"name": "Boss Raid", "type": "voice"},
            {"name": "AFK", "type": "voice"},
            {"name": "Streaming", "type": "voice"},
        ],
    },
    {
        "name": "🛡️ STAFF ONLY",
        "access": "staff",
        "channels": [
            {"name": "staff-chat", "topic": "Staff coordination."},
            {"name": "mod-log", "topic": "Audit and bot logs.", "access": "dev"},
            {"name": "dev-notes", "topic": "Server maintenance and dev coordination.", "access": "dev"},
            {"name": "ticket-archive", "topic": "Closed support tickets."},
        ],
    },
]
