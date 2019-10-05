namespace Server.Mobiles
{
  public class SeaHorse : BaseMount
  {
    [Constructible]
    public SeaHorse(string name = "a sea horse") :
      base(name, 0x90, 0x3EB3, AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4)
    {
      InitStats(Utility.Random(50, 30), Utility.Random(50, 30), 10);
      Skills.MagicResist.Base = 25.0 + Utility.RandomDouble() * 5.0;
      Skills.Wrestling.Base = 35.0 + Utility.RandomDouble() * 10.0;
      Skills.Tactics.Base = 30.0 + Utility.RandomDouble() * 15.0;
    }

    public SeaHorse(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a sea horse corpse";

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
