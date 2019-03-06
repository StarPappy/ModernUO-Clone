using System.Collections.Generic;
using System.Linq;
using Server.Targeting;

namespace Server.Spells.Sixth
{
  public class MassCurseSpell : MagerySpell
  {
    private static SpellInfo m_Info = new SpellInfo(
      "Mass Curse", "Vas Des Sanct",
      218,
      9031,
      false,
      Reagent.Garlic,
      Reagent.Nightshade,
      Reagent.MandrakeRoot,
      Reagent.SulfurousAsh
    );

    public MassCurseSpell(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
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
      else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
      {
        SpellHelper.Turn(Caster, p);

        SpellHelper.GetSurfaceTop(ref p);

        List<Mobile> targets = new List<Mobile>();

        Map map = Caster.Map;

        if (map != null)
        {
          IEnumerable<Mobile> eable = map.GetMobilesInRange(new Point3D(p), 2)
          .Where(m => !Core.AOS || m != Caster).Where(m => SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanSee(m) && Caster.CanBeHarmful(m, false));

          // eable.Free();

          foreach (Mobile m in eable)
          {
            Caster.DoHarmful(m);

            SpellHelper.AddStatCurse(Caster, m, StatType.Str);
            SpellHelper.DisableSkillCheck = true;
            SpellHelper.AddStatCurse(Caster, m, StatType.Dex);
            SpellHelper.AddStatCurse(Caster, m, StatType.Int);
            SpellHelper.DisableSkillCheck = false;

            m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);
            m.PlaySound(0x1FB);

            HarmfulSpell(m);
          }
        }
      }

      FinishSequence();
    }

    private class InternalTarget : Target
    {
      private MassCurseSpell m_Owner;

      public InternalTarget(MassCurseSpell owner) : base(Core.ML ? 10 : 12, true, TargetFlags.None)
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
