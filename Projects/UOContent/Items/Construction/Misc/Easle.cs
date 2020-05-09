namespace Server.Items
{
  [Furniture]
  [Flippable(0xF65, 0xF67, 0xF69)]
  public class Easle : Item
  {
    [Constructible]
    public Easle() : base(0xF65) => Weight = 25.0;

    public Easle(Serial serial) : base(serial)
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

      if (Weight == 10.0)
        Weight = 25.0;
    }
  }
}