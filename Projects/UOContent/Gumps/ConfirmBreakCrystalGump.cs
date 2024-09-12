using Server.Items;

namespace Server.Gumps;

public class ConfirmBreakCrystalGump : BaseConfirmGump
{
    private readonly BaseImprisonedMobile m_Item;

    public ConfirmBreakCrystalGump(BaseImprisonedMobile item) => m_Item = item;

    public override int LabelNumber =>
        1075084; // This statuette will be destroyed when its trapped creature is summoned. The creature will be bonded to you but will disappear if released. <br><br>Do you wish to proceed?

    public override void Confirm(Mobile from)
    {
        if (m_Item?.Deleted != false)
        {
            return;
        }

        var summon = m_Item.Summon;

        if (summon == null)
        {
            return;
        }

        if (!summon.SetControlMaster(from))
        {
            summon.Delete();
        }
        else
        {
            from.SendLocalizedMessage(1049666); // Your pet has bonded with you!

            summon.MoveToWorld(from.Location, from.Map);
            summon.IsBonded = true;

            summon.Skills.Wrestling.Base = 100;
            summon.Skills.Tactics.Base = 100;
            summon.Skills.MagicResist.Base = 100;
            summon.Skills.Anatomy.Base = 100;

            Effects.PlaySound(summon.Location, summon.Map, summon.BaseSoundID);
            Effects.SendLocationParticles(
                EffectItem.Create(summon.Location, summon.Map, EffectItem.DefaultDuration),
                0x3728,
                1,
                10,
                0x26B6
            );

            m_Item.Release(from, summon);
            m_Item.Delete();
        }
    }
}
