using System;

namespace Server.Items
{
  public class JackOLantern : BaseAddon
  {
    [Constructible]
    public JackOLantern()
      : this(Utility.Random(2) < 1)
    {
    }

    [Constructible]
    public JackOLantern(bool south)
    {
      AddComponent(new AddonComponent(5703), 0, 0, +0);

      int hue = 1161;
      // ( 1 > Utility.Random( 5 ) ? 2118 : 1161 );

      if (!south)
      {
        AddComponent(GetComponent(3178, 0000), 0, 0, -1);
        AddComponent(GetComponent(3883, hue), 0, 0, +1);
        AddComponent(GetComponent(3862, hue), 0, 0, +0);
      }
      else
      {
        AddComponent(GetComponent(3179, 0000), 0, 0, +0);
        AddComponent(GetComponent(3885, hue), 0, 0, -1);
        AddComponent(GetComponent(3871, hue), 0, 0, +0);
      }
    }

    public JackOLantern(Serial serial)
      : base(serial)
    {
    }

    public override bool ShareHue => false;

    private AddonComponent GetComponent(int itemID, int hue)
    {
      AddonComponent ac = new AddonComponent(itemID);

      ac.Hue = hue;
      ac.Name = "jack-o-lantern";

      return ac;
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write((byte)2); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadByte();

      if (version == 0)
        Timer.DelayCall(TimeSpan.Zero, () =>
        {
          for (int i = 0; i < Components.Count; ++i)
            if (Components[i] is AddonComponent ac && ac.Hue == 2118)
              ac.Hue = 1161;
        });

      if (version <= 1)
        Timer.DelayCall(TimeSpan.Zero, () =>
        {
          for (int i = 0; i < Components.Count; ++i)
            if (Components[i] is AddonComponent ac)
              ac.Name = "jack-o-lantern";
        });
    }
  }
}
