namespace Server.Items
{
  public class DirtPatch : Item
  {
    [Constructible]
    public DirtPatch() : base(0x0913)
    {
    }

    public DirtPatch(Serial serial) : base(serial)
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