namespace Server.Items
{
  public class Coal : Item
  {
    [Constructible]
    public Coal() : base(0x19b9)
    {
      Stackable = false;
      LootType = LootType.Blessed;
      Hue = 0x965;
    }

    public Coal(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Coal";

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

  public class BadCard : Item
  {
    [Constructible]
    public BadCard() : base(0x14ef)
    {
      int[] m_CardHues = { 0x45, 0x27, 0x3d0 };
      Hue = m_CardHues[Utility.Random(m_CardHues.Length)];
      Stackable = false;
      LootType = LootType.Blessed;
      Movable = true;
    }

    public BadCard(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber // Maybe next year youll get a better...
      => 1041428;

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

  public class Spam : Food
  {
    [Constructible]
    public Spam() : base(0x1044, 1)
    {
      Stackable = false;
      LootType = LootType.Blessed;
    }

    public Spam(Serial serial) : base(serial)
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
