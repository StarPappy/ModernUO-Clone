namespace Server.Items
{
  public class SummonWaterElementalScroll : SpellScroll
  {
    [Constructible]
    public SummonWaterElementalScroll(int amount = 1) : base(63, 0x1F6C, amount)
    {
    }

    public SummonWaterElementalScroll(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
