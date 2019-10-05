using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Spells.Eighth
{
  public class EarthquakeSpell : MagerySpell
  {
    private static SpellInfo m_Info = new SpellInfo(
      "Earthquake", "In Vas Por",
      233,
      9012,
      false,
      Reagent.Bloodmoss,
      Reagent.Ginseng,
      Reagent.MandrakeRoot,
      Reagent.SulfurousAsh
    );

    public EarthquakeSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Eighth;

    public override bool DelayedDamage => !Core.AOS;

    public override void OnCast()
    {
      if (SpellHelper.CheckTown(Caster, Caster) && CheckSequence())
      {
        Caster.PlaySound(0x220);

        if (Caster.Map == null)
        {
          FinishSequence();
          return;
        }

        IEnumerable<Mobile> targets = Caster.GetMobilesInRange(1 + (int)(Caster.Skills.Magery.Value / 15.0))
          .Where(m => Caster != m && SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false) && (!Core.AOS || Caster.InLOS(m)));

        foreach (Mobile m in targets)
        {
          int damage;

          if (Core.AOS)
          {
            damage = m.Hits / 2;

            if (!m.Player)
              damage = Math.Max(Math.Min(damage, 100), 15);
            damage += Utility.RandomMinMax(0, 15);
          }
          else
          {
            damage = m.Hits * 6 / 10;

            if (!m.Player && damage < 10)
              damage = 10;
            else if (damage > 75)
              damage = 75;
          }

          Caster.DoHarmful(m);
          SpellHelper.Damage(TimeSpan.Zero, m, Caster, damage, 100, 0, 0, 0, 0);
        }
      }

      FinishSequence();
    }
  }
}
