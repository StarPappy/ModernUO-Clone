namespace Server.Items
{
  public class ParalyzeScroll : SpellScroll
  {
    [Constructible]
    public ParalyzeScroll(int amount = 1) : base(37, 0x1F52, amount)
    {
    }

    public ParalyzeScroll(Serial serial) : base(serial)
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
