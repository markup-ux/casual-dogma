/*
 * Settings file for Server customization.
 * This file supports hotloading.
 */

// When enabled, items dropped by defeated enemies are sent straight to the player's
// inventory (and gold/coin pouches handled per the gold settings) instead of spawning a
// loot bag that must be picked up manually. Anything that does not fit (e.g. a full
// inventory) still drops as a bag for manual pickup.
bool EnableAutoLoot = true;

// Release all story-gated content without requiring MSQ progress. Characters can still
// play the main story normally; this only removes the requirement to finish it first.
bool UnlockAllStoryContent = true;

// Strip quest-completion prerequisites from order conditions so any quest can be taken
// without clearing the quests that normally chain in front of it.
bool MakeAllQuestsOptional = true;

// Grant all five jewelry slots on login (no orb unlocks required).
bool GrantMaxJewelrySlots = true;

// Repop dungeon trash in-place for exploration farming (non-boss, non-quest).
bool EnableDungeonMobRepop = true;

// Repop wait = clamp(baseWait × multiplier, min, max). Base wait = syncLevel × secondsPerSync (or mobLv÷2).
double ExplorationMobRepopWaitMultiplier = 0.35;
uint ExplorationMobRepopSecondsPerSyncLevel = 2;
uint ExplorationMobRepopMinWaitSeconds = 5;
uint ExplorationMobRepopMaxWaitSeconds = 90;
uint ExplorationMobRepopBaseSeconds = 0;

// Roll crafted performance gear (weapons/armor/jewelry/lanterns) from enemy kills.
// Disabled by default — Casual Dogma uses EnableExplorationProgressionDrops instead.
bool EnableWildCraftedGearDrops = false;

// Roll dress/cosmetic equipment (clothing, overwear, ensembles) from enemy kills.
bool EnableWildCosmeticDrops = false;

// Personal finished +0 gear and regional craft materials from exploration kills.
bool EnableExplorationProgressionDrops = true;

// Per-kill gear drop chance (0.0–1.0). Empty-slot bonus and pity apply on top.
double ExplorationGearDropChance = 0.35;

// Guarantee a gear roll after this many kills in a row with no exploration gear (0 = off).
uint ExplorationGearPityKillThreshold = 3;

// Extra gear chance per empty performance equip slot (weapons/armor/jewelry/lantern).
double ExplorationEmptySlotDropChanceBonus = 0.05;

// Chance that a gear roll tries weapons first before armor or jewelry (0.0–1.0).
double ExplorationWeaponFirstRollChance = 0.55;

// Keep recoverable HP (gray bar) at 100% until JobLevelMax so Priests can fully heal during leveling.
bool DisableRecoverableHpLossBelowMaxLevel = true;

// New characters start with teleports to level-sync areas (recommended level 1-20) and nearby field hubs.
// Higher-level destinations stay locked until discovered in the world.
bool UnlockStarterLevelSyncWarps = true;

// Use recommended level (not real level) for EXP spread penalties in level-sync dungeons.
bool LevelSyncUseDisplayLevelForExp = true;

// Per-use Revival Power and Golden Gemstone recharge timers.
uint RevivalRechargeIntervalMinutes = 45;
byte RevivalPowerMax = 3;
uint RevivalRechargeGoldenGemstoneAmount = 1;
