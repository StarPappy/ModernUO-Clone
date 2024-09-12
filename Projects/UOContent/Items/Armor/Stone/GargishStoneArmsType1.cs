using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
[TypeAlias("Server.Items.GargishStoneArms")]
public partial class GargishStoneArmsType1 : BaseArmor
{
    [Constructible]
    public GargishStoneArmsType1() : base(0x284) => Weight = 10.0;

    public override int RequiredRaces => Race.AllowGargoylesOnly;
    public override int BasePhysicalResistance => 6;
    public override int BaseFireResistance => 6;
    public override int BaseColdResistance => 4;
    public override int BasePoisonResistance => 8;
    public override int BaseEnergyResistance => 6;

    public override int InitMinHits => 40;
    public override int InitMaxHits => 50;

    public override int AosStrReq => 40;
    public override int OldStrReq => 40;
    public override int ArmorBase => 40;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Stone;
}
