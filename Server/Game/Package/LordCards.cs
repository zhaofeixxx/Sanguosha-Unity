﻿using CommonClass;
using CommonClass.Game;
using SanguoshaServer.Game;
using System.Collections.Generic;

namespace SanguoshaServer.Package
{
    public class LordCards : CardPackage
    {
        public LordCards() : base("LordCards")
        {
            cards = new List<FunctionCard>
            {
                new DragonPhoenix(),
                new PeaceSpell(),
                new LuminouSpearl()
            };
            skills = new List<Skill>
            {
                new DragonPhoenixSkill(),
                new DragonPhoenixSkill2(),
                new PeaceSpellSkill(),
                new PeaceSpellSkillMaxCards(),
                new LuminouSpearlSkill(),
                new ZhihengVH()
            };
        }
    }
}

namespace SanguoshaServer.Game
{
    public class DragonPhoenix : Weapon
    {
        public DragonPhoenix() : base("DragonPhoenix", 2)
        {
        }
    }
    public class DragonPhoenixSkill : WeaponSkill
    {
        public DragonPhoenixSkill() : base("DragonPhoenix")
        {
            events.Add(TriggerEvent.TargetChosen);
        }
        public override TriggerStruct Triggerable(TriggerEvent triggerEvent, Room room, Player player, ref object data, Player ask_who)
        {
            CardUseStruct use = (CardUseStruct)data;
            FunctionCard fcard = use.Card != null ? Engine.GetFunctionCard(use.Card.Name) : null;
            if (base.Triggerable(player, room) && use.Card != null && fcard is Slash)
            {
                List<Player> targets = new List<Player>();
                foreach (Player to in use.To) {
                    if (RoomLogic.CanDiscard(room, to, to, "he"))
                        targets.Add(to);
                }
                if (targets.Count > 0)
                    return new TriggerStruct(Name, player, targets);
            }
            return new TriggerStruct();
        }
        public override TriggerStruct Cost(TriggerEvent triggerEvent, Room room, Player player, ref object data, Player ask_who, TriggerStruct info)
        {
            if (room.AskForSkillInvoke(ask_who, Name, player))
            {
                room.SetEmotion(ask_who, "dragonphoenix");
                return info;
            }
            return new TriggerStruct();
        }
        public override bool Effect(TriggerEvent triggerEvent, Room room, Player player, ref object data, Player ask_who, TriggerStruct info)
        {
            room.AskForDiscard(player, Name, 1, 1, false, true, "@dragonphoenix-discard");
            return false;
        }
    }

    public class DragonPhoenixSkill2 : WeaponSkill
    {
        public DragonPhoenixSkill2() : base("#DragonPhoenix")
        {
            events.Add(TriggerEvent.BuryVictim);
        }
        public override int GetPriority() => -4;
        public override bool Triggerable(Player target, Room room)
        {
            return target != null;
        }
        public override bool Effect(TriggerEvent triggerEvent, Room room, Player player, ref object data, Player ask_who, TriggerStruct info)
        {
            if (room.Setting.GameMode != "Hegemony") return false;

            Player dfowner = null;
            foreach (Player p in room.AlivePlayers) {
                if (p.HasWeapon("DragonPhoenix"))
                {
                    dfowner = p;
                    break;
                }
            }
            if (dfowner == null || !dfowner.HasShownOneGeneral() || dfowner.Role == "careerist")
                return false;

            DeathStruct death = (DeathStruct)data;
            DamageStruct damage = death.Damage;
            if (damage.From != null || damage.From != dfowner) return false;
            if (damage.Card == null) return false;
            FunctionCard fcard = Engine.GetFunctionCard(damage.Card.Name);
            if (!(fcard is Slash)) return false;

            List<string> kingdom_list = new List<string> { "wei", "shu", "wu", "qun" };
            kingdom_list.Remove(dfowner.Kingdom);
            kingdom_list.Add("careerist");

            int n = RoomLogic.GetPlayerNumWithSameKingdom(room, dfowner);           //could be canceled later
            foreach (string kingdom in kingdom_list) {
                int other_num = RoomLogic.GetPlayerNumWithSameKingdom(room, dfowner, kingdom);
                if (other_num > 0 && other_num < n)
                {
                    return false;
                }
            }

            List<string> generals = room.Generals;
            Shuffle.shuffle<string>(ref generals);
            foreach (string name in room.UsedGeneral)
            if (generals.Contains(name)) generals.Remove(name);
            List<string> avaliable_generals = new List<string>();
            foreach (string general in generals) {
                if (Engine.GetGeneral(general).Kingdom != dfowner.Kingdom)
                    continue;
                avaliable_generals.Add(general);
                if (avaliable_generals.Count >= 3) break;
            }

            if (avaliable_generals.Count == 0)
                return false;

            bool invoke = room.AskForSkillInvoke(dfowner, "DragonPhoenix", data) && room.AskForSkillInvoke(player, "DragonPhoenix", "revive");

            if (invoke)
            {
                room.SetEmotion(dfowner, "dragonphoenix");
                player.DuanChang = string.Empty;
                room.BroadcastProperty(player, "DuanChang");
                string to_change = room.AskForGeneral(player, avaliable_generals, null, true, "dpRevive", dfowner.Kingdom, true, true);

                if (!string.IsNullOrEmpty(to_change))
                {
                    room.DoDragonPhoenix(player, to_change, null, false, dfowner.Kingdom, true, "h");
                    player.Hp = 2;
                    room.BroadcastProperty(player, "Hp");

                    room.SetPlayerChained(player, false, false);
                    player.FaceUp = true;
                    room.BroadcastProperty(player, "FaceUp");

                    LogMessage l = new LogMessage
                    {
                        Type = "#DragonPhoenixRevive",
                        From = player.Name,
                        Arg = to_change
                    };
                    room.SendLog(l);

                    room.DrawCards(player, 1);
                }
            }
            return false;
        }
    }
    public class PeaceSpell : Armor
    { 
        public PeaceSpell() : base("PeaceSpell") { }
        public override void OnUninstall(Room room, Player player, WrappedCard card)
        {
            if (player.Alive && RoomLogic.HasArmorEffect(room, player, Name, false))
                player.SetFlags("peacespell_throwing");
            base.OnUninstall(room, player, card);
        }
    }
    public class PeaceSpellSkill : ArmorSkill
    {
        public PeaceSpellSkill() : base("PeaceSpell")
        {
            events = new List<TriggerEvent> { TriggerEvent.DamageInflicted, TriggerEvent.CardsMoveOneTime, TriggerEvent.QuitDying };
            frequency = Frequency.Compulsory;
        }
        public override TriggerStruct Triggerable(TriggerEvent triggerEvent, Room room, Player player, ref object data, Player ask_who)
        {
            if (triggerEvent == TriggerEvent.DamageInflicted && data is DamageStruct damage)
            {
                if (base.Triggerable(player, room) && damage.Nature != DamageStruct.DamageNature.Normal)
                    return new TriggerStruct(Name, player);
            }
            else if (triggerEvent == TriggerEvent.CardsMoveOneTime && data is CardsMoveOneTimeStruct move && move.From != null
                && move.From_places.Contains(Player.Place.PlaceEquip) && move.From.HasFlag("peacespell_throwing"))
            {
                for (int i = 0; i < move.Card_ids.Count; i++)
                {
                    if (move.From_places[i] != Player.Place.PlaceEquip) continue;
                    WrappedCard card = Engine.GetRealCard(move.Card_ids[i]);
                    if (card.Name == Name)
                        return new TriggerStruct(Name, move.From);
                }
            }
            else if (triggerEvent == TriggerEvent.QuitDying && player.HasFlag("peacespell_dying") && player.Alive)
                return new TriggerStruct(Name, player);

            return new TriggerStruct();
        }
        public override TriggerStruct Cost(TriggerEvent triggerEvent, Room room, Player player, ref object data, Player ask_who, TriggerStruct info)
        {
            if (triggerEvent ==  TriggerEvent.CardsMoveOneTime || triggerEvent == TriggerEvent.QuitDying) return info;
            return base.Cost(room, ref data, info);
        }
        public override bool Effect(TriggerEvent triggerEvent, Room room, Player player, ref object data, Player ask_who, TriggerStruct info)
        {
            if (triggerEvent == TriggerEvent.DamageInflicted && data is DamageStruct damage)
            {
                LogMessage l = new LogMessage
                {
                    Type = "#PeaceSpellNatureDamage",
                    From = damage.From.Name,
                    To = new List<string> { damage.To.Name },
                    Arg = damage.Damage.ToString()
                };
                switch (damage.Nature)
                {
                    case DamageStruct.DamageNature.Normal: l.Arg2 = "normal_nature"; break;
                    case DamageStruct.DamageNature.Fire: l.Arg2 = "fire_nature"; break;
                    case DamageStruct.DamageNature.Thunder: l.Arg2 = "thunder_nature"; break;
                }

                room.SendLog(l);
                room.SetEmotion(damage.To, "peacespell");
                return true;
            }
            else if (triggerEvent == TriggerEvent.CardsMoveOneTime && data is CardsMoveOneTimeStruct move)
            {
                move.From.SetFlags("-peacespell_throwing");

                if (move.From.HasFlag("Global_Dying"))
                    move.From.SetFlags("peacespell_dying");
                else
                {
                    LogMessage l = new LogMessage
                    {
                        Type = "#PeaceSpellLost",
                        From = move.From.Name
                    };
                    room.SendLog(l);

                    room.LoseHp(move.From);
                    if (move.From.Alive)
                        room.DrawCards(move.From, 2, Name);
                }
            }
            else
            {
                player.SetFlags("-peacespell_dying");
                LogMessage l = new LogMessage
                {
                    Type = "#PeaceSpellLost",
                    From = player.Name
                };
                room.SendLog(l);

                room.LoseHp(player);
                if (player.Alive)
                    room.DrawCards(player, 2, Name);
            }
            return false;
        }
    }
    public class PeaceSpellSkillMaxCards : MaxCardsSkill
    {
        public PeaceSpellSkillMaxCards() : base("#PeaceSpell-max")
        {
        }
        public override int GetExtra(Room room, Player target)
        {
            if (!target.HasShownOneGeneral())
                return 0;

            List<Player> targets = room.AlivePlayers;

            Player ps_owner = null;
            foreach (Player p in targets) {
                if (RoomLogic.HasArmorEffect(room, p, "PeaceSpell"))
                {
                    ps_owner = p;
                    break;
                }
            }

            if (ps_owner == null)
                return 0;

            if (RoomLogic.IsFriendWith(room, target ,ps_owner))
                return RoomLogic.GetPlayerNumWithSameKingdom(room, ps_owner) + ps_owner.GetPile("heavenly_army").Count;

            return 0;
        }
        
    }
    public class LuminouSpearl : Treasure
    {
        public LuminouSpearl() : base("LuminouSpearl")
        {
        }
        public override void OnUninstall(Room room, Player player, WrappedCard card)
        {
            if (!player.GetAcquiredSkills().Contains("zhiheng") && (!player.OwnSkill("zhiheng")
                || ((!RoomLogic.InPlayerHeadSkills(player, "zhiheng") || !player.General1Showed)
                && (!RoomLogic.InPlayerDeputykills(player, "zhiheng") || !player.General2Showed))))
                player.ClearHistory("ZhihengCard");
            base.OnUninstall(room, player, card);
        }
    }
    public class LuminouSpearlSkill : ViewAsSkill
    {
        public LuminouSpearlSkill() : base("LuminouSpearl")
        {
        }
        public override bool ViewFilter(Room room, List<WrappedCard> selected, WrappedCard to_select, Player player)
        {
            return !RoomLogic.IsJilei(room, player, to_select) && selected.Count < player.MaxHp && to_select.Id != player.Treasure.Key;
        }
        public override WrappedCard ViewAs(Room room, List<WrappedCard> cards, Player player)
        {
            if (cards.Count == 0)
                return null;

            WrappedCard zhiheng_card = new WrappedCard("ZhihengCard");
            zhiheng_card.AddSubCards(cards);
            zhiheng_card.Skill = "zhiheng";
            return zhiheng_card;
        }
        public override bool IsEnabledAtPlay(Room room, Player player)
        {
            return RoomLogic.CanDiscard(room, player, player, "he") && !player.HasUsed("ZhihengCard")
                    && !player.GetAcquiredSkills().Contains("zhiheng") && (!player.OwnSkill("zhiheng")
                        || !((RoomLogic.InPlayerHeadSkills(player, "zhiheng") && player.General1Showed)
                        || (RoomLogic.InPlayerDeputykills(player, "zhiheng") && player.General2Showed)));
        }
    }

    public class ZhihengVH : ViewHasSkill
    {
        public ZhihengVH() : base("zhiheng-viewhas")
        {
            global = true;
            viewhas_skills.Add("zhiheng");
        }
        public override bool ViewHas(Room room, Player player, string skill_name) => player.HasTreasure("LuminouSpearl");
    }
}
