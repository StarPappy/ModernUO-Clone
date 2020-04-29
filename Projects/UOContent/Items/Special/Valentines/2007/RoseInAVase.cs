﻿namespace Server.Items
{
  public class RoseInAVase : Item /* TODO: when dye tub changes are implemented, furny dyable this */
  {
    [Constructible]
    public RoseInAVase()
      : base(0x0EB0)
    {
      Hue = 0x20;
      LootType = LootType.Blessed;
    }

    public RoseInAVase(Serial serial)
      : base(serial)
    {
    }

    public override int LabelNumber => 1023760; // A Rose in a Vase

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
