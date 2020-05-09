using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
  public class YoungRonin : BaseCreature
  {
    [Constructible]
    public YoungRonin() : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.2, 0.4)
    {
      InitStats(45, 30, 5);
      SetHits(10, 20);

      Hue = Race.Human.RandomSkinHue();
      Body = 0x190;

      Utility.AssignRandomHair(this);
      Utility.AssignRandomFacialHair(this);

      AddItem(new LeatherDo());
      AddItem(new LeatherHiroSode());
      AddItem(new SamuraiTabi());

      switch (Utility.Random(3))
      {
        case 0:
          AddItem(new StuddedHaidate());
          break;
        case 1:
          AddItem(new PlateSuneate());
          break;
        default:
          AddItem(new LeatherSuneate());
          break;
      }

      AddItem(new Bandana(Utility.RandomNondyedHue()));

      switch (Utility.Random(3))
      {
        case 0:
          AddItem(new NoDachi());
          break;
        case 1:
          AddItem(new Lajatang());
          break;
        default:
          AddItem(new Wakizashi());
          break;
      }

      SetSkill(SkillName.Swords, 50.0);
      SetSkill(SkillName.Tactics, 50.0);
    }

    public YoungRonin(Serial serial) : base(serial)
    {
    }

    public override string CorpseName => "a young ronin's corpse";
    public override string DefaultName => "a young ronin";

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