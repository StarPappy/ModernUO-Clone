using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Ninjitsu
{
  public class KiAttack : NinjaMove
  {
    private static Dictionary<Mobile, KiAttackInfo> m_Table = new Dictionary<Mobile, KiAttackInfo>();

    public override int BaseMana => 25;
    public override int RequiredSkill => 800;

    public override TextDefinition AbilityMessage =>
      new TextDefinition(1063099); // Your Ki Attack must be complete within 2 seconds for the damage bonus!

    public override void OnUse(Mobile from)
    {
      if (!Validate(from))
        return;

      KiAttackInfo info = new KiAttackInfo(from);
      info.m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(2.0), EndKiAttack, info);

      m_Table[from] = info;
    }

    public override bool Validate(Mobile from)
    {
      if (from.Hidden && from.AllowedStealthSteps > 0)
      {
        from.SendLocalizedMessage(1063127); // You cannot use this ability while in stealth mode.
        return false;
      }

      if (Core.ML && from.Weapon is BaseRanged)
      {
        from.SendLocalizedMessage(1075858); // You can only use this with melee attacks.
        return false;
      }

      return base.Validate(from);
    }

    public override double GetDamageScalar(Mobile attacker, Mobile defender)
    {
      if (attacker.Hidden)
        return 1.0;

      /*
       * Pub40 changed pvp damage max to 55%
       */

      return 1.0 + GetBonus(attacker) / (Core.ML && attacker.Player && defender.Player ? 40 : 10);
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
      if (!Validate(attacker) || !CheckMana(attacker, true))
        return;

      if (GetBonus(attacker) == 0.0)
      {
        attacker.SendLocalizedMessage(1063101); // You were too close to your target to cause any additional damage.
      }
      else
      {
        attacker.FixedParticles(0x37BE, 1, 5, 0x26BD, 0x0, 0x1, EffectLayer.Waist);
        attacker.PlaySound(0x510);

        attacker.SendLocalizedMessage(
          1063100); // Your quick flight to your target causes extra damage as you strike!
        defender.FixedParticles(0x37BE, 1, 5, 0x26BD, 0, 0x1, EffectLayer.Waist);

        CheckGain(attacker);
      }

      ClearCurrentMove(attacker);
    }

    public override void OnClearMove(Mobile from)
    {
      if (!m_Table.TryGetValue(from, out KiAttackInfo info))
        return;

      info.m_Timer.Stop();
      m_Table.Remove(info.m_Mobile);
    }

    public static double GetBonus(Mobile from)
    {
      if (!m_Table.TryGetValue(from, out KiAttackInfo info))
        return 0;

      int xDelta = info.m_Location.X - from.X;
      int yDelta = info.m_Location.Y - from.Y;

      double bonus = Math.Sqrt(xDelta * xDelta + yDelta * yDelta);

      if (bonus > 20.0)
        bonus = 20.0;

      return bonus;
    }

    private static void EndKiAttack(KiAttackInfo info)
    {
      info.m_Timer?.Stop();

      ClearCurrentMove(info.m_Mobile);
      info.m_Mobile.SendLocalizedMessage(1063102); // You failed to complete your Ki Attack in time.

      m_Table.Remove(info.m_Mobile);
    }

    private class KiAttackInfo
    {
      public Point3D m_Location;
      public Mobile m_Mobile;
      public Timer m_Timer;

      public KiAttackInfo(Mobile m)
      {
        m_Mobile = m;
        m_Location = m.Location;
      }
    }
  }
}
