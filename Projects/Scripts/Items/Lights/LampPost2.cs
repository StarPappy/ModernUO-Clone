using System;

namespace Server.Items
{
  public class LampPost2 : BaseLight
  {
    [Constructible]
    public LampPost2() : base(0xB23)
    {
      Movable = false;
      Duration = TimeSpan.Zero; // Never burnt out
      Burning = false;
      Light = LightType.Circle300;
      Weight = 40.0;
    }

    public LampPost2(Serial serial) : base(serial)
    {
    }

    public override int LitItemID => 0xB22;
    public override int UnlitItemID => 0xB23;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();
    }
  }
}