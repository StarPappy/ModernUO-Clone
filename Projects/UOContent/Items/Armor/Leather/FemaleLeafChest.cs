namespace Server.Items
{
  [Flippable(0x2FCB, 0x3181)]
  public class FemaleLeafChest : BaseArmor
  {
    [Constructible]
    public FemaleLeafChest() : base(0x2FCB) => Weight = 2.0;

    public FemaleLeafChest(Serial serial) : base(serial)
    {
    }

    public override int BasePhysicalResistance => 2;
    public override int BaseFireResistance => 3;
    public override int BaseColdResistance => 2;
    public override int BasePoisonResistance => 4;
    public override int BaseEnergyResistance => 4;

    public override int InitMinHits => 30;
    public override int InitMaxHits => 40;

    public override int AosStrReq => 20;
    public override int OldStrReq => 20;

    public override int ArmorBase => 13;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

    public override bool AllowMaleWearer => false;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}