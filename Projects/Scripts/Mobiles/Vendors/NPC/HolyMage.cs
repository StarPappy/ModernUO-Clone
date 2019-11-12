using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
  public class HolyMage : BaseVendor
  {
    private List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public HolyMage() : base("the Holy Mage")
    {
      SetSkill(SkillName.EvalInt, 65.0, 88.0);
      SetSkill(SkillName.Inscribe, 60.0, 83.0);
      SetSkill(SkillName.Magery, 64.0, 100.0);
      SetSkill(SkillName.Meditation, 60.0, 83.0);
      SetSkill(SkillName.MagicResist, 65.0, 88.0);
      SetSkill(SkillName.Wrestling, 36.0, 68.0);
    }

    public HolyMage(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBHolyMage());
    }

    public Item ApplyHue(Item item, int hue)
    {
      item.Hue = hue;

      return item;
    }

    public override void InitOutfit()
    {
      AddItem(ApplyHue(new Robe(), 0x47E));
      AddItem(ApplyHue(new ThighBoots(), 0x47E));
      AddItem(ApplyHue(new BlackStaff(), 0x47E));

      if (Female)
      {
        AddItem(ApplyHue(new LeatherGloves(), 0x47E));
        AddItem(ApplyHue(new GoldNecklace(), 0x47E));
      }
      else
      {
        AddItem(ApplyHue(new PlateGloves(), 0x47E));
        AddItem(ApplyHue(new PlateGorget(), 0x47E));
      }

      HairItemID = Utility.Random(Female ? 2 : 1) switch
      {
        0 => 0x203C,
        1 => 0x203D,
        _ => HairItemID
      };

      HairHue = 0x47E;

      PackGold(100, 200);
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