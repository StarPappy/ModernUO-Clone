namespace Server.Items
{
  public class RegStone : Item
  {
    [Constructible]
    public RegStone() : base(0xED4)
    {
      Movable = false;
      Hue = 0x2D1;
    }

    public RegStone(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "a reagent stone";

    public override void OnDoubleClick(Mobile from)
    {
      BagOfReagents regBag = new BagOfReagents();

      if (!from.AddToBackpack(regBag))
        regBag.Delete();
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