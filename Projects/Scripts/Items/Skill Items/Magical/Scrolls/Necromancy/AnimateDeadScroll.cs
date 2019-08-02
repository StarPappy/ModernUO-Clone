namespace Server.Items
{
  public class AnimateDeadScroll : SpellScroll
  {
    [Constructible]
    public AnimateDeadScroll(int amount = 1) : base(100, 0x2260, amount)
    {
    }

    public AnimateDeadScroll(Serial serial) : base(serial)
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
