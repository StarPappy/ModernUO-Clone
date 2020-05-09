using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
  public class YoungNinja : BaseCreature
  {
    [Constructible]
    public YoungNinja() : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.2, 0.4)
    {
      InitStats(45, 30, 5);
      SetHits(20, 30);

      Hue = Race.Human.RandomSkinHue();
      Body = 0x190;

      Utility.AssignRandomHair(this);
      Utility.AssignRandomFacialHair(this);

      AddItem(new NinjaTabi());
      AddItem(new LeatherNinjaPants());
      AddItem(new LeatherNinjaJacket());
      AddItem(new LeatherNinjaBelt());

      AddItem(new Bandana(Utility.RandomNondyedHue()));

      switch (Utility.Random(3))
      {
        case 0:
          AddItem(new Tessen());
          break;
        case 1:
          AddItem(new Kama());
          break;
        default:
          AddItem(new Lajatang());
          break;
      }

      SetSkill(SkillName.Swords, 50.0);
      SetSkill(SkillName.Tactics, 50.0);
    }

    public YoungNinja(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a young ninja's corpse";
    public override string DefaultName => "a young ninja";

    public override bool AlwaysMurderer => true;

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