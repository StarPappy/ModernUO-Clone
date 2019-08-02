namespace Server.Items
{
  public class WraithFormScroll : SpellScroll
  {
    [Constructible]
    public WraithFormScroll(int amount = 1) : base(115, 0x226F, amount)
    {
    }

    public WraithFormScroll(Serial serial) : base(serial)
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
