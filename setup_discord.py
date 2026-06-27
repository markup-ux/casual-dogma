"""Casual Dogma — one-shot Discord server setup script.

Creates all roles, categories, channels, and permission overwrites defined in
`server_blueprint.py`. Safe to re-run: existing roles/channels are detected by
name and skipped (only missing pieces are created).

Usage:
    1. pip install -r requirements.txt
    2. Copy .env.example to .env and fill in DISCORD_TOKEN and GUILD_ID.
    3. Invite the bot to your server with the Administrator permission.
    4. python setup_discord.py
"""

import os
import sys

import discord
from dotenv import load_dotenv

from server_blueprint import (
    ROLES,
    VOCATION_ROLES,
    PING_ROLES,
    CATEGORIES,
)

load_dotenv()

TOKEN = os.getenv("DISCORD_TOKEN")
GUILD_ID = os.getenv("GUILD_ID")


def permission_preset(name: str) -> discord.Permissions:
    """Map a blueprint perm keyword to a discord.Permissions object."""
    if name == "admin":
        return discord.Permissions(administrator=True)
    if name == "manage":
        return discord.Permissions(
            manage_guild=True, manage_channels=True, manage_roles=True,
            manage_messages=True, kick_members=True, ban_members=True,
            manage_webhooks=True, view_audit_log=True, mention_everyone=True,
        )
    if name == "mod":
        return discord.Permissions(
            kick_members=True, ban_members=True, manage_messages=True,
            manage_nicknames=True, moderate_members=True, view_audit_log=True,
            mute_members=True, move_members=True, deafen_members=True,
        )
    if name == "helper":
        return discord.Permissions(
            manage_messages=True, move_members=True,
        )
    # member / newcomer / default
    return discord.Permissions.none()


async def ensure_roles(guild: discord.Guild) -> dict:
    """Create main, vocation, and ping roles. Returns {name: Role}."""
    existing = {r.name: r for r in guild.roles}
    created = {}

    # Cosmetic roles first so they settle at the bottom of the hierarchy.
    for name in VOCATION_ROLES + PING_ROLES:
        if name in existing:
            created[name] = existing[name]
            continue
        role = await guild.create_role(
            name=name, mentionable=True, reason="Casual Dogma setup"
        )
        created[name] = role
        print(f"  + role (cosmetic): {name}")

    # Main hierarchy created bottom -> top so order ends up correct.
    for spec in reversed(ROLES):
        name = spec["name"]
        if name in existing:
            created[name] = existing[name]
            print(f"  = role exists: {name}")
            continue
        role = await guild.create_role(
            name=name,
            colour=discord.Colour(spec["color"]),
            hoist=spec.get("hoist", False),
            mentionable=True,
            permissions=permission_preset(spec.get("perms", "member")),
            reason="Casual Dogma setup",
        )
        created[name] = role
        print(f"  + role: {name}")

    return created


def build_overwrites(guild, roles, access):
    """Return permission overwrites for a given access level."""
    everyone = guild.default_role
    staff_names = ["Moderators", "Pawn Engineers", "Arisen Council", "Support Pawns"]
    staff = [roles[n] for n in staff_names if n in roles]
    dev = [roles[n] for n in ["Pawn Engineers", "Arisen Council"] if n in roles]

    ov = {}

    if access == "info":
        # Everyone can read; only staff can post.
        ov[everyone] = discord.PermissionOverwrite(
            view_channel=True, send_messages=False, add_reactions=True,
            create_public_threads=False, create_private_threads=False,
        )
        for r in staff:
            ov[r] = discord.PermissionOverwrite(send_messages=True, manage_messages=True)

    elif access == "members":
        ov[everyone] = discord.PermissionOverwrite(view_channel=False)
        if "Newcomer" in roles:
            ov[roles["Newcomer"]] = discord.PermissionOverwrite(view_channel=False)
        for n in ["Arisen", "Veteran Arisen", "Booster"]:
            if n in roles:
                ov[roles[n]] = discord.PermissionOverwrite(
                    view_channel=True, send_messages=True, connect=True, speak=True,
                )
        for r in staff:
            ov[r] = discord.PermissionOverwrite(
                view_channel=True, send_messages=True, connect=True, speak=True,
            )

    elif access == "staff":
        ov[everyone] = discord.PermissionOverwrite(view_channel=False)
        for r in staff:
            ov[r] = discord.PermissionOverwrite(
                view_channel=True, send_messages=True, connect=True, speak=True,
            )

    elif access == "dev":
        ov[everyone] = discord.PermissionOverwrite(view_channel=False)
        for r in dev:
            ov[r] = discord.PermissionOverwrite(
                view_channel=True, send_messages=True,
            )

    return ov


def apply_send_override(ov, guild, roles, send):
    """Mutate overwrites for per-channel send rules."""
    everyone = guild.default_role
    if send == "everyone":
        cur = ov.get(everyone, discord.PermissionOverwrite())
        cur.update(send_messages=True, create_public_threads=True, add_reactions=True)
        ov[everyone] = cur
    elif send == "staff":
        cur = ov.get(everyone, discord.PermissionOverwrite())
        cur.update(send_messages=False)
        ov[everyone] = cur
    return ov


async def ensure_channels(guild: discord.Guild, roles: dict):
    existing_cats = {c.name: c for c in guild.categories}
    existing_chans = {c.name: c for c in guild.channels}

    for cat in CATEGORIES:
        cat_access = cat["access"]
        cat_ov = build_overwrites(guild, roles, cat_access)

        category = existing_cats.get(cat["name"])
        if category is None:
            category = await guild.create_category(
                cat["name"], overwrites=cat_ov, reason="Casual Dogma setup"
            )
            print(f"+ category: {cat['name']}")
        else:
            print(f"= category exists: {cat['name']}")

        for ch in cat["channels"]:
            name = ch["name"]
            ctype = ch.get("type", "text")
            # Channel-level access override (e.g. dev channels in staff category).
            access = ch.get("access", cat_access)
            ov = build_overwrites(guild, roles, access)
            if "send" in ch:
                ov = apply_send_override(ov, guild, roles, ch["send"])

            # Match existing by normalized name (Discord lowercases text channels).
            key_variants = {name, name.lower(), name.lower().replace(" ", "-")}
            if any(k in existing_chans for k in key_variants):
                print(f"    = channel exists: {name}")
                continue

            try:
                if ctype == "voice":
                    await guild.create_voice_channel(
                        name, category=category, overwrites=ov,
                        user_limit=ch.get("user_limit", 0),
                        reason="Casual Dogma setup",
                    )
                elif ctype == "forum":
                    await guild.create_forum(
                        name, category=category, overwrites=ov,
                        topic=ch.get("topic"), reason="Casual Dogma setup",
                    )
                else:
                    await guild.create_text_channel(
                        name, category=category, overwrites=ov,
                        topic=ch.get("topic"), reason="Casual Dogma setup",
                    )
                print(f"    + {ctype}: {name}")
            except discord.Forbidden:
                print(f"    ! missing permission to create {name}")
            except discord.HTTPException as e:
                # Forum channels require the COMMUNITY feature enabled.
                print(f"    ! failed to create {name}: {e}")


def main():
    if not TOKEN or not GUILD_ID:
        sys.exit("ERROR: Set DISCORD_TOKEN and GUILD_ID in your .env file.")

    intents = discord.Intents.default()
    client = discord.Client(intents=intents)

    @client.event
    async def on_ready():
        print(f"Logged in as {client.user}")
        guild = client.get_guild(int(GUILD_ID))
        if guild is None:
            print("ERROR: Bot is not in the guild with that GUILD_ID.")
            await client.close()
            return

        print(f"\nSetting up: {guild.name}\n")
        print("Roles:")
        roles = await ensure_roles(guild)
        print("\nChannels:")
        await ensure_channels(guild, roles)
        print("\nDone. Casual Dogma is ready, Arisen.")
        await client.close()

    client.run(TOKEN)


if __name__ == "__main__":
    main()
