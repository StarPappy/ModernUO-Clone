namespace Server.Items
{
  [Flippable(0x105D, 0x105E)]
  public class Springs : Item
  {
    [Constructible]
    public Springs(int amount = 1) : base(0x105D)
    {
      Stackable = true;
      Amount = amount;
      Weight = 1.0;
    }

    public Springs(Serial serial) : base(serial)
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
