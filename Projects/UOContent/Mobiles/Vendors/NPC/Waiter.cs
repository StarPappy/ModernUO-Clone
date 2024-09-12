using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Waiter : BaseVendor
{
    private readonly List<SBInfo> m_SBInfos = new();

    [Constructible]
    public Waiter() : base("the waiter")
    {
        SetSkill(SkillName.Discordance, 36.0, 68.0);
    }
    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override void InitSBInfo()
    {
        m_SBInfos.Add(new SBWaiter());
    }

    public override void InitOutfit()
    {
        base.InitOutfit();

        AddItem(new HalfApron());
    }
}
