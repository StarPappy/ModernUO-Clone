namespace Server.Items
{
  public class LichFormScroll : SpellScroll
  {
    [Constructible]
    public LichFormScroll(int amount = 1) : base(106, 0x2266, amount)
    {
    }

    public LichFormScroll(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
