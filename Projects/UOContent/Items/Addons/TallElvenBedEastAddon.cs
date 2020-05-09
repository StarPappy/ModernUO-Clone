namespace Server.Items
{
  public class TallElvenBedEastAddon : BaseAddon
  {
    [Constructible]
    public TallElvenBedEastAddon()
    {
      AddComponent(new AddonComponent(0x3054), 0, 0, 0);
      AddComponent(new AddonComponent(0x3053), 1, 0, 0);
      AddComponent(new AddonComponent(0x3055), 2, -1, 0);
      AddComponent(new AddonComponent(0x3052), 2, 0, 0);
    }

    public TallElvenBedEastAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new TallElvenBedEastDeed();

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }

  public class TallElvenBedEastDeed : BaseAddonDeed
  {
    [Constructible]
    public TallElvenBedEastDeed()
    {
    }

    public TallElvenBedEastDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new TallElvenBedEastAddon();
    public override int LabelNumber => 1072859; // tall elven bed (east)

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}