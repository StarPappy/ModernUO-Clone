using System;
using Server.Engines.MLQuests;
using Server.Mobiles;

namespace Server.Spells.Spellweaving
{
  public class SummonFiendSpell : ArcaneSummon<ArcaneFiend>
  {
    private static SpellInfo m_Info = new SpellInfo(
      "Summon Fiend", "Nylisstra",
      -1
    );

    public SummonFiendSpell(Mobile caster, Item scroll = null)
      : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

    public override double RequiredSkill => 38.0;
    public override int RequiredMana => 10;

    public override int Sound => 0x216;

    public override bool CheckSequence()
    {
      Mobile caster = Caster;

      // This is done after casting completes
      if (caster is PlayerMobile mobile)
      {
        MLQuestContext context = MLQuestSystem.GetContext(mobile);

        if (context?.SummonFiend != true)
        {
          mobile.SendLocalizedMessage(1074564); // You haven't demonstrated mastery to summon a fiend.
          return false;
        }
      }

      return base.CheckSequence();
    }
  }
}
