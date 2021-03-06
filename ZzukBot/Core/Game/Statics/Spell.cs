﻿using System;
using System.Collections.Generic;
using ZzukBot.Core.Constants;
using ZzukBot.Core.Mem;
using ZzukBot.Core.Mem.AntiWarden;
using ZzukBot.Core.Game.Objects;
using ZzukBot.Core.Utilities.Extensions;
using ZzukBot.Core.Utilities.Helpers;

namespace ZzukBot.Core.Game.Statics
{
    /// <summary>
    ///     Represents a spell manager
    /// </summary>
    public sealed class Spell
    {
        private const int ShootBowId = 2480;
        private const int ShootCrossbowId = 7919;
        private const int AutoShotId = 75;
        private const int WandId = 5019;
        private const int AttackId = 6603;

        private const string AutoShot = "Auto Shot";
        private const string BowShoot = "Shoot Bow";
        private const string CrossBowShoot = "Shoot Crossbow";
        private const string Wand = "Shoot";
        private static Lazy<Spell> _instance = new Lazy<Spell>(() => new Spell());

        /// <summary>
        ///     Holds blacklisted spells
        /// </summary>
        internal static Dictionary<string, SpellBlacklistItem> SpellBlacklist =
            new Dictionary<string, SpellBlacklistItem>();

        private readonly IReadOnlyDictionary<string, uint[]> PlayerSpells;


        private Spell()
        {
            var tmpPlayerSpells = new Dictionary<string, uint[]>();
            const uint currentPlayerSpellPtr = 0x00B700F0;
            uint index = 0;
            while (index < 1024)
            {
                var currentSpellId = (currentPlayerSpellPtr + 4 * index).ReadAs<uint>();
                if (currentSpellId == 0) break;
                var entryPtr = ((0x00C0D780 + 8).ReadAs<uint>() + currentSpellId * 4).ReadAs<uint>();

                var entrySpellId = entryPtr.ReadAs<uint>();
                var namePtr = (entryPtr + 0x1E0).ReadAs<uint>();
                var name = namePtr.ReadString();

#if DEBUG
                Console.WriteLine(entrySpellId + " " + name);
#endif

                if (tmpPlayerSpells.ContainsKey(name))
                {
                    var tmpIds = new List<uint>();
                    tmpIds.AddRange(tmpPlayerSpells[name]);
                    tmpIds.Add(entrySpellId);
                    tmpPlayerSpells[name] = tmpIds.ToArray();
                }
                else
                {
                    uint[] ranks = {entrySpellId};
                    tmpPlayerSpells.Add(name, ranks);
                }
                index += 1;
            }
            PlayerSpells = tmpPlayerSpells;
            ApplyActionbarHacks();
        }

        /// <summary>
        ///     Access to the characters spell manager
        /// </summary>
        /// <value>
        ///     The instance.
        /// </value>
        public static Spell Instance => _instance.Value;


        /// <summary>
        ///     Tells if we are shapeshifted
        /// </summary>
        public bool IsShapeShifted
        {
            get
            {
                var player = ObjectManager.Instance.Player;
                if (player == null) return false;
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (player.Class)
                {
                    case Enums.ClassId.Druid:
                        return (player.PlayerBytes & 0x000FEE00) != 0x0000EE00;
                }
                return false;
            }
        }

        private void ApplyActionbarHacks()
        {
            Hack tmpHack;
            if (PlayerSpells.ContainsKey(BowShoot))
            {
                if ((tmpHack = HookWardenMemScan.GetHack("BowShootPlace")) == null)
                {
                    tmpHack = new Hack((IntPtr)0xBC69CC, BitConverter.GetBytes(ShootBowId), "BowShootPlace");
                    tmpHack.Apply();
                    HookWardenMemScan.AddHack(tmpHack);
                }
                else
                {
                    tmpHack.Apply();
                }
            }

            if (PlayerSpells.ContainsKey(CrossBowShoot))
            {
                if ((tmpHack = HookWardenMemScan.GetHack("CrossbowShootPlace")) == null)
                {
                    tmpHack = new Hack((IntPtr)0xBC69D0, BitConverter.GetBytes(ShootCrossbowId), "CrossbowShootPlace");
                    tmpHack.Apply();
                    HookWardenMemScan.AddHack(tmpHack);
                }
                else
                {
                    tmpHack.Apply();
                }
            }


            if (PlayerSpells.ContainsKey(AutoShot))
            {
                if ((tmpHack = HookWardenMemScan.GetHack("AutoShotPlace")) == null)
                {
                    tmpHack = new Hack((IntPtr) 0xBC69D4, BitConverter.GetBytes(AutoShotId), "AutoShotPlace");
                    tmpHack.Apply();
                    HookWardenMemScan.AddHack(tmpHack);
                }
                else
                {
                    tmpHack.Apply();
                }
            }

            if (PlayerSpells.ContainsKey(Wand))
            {
                if ((tmpHack = HookWardenMemScan.GetHack("WandPlace")) == null)
                {
                    tmpHack = new Hack((IntPtr) 0x00BC69D8, BitConverter.GetBytes(WandId), "WandPlace");
                    tmpHack.Apply();
                    HookWardenMemScan.AddHack(tmpHack);
                }
                else
                {
                    tmpHack.Apply();
                }
            }

            if ((tmpHack = HookWardenMemScan.GetHack("AttackPlace")) == null)
            {
                tmpHack = new Hack((IntPtr)0xBC69DC, BitConverter.GetBytes(AttackId), "AttackPlace");
                tmpHack.Apply();
                HookWardenMemScan.AddHack(tmpHack);
            }
            else
            {
                tmpHack.Apply();
            }
        }

        /// <summary>
        ///     Updates the spellbook.
        /// </summary>
        public static void UpdateSpellbook()
        {
            _instance = new Lazy<Spell>(() => new Spell());
        }

        /// <summary>
        ///     Gets the name of a spell
        /// </summary>
        /// <param name="parId">The spell ID</param>
        /// <returns>The spell name</returns>
        public string GetName(int parId)
        {
            if (parId >= (0x00C0D780 + 0xC).ReadAs<uint>() ||
                parId <= 0)
                return "";
            var entryPtr = ((uint) ((0x00C0D780 + 8).ReadAs<uint>() + parId * 4)).ReadAs<uint>();
            var namePtr = (entryPtr + 0x1E0).ReadAs<uint>();
            return namePtr.ReadString();
        }

        /// <summary>
        /// Gets the id of a spell
        /// </summary>
        /// <param name="parName"></param>
        /// <param name="parRank"></param>
        /// <returns></returns>
        public int GetId(string parName, int parRank = -1)
        {
            if (!PlayerSpells.ContainsKey(parName)) return 0;
            var maxRank = PlayerSpells[parName].Length;
            if (parRank < 1 || parRank > maxRank)
                return (int) PlayerSpells[parName][maxRank - 1];
            return (int) PlayerSpells[parName][parRank - 1];
        }

        /// <summary>
        ///     Cast a spell by name
        /// </summary>
        /// <param name="parName">Name of the spell</param>
        /// <param name="parRank">Rank of the spell</param>
        public void Cast(string parName, int parRank = -1)
        {

            if (string.Equals(parName, BowShoot, StringComparison.InvariantCultureIgnoreCase))
            {
                if (!Wait.For2("20SpallSpam", 300, true)) return;
                const string useBow = "UseAction(20)";
                Lua.Instance.Execute(useBow);
                return;
            }
            if (string.Equals(parName, CrossBowShoot, StringComparison.InvariantCultureIgnoreCase))
            {
                if (!Wait.For2("21SpallSpam", 300, true)) return;
                const string useCrossbow = "UseAction(21)";
                Lua.Instance.Execute(useCrossbow);
                return;
            }
            

            var spellEscaped = parName.Replace("'", "\\'");
            var rankText = parRank != -1 ? $"(Rank {parRank})" : "";
            var spellCastString = $"CastSpellByName('{spellEscaped}{rankText}')";
            Lua.Instance.Execute(spellCastString);
        }

        /// <summary>
        ///     Trys to cast the specified spell and blacklist it for the usage by CastWait for a specified timeframe
        /// </summary>
        /// <param name="parName">Name of the spell</param>
        /// <param name="parBlacklistForMs">The time in ms from now where CastWait wont be able to try casting this spell again</param>
        /// <param name="parRank">Rank of the spell</param>
        public void CastWait(string parName, int parBlacklistForMs, int parRank = -1)
        {
            var currentCast = ObjectManager.Instance.Player.CastingAsName;
            //If we are casting do nothing
            if (currentCast != "")
                return;

            if (SpellBlacklist.ContainsKey(parName))
            {
                //If the spell is still blacklisted
                if (!SpellBlacklist[parName].IsReady)
                    return;

                //Update the spells blacklist time
                SpellBlacklist[parName].UpdateSpell(parBlacklistForMs);
                //Cast
                Cast(parName, parRank);
                return;
            }

            //Add the spell to the dictionary
            SpellBlacklist.Add(parName, new SpellBlacklistItem(parBlacklistForMs));
            //Cast
            Cast(parName, parRank);
        }

        /// <summary>
        ///     Casts an aoe spell at a position
        /// </summary>
        /// <param name="parName">Name of the spell</param>
        /// <param name="parPos">Position</param>
        /// <param name="parRank">Rank of the spell</param>
        public void CastAtPos(string parName, Location parPos, int parRank = -1)
        {
            Functions.CastAtPos(parName, parPos, parRank);
        }

        /// <summary>
        ///     Check if a spell is ready to be cast
        /// </summary>
        /// <param name="parName">Name of the spell</param>
        /// <returns></returns>
        public bool IsReady(string parName)
        {
            var id = GetId(parName);
            return id != 0 && Functions.IsSpellReady(id);
        }

        /// <summary>
        ///     Get the rank of the spell
        /// </summary>
        /// <param name="parSpell">The spell name</param>
        /// <returns></returns>
        public int GetSpellRank(string parSpell)
        {
            if (!PlayerSpells.ContainsKey(parSpell)) return 0;
            return PlayerSpells[parSpell].Length;
        }

        /*/// <summary>
        ///     Gets the cost of the specified spell using the spell's id
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public int Cost(int spell) => Lua.Instance.GetSpellCost(spell);

        /// <summary>
        ///     Gets the cost of the specified spell using the spell's name
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public int Cost(string spell) => Lua.Instance.GetSpellCost(spell);*/

        /// <summary>
        /// Determines if the global cooldown is active by checking if we can use
        /// at least one of 3 different spell schools.
        /// This is useful in that it can help to keep us from skipping spells
        /// that we want to cast.
        /// </summary>
        /// <param name="spellFromSchool1"></param>
        /// <param name="spellFromSchool2"></param>
        /// <param name="spellFromSchool3"></param>
        /// <returns></returns>
        public bool GCDReady(string spellFromSchool1, string spellFromSchool2, string spellFromSchool3)
        {
            return IsReady(spellFromSchool1) || IsReady(spellFromSchool2) || IsReady(spellFromSchool3);
        }

        /// <summary>
        /// Checks spell rank and if the spell is ready to be cast
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool CanCast(string spell)
        {
            return IsKnown(spell) && IsReady(spell);
        }

        /// <summary>
        /// The spell is known (the id is not equal to 0)
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool IsKnown(string spell)
        {
            return GetSpellRank(spell) != 0;
        }

        /// <summary>
        ///     Start auto attack
        /// </summary>
        public void Attack()
        {
            const string attack = "if IsCurrentAction('24') == nil then CastSpellByName('Attack') end";
            Lua.Instance.Execute(attack);
            if (Wait.For("AutoAttackTimer12", 1250))
            {
                var target = ObjectManager.Instance.Target;
                if (target == null) return;
                ObjectManager.Instance.Player.DisableCtm();
                ObjectManager.Instance.Player.RightClick(target);
                ObjectManager.Instance.Player.EnableCtm();
            }
        }

        /// <summary>
        ///     Stop auto attack
        /// </summary>
        public void StopAttack()
        {
            const string stopAttack = "if IsCurrentAction('24') ~= nil then CastSpellByName('Attack') end";
            Lua.Instance.Execute(stopAttack);
        }

        /// <summary>
        ///     Start wanding
        /// </summary>
        public void StartWand()
        {
            const string wandStart = "if IsAutoRepeatAction(23) == nil then CastSpellByName('Shoot') end";
            if (PlayerSpells.ContainsKey(Wand))
                Lua.Instance.Execute(wandStart);
        }

        /// <summary>
        ///     Start ranged attacking
        /// </summary>
        public void StartRangedAttack()
        {
            const string rangedAttackStart =
                "if IsAutoRepeatAction(22) == nil then CastSpellByName('Auto Shot') end";
            if (PlayerSpells.ContainsKey(AutoShot))
                Lua.Instance.Execute(rangedAttackStart);
        }

        /// <summary>
        ///     Stop ranged attacking
        /// </summary>
        public void StopRangedAttack()
        {
            const string rangedAttackStop = "if IsAutoRepeatAction(22) == 1 then CastSpellByName('Auto Shot') end";
            if (PlayerSpells.ContainsKey(AutoShot))
                Lua.Instance.Execute(rangedAttackStop);
        }

        /// <summary>
        ///     Stop wanding
        /// </summary>
        public void StopWand()
        {
            const string wandStop = "if IsAutoRepeatAction(23) == 1 then CastSpellByName('Shoot') end";
            if (PlayerSpells.ContainsKey(Wand))
                Lua.Instance.Execute(wandStop);
        }

        /// <summary>
        ///     Stops the current cast
        /// </summary>
        public void StopCasting()
        {
            Lua.Instance.Execute("SpellStopCasting()");
        }

        /// <summary>
        ///     Cancels all shapeshift forms
        /// </summary>
        public void CancelShapeshift()
        {
            var player = ObjectManager.Instance.Player;
            if (player == null) return;
            if (!IsShapeShifted) return;
            foreach (var x in player.Auras)
            {
                var tmp = GetName(x);
                if (tmp.Contains("Form"))
                    Lua.Instance.Execute("CastSpellByName('" + tmp + "')");
            }
        }

        /// <summary>
        ///     Object used for blacklist dictionary
        /// </summary>
        internal class SpellBlacklistItem
        {
            internal int BlacklistUntil;

            internal SpellBlacklistItem(int parBlacklistFor)
            {
                BlacklistUntil = parBlacklistFor + Environment.TickCount;
            }

            internal bool IsReady => Environment.TickCount > BlacklistUntil;

            internal void UpdateSpell(int parBlacklistFor)
            {
                BlacklistUntil = parBlacklistFor + Environment.TickCount;
            }
        }
    }
}