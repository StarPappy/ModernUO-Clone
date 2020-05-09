namespace Server.Items
{
  public class StuddedDo : BaseArmor
  {
    [Constructible]
    public StuddedDo() : base(0x27C7) => Weight = 8.0;

    public StuddedDo(Serial serial) : base(serial)
    {
    }

    public override int BasePhysicalResistance => 2;
    public override int BaseFireResistance => 4;
    public override int BaseColdResistance => 3;
    public override int BasePoisonResistance => 3;
    public override int BaseEnergyResistance => 4;

    public override int InitMinHits => 40;
    public override int InitMaxHits => 50;

    public override int AosStrReq => 55;
    public override int OldStrReq => 55;

    public override int ArmorBase => 3;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();
    }
  }
}