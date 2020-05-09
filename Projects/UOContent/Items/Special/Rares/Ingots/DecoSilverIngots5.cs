namespace Server.Items
{
  public class DecoSilverIngots5 : Item
  {
    [Constructible]
    public DecoSilverIngots5() : base(0x1BFA)
    {
      Movable = true;
      Stackable = false;
    }

    public DecoSilverIngots5(Serial serial) : base(serial)
    {
    }

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