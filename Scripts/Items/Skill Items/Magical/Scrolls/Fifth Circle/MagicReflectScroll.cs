namespace Server.Items
{
  public class MagicReflectScroll : SpellScroll
  {
    [Constructible]
    public MagicReflectScroll(int amount = 1) : base(35, 0x1F50, amount)
    {
    }

    public MagicReflectScroll(Serial serial) : base(serial)
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
