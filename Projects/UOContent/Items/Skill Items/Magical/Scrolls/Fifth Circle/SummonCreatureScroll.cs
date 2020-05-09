namespace Server.Items
{
  public class SummonCreatureScroll : SpellScroll
  {
    [Constructible]
    public SummonCreatureScroll(int amount = 1) : base(39, 0x1F54, amount)
    {
    }

    public SummonCreatureScroll(Serial serial) : base(serial)
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
