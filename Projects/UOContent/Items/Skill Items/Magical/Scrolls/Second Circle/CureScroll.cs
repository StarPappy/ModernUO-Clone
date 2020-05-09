namespace Server.Items
{
  public class CureScroll : SpellScroll
  {
    [Constructible]
    public CureScroll(int amount = 1) : base(10, 0x1F37, amount)
    {
    }

    public CureScroll(Serial serial) : base(serial)
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
