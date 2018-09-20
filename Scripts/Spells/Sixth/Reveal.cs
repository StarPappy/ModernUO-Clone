using System.Collections.Generic;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Sixth
{
  public class RevealSpell : MagerySpell
  {
    private static SpellInfo m_Info = new SpellInfo(
      "Reveal", "Wis Quas",
      206,
      9002,
      Reagent.Bloodmoss,
      Reagent.SulfurousAsh
    );

    public RevealSpell(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Sixth;

    public override void OnCast()
    {
      Caster.Target = new InternalTarget(this);
    }

    public void Target(IPoint3D p)
    {
      if (!Caster.CanSee(p))
      {
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      }
      else if (CheckSequence())
      {
        SpellHelper.Turn(Caster, p);

        SpellHelper.GetSurfaceTop(ref p);

        List<Mobile> targets = new List<Mobile>();

        Map map = Caster.Map;

        if (map != null)
        {
          IPooledEnumerable<Mobile> eable = map.GetMobilesInRange(new Point3D(p),
            1 + (int)(Caster.Skills.Magery.Value / 20.0));

          foreach (Mobile m in eable)
          {
            if (m is ShadowKnight && (m.X != p.X || m.Y != p.Y))
              continue;

            if (m.Hidden && (m.AccessLevel == AccessLevel.Player || Caster.AccessLevel > m.AccessLevel) &&
                CheckDifficulty(Caster, m))
              targets.Add(m);
          }

          eable.Free();
        }

        for (int i = 0; i < targets.Count; ++i)
        {
          Mobile m = targets[i];

          m.RevealingAction();

          m.FixedParticles(0x375A, 9, 20, 5049, EffectLayer.Head);
          m.PlaySound(0x1FD);
        }
      }

      FinishSequence();
    }

    // Reveal uses magery and detect hidden vs. hide and stealth
    private static bool CheckDifficulty(Mobile from, Mobile m)
    {
      // Reveal always reveals vs. invisibility spell
      if (!Core.AOS || InvisibilitySpell.HasTimer(m))
        return true;

      int magery = from.Skills.Magery.Fixed;
      int detectHidden = from.Skills.DetectHidden.Fixed;

      int hiding = m.Skills.Hiding.Fixed;
      int stealth = m.Skills.Stealth.Fixed;
      int divisor = hiding + stealth;

      int chance;
      if (divisor > 0)
        chance = 50 * (magery + detectHidden) / divisor;
      else
        chance = 100;

      return chance > Utility.Random(100);
    }

    public class InternalTarget : Target
    {
      private RevealSpell m_Owner;

      public InternalTarget(RevealSpell owner) : base(Core.ML ? 10 : 12, true, TargetFlags.None)
      {
        m_Owner = owner;
      }

      protected override void OnTarget(Mobile from, object o)
      {
        if (o is IPoint3D p)
          m_Owner.Target(p);
      }

      protected override void OnTargetFinish(Mobile from)
      {
        m_Owner.FinishSequence();
      }
    }
  }
}