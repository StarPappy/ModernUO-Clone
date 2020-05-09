namespace Server.Items
{
  public class ArtifactLargeVase : Item
  {
    [Constructible]
    public ArtifactLargeVase() : base(0x0B47)
    {
    }

    public ArtifactLargeVase(Serial serial) : base(serial)
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