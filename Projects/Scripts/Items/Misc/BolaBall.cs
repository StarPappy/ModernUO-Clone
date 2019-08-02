namespace Server.Items
{
  public class BolaBall : Item
  {
    [Constructible]
    public BolaBall(int amount = 1) : base(0xE73)
    {
      Weight = 4.0;
      Stackable = true;
      Amount = amount;
      Hue = 0x8AC;
    }

    public BolaBall(Serial serial) : base(serial)
    {
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}
