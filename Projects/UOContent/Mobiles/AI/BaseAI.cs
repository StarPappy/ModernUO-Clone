/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseAI.cs                                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

 /*DISCLAIMER: ***********************************************************
 * This is a development version of BaseAI.cs pending review and merge.  *
 * Use in production environments is not recommended until this file     *
 * has been properly reviewed and merged into the main repository.       *
 * Breaking changes and major refactoring may occur before final merge.  *
 * Contact Bohicatv in the official MUO discord for more information.    */


using System;
using System.Collections.Generic;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Quests.Necro;
using Server.Engines.Spawners;
using Server.Engines.Virtues;
using Server.Factions;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Spells;
using Server.Spells.Spellweaving;
using Server.Targets;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.Mobiles;

public enum AIType
{
    AI_Use_Default,
    AI_Melee,
    AI_Animal,
    AI_Archer,
    AI_Healer,
    AI_Vendor,
    AI_Mage,
    AI_Berserk,
    AI_Predator,
    AI_Thief
}

public enum ActionType
{
    Wander,
    Combat,
    Guard,
    Flee,
    Backoff,
    Interact
}

public abstract class BaseAI
{
    private static readonly SkillName[] m_KeywordTable =
    {
        SkillName.Parry,
        SkillName.Healing,
        SkillName.Hiding,
        SkillName.Stealing,
        SkillName.Alchemy,
        SkillName.AnimalLore,
        SkillName.ItemID,
        SkillName.ArmsLore,
        SkillName.Begging,
        SkillName.Blacksmith,
        SkillName.Fletching,
        SkillName.Peacemaking,
        SkillName.Camping,
        SkillName.Carpentry,
        SkillName.Cartography,
        SkillName.Cooking,
        SkillName.DetectHidden,
        SkillName.Discordance, // ?? > was Enticement i tihnk for pre-UOTD
        SkillName.EvalInt,
        SkillName.Fishing,
        SkillName.Provocation,
        SkillName.Lockpicking,
        SkillName.Magery,
        SkillName.MagicResist,
        SkillName.Tactics,
        SkillName.Snooping,
        SkillName.RemoveTrap,
        SkillName.Musicianship,
        SkillName.Poisoning,
        SkillName.Archery,
        SkillName.SpiritSpeak,
        SkillName.Tailoring,
        SkillName.AnimalTaming,
        SkillName.TasteID,
        SkillName.Tinkering,
        SkillName.Veterinary,
        SkillName.Forensics,
        SkillName.Herding,
        SkillName.Tracking,
        SkillName.Stealth,
        SkillName.Inscribe,
        SkillName.Swords,
        SkillName.Macing,
        SkillName.Fencing,
        SkillName.Wrestling,
        SkillName.Lumberjacking,
        SkillName.Mining,
        SkillName.Meditation
    };

    protected ActionType m_Action;
    public readonly BaseCreature m_Mobile;
    private long m_NextDetectHidden;
    private long m_NextStopGuard;
    protected PathFollower m_Path;
    public Timer m_Timer;

    public BaseAI(BaseCreature m)
    {
        m_Mobile = m;
        m_Timer = new AITimer(this);

        var activate = !m.PlayerRangeSensitive || 
            (!World.Loading && m.Map != null && m.Map != Map.Internal && 
             m.Map.GetSector(m.Location).Active);

        if (activate)
        {   
            m_Timer.Start();
        }

        Action = ActionType.Wander;
    }

    public ActionType Action
    {
        get => m_Action;
        set
        {
            if (m_Action != value)
            {
                m_Action = value;
                OnActionChanged();
            }
        }
    }

    public long NextMove { get; set; }

    // OSI, DetectHidden >= 50.0?
    public virtual bool CanDetectHidden => m_Mobile.Skills.DetectHidden.Value > 0;
    
    public virtual bool WasNamed(string speech)
    {
        var name = m_Mobile.Name;
        return !string.IsNullOrEmpty(name) && speech.InsensitiveStartsWith(name);
    }

    public virtual void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {   // OSI has 16 tiles for opening the context menu.
        if (!from.Alive || !m_Mobile.Controlled || !from.InRange(m_Mobile, 16))
        {
            return;
        }
    
        if (from == m_Mobile.ControlMaster)
        {
            AddControlMasterEntries(ref list);
        }
        else if (m_Mobile.IsPetFriend(from))
        {
            AddPetFriendEntries(ref list);
        }
    }
    
    private void AddControlMasterEntries(ref PooledRefList<ContextMenuEntry> list)
    {
        var isDeadPet = m_Mobile.IsDeadPet; // Ordered list to match OSI.

        // OSI has 14 tiles for context menu commands to be available.
        // On OSI, commands grey out. Here, they do when out of range which is better, imo.
        list.Add(new InternalEntry(3006111, 14, OrderType.Attack, !isDeadPet));     // Command: Kill
        list.Add(new InternalEntry(3006108, 14, OrderType.Follow, true));           // Command: Follow
        list.Add(new InternalEntry(3006107, 14, OrderType.Guard, !isDeadPet));      // Command: Guard
        list.Add(new InternalEntry(3006112, 14, OrderType.Stop, true));             // Command: Stop
        list.Add(new InternalEntry(3006114, 14, OrderType.Stay, true));             // Command: Stay

        // The rename feature is on OSI, but not here.
        // Have not coded rename entry command logic in BaseCreature.cs yet.
        // list.Add(new InternalEntry(3006116, 14, OrderType.Rename, true));          // Command: Rename

        if (m_Mobile.CanDrop)
        {
            list.Add(new InternalEntry(3006109, 14, OrderType.Drop, !isDeadPet));   // Command: Drop
        }
    
        if (!m_Mobile.Summoned && m_Mobile is not GrizzledMare)
        {
            list.Add(new InternalEntry(3006110, 14, OrderType.Friend, true));       // Add Friend
            list.Add(new InternalEntry(3006099, 14, OrderType.Unfriend, true));     // Remove Friend
            list.Add(new InternalEntry(3006113, 14, OrderType.Transfer, !isDeadPet)); // Transfer
        }

        // OSI has release command set to 13 tiles for no apparent reason.
        list.Add(new InternalEntry(3006118, 13, OrderType.Release, true));          // Release
    }
    
    private void AddPetFriendEntries(ref PooledRefList<ContextMenuEntry> list)
    {
        var isDeadPet = m_Mobile.IsDeadPet;
    
        list.Add(new InternalEntry(3006108, 14, OrderType.Follow, true));           // Command: Follow
        list.Add(new InternalEntry(3006112, 14, OrderType.Stop, !isDeadPet));       // Command: Stop
        list.Add(new InternalEntry(3006114, 14, OrderType.Stay, true));             // Command: Stay
    }

    public virtual void BeginPickTarget(Mobile from, OrderType order)
    {
        if (!IsValidTarget(from, order))
        {
            return;
        }
    
        if (from.Target == null)
        {
            SendOrderMessage(from, order);
            from.Target = new AIControlMobileTarget(this, order);
        }
        else if (from.Target is AIControlMobileTarget t && t.Order == order)
        {
            t.AddAI(this);
        }
    }
    
    private void SendOrderMessage(Mobile from, OrderType order)
    {
        switch (order)
        {
            case OrderType.Transfer:
                from.SendLocalizedMessage(502038);
                // 502038: Click on the person to transfer ownership to.
                break;
            case OrderType.Friend:
                from.SendLocalizedMessage(502020);
                // 502020: Click on the player whom you wish to make a co-owner.
                break;
            case OrderType.Unfriend:
                from.SendLocalizedMessage(1070948);
                // 1070948: Click on the player whom you wish to remove as a co-owner.
                break;
        }
    }

    public virtual void OnAggressiveAction(Mobile aggressor)
    {
        if (aggressor.Hidden)
        {
            return;
        }
    
        var currentCombat = m_Mobile.Combatant;
    
        if (currentCombat == null || currentCombat == aggressor)
        {
            return;
        }
    
        var currentDistance = m_Mobile.GetDistanceToSqrt(currentCombat);
        var aggressorDistance = m_Mobile.GetDistanceToSqrt(aggressor);
    
        if (aggressorDistance < currentDistance)
        {
            m_Mobile.Combatant = aggressor;
        }
    }

    public virtual void EndPickTarget(Mobile from, Mobile target, OrderType order)
    {
        if (!IsValidTarget(from, order) || 
            (order == OrderType.Attack && !CanAttackTarget(from, target)))
        {
            return;
        }

        if (m_Mobile.CheckControlChance(from))
        {
            m_Mobile.ControlTarget = target;
            m_Mobile.ControlOrder = order;
        }
    }

    private bool IsValidTarget(Mobile from, OrderType order)
    {
        if (m_Mobile.Deleted || !m_Mobile.Controlled || !from.InRange(m_Mobile, 14)
            || from.Map != m_Mobile.Map || !from.CheckAlive())
        {
            return false;
        }

        var isOwner = from == m_Mobile.ControlMaster;
        var isFriend = !isOwner && m_Mobile.IsPetFriend(from);

        if (!isOwner && !isFriend)
        {
            return false;
        }

        if (isFriend && order != OrderType.Follow && order != OrderType.Stay && order != OrderType.Stop)
        {
            return false;
        }
    
        return true;
    }

    private bool CanAttackTarget(Mobile from, Mobile target)
    {
        if (target is BaseCreature creature && creature.IsScaryToPets && m_Mobile.IsScaredOfScaryThings)
        {
            m_Mobile.SayTo(from, "Your pet refuses to attack this creature!");
            return false;
        }
    
        if (SolenHelper.CheckRedFriendship(from) &&
                target is RedSolenInfiltratorQueen or RedSolenInfiltratorWarrior
                    or RedSolenQueen or RedSolenWarrior or RedSolenWorker ||
            SolenHelper.CheckBlackFriendship(from) &&
                target is BlackSolenInfiltratorQueen or BlackSolenInfiltratorWarrior
                    or BlackSolenQueen or BlackSolenWarrior or BlackSolenWorker)
        {
            from.SendAsciiMessage("You cannot command your pet to attack a creature you are protected from.");
            return false;
        }

        if (target is BaseFactionGuard)
        {
            m_Mobile.SayTo(from, "Your pet refuses to attack the guard.");
            return false;
        }

        return true;
    }

    // currently broken, needs to be fixed.
    // on speech by players do not trigger orders for pets.
    // i don't want to fix it the way runuo does it.
    public virtual bool HandlesOnSpeech(Mobile from)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        if (from.Alive && m_Mobile.Controlled && m_Mobile.Commandable &&
            (from == m_Mobile.ControlMaster || m_Mobile.IsPetFriend(from)))
        {
            return true;
        }

        return from.Alive && from.InRange(m_Mobile.Location, 3) && m_Mobile.IsHumanInTown();
    }

    public virtual void OnSpeech(SpeechEventArgs e)
    {
        if (!e.Mobile.Alive || !e.Mobile.InRange(m_Mobile.Location, 3) || !m_Mobile.IsHumanInTown())
        {
            return;
        }
    
        if (m_Mobile.Combatant != null)
        {
            m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482); 
            // 501482: I am too busy fighting to deal with thee!
            return;
        }
    
        var actions = new Dictionary<int, Action>
        {
            { 0x9D, () => HandleMove() }, // *move*
            { 0x9E, () => HandleTime() }, // *time*
            { 0x6C, () => HandleTraining(e.Mobile) } // *train*
        };
    
        foreach (var (keyword, action) in actions)
        {
            if (e.HasKeyword(keyword) && WasNamed(e.Speech))
            {
                action();
                return;
            }
        }
    
        HandleSkillTraining(e);
    
        if (m_Mobile.Controlled && m_Mobile.Commandable)
        {
            HandleCommands(e);
        }
        else if (e.Mobile.AccessLevel >= AccessLevel.GameMaster)
        {
            HandleGMCommands(e);
        }
    }
    
    private void HandleMove()
    {
        m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501516);
        WalkRandomInHome(2, 2, 1);
    }
    
    private void HandleTime()
    {
        Clock.GetTime(m_Mobile, out var generalNumber, out _);
        m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, generalNumber);
    }
    
    private void HandleTraining(Mobile from)
    {
        var foundSomething = false;
        var ourSkills = m_Mobile.Skills;
    
        for (var i = 0; i < ourSkills.Length; ++i)
        {
            if (TryTeachSkill(ourSkills[i], from, i, ref foundSomething))
            {
                continue;
            }
        }
    
        if (!foundSomething)
        {
            m_Mobile.Say(501505); 
            // 501505: Alas, I cannot teach thee anything.
        }
    }
    
    private bool TryTeachSkill(Skill skill, Mobile student, int index, ref bool foundSomething)
    {
        if (skill.Base < 60.0 || !m_Mobile.CheckTeach(skill.SkillName, student))
        {
            return false;
        }
    
        var toTeach = Math.Min(skill.Base / 3.0, 42.0);
    
        if (toTeach <= student.Skills[skill.SkillName].Base)
        {
            return false;
        }
    
        var number = 1043059 + index;
        if (number > 1043107)
        {
            return false;
        }
    
        if (!foundSomething)
        {
            m_Mobile.Say(1043058); // I can train the following:
            foundSomething = true;
        }
    
        m_Mobile.Say(number);
        return true;
    }

    // I broke this during refactoring or it never worked right.
    // player mobile speech comamnds are not registered by control pet.
    // probably missing something in the OnSpeech events or methods.
    private void HandleCommands(SpeechEventArgs e)
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Listening...");
        }
    
        var isOwner = e.Mobile == m_Mobile.ControlMaster;
        var isFriend = !isOwner && m_Mobile.IsPetFriend(e.Mobile);
    
        if (e.Mobile.Alive && (isOwner || isFriend))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("It is from my Master.");
            }
    
            var commands = new Dictionary<int, Action>
            {
                { 0x164, () => HandleComeCommand(e.Mobile, isOwner) },          // all come
                { 0x165, () => BeginPickTarget(e.Mobile, OrderType.Follow) },   // all follow
                { 0x166, () => HandleGuardCommand(e.Mobile, isOwner) },         // all guard
                { 0x16B, () => HandleGuardCommand(e.Mobile, isOwner) },         // all guard me
                { 0x167, () => HandleSimpleCommand(e.Mobile, OrderType.Stop) }, // all stop
                { 0x168, () => HandleAttackCommand(e.Mobile, isOwner) },        // all kill
                { 0x169, () => HandleAttackCommand(e.Mobile, isOwner) },        // all attack
                { 0x16C, () => HandleSimpleCommand(e.Mobile, OrderType.Follow, e.Mobile) }, // all follow me
                { 0x170, () => HandleSimpleCommand(e.Mobile, OrderType.Stay) }  // all stay
            };
    
            foreach (var keyword in e.Keywords)
            {
                if (commands.TryGetValue(keyword, out var action))
                {
                    action.Invoke();
                    return;
                }
            }
    
            HandleSpecificCommands(e, isOwner, e.Speech);
        }
    }
    
    private void HandleSpecificCommands(SpeechEventArgs e, bool isOwner, string speech)
    {
        var specificCommands = new Dictionary<int, Action>
        {
            { 0x155, () => HandleComeCommand(e.Mobile, isOwner) },              // *come
            { 0x156, () => HandleDropCommand(e.Mobile, isOwner, speech) },      // *drop
            { 0x15A, () => BeginPickTarget(e.Mobile, OrderType.Follow) },       // *follow
            { 0x15B, () => HandleFriendCommand(e.Mobile, isOwner, speech) },    // *friend
            { 0x15C, () => HandleGuardCommand(e.Mobile, isOwner) },             // *guard
            { 0x15D, () => HandleAttackCommand(e.Mobile, isOwner) },            // *kill
            { 0x15E, () => HandleAttackCommand(e.Mobile, isOwner) },            // *attack
            { 0x161, () => HandleSimpleCommand(e.Mobile, OrderType.Stop) },     // *stop
            { 0x163, () => HandleSimpleCommand(e.Mobile, OrderType.Follow, e.Mobile) }, // *follow me
            { 0x16D, () => HandleReleaseCommand(e.Mobile, isOwner, speech) },   // *release
            { 0x16E, () => HandleTransferCommand(e.Mobile, isOwner, speech) },  // *transfer
            { 0x16F, () => HandleSimpleCommand(e.Mobile, OrderType.Stay) }      // *stay
        };
    
        foreach (var keyword in e.Keywords)
        {
            if (specificCommands.TryGetValue(keyword, out var action))
            {
                action.Invoke();
                return;
            }
        }
    }
    
    private void HandleComeCommand(Mobile from, bool isOwner)
    {
        if (isOwner && m_Mobile.CheckControlChance(from))
        {
            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.Come;
        }
    }
    
    private void HandleGuardCommand(Mobile from, bool isOwner)
    {
        if (isOwner && m_Mobile.CheckControlChance(from))
        {
            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.Guard;
        }
    }
    
    private void HandleSimpleCommand(Mobile from, OrderType order, Mobile target = null)
    {
        if (m_Mobile.CheckControlChance(from))
        {
            m_Mobile.ControlTarget = target;
            m_Mobile.ControlOrder = order;
        }
    }
    
    private void HandleAttackCommand(Mobile from, bool isOwner)
    {
        if (isOwner)
        {
            BeginPickTarget(from, OrderType.Attack);
        }
    }
    
    private void HandleDropCommand(Mobile from, bool isOwner, string speech)
    {
        if (isOwner && !m_Mobile.IsDeadPet && !m_Mobile.Summoned && WasNamed(speech) && m_Mobile.CheckControlChance(from))
        {
            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.Drop;
        }
    }
    
    private void HandleFriendCommand(Mobile from, bool isOwner, string speech)
    {
        if (isOwner && WasNamed(speech) && m_Mobile.CheckControlChance(from))
        {
            if (m_Mobile.Summoned || m_Mobile is GrizzledMare)
            {
                from.SendLocalizedMessage(1005481);
                // 1005481: Summoned creatures are loyal only to their summoners.
            }
            else if (from.HasTrade)
            {
                from.SendLocalizedMessage(1070947);
                // 1070947: You cannot friend a pet with a trade pending
            }
            else
            {
                BeginPickTarget(from, OrderType.Friend);
            }
        }
    }
    
    private void HandleReleaseCommand(Mobile from, bool isOwner, string speech)
    {
        if (isOwner && WasNamed(speech) && m_Mobile.CheckControlChance(from))
        {
            if (!m_Mobile.Summoned)
            {
                from.SendGump(new ConfirmReleaseGump(from, m_Mobile));
            }
            else
            {
                m_Mobile.ControlTarget = null;
                m_Mobile.ControlOrder = OrderType.Release;
            }
        }
    }
    
    private void HandleTransferCommand(Mobile from, bool isOwner, string speech)
    {
        if (isOwner && !m_Mobile.IsDeadPet && WasNamed(speech) && m_Mobile.CheckControlChance(from))
        {
            if (m_Mobile.Summoned || m_Mobile is GrizzledMare)
            {
                from.SendLocalizedMessage(1005487);
                // 1005487: You cannot transfer ownership of a summoned creature.
            }
            else if (from.HasTrade)
            {
                from.SendLocalizedMessage(1010507);
                // 1010507: You cannot transfer a pet with a trade pending
            }
            else
            {
                BeginPickTarget(from, OrderType.Transfer);
            }
        }
    }
    
    private void HandleGMCommands(SpeechEventArgs e)
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("It is from a GM");
        }
    
        if (m_Mobile.FindMyName(e.Speech, true))
        {
            foreach (var word in e.Speech.Split(' '))
            {
                if (word.InsensitiveEquals("obey"))
                {
                    m_Mobile.SetControlMaster(e.Mobile);
    
                    if (m_Mobile.Summoned)
                    {
                        m_Mobile.SummonMaster = e.Mobile;
                    }
    
                    return;
                }
            }
        }
    }

    public virtual bool Think()
    {
        if (m_Mobile.Deleted)
        {
            return false;
        }

        /*if (CheckLeashed())
        {
            return true;
        }*/

        if (CheckFlee())
        {
            return true;
        }
    
        var actionHandlers = new Dictionary<ActionType, Func<bool>>
        {
            { ActionType.Wander, () => { m_Mobile.OnActionWander(); return DoActionWander(); } },
            { ActionType.Combat, () => { m_Mobile.OnActionCombat(); return DoActionCombat(); } },
            { ActionType.Guard, () => { m_Mobile.OnActionGuard(); return DoActionGuard(); } },
            { ActionType.Flee, () => { m_Mobile.OnActionFlee(); return DoActionFlee(); } },
            { ActionType.Interact, () => { m_Mobile.OnActionInteract(); return DoActionInteract(); } },
            { ActionType.Backoff, () => { m_Mobile.OnActionBackoff(); return DoActionBackoff(); } }
        };
    
        return actionHandlers.TryGetValue(Action, out var handler) && handler();
    }

    public virtual void OnActionChanged()
    {
        var actionHandlers = new Dictionary<ActionType, Action>
        {
            { ActionType.Wander, HandleWanderAction },
            { ActionType.Combat, HandleCombatAction },
            { ActionType.Guard, HandleGuardAction },
            { ActionType.Flee, HandleFleeAction },
            { ActionType.Interact, HandleInteractAction },
            { ActionType.Backoff, HandleBackoffAction }
        };
    
        if (actionHandlers.TryGetValue(Action, out var handler))
        {
            handler();
        }
    }
    
    private void HandleWanderAction()
    {
        m_Mobile.Warmode = false;
        m_Mobile.Combatant = null;
        m_Mobile.FocusMob = null;
        m_Mobile.SetCurrentSpeedToPassive();
    }
    
    private void HandleCombatAction()
    {
        m_Mobile.Warmode = true;
        m_Mobile.FocusMob = null;
        m_Mobile.SetCurrentSpeedToActive();
    }
    
    private void HandleGuardAction()
    {
        m_Mobile.Warmode = true;
        m_Mobile.FocusMob = null;
        m_Mobile.Combatant = null;
        m_Mobile.SetCurrentSpeedToActive();
    }
    
    private void HandleFleeAction()
    {
        m_Mobile.Warmode = true;
        m_Mobile.FocusMob = null;
        m_Mobile.SetCurrentSpeedToActive();
    }
    
    private void HandleInteractAction()
    {
        m_Mobile.Warmode = false;
        m_Mobile.SetCurrentSpeedToPassive();
    }
    
    private void HandleBackoffAction()
    {
        m_Mobile.Warmode = false;
        m_Mobile.SetCurrentSpeedToPassive();
    }

    public virtual bool OnAtWayPoint() => true;

    public virtual bool DoActionWander()
    {
        if (CheckHerding())
        {
            DebugSay("I am being herded.");
        }
        else if (m_Mobile.CurrentWayPoint != null)
        {
            HandleWayPoint();
        }
        else if (m_Mobile.IsAnimatedDead)
        {
            FollowMaster();
        }
        else if (CanMoveNow(out _) && !m_Mobile.CheckIdle())
        {
            WalkRandomInHome(2, 2, 1);
        }
    
        UpdateCombatantDirection();
    
        return true;
    }
    
    private void HandleWayPoint()
    {
        var point = m_Mobile.CurrentWayPoint;
    
        if ((point.X != m_Mobile.Location.X || point.Y != m_Mobile.Location.Y) && point.Map == m_Mobile.Map &&
            point.Parent == null && !point.Deleted)
        {
            DebugSay("I will move towards my waypoint.");
            DoMove(m_Mobile.GetDirectionTo(point));
        }
        else if (OnAtWayPoint())
        {
            DebugSay("I will go to the next waypoint");
            m_Mobile.CurrentWayPoint = point.NextPoint;
    
            if (point.NextPoint?.Deleted == true)
            {
                m_Mobile.CurrentWayPoint = point.NextPoint = point.NextPoint.NextPoint;
            }
        }
    }
    
    private void FollowMaster()
    {
        var master = m_Mobile.SummonMaster;
    
        if (master != null && master.Map == m_Mobile.Map && master.InRange(m_Mobile, m_Mobile.RangePerception))
        {
            MoveTo(master, false, 1);
        }
        else
        {
            WalkRandomInHome(2, 2, 1);
        }
    }
    
    private void UpdateCombatantDirection()
    {
        if (m_Mobile.Combatant?.Deleted == false && m_Mobile.Combatant.Alive && !m_Mobile.Combatant.IsDeadBondedPet)
        {
            m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
        }
    }

    public virtual bool DoActionCombat()
    {
        if (Core.AOS && CheckHerding())
        {
            DebugSay("I am being herded.");
            return true;
        }
    
        var combatant = m_Mobile.Combatant;
    
        if (!IsValidCombatantOne(combatant))
        {
            DebugSay("My combatant is missing.");
            Action = ActionType.Wander;
            return true;
        }
    
        m_Mobile.Direction = m_Mobile.GetDirectionTo(combatant);
    
        if (m_Mobile.TriggerAbility(MonsterAbilityTrigger.CombatAction, combatant))
        {
            DebugSay($"I used my abilities on: {combatant.Name}.");
        }
    
        return true;
    }
    
    private bool IsValidCombatantOne(Mobile combatant)
    {
        return combatant != null && !combatant.Deleted && combatant.Map == m_Mobile.Map
            && combatant.Alive && !combatant.IsDeadBondedPet;
    }

    public virtual bool DoActionGuard()
    {
        if (m_Mobile.IsDeadPet)
        {
            return true;
        }

        var controlMaster = m_Mobile.ControlMaster;
        if (controlMaster?.Deleted != false)
        {
            return true;
        }

        var combatant = FindGuardTarget(controlMaster);
        if (IsValidCombatant(combatant))
        {
            GuardFromTarget(combatant);
        }
        else
        {
            GuardNothing(controlMaster);
        }

        return true;
    }

    private Mobile FindGuardTarget(Mobile controlMaster)
    {
        var combatant = m_Mobile.Combatant;
        var aggressors = controlMaster.Aggressors;

        if (aggressors.Count <= 0)
        {
            return combatant;
        }

        for (var i = 0; i < aggressors.Count; ++i)
        {
            var info = aggressors[i];
            var attacker = info.Attacker;

            if (IsValidAggressor(attacker) && IsCloserThanCurrent(attacker, controlMaster, combatant))
            {
                combatant = attacker;
            }
        }

        if (combatant != null)
        {
            DebugSay("Master is under attack. Assisting...");
        }

        return combatant;
    }

    private bool IsValidAggressor(Mobile attacker) =>
        attacker?.Deleted == false && attacker.GetDistanceToSqrt(m_Mobile) <= m_Mobile.RangePerception;

    private bool IsCloserThanCurrent(Mobile attacker, Mobile controlMaster, Mobile currentCombatant) =>
        currentCombatant == null || 
        attacker.GetDistanceToSqrt(controlMaster) < currentCombatant.GetDistanceToSqrt(controlMaster);

    public virtual bool DoActionFlee()
    {
        var from = m_Mobile.FocusMob;
    
        if (!IsValidFocusMob(from))
        {
            DebugSay("Focused target lost.");
            Action = ActionType.Guard;
            return true;
        }
    
        if (WalkMobileRange(from, 1, true, m_Mobile.RangePerception * 2, m_Mobile.RangePerception * 1))
        {
            DebugSay("I have fled.");
            Action = ActionType.Guard;
            return true;
        }
    
        DebugSay("I am fleeing!");
        return true;
    }
    
    private bool IsValidFocusMob(Mobile focusMob)
    {
        return focusMob != null && !focusMob.Deleted && focusMob.Map == m_Mobile.Map;
    }
    
    private void DebugSay(string message)
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay(message);
        }
    }

    public virtual bool DoActionInteract() => true;
    
    public virtual bool DoActionBackoff() => true;
    
    public virtual bool Obey() =>
        !m_Mobile.Deleted && m_Mobile.ControlOrder switch
        {
            OrderType.None     => DoOrderNone(),
            OrderType.Come     => DoOrderCome(),
            OrderType.Drop     => DoOrderDrop(),
            OrderType.Friend   => DoOrderFriend(),
            OrderType.Unfriend => DoOrderUnfriend(),
            OrderType.Guard    => DoOrderGuard(),
            OrderType.Attack   => DoOrderAttack(),
            OrderType.Release  => DoOrderRelease(),
            OrderType.Stay     => DoOrderStay(),
            OrderType.Stop     => DoOrderStop(),
            OrderType.Follow   => DoOrderFollow(),
            OrderType.Transfer => DoOrderTransfer(),
            _                  => false
        };
    
    public virtual void OnCurrentOrderChanged()
    {
        if (m_Mobile.Deleted || m_Mobile.ControlMaster?.Deleted != false)
        {
            return;
        }
    
        m_Mobile.ControlMaster.RevealingAction();
    
        switch (m_Mobile.ControlOrder)
        {
            case OrderType.None:
            case OrderType.Come:
            case OrderType.Drop:
            case OrderType.Release:
            case OrderType.Stay:
            case OrderType.Stop:
            case OrderType.Transfer:
                HandlePassiveOrder();
                break;
    
            case OrderType.Friend:
            case OrderType.Unfriend:
                break;
    
            case OrderType.Guard:
                HandleGuardOrder();
                break;
    
            case OrderType.Attack:
                HandleAttackOrder();
                break;
    
            case OrderType.Follow:
                HandleFollowOrder();
                break;
        }
    }
    
    private void HandlePassiveOrder()
    {
        m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
        m_Mobile.PlaySound(m_Mobile.GetIdleSound());
        m_Mobile.Warmode = false;
        m_Mobile.Combatant = null;
        m_Mobile.FocusMob = null;
    }
    
    private void HandleGuardOrder()
    {
        if (m_Mobile.ControlMaster == null || !m_Mobile.ControlMaster.Alive)
        {
            return;
        }

        if (!m_Mobile.InRange(m_Mobile.ControlMaster, 1))
        {
            m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
            MoveTo(m_Mobile.ControlMaster, true, 1);
            return;
        }
        
        m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
        m_Mobile.Warmode = true;
        m_Mobile.FocusMob = null;
        m_Mobile.Home = m_Mobile.ControlMaster.Location;
        m_Mobile.RangeHome = 3;
        m_Mobile.ControlOrder = OrderType.Stay;
        m_Mobile.PlaySound(m_Mobile.GetAttackSound());
        m_Mobile.ControlMaster.SendLocalizedMessage(1049671, m_Mobile.Name);
        // 1049671: ~1_NAME~ is now guarding you.
    }
    
    private void HandleAttackOrder()
    {
        if (m_Mobile.ControlMaster == null || !m_Mobile.ControlMaster.Alive)
        {
            return;
        }

        m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
        m_Mobile.PlaySound(m_Mobile.GetIdleSound());
        m_Mobile.Warmode = true;
        m_Mobile.FocusMob = null;
    }
    
    private void HandleFollowOrder()
    {
        if (m_Mobile.ControlMaster == null || !m_Mobile.ControlMaster.Alive)
        {
            return;
        }

        m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
        m_Mobile.PlaySound(m_Mobile.GetIdleSound());
        m_Mobile.Warmode = false;
        m_Mobile.Combatant = null;
        m_Mobile.FocusMob = null;
    }

    public virtual bool DoOrderNone()
    {
        DebugSay("I currently have no orders.");
        WalkRandomInHome(3, 2, 1);
        UpdateCombatantState();
        return true;
    }

    private void UpdateCombatantState()
    {
        if (IsValidCombatant(m_Mobile.Combatant))
        {
            m_Mobile.Warmode = true;
            m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
        }
        else
        {
            m_Mobile.Warmode = false;
        }
    }

    private bool IsValidCombatant(Mobile combatant) =>
        combatant != null && !combatant.Deleted && combatant.Alive && !combatant.IsDeadBondedPet;

    public virtual bool DoOrderCome()
    {
        if (m_Mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }

        var currentDistance = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.ControlMaster);

        if (currentDistance > m_Mobile.RangePerception)
        {
            HandleLostMaster();
        }
        else
        {
            HandleComeOrder(currentDistance);
        }

        return true;
    }

    private void HandleLostMaster()
    {
        DebugSay("Master is missing. Staying put.");
        m_Mobile.ControlTarget = null;
        m_Mobile.ControlOrder = OrderType.None;
    }

    private void HandleComeOrder(int currentDistance)
    {
        DebugSay("I am ordered to come.");
        var shouldRun = currentDistance > 5;

        if (WalkMobileRange(m_Mobile.ControlMaster, 1, shouldRun, 0, 1))
        {
            UpdateCombatantState();
        }
    }

    public virtual bool DoOrderDrop()
    {
        if (m_Mobile.IsDeadPet || !m_Mobile.CanDrop)
        {
            return true;
        }

        DebugSay("I am ordered to drop my items.");
        DropItems();

        m_Mobile.ControlTarget = null;
        m_Mobile.ControlOrder = OrderType.None;

        return true;
    }

    private void DropItems()
    {
        var pack = m_Mobile.Backpack;
    
        if (pack == null)
        {
            return;
        }
    
        var items = pack.Items;
    
        for (var i = items.Count - 1; i >= 0; --i)
        {
            if (i < items.Count)
            {
                items[i].MoveToWorld(m_Mobile.Location, m_Mobile.Map);
            }
        }
    }

    public virtual bool CheckHerding()
    {
        var target = m_Mobile.TargetLocation;
    
        if (target == null)
        {
            return false; // Creature is not being herded
        }
    
        var distance = m_Mobile.GetDistanceToSqrt(target);
    
        if (distance >= 1 && distance <= 15)
        {
            DoMove(m_Mobile.GetDirectionTo(target));
            return true;
        }
    
        if (distance < 1 && IsSpecialHerdingCase(target))
        {
            HandleSpecialHerdingCase();
        }
    
        m_Mobile.TargetLocation = null;
        return false; // At the target or too far away
    }
    
    private bool IsSpecialHerdingCase(IPoint2D target)
    {
        return target.X == 1076 && target.Y == 450 && m_Mobile is HordeMinionFamiliar;
    }
    
    private void HandleSpecialHerdingCase()
    {
        if (m_Mobile.ControlMaster is PlayerMobile pm && pm.Quest is DarkTidesQuest qs)
        {
            var obj = qs.FindObjective<FetchAbraxusScrollObjective>();
    
            if (obj?.Completed == false)
            {
                m_Mobile.AddToBackpack(new ScrollOfAbraxus());
                obj.Complete();
            }
        }
    }

    public virtual bool DoOrderFollow()
    {
        if (CheckHerding())
        {
            DebugSay("I am being herded.");
            return true;
        }
    
        if (m_Mobile.ControlTarget?.Deleted == false && m_Mobile.ControlTarget != m_Mobile)
        {
            HandleFollowTarget();
        }
        else
        {
            DebugSay("I have no one to follow.");
            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.None;
        }
    
        return true;
    }
    
    private void HandleFollowTarget()
    {
        var currentDistance = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.ControlTarget);
    
        if (currentDistance > m_Mobile.RangePerception)
        {
            DebugSay("Follow target missing. Staying put.");
            UpdateCombatantState();
        }
        else
        {
            DebugSay($"I am ordered to follow: {m_Mobile.ControlTarget.Name}");
            var shouldRun = currentDistance > 5;
    
            if (WalkMobileRange(m_Mobile.ControlTarget, 1, shouldRun, 0, 1))
            {
                UpdateCombatantState();
            }
        }
    }
    
    private void DebugSayMessage(string message)
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay(message);
        }
    }

    public virtual bool DoOrderFriend()
    {
        var from = m_Mobile.ControlMaster;
        var to = m_Mobile.ControlTarget;
    
        if (IsInvalidFriendRequest(from, to))
        {
            m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039); // *looks confused*
        }
        else if (IsYoungPlayerMismatch(from, to))
        {
            // messages are now handled inside of IsYoungPlayerMismatch method
        }
        else if (from.CanBeBeneficial(to, true))
        {
            HandleFriendRequest(from, to);
        }
    
        m_Mobile.ControlTarget = from;
        m_Mobile.ControlOrder = OrderType.Follow;
    
        return true;
    }
    
    private bool IsInvalidFriendRequest(Mobile from, Mobile to) =>
        from?.Deleted != false || to?.Deleted != false || from == to || !to.Player;
    
    private bool IsYoungPlayerMismatch(Mobile from, Mobile to)
    {
        var youngFrom = from is PlayerMobile mobile && mobile.Young;
        var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;
    
        if (youngFrom && !youngTo)
        {
            from.SendLocalizedMessage(502040);
            // 502040: As a young player, you may not friend pets to older players.
            return true;
        }
        else if (!youngFrom && youngTo)
        {
            from.SendLocalizedMessage(502041);
            // 502041: As an older player, you may not friend pets to young players.
            return true;
        }
    
        return false;
    }
    
    private void HandleFriendRequest(Mobile from, Mobile to)
    {
        if (from.HasTrade)
        {
            from.SendLocalizedMessage(1070947);
            // 1070947: You cannot friend a pet with a trade pending
        }
        else if (to.HasTrade)
        {
            to.SendLocalizedMessage(1070947);
            // 1070947: You cannot friend a pet with a trade pending
        }
        else if (m_Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1049691);
            // 1049691: That person is already a friend.
        }
        else if (!m_Mobile.AllowNewPetFriend)
        {
            from.SendLocalizedMessage(1005482);
            // 1005482: Your pet does not seem to be interested in making new friends right now.
        }
        else
        {
            from.SendLocalizedMessage(1049676, $"{m_Mobile.Name}\t{to.Name}");
            to.SendLocalizedMessage(1043246, $"{from.Name}\t{m_Mobile.Name}");
            // 1049676: ~1_NAME~ will now accept movement commands from ~2_NAME~.
            // 1043246: ~1_NAME~ has granted you the ability to give orders to their pet ~2_PET_NAME~.
    
            m_Mobile.AddPetFriend(to);
    
            m_Mobile.ControlTarget = to;
            m_Mobile.ControlOrder = OrderType.Follow;
        }
    }

    public virtual bool DoOrderUnfriend()
    {
        var from = m_Mobile.ControlMaster;
        var to = m_Mobile.ControlTarget;
    
        if (IsInvalidUnfriendRequest(from, to))
        {
            m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 502039);
            // 502039: *looks confused*
        }
        else if (!m_Mobile.IsPetFriend(to))
        {
            from.SendLocalizedMessage(1070953);
            // 1070953: That person is not a friend.
        }
        else
        {
            HandleUnfriendRequest(from, to);
        }
    
        m_Mobile.ControlTarget = from;
        m_Mobile.ControlOrder = OrderType.Follow;
    
        return true;
    }
    
    private bool IsInvalidUnfriendRequest(Mobile from, Mobile to) =>
        from?.Deleted != false || to?.Deleted != false || from == to || !to.Player;
    
    private void HandleUnfriendRequest(Mobile from, Mobile to)
    {
        from.SendLocalizedMessage(1070951, $"{m_Mobile.Name}\t{to.Name}");
        to.SendLocalizedMessage(1070952, $"{from.Name}\t{m_Mobile.Name}");
        // 1070951: ~1_NAME~ will no longer accept movement commands from ~2_NAME~.
        // 1070952: ~1_NAME~ has no longer granted you the ability to give orders to their pet ~2_PET_NAME~.
    
        m_Mobile.RemovePetFriend(to);
    }

    public virtual bool DoOrderGuard()
    {
        if (m_Mobile.IsDeadPet)
        {
            return true;
        }
    
        var controlMaster = m_Mobile.ControlMaster;
    
        if (controlMaster?.Deleted != false)
        {
            return true;
        }
    
        var combatant = FindGuardTarget(controlMaster);
    
        if (IsValidCombatant(combatant))
        {
            GuardFromTarget(combatant);
        }
        else
        {
            GuardNothing(controlMaster);
        }
    
        return true;
    }
    
    private void GuardFromTarget(Mobile combatant)
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Guarding target.");
        }
    
        m_Mobile.Combatant = combatant;
        m_Mobile.FocusMob = combatant;
        Action = ActionType.Combat;
    
        // used to update spell caster combat states
        Think();
    }
    
    private void GuardNothing(Mobile controlMaster)
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("There is nothing to guard.");
        }
    
        m_Mobile.Warmode = false;
        
        if (Core.AOS)
        {
            m_Mobile.CurrentSpeed = 0.1;
        }
    
        WalkMobileRange(controlMaster, 1, false, 0, 1);
    }

    public virtual bool DoOrderAttack()
    {
        if (m_Mobile.IsDeadPet)
        {
            return true;
        }
    
        if (IsInvalidControlTarget(m_Mobile.ControlTarget))
        {
            HandleInvalidControlTarget();
        }
        else
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Attacking target...");
            }
    
            Think();
        }
    
        return true;
    }
    
    private bool IsInvalidControlTarget(Mobile target)
    {
        return target?.Deleted != false || target.Map != m_Mobile.Map || !target.Alive || target.IsDeadBondedPet;
    }
    
    private void HandleInvalidControlTarget()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Target missing. Either dead, hidden, or out of range.");
        }
    
        if (Core.AOS)
        {
            m_Mobile.ControlTarget = m_Mobile.ControlMaster;
            m_Mobile.ControlOrder = OrderType.Follow;
        }
        else
        {
            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.None;
        }
    
        if (m_Mobile.FightMode is FightMode.Closest or FightMode.Aggressor)
        {
            FindNewCombatant();
        }
    }
    
    private void FindNewCombatant()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Searching for combatant...");
        }

        if (ShouldFindNewTarget())
        {
            var newCombatant = ScanForTargets(m_Mobile.RangePerception);

            if (newCombatant != null)
            {
                SetNewTarget(newCombatant);
            }
            else
            {
                StandDownAndWanderHome();
            }
        }
    }

    private bool ShouldFindNewTarget()
    {
        var combatant = m_Mobile.Combatant;
        return m_Mobile.Warmode && (
            combatant == null ||
            combatant.Deleted ||
            !combatant.Alive ||
            combatant.IsDeadBondedPet ||
            !m_Mobile.InRange(combatant, m_Mobile.RangePerception)
        );
    }

    private Mobile ScanForTargets(int range)
    {
        Mobile bestTarget = null;
        var bestScore = 0.0;

        foreach (var m in m_Mobile.Map.GetMobilesInRange(m_Mobile.Location, range))
        {
            if (!IsValidCombatTarget(m))
            {
                continue;
            }

            var score = m_Mobile.GetFightModeRanking(m, m_Mobile.FightMode, false);
            
            if (score > bestScore)
            {
                bestTarget = m;
                bestScore = score;
            }
        }

        return bestTarget;
    }

    private void SetNewTarget(Mobile target)
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay($"New target acquired: {target.Name}");
        }

        m_Mobile.ControlTarget = target;
        m_Mobile.ControlOrder = OrderType.Attack;
        m_Mobile.Combatant = target;
        Think();
    }

    private void StandDownAndWanderHome()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("No targets found. Standing down and wandering home.");
        }
        
        m_Mobile.Warmode = false;
        m_Mobile.ControlTarget = null;
        m_Mobile.ControlOrder = OrderType.None;
        m_Mobile.Combatant = null;
        
        Action = ActionType.Wander;

        if (m_Mobile.Home != Point3D.Zero && !m_Mobile.InRange(m_Mobile.Home, m_Mobile.RangeHome))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Moving back to home location.");
            }

            DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
        }
        else
        {
            WalkRandomInHome(2, 2, 1);
        }
    }

    private bool IsValidCombatTarget(Mobile m)
    {
        return m != null && 
               m != m_Mobile && 
               m.Alive && 
               !m.IsDeadBondedPet && 
               m_Mobile.CanSee(m) && 
               m.Combatant == m_Mobile && 
               m_Mobile.InLOS(m);
    }

    public virtual bool DoOrderRelease()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I have been released to the wild.");
        }
    
        m_Mobile.PlaySound(m_Mobile.GetAngerSound());
    
        ReleaseControl();
        ResetBonding();
        SetHomeLocation();
        HandleDeletion();
    
        return true;
    }
    
    private void ReleaseControl()
    {
        m_Mobile.SetControlMaster(null);
        m_Mobile.SummonMaster = null;
    }
    
    private void ResetBonding()
    {
        m_Mobile.BondingBegin = DateTime.MinValue;
        m_Mobile.OwnerAbandonTime = DateTime.MinValue;
        m_Mobile.IsBonded = false;
    }
    
    private void SetHomeLocation()
    {
        var spawner = m_Mobile.Spawner;
    
        if (spawner != null && spawner.HomeLocation != Point3D.Zero)
        {
            m_Mobile.Home = spawner.HomeLocation;
            m_Mobile.RangeHome = spawner.HomeRange;
        }
    }
    
    private void HandleDeletion()
    {
        if (m_Mobile.DeleteOnRelease || m_Mobile.IsDeadPet)
        {
            m_Mobile.Delete();
        }
        else
        {
            m_Mobile.BeginDeleteTimer();
    
            if (m_Mobile.CanDrop)
            {
                m_Mobile.DropBackpack();
            }
        }
    }

    public virtual bool DoOrderStay()
    {
        if (m_Mobile.Debug)
        {
            if (CheckHerding())
            {
                m_Mobile.DebugSay("I am being herded.");
            }
            else
            {
                m_Mobile.DebugSay("I have been ordered to stay.");
            }
        }
    
        HandleStayOrder();
    
        return true;
    }
    
    private void HandleStayOrder()
    {
        if (m_Mobile.ControlOrder != OrderType.None)
        {
            m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.ControlMaster);
            m_Mobile.Home = m_Mobile.Location;
            m_Mobile.ControlTarget = null;
            m_Mobile.Warmode = false;
            m_Mobile.Combatant = null;
            m_Mobile.FocusMob = null;
            m_Mobile.ControlOrder = OrderType.None;
        }
    }

    public virtual bool DoOrderStop()
    {
        if (m_Mobile.ControlMaster?.Deleted != false)
        {
            return true;
        }
    
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I have been ordered to stop.");
        }
    
        HandleStopOrder();
    
        return true;
    }
    
    private void HandleStopOrder()
    {
        if (m_Mobile.ControlOrder != OrderType.Stop)
        {
            m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.ControlMaster);
            m_Mobile.Home = m_Mobile.Location;
            m_Mobile.ControlTarget = null;
            m_Mobile.Warmode = false;
            m_Mobile.Combatant = null;
            m_Mobile.FocusMob = null;
            m_Mobile.ControlOrder = OrderType.Stop;
        }
    }

    public virtual bool DoOrderTransfer()
    {
        if (m_Mobile.IsDeadPet)
        {
            return true;
        }

        var from = m_Mobile.ControlMaster;
        var to = m_Mobile.ControlTarget;

        if (IsValidTransferRequest(from, to))
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"Beginning transfer with {to.Name}");
            }

            if (IsYoungPlayerMismatchForTransfer(from, to))
            {
                // messages are now handled inside of IsYoungPlayerMismatchForTransfer method
            }
            else if (!m_Mobile.CanBeControlledBy(to))
            {
                SendTransferRefusalMessages(from, to, 1043248, 1043249);
                // 1043248: The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                // 1043249: The pet will not accept you as a master because it does not trust you.~3_BLANK~
            }
            else if (!m_Mobile.CanBeControlledBy(from))
            {
                SendTransferRefusalMessages(from, to, 1043250, 1043251);
                // 1043250: The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                // 1043251: The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
            }
            else if (IsInCombatState())
            {
                from.SendLocalizedMessage(1042561);
                // 1042561: You may not transfer a pet that has recently been in combat.
                to.SendLocalizedMessage(1042562);
                // 1042562: The pet may not be transferred to you because it has recently been in combat.
            }
            else
            {
                HandleTransferOrder(from, to);
            }

            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.Stay;

            return true;
        }

        return false;
    }

    private bool IsInCombatState()
    {
        return m_Mobile.Combatant != null || 
               m_Mobile.Aggressors.Count > 0 || 
               m_Mobile.Aggressed.Count > 0 ||
               Core.TickCount < m_Mobile.NextCombatTime;
    }
    
    private bool IsValidTransferRequest(Mobile from, Mobile to)
    {
        return from?.Deleted == false && to?.Deleted == false && from != to && to.Player;
    }
    
    private bool IsYoungPlayerMismatchForTransfer(Mobile from, Mobile to)
    {
        var youngFrom = from is PlayerMobile mobile && mobile.Young;
        var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;
    
        if (youngFrom && !youngTo)
        {
            from.SendLocalizedMessage(502051);
            // 502051: As a young player, you may not transfer pets to older players.
            return true;
        }
        else if (!youngFrom && youngTo)
        {
            from.SendLocalizedMessage(502052);
            // 502052: As an older player, you may not transfer pets to young players.
            return true;
        }
    
        return false;
    }
    
    private void SendTransferRefusalMessages(Mobile from, Mobile to, int fromMessage, int toMessage)
    {
        var args = $"{to.Name}\t{from.Name}\t ";
        from.SendLocalizedMessage(fromMessage, args);
        to.SendLocalizedMessage(toMessage, args);
    }

    // I broke this during refactoring or it never worked right.
    // Its just setup as auto transfer of pet without trade window, for now.
    // Needs rework of secure trade window logic to be implemented.
    // But players can just trade the gold before the transfer.
    private void HandleTransferOrder(Mobile from, Mobile to)
    {
        var fromState = from.NetState;
        var toState = to.NetState;

        if (fromState == null || toState == null)
        {
            return;
        }

        if (from.HasTrade || to.HasTrade)
        {
            from.SendLocalizedMessage(1010507);
            // 1010507: You cannot transfer a pet with a trade pending
            to.SendLocalizedMessage(1010507);
            // 1010507: You cannot transfer a pet with a trade pending
            return;
        }
        else
        {
            var container = fromState.AddTrade(toState);
            container.DropItem(new TransferItem(m_Mobile));
        }

        m_Mobile.ControlMaster = to;
        m_Mobile.ControlTarget = null;

        if (m_Mobile.ControlOrder != OrderType.Stay)
        {
            m_Mobile.ControlOrder = OrderType.Stay;
        }

        m_Mobile.BondingBegin = DateTime.MinValue;
        m_Mobile.OwnerAbandonTime = DateTime.MinValue;
        m_Mobile.IsBonded = false;
    }

    public virtual bool DoBardPacified()
    {
        if (Core.Now < m_Mobile.BardEndTime)
        {
            HandlePacifiedState();
        }
        else
        {
            HandleEndOfPacification();
        }
    
        return true;
    }
    
    private void HandlePacifiedState()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I am pacified. Can not fight.");
        }
    
        m_Mobile.Combatant = null;
        m_Mobile.Warmode = false;
    }
    
    private void HandleEndOfPacification()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("I am free from pacification.");
        }
    
        m_Mobile.BardPacified = false;
    }

    public virtual bool DoBardProvoked()
    {
        if (Core.Now >= m_Mobile.BardEndTime && IsProvokerLost())
        {
            HandleLostProvoker();
        }
        else if (IsProvokeTargetLost())
        {
            HandleLostProvokeTarget();
        }
        else
        {
            HandleProvokeCombat();
        }
    
        return true;
    }
    
    private bool IsProvokerLost()
    {
        return m_Mobile.BardMaster?.Deleted != false ||
               m_Mobile.BardMaster.Map != m_Mobile.Map ||
               m_Mobile.GetDistanceToSqrt(m_Mobile.BardMaster) > m_Mobile.RangePerception;
    }
    
    private bool IsProvokeTargetLost()
    {
        return m_Mobile.BardTarget?.Deleted != false ||
               m_Mobile.BardTarget.Map != m_Mobile.Map ||
               m_Mobile.GetDistanceToSqrt(m_Mobile.BardTarget) > m_Mobile.RangePerception;
    }
    
    private void HandleLostProvoker()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Provoker missing.");
        }
    
        ResetBardState();
    }
    
    private void HandleLostProvokeTarget()
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Provoke target missing.");
        }
    
        ResetBardState();
    }
    
    private void HandleProvokeCombat()
    {
        m_Mobile.Combatant = m_Mobile.BardTarget;
        m_Action = ActionType.Combat;
    
        m_Mobile.OnThink();
        
        Think();
    }

    private void ResetBardState()
    {
        m_Mobile.BardProvoked = false;
        m_Mobile.BardMaster = null;
        m_Mobile.BardTarget = null;
        m_Mobile.Combatant = null;
        m_Mobile.Warmode = false;
    }

    public virtual void WalkRandom(int chanceToNotMove, int chanceToDir, int steps)
    {
        if (ShouldNotMove(chanceToNotMove))
        {
            return;
        }
    
        for (var i = 0; i < steps; i++)
        {
            if (Utility.Random(1 + chanceToNotMove) == 0)
            {
                var direction = GetRandomDirection(chanceToDir);
                DoMove(direction);
            }
        }
    }
    
    private bool ShouldNotMove(int chanceToNotMove)
    {
        return m_Mobile.Deleted || m_Mobile.DisallowAllMoves || chanceToNotMove <= 0;
    }
    
    private Direction GetRandomDirection(int chanceToDir)
    {
        var randomMove = Utility.Random(8 * (chanceToDir + 1));
        return randomMove < 8 ? (Direction)randomMove : m_Mobile.Direction;
    }

    public static double TransformMoveDelay(BaseCreature bc)
    {
        if (bc == null)
        {
            return 0.1;
        }

        var moveSpeed = bc.CurrentSpeed;

        if (!bc.IsDeadPet && (bc.ReduceSpeedWithDamage || bc.IsSubdued))
        {
            int stats, statsMax;
            if (Core.HS)
            {
                stats = bc.Stam;
                statsMax = bc.StamMax;
            }
            else
            {
                stats = bc.Hits;
                statsMax = bc.HitsMax;
            }

            var offset = statsMax <= 0 ? 0.4 : Math.Max(0, stats) / (double)statsMax;
            moveSpeed += (1.0 - offset) * 0.4;
        }

        return Math.Max(0.1, moveSpeed);
    }
    
    private static double CalculateDamageSpeedOffset(BaseCreature bc)
    {
        int stats, statsMax;
        if (Core.HS)
        {
            stats = bc.Stam;
            statsMax = bc.StamMax;
        }
        else
        {
            stats = bc.Hits;
            statsMax = bc.HitsMax;
        }
    
        var offset = statsMax <= 0 ? 1.0 : Math.Max(0, stats) / (double)statsMax;
    
        return (1.0 - offset) * 0.8;
    }

    private const int MovementTimingTolerance = 50; // this needs to match ai interval

    public bool CanMoveNow(out long delay)
    {
        delay = NextMove - Core.TickCount;
        return delay <= MovementTimingTolerance;
    }
    
    public virtual bool DoMove(Direction d, bool badStateOk = false)
    {
        var res = DoMoveImpl(d, badStateOk);
        return IsMoveSuccessful(res, badStateOk);
    }
    
    private bool IsMoveSuccessful(MoveResult res, bool badStateOk)
    {
        return res is MoveResult.Success or MoveResult.SuccessAutoTurn ||(badStateOk && res == MoveResult.BadState);
    }

    public virtual MoveResult DoMoveImpl(Direction d, bool badStateOk)
    {
        if (IsInBadState())
        {
            return MoveResult.BadState;
        }
        
        if (!CanMoveNow(out var timeUntilMove))
        {
            if (badStateOk)
            {
                AIMovementTimerPool.GetTimer(TimeSpan.FromMilliseconds(timeUntilMove), this, d).Start();
            }
            return MoveResult.BadState;
        }

        if ((m_Mobile.Direction & Direction.Mask) != (d & Direction.Mask))
        {
            m_Mobile.Direction = d;
        }

        m_Mobile.Pushing = false;
        var mobDirection = m_Mobile.Direction;

        var moveResult = TryMove(d);

        if (moveResult)
        {
            HandleCombatDelay();
            return MoveResult.Success;
        }

        if ((mobDirection & Direction.Mask) != (d & Direction.Mask))
        {
            m_Mobile.Direction = d;
            return MoveResult.SuccessAutoTurn;
        }

        return HandleBlockedMovement(d, mobDirection);
    }

    private void HandleCombatDelay()  
    {
        if (m_Mobile == null || !ShouldApplyCombatDelay(m_Mobile))
        {
            return;
        }

        var maxWait = Utility.RandomMinMax(50, 100);
        var remaining = Math.Max(0, m_Mobile.NextCombatTime - Core.TickCount);

        if (remaining < maxWait)
        {
            m_Mobile.NextCombatTime = Core.TickCount + maxWait;
        }
    }

    private bool ShouldApplyCombatDelay(Mobile mobile) => 
        mobile.Warmode && mobile.Combatant != null;

    private bool TryMove(Direction d)
    {
        MoveImpl.IgnoreMovableImpassables = m_Mobile.CanMoveOverObstacles && !m_Mobile.CanDestroyObstacles;
        var result = m_Mobile.Move(d);
        MoveImpl.IgnoreMovableImpassables = false;
        return result;
    }

    private bool IsInBadState() =>
        m_Mobile == null || m_Mobile.Deleted || m_Mobile.Frozen || m_Mobile.Paralyzed ||
        m_Mobile.Spell?.IsCasting == true || m_Mobile.DisallowAllMoves;

    private MoveResult HandleBlockedMovement(Direction d, Direction mobDirection)
    {
        var wasPushing = m_Mobile.Pushing;

        if ((m_Mobile.CanOpenDoors || m_Mobile.CanDestroyObstacles) && !TryClearObstacles(d))
        {
            return MoveResult.Success;
        }

        return TryAlternateMovement(wasPushing);
    }

    private MoveResult TryAlternateMovement(bool wasPushing)
    {
        var offset = Utility.RandomDouble() < 0.4 ? 1 : -1;

        for (var i = 0; i < 2; ++i)
        {
            m_Mobile.TurnInternal(offset);

            if (m_Mobile.Move(m_Mobile.Direction))
            {
                return MoveResult.SuccessAutoTurn;
            }
        }

        return wasPushing ? MoveResult.BadState : MoveResult.Blocked;
    }

    private bool TryClearObstacles(Direction d)
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("My movement is blocked. Trying to push through.");
        }

        var map = m_Mobile.Map;
        if (map == null)
        {
            return true;
        }

        var (x, y) = GetOffsetLocation(d);
        using var queue = PooledRefQueue<Item>.Create();
        var destroyables = GatherObstacles(x, y, queue);

        if (destroyables > 0)
        {
            Effects.PlaySound(new Point3D(x, y, m_Mobile.Z), m_Mobile.Map, 0x3B3);
        }

        return ProcessObstacles(queue, d);
    }

    private (int x, int y) GetOffsetLocation(Direction d)
    {
        var x = m_Mobile.X;
        var y = m_Mobile.Y;
        Movement.Movement.Offset(d, ref x, ref y);
        return (x, y);
    }

    private int GatherObstacles(int x, int y, PooledRefQueue<Item> queue)
    {
        var destroyables = 0;

        foreach (var item in m_Mobile.Map.GetItemsInRange(new Point2D(x, y), 1))
        {
            if (IsValidDoor(item, x, y) || IsValidDestroyableItem(item))
            {
                queue.Enqueue(item);
                if (item is not BaseDoor)
                {
                    destroyables++;
                }
            }
        }

        return destroyables;
    }

    private bool IsValidDoor(Item item, int x, int y) =>
        m_Mobile.CanOpenDoors && item is BaseDoor door &&
        door.Z + door.ItemData.Height > m_Mobile.Z && m_Mobile.Z + 16 > door.Z &&
        door.X == x && door.Y == y && (!door.Locked || !door.UseLocks());

    private bool IsValidDestroyableItem(Item item) =>
        m_Mobile.CanDestroyObstacles && item.Movable && item.ItemData.Impassable &&
        item.Z + item.ItemData.Height > m_Mobile.Z && m_Mobile.Z + 16 > item.Z &&
        m_Mobile.InRange(item.GetWorldLocation(), 1);

    private bool ProcessObstacles(PooledRefQueue<Item> queue, Direction d)
    {
        if (queue.Count == 0)
        {
            return true;
        }

        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            ProcessObstacle(item, queue);
        }

        return !m_Mobile.Move(d);
    }

    private void ProcessObstacle(Item item, PooledRefQueue<Item> queue)
    {
        if (item is BaseDoor door)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("Opening the door.");
            }
            door.Use(m_Mobile);
        }
        else
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay($"Destroying item: {item.GetType().Name}");
            }

            if (item is Container cont)
            {
                ProcessContainer(cont, queue);
                cont.Destroy();
            }
            else
            {
                item.Delete();
            }
        }
    }

    private void ProcessContainer(Container cont, PooledRefQueue<Item> queue)
    {
        for (var i = 0; i < cont.Items.Count; ++i)
        {
            var check = cont.Items[i];
            if (check.Movable && check.ItemData.Impassable && cont.Z + check.ItemData.Height > m_Mobile.Z)
            {
                queue.Enqueue(check);
            }
        }
    }

    public virtual void WalkRandomInHome(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
        {
            return;
        }

        if (m_Mobile.Home == Point3D.Zero)
        {
            HandleNoHome(iChanceToNotMove, iChanceToDir, iSteps);
        }
        else
        {
            HandleHomeMovement(iChanceToNotMove, iChanceToDir, iSteps);
        }
    }

    private void HandleNoHome(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (m_Mobile.Spawner is RegionSpawner rs)
        {
            Region region = rs.SpawnRegion;

            if (m_Mobile.Region.AcceptsSpawnsFrom(region))
            {
                m_Mobile.WalkRegion = region;
                WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
                m_Mobile.WalkRegion = null;
            }
            else if (region.GoLocation != Point3D.Zero && Utility.RandomBool())
            {
                DoMove(m_Mobile.GetDirectionTo(region.GoLocation));
            }
            else
            {
                WalkRandom(iChanceToNotMove, iChanceToDir, 1);
            }
        }
        else
        {
            WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
        }
    }

    private void HandleHomeMovement(int iChanceToNotMove, int iChanceToDir, int iSteps)
    {
        if (m_Mobile.RangeHome == 0)
        {
            if (m_Mobile.Location != m_Mobile.Home)
            {
                DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
            }
            return;
        }

        for (var i = 0; i < iSteps; i++)
        {
            var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.Home);

            if (iCurrDist > m_Mobile.RangeHome)
            {
                DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
            }
            else if (iCurrDist < m_Mobile.RangeHome * 2 / 3 || Utility.Random(10) <= 5)
            {
                WalkRandom(iChanceToNotMove, iChanceToDir, 1);
            }
            else
            {
                DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
            }
        }
    }

    public virtual bool CheckFlee()
    {
        if (!m_Mobile.CheckFlee())
        {
            return false;
        }
    
        var combatant = m_Mobile.Combatant;
    
        if (combatant == null)
        {
            WalkRandom(1, 2, 1);
        }
        else
        {
            FleeFromCombatant(combatant);
        }
    
        return true;
    }
    
    private void FleeFromCombatant(Mobile combatant)
    {
        var direction = combatant.GetDirectionTo(m_Mobile);
        direction = (Direction)((int)direction + Utility.RandomMinMax(-1, +1));
    
        m_Mobile.Direction = direction;
        m_Mobile.Move(direction);
    }

    public virtual void OnTeleported()
    {
        if (m_Mobile.Debug && m_Path != null)
        {
            m_Mobile.DebugSay("Teleported; recalculating path.");
        }

        m_Path?.ForceRepath();
    }

    public virtual bool MoveTo(Mobile m, bool run, int range)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || m?.Deleted != false)
        {
            return false;
        }

        if (m_Mobile.InRange(m, range))
        {
            m_Path = null;
            return true;
        }

        if (m_Path == null && DoMove(m_Mobile.GetDirectionTo(m), true))
        {
            return true;
        }

        if (m_Path?.Goal != m)
        {
            m_Path = new PathFollower(m_Mobile, m) { Mover = DoMoveImpl };
        }

        if (m_Path.Follow(run, 1))
        {
            m_Path = null;
            return true;
        }

        return false;
    }

    /*
     *  Walk at range distance from mobile
     *
     *  iSteps : Number of steps
     *  bRun   : Do we run
     *  iWantDistMin : The minimum distance we want to be
     *  iWantDistMax : The maximum distance we want to be
     *
     */
    public virtual bool WalkMobileRange(Mobile m, int iSteps, bool run, int iWantDistMin, int iWantDistMax)
    {
        if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || m == null)
        {
            return false;
        }

        for (var i = 0; i < iSteps; i++)
        {
            var iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m);

            if (iCurrDist < iWantDistMin || iCurrDist > iWantDistMax)
            {
                if (!MoveTowardsOrAwayFrom(m, run, iCurrDist, iWantDistMax))
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        return m_Mobile.GetDistanceToSqrt(m) is var dist && 
               dist >= iWantDistMin && dist <= iWantDistMax;
    }

    private bool MoveTowardsOrAwayFrom(Mobile m, bool run, int iCurrDist, int iWantDistMax)
    {
        var needCloser = iCurrDist > iWantDistMax;

        if (needCloser && m_Path?.Goal == m)
        {
            if (m_Path.Follow(run, 1))
            {
                m_Path = null;
                return true;
            }
        }
        else
        {
            var dirTo = needCloser ? m_Mobile.GetDirectionTo(m, run) : m.GetDirectionTo(m_Mobile, run);

            if (DoMove(dirTo, true))
            {
                m_Path = null;
                return true;
            }

            if (needCloser)
            {
                m_Path = new PathFollower(m_Mobile, m) { Mover = DoMoveImpl };
                if (m_Path.Follow(run, 1))
                {
                    m_Path = null;
                    return true;
                }
            }
        }

        return false;
    }

    /*
     * Here we check to acquire a target from our surrounding
     *
     *  iRange : The range
     *  acqType : A type of acquire we want (closest, strongest, etc)
     *  bPlayerOnly : Don't bother with other creatures or NPCs, want a player
     *  bFacFriend : Check people in my faction
     *  bFacFoe : Check people in other factions
     *
     */
    public virtual bool AcquireFocusMob(int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        if (m_Mobile.Deleted)
        {
            return false;
        }

        if (HandleBardProvoked() || HandleControlled() || HandleConstantFocus())
        {
            return true;
        }

        if (acqType == FightMode.None)
        {
            m_Mobile.FocusMob = null;
            return false;
        }

        if (HandleAggressor(acqType))
        {
            return false;
        }

        if (Core.TickCount - m_Mobile.NextReacquireTime < 0)
        {
            m_Mobile.FocusMob = null;
            return false;
        }

        m_Mobile.NextReacquireTime = Core.TickCount + (int)m_Mobile.ReacquireDelay.TotalMilliseconds;

        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Acquiring new target...");
        }

        if (m_Mobile.Map == null)
        {
            return m_Mobile.FocusMob != null;
        }

        return AcquireNewFocusMob(m_Mobile.Map, iRange, acqType, bPlayerOnly, bFacFriend, bFacFoe);
    }
    
    private bool HandleBardProvoked()
    {
        if (!m_Mobile.BardProvoked)
        {
            return false;
        }

        if (m_Mobile.BardTarget?.Deleted != false)
        {
            m_Mobile.FocusMob = null;
            return false;
        }

        m_Mobile.FocusMob = m_Mobile.BardTarget;
        return true;
    }
    
    private bool HandleControlled()
    {
        if (!m_Mobile.Controlled)
        {
            return false;
        }

        if (m_Mobile.ControlTarget?.Deleted != false || m_Mobile.ControlTarget.Hidden ||
            !m_Mobile.ControlTarget.Alive || m_Mobile.ControlTarget.IsDeadBondedPet ||
            !m_Mobile.InRange(m_Mobile.ControlTarget, m_Mobile.RangePerception * 2))
        {
            if (m_Mobile.ControlTarget != null && m_Mobile.ControlTarget != m_Mobile.ControlMaster)
            {
                m_Mobile.ControlTarget = null;
            }

            m_Mobile.FocusMob = null;
            return false;
        }

        m_Mobile.FocusMob = m_Mobile.ControlTarget;
        return true;
    }
    
    private bool HandleConstantFocus()
    {
        if (m_Mobile.ConstantFocus == null)
        {
            return false;
        }

        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Acquired focused target.");
        }

        m_Mobile.FocusMob = m_Mobile.ConstantFocus;
        return true;
    }
    
    private bool HandleAggressor(FightMode acqType)
    {
        if (acqType != FightMode.Aggressor || 
            m_Mobile.Aggressors.Count > 0 || 
            m_Mobile.Aggressed.Count > 0 ||
            m_Mobile.FactionAllegiance != null || 
            m_Mobile.EthicAllegiance != null)
        {
            return false;
        }

        m_Mobile.FocusMob = null;
        return true;
    }
    
    private bool AcquireNewFocusMob(Map map, int iRange, FightMode acqType, bool bPlayerOnly, bool bFacFriend, bool bFacFoe)
    {
        Mobile newFocusMob = null;
        var val = double.MinValue;
        Mobile enemySummonMob = null;
        var enemySummonVal = double.MinValue;
    
        foreach (var m in map.GetMobilesInRange(m_Mobile.Location, iRange))
        {
            if (IsInvalidTarget(m, bPlayerOnly))
            {
                continue;
            }
    
            var bc = m as BaseCreature;
            var pm = m as PlayerMobile;
    
            if (IsInvalidSummonTarget(m, bc, pm) || 
                IsInvalidFactionTarget(m, bFacFriend, bFacFoe) ||
                IsInvalidFightModeTarget(m, acqType, bc))
            {
                continue;
            }
    
            var theirVal = m_Mobile.GetFightModeRanking(m, acqType, bPlayerOnly);
            if (theirVal > val && m_Mobile.InLOS(m))
            {
                newFocusMob = m;
                val = theirVal;
            }
            else if (Core.AOS && theirVal > enemySummonVal && m_Mobile.InLOS(m) && bc?.Summoned == true && bc?.Controlled != true)
            {
                enemySummonMob = m;
                enemySummonVal = theirVal;
            }
        }
    
        m_Mobile.FocusMob = newFocusMob ?? enemySummonMob;
        return m_Mobile.FocusMob != null;
    }
    
    private bool IsInvalidTarget(Mobile m, bool bPlayerOnly)
    {
        return m.Deleted || m.Blessed || m == m_Mobile || m is BaseFamiliar || !m.Alive || m.IsDeadBondedPet ||
               m.AccessLevel > AccessLevel.Player || (bPlayerOnly && !m.Player) || !m_Mobile.CanSee(m);
    }
    
    private bool IsInvalidSummonTarget(Mobile m, BaseCreature bc, PlayerMobile pm)
    {
        if (Core.AOS && bc?.Summoned == true && 
            (bc.SummonMaster == m_Mobile || (!bc.SummonMaster.Player && IsHostile(bc.SummonMaster))))
        {
            return true;
        }

        if (m_Mobile.Summoned && m_Mobile.SummonMaster != null)
        {
            return m == m_Mobile.SummonMaster || 
                   !SpellHelper.ValidIndirectTarget(m_Mobile.SummonMaster, m) ||
                   (pm != null && m_Mobile.IsAnimatedDead) || 
                   (m_Mobile.IsAnimatedDead && bc?.IsAnimatedDead == true) ||
                   (m_Mobile.IsAnimatedDead && bc?.Controlled == true);
        }

        return false;
    }
    
    private bool IsInvalidFactionTarget(Mobile m, bool bFacFriend, bool bFacFoe)
    {
        if (bFacFriend && !m_Mobile.IsFriend(m))
        {
            return true;
        }

        if (TransformationSpellHelper.UnderTransformation(m, typeof(EtherealVoyageSpell)) ||
            (m_Mobile.Combatant != m && VirtueSystem.GetVirtues(m as PlayerMobile)?.HonorActive == true))
        {
            return true;
        }

        return bFacFoe && (!m_Mobile.IsEnemy(m) || (!bFacFriend && !m_Mobile.CanBeHarmful(m, false)));
    }
    
    private bool IsInvalidFightModeTarget(Mobile m, FightMode acqType, BaseCreature bc)
    {
        if (acqType is not (FightMode.Aggressor or FightMode.Evil))
        {
            return false;
        }

        var bValid = IsHostile(m) || 
                     m_Mobile.GetFactionAllegiance(m) == BaseCreature.Allegiance.Enemy ||
                     m_Mobile.GetEthicAllegiance(m) == BaseCreature.Allegiance.Enemy;

        if (!bValid && acqType == FightMode.Evil)
        {
            bValid = bc?.Controlled == true && bc?.ControlMaster != null 
                ? bc.ControlMaster.Karma < 0 
                : m.Karma < 0;
        }

        return !bValid;
    }
    
    private bool IsHostile(Mobile from)
    {
        return m_Mobile.Combatant == from || 
               from.Combatant == m_Mobile || 
               IsAggressor(from) || 
               IsAggressed(from);
    }
    
    private bool IsAggressor(Mobile from)
    {
        foreach (var aggressor in m_Mobile.Aggressors)
        {
            if (aggressor.Defender == from)
            {
                return true;
            }
        }
        return false;
    }
    
    private bool IsAggressed(Mobile from)
    {
        foreach (var aggressed in m_Mobile.Aggressed)
        {
            if (aggressed.Attacker == from)
            {
                return true;
            }
        }
        return false;
    }

    public virtual void DetectHidden()
    {
        if (m_Mobile.Deleted || m_Mobile.Map == null)
        {
            return;
        }
    
    
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Checking for hidden entities...");
        }
    

        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Checking for hidden entities...");
        }
    
        var srcSkill = m_Mobile.Skills.DetectHidden.Value;
        if (srcSkill <= 0)
        {
            return;
        }

        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay("Checking for hidden entities...");
        }

        foreach (var trg in m_Mobile.GetMobilesInRange(m_Mobile.RangePerception))
        {
            if (IsValidTargetCombatTarget(trg))
            {
                TryDetectHidden(trg, srcSkill);
            }
        }
    }

    private bool IsValidTargetCombatTarget(Mobile trg) =>
        trg != m_Mobile && 
        trg.Player && 
        trg.Alive && 
        trg.Hidden && 
        trg.AccessLevel == AccessLevel.Player && 
        m_Mobile.InLOS(trg);

    private void TryDetectHidden(Mobile trg, double srcSkill)
    {
        if (m_Mobile.Debug)
        {
            m_Mobile.DebugSay($"Trying to detect: {trg.Name}");
        }

        var trgHiding = trg.Skills.Hiding.Value / 2.9;
        var trgStealth = trg.Skills.Stealth.Value / 1.8;
        var chance = Math.Max(srcSkill / 10, srcSkill / 1.2 - Math.Min(trgHiding, trgStealth)) / 100;

        if (chance > Utility.RandomDouble())
        {
            trg.RevealingAction();
            trg.SendLocalizedMessage(500814); // You have been revealed!
        }
    }

    public virtual void Deactivate()
    {
        if (!m_Mobile.PlayerRangeSensitive)
        {
            return;
        }

        m_Timer.Stop();

        var spawner = m_Mobile.Spawner;

        if (ShouldReturnToHome((Server.Engines.Spawners.Spawner)spawner))
        {
            Timer.StartTimer(ReturnToHome);
        }
    }

    private bool ShouldReturnToHome(Spawner spawner)
    {
        return spawner?.ReturnOnDeactivate == true && 
               !m_Mobile.Controlled && (
                   spawner.HomeLocation == Point3D.Zero || 
                   !m_Mobile.InRange(spawner.HomeLocation, spawner.HomeRange)
               );
    }

    private void ReturnToHome()
    {
        if (m_Mobile.Spawner is not { } spawner)
        {
            return;
        }

        var loc = spawner.GetSpawnPosition(m_Mobile, spawner.Map);
        
        if (loc != Point3D.Zero)
        {
            m_Mobile.MoveToWorld(loc, spawner.Map);
        }

        m_Timer.Start();
    }

    public virtual void Activate()
    {
        if (!m_Timer.Running)
        {
            m_Timer.Start();
        }
    }
    
    public virtual void OnCurrentSpeedChanged()
    {
        // changed to 50ms, because at 8ms, AI updates 125 times a second
        // at 50ms, AI updates 20 times a second which is better for performance
        m_Timer.Interval = TimeSpan.FromMilliseconds(Math.Max(50, m_Mobile.CurrentSpeed * 1000));
    }

    private class InternalEntry : ContextMenuEntry
    {
        private readonly OrderType _order;
    
        public InternalEntry(int number, int range, OrderType order, bool enabled) : base(number, range)
        {
            _order = order;
            Enabled = enabled;
        }
    
        public override void OnClick(Mobile from, IEntity target)
        {
            if (!IsValidClick(from, target, out var bc) || 
                IsInvalidOrderForDeadPet(bc) || 
                !IsOwnerOrFriend(from, bc, out var isFriend) || 
                IsInvalidOrderForFriend(isFriend))
            {
                return;
            }
    
            HandleOrder(from, bc);
        }
    
        private bool IsValidClick(Mobile from, IEntity target, out BaseCreature bc)
        {
            bc = target as BaseCreature;
            return from.CheckAlive() && bc != null && !bc.Deleted && bc.Controlled;
        }
    
        private bool IsInvalidOrderForDeadPet(BaseCreature bc) =>
            bc.IsDeadPet && _order is OrderType.Guard or OrderType.Attack or OrderType.Transfer or OrderType.Drop;
    
        private bool IsOwnerOrFriend(Mobile from, BaseCreature bc, out bool isFriend)
        {
            var isOwner = from == bc.ControlMaster;
            isFriend = !isOwner && bc.IsPetFriend(from);
            return isOwner || isFriend;
        }
    
        private bool IsInvalidOrderForFriend(bool isFriend) =>
            isFriend && _order is not (OrderType.Follow or OrderType.Stay or OrderType.Stop);
    
        private void HandleOrder(Mobile from, BaseCreature bc)
        {
            switch (_order)
            {
                case OrderType.Follow:
                case OrderType.Attack:
                case OrderType.Transfer:
                case OrderType.Friend:
                case OrderType.Unfriend:
                    HandleTargetOrder(from, bc);
                    break;
                case OrderType.Release:
                    HandleReleaseOrder(from, bc);
                    break;
                default:
                    HandleDefaultOrder(from, bc);
                    break;
            }
        }
    
        private void HandleTargetOrder(Mobile from, BaseCreature bc)
        {
            if (_order is OrderType.Transfer or OrderType.Friend && from.HasTrade)
            {
                from.SendLocalizedMessage(_order == OrderType.Transfer ? 1010507 : 1070947);
                // 1010507: You cannot transfer a pet with a trade pending
                // 1070947: You cannot friend a pet with a trade pending
                return;
            }
    
            bc.AIObject.BeginPickTarget(from, _order);
        }
    
        private void HandleReleaseOrder(Mobile from, BaseCreature bc)
        {
            if (bc.Summoned)
            {
                HandleDefaultOrder(from, bc);
                return;
            }
    
            from.SendGump(new ConfirmReleaseGump(from, bc));
        }
    
        private void HandleDefaultOrder(Mobile from, BaseCreature bc)
        {
            if (bc.CheckControlChance(from))
            {
                bc.ControlOrder = _order;
            }
        }
    }

    private class TransferItem : Item
    {
        private readonly BaseCreature m_Creature;
    
        public TransferItem(BaseCreature creature)
            : base(ShrinkTable.Lookup(creature))
        {
            m_Creature = creature;
            Movable = false;
    
            if (!Core.AOS)
            {
                Name = creature.Name;
            }
            else if (ItemID == ShrinkTable.DefaultItemID ||
                     creature.GetType().IsDefined(typeof(FriendlyNameAttribute), false) || 
                     creature is Reptalon)
            {
                Name = FriendlyNameAttribute.GetFriendlyNameFor(creature.GetType()).ToString();
            }
    
            Hue = creature.Hue & 0x0FFF;
        }
    
        public TransferItem(Serial serial)
            : base(serial)
        {
        }
    
        public static bool IsInCombat(BaseCreature creature) =>
            creature?.Aggressors.Count > 0 || creature?.Aggressed.Count > 0;
    
        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }
    
        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt(); // version
            Delete();
        }
    
        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1041603); 
            // 1041603: This item represents a pet currently in consideration for trade
            list.Add(1041601, m_Creature.Name); 
            // 1041601: Pet Name: ~1_val~
    
            if (m_Creature.ControlMaster != null)
            {
                list.Add(1041602, m_Creature.ControlMaster.Name); // Owner: ~1_val~
            }
        }
    
        public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            if (!base.AllowSecureTrade(from, to, newOwner, accepted) || IsInvalidTrade(from, to))
            {
                return false;
            }

            return !accepted || HandleAcceptedTrade(from, to);
        }
    
        private bool IsInvalidTrade(Mobile from, Mobile to) =>
            Deleted || m_Creature?.Deleted != false || m_Creature.ControlMaster != from ||
            !from.CheckAlive() || !to.CheckAlive() || from.Map != m_Creature.Map || !from.InRange(m_Creature, 14);
    
        private bool HandleAcceptedTrade(Mobile from, Mobile to)
        {
            if (!ValidateYoungStatus(from, to) || 
                !ValidateControlStatus(from, to) || 
                !ValidateFollowerLimit(to) || 
                IsInCombat(m_Creature))
            {
                return false;
            }

            return true;
        }
    
        private bool ValidateYoungStatus(Mobile from, Mobile to)
        {
            var youngFrom = from is PlayerMobile mobile && mobile.Young;
            var youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

            if (youngFrom && !youngTo)
            {
                from.SendLocalizedMessage(502051);
                // 502051: As a young player, you may not transfer pets to older players.
                return false;
            }

            if (!youngFrom && youngTo)
            {
                from.SendLocalizedMessage(502052);
                // 502052: As an older player, you may not transfer pets to young players.
                return false;
            }

            return true;
        }
    
        private bool ValidateControlStatus(Mobile from, Mobile to)
        {
            if (!m_Creature.CanBeControlledBy(to))
            {
                SendTransferRefusalMessages(from, to, 1043248, 1043249);
                // 1043248: The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                // 1043249: The pet will not accept you as a master because it does not trust you.~3_BLANK~
                return false;
            }

            if (!m_Creature.CanBeControlledBy(from))
            {
                SendTransferRefusalMessages(from, to, 1043250, 1043251);
                // 1043250: The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                // 1043251: The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                return false;
            }

            return true;
        }
    
        private bool ValidateFollowerLimit(Mobile to)
        {
            if (to.Followers + m_Creature.ControlSlots > to.FollowersMax)
            {
                to.SendLocalizedMessage(1049607);
                // 1049607: You cannot have more followers than your follower limit.
                return false;
            }
            return true;
        }
    
        private void SendTransferRefusalMessages(Mobile from, Mobile to, int fromMessage, int toMessage)
        {
            var args = $"{to.Name}\t{from.Name}\t ";
            from.SendLocalizedMessage(fromMessage, args);
            to.SendLocalizedMessage(toMessage, args);
        }
    
        public override void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            if (Deleted || IsInvalidTrade(from, to))
            {
                Delete();
                return;
            }

            Delete();

            if (!accepted || !m_Creature.SetControlMaster(to))
            {
                return;
            }

            TransferPetOwnership(from, to);
        }
    
        private void TransferPetOwnership(Mobile from, Mobile to)
        {
            if (m_Creature.Summoned)
            {
                m_Creature.SummonMaster = to;
            }

            m_Creature.ControlTarget = to;
            m_Creature.ControlOrder = OrderType.Follow;

            m_Creature.BondingBegin = DateTime.MinValue;
            m_Creature.OwnerAbandonTime = DateTime.MinValue;
            m_Creature.IsBonded = false;

            m_Creature.PlaySound(m_Creature.GetIdleSound());

            var args = $"{from.Name}\t{m_Creature.Name}\t{to.Name}";
            from.SendLocalizedMessage(1043253, args);
            // 1043253: You have transferred your pet to ~3_GETTER~.
            to.SendLocalizedMessage(1043252, args);
            // 1043252: ~1_NAME~ has transferred the allegiance of ~2_PET_NAME~ to you.
        }
    }

    private class AITimer : Timer
    {
        private readonly BaseAI m_Owner;
        private int _detectHiddenMinDelay;
        private int _detectHiddenMaxDelay;

        public AITimer(BaseAI owner) : base(
            TimeSpan.FromMilliseconds(Utility.RandomMinMax(100, 200)),
            TimeSpan.FromMilliseconds(GetBaseInterval(owner)))
        {
            m_Owner = owner;
            m_Owner.m_NextDetectHidden = Core.TickCount;
        }

        private static double GetBaseInterval(BaseAI owner)
        {
            var baseInterval = Math.Max(100.0, owner.m_Mobile.CurrentSpeed * 1000);

            if (owner.m_Mobile.Warmode || owner.m_Mobile.Combatant != null)
            {
                return Math.Max(200.0, baseInterval * 0.5);
            }

            return Math.Max(baseInterval, owner.m_Mobile.Controlled ? 100.0 : 200.0);
        }

        protected override void OnTick()
        {
            if (ShouldStop())
            {
                HideMobile();
                return;
            }
        
            Interval = TimeSpan.FromMilliseconds(GetBaseInterval(m_Owner));
        
            HandleBardEffects();
            HandleDetectHidden();

            // additional check
            if (ShouldStop())
            {
                HideMobile();
                return;
            }
        
            if (m_Owner.m_Mobile.Controlled)
            {
                if (!m_Owner.Obey())
                {
                    HideMobile();
                    return;
                }
            }
            else if (!m_Owner.Think())
            {
                HideMobile();
                return;
            }
        
            if (!ShouldStop())
            {
                m_Owner.m_Mobile.OnThink();
            }
        }
        
        private void HideMobile()
        {
            if (!m_Owner.m_Mobile.Hidden)
            {
                m_Owner.m_Mobile.Name = $"Invalid {m_Owner.m_Mobile.Name}";
                m_Owner.m_Mobile.Hidden = true;
            }
            Stop();
        }
        
        private bool ShouldStop()
        {
            if (m_Owner.m_Mobile.Hidden)
            {
                return true;
            }
        
            if (m_Owner.m_Mobile.Map == null || m_Owner.m_Mobile.Map == Map.Internal)
            {
                m_Owner.Deactivate();
                return true;
            }
        
            if (m_Owner.m_Mobile.PlayerRangeSensitive && 
                !m_Owner.m_Mobile.Map.GetSector(m_Owner.m_Mobile.Location).Active)
            {
                HideMobile();
                return true;
            }
        
            return false;
        }

        private void HandleBardEffects()
        {
            if (m_Owner.m_Mobile.BardPacified)
            {
                m_Owner.DoBardPacified();
            }
            else if (m_Owner.m_Mobile.BardProvoked)
            {
                m_Owner.DoBardProvoked();
            }
        }
        
        private void CacheDetectHiddenDelays()
        {
            var delay = Math.Min(30000 / m_Owner.m_Mobile.Int, 120);
            _detectHiddenMinDelay = delay * 900;  // 26s to 108s
            _detectHiddenMaxDelay = delay * 1100; // 32s to 132s
        }

        private void HandleDetectHidden()
        {
            if (!m_Owner.CanDetectHidden || Core.TickCount - m_Owner.m_NextDetectHidden < 0)
            {
                return;
            }

            m_Owner.DetectHidden();

            if (_detectHiddenMinDelay == 0 || _detectHiddenMaxDelay == 0)
            {
                CacheDetectHiddenDelays();
            }

            m_Owner.m_NextDetectHidden = Core.TickCount + 
                Utility.RandomMinMax(_detectHiddenMinDelay, _detectHiddenMaxDelay);
        }
    }

    public static class AIMovementTimerPool
    {   // might need to increase to 2048, more testing needed
        private const int _poolSize = 1024;
        private static readonly Queue<AIMovementTimer> _pool = new(_poolSize);
    
        public static void Configure()
        {
            for (var i = 0; i < _poolSize; i++)
            {
                _pool.Enqueue(new AIMovementTimer());
            }
        }
    
        public static AIMovementTimer GetTimer(TimeSpan delay, BaseAI ai, Direction direction)
        {
            var timer = _pool.TryDequeue(out var pooledTimer) ? pooledTimer : new AIMovementTimer();
            timer.Set(delay, ai, direction);
            return timer;
        }
    
        public class AIMovementTimer : Timer
        {
            public BaseAI AI { get; private set; }
            public Direction Direction { get; private set; }
    
            public AIMovementTimer() : base(TimeSpan.Zero)
            {
            }
    
            public void Set(TimeSpan delay, BaseAI ai, Direction direction)
            {
                if (Running)
                {
                    return;
                }
    
                Delay = delay;
                AI = ai;
                Direction = direction;
            }
    
            protected override void OnTick()
            {
                try
                {
                    AI?.DoMove(Direction);
                }
                finally
                {
                    AI = null;
                    _pool.Enqueue(this);
                }
            }
        }
    }

    private void HandleSkillTraining(SpeechEventArgs e)
    {
        if (!e.Mobile.Alive || !m_Mobile.IsHumanInTown())
        {
            return;
        }

        var ourSkills = m_Mobile.Skills;
        var theirSkills = e.Mobile.Skills;

        for (var i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
        {
            if (e.HasKeyword(i + 1) && WasNamed(e.Speech))
            {
                var skill = ourSkills[i];
                var theirSkill = theirSkills[i];

                if (skill?.Base >= 60.0 && m_Mobile.CheckTeach(skill.SkillName, e.Mobile))
                {
                    var toTeach = Math.Min(skill.Base / 3.0, 42.0);

                    if (toTeach > theirSkill.Base)
                    {
                        m_Mobile.Teach(skill.SkillName, e.Mobile, (int)(toTeach - theirSkill.Base), true);
                        return;
                    }
                }
            }
        }
    }

    /*public virtual bool CheckLeashed()
    {
        if (m_Mobile.Deleted || !m_Mobile.Alive)
        {
            return false;
        }

        var combatant = m_Mobile.Combatant;
        var controlMaster = m_Mobile.ControlMaster;

        if (m_Mobile.Controlled && controlMaster?.Deleted == false)
        {
            if (m_Mobile.GetDistanceToSqrt(controlMaster) > 16)
            {
                if (m_Mobile.Debug)
                {
                    m_Mobile.DebugSay("I am leashed away from my master! Returning...");
                }

                m_Mobile.Combatant = null;
                m_Mobile.FocusMob = null;
                m_Mobile.Warmode = false;
                m_Mobile.ControlTarget = controlMaster;
                m_Mobile.ControlOrder = OrderType.Come;
                return true;
            }
        }

        if (combatant != null && m_Mobile.GetDistanceToSqrt(combatant) > 16)
        {
            if (m_Mobile.Debug)
            {
                m_Mobile.DebugSay("I have been leashed! Returning home...");
            }

            m_Mobile.Combatant = null;
            m_Mobile.FocusMob = null;
            m_Mobile.Warmode = false;

            if ((m_Mobile.Controlled || m_Mobile.Summoned) && controlMaster?.Deleted == false)
            {
                m_Mobile.ControlTarget = controlMaster;
                m_Mobile.ControlOrder = OrderType.Come;
            }

            return true;
        }

        return false;
    }*/
}
