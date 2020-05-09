namespace Server.Items
{
  public class SummonDaemonScroll : SpellScroll
  {
    [Constructible]
    public SummonDaemonScroll(int amount = 1) : base(60, 0x1F69, amount)
    {
    }

    public SummonDaemonScroll(Serial serial) : base(serial)
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
