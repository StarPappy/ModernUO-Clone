using System;
using System.Collections.Generic;
using Server.Engines.Craft;

namespace Server.Engines.BulkOrders
{
  public class SmallTailorBOD : SmallBOD
  {
    public static double[] m_TailoringMaterialChances =
    {
      0.857421875, // None
      0.125000000, // Spined
      0.015625000, // Horned
      0.001953125 // Barbed
    };

    private SmallTailorBOD(SmallBulkEntry entry, BulkMaterialType mat, int amountMax, bool reqExceptional)
      : base(0x483, 0, amountMax, entry.Type, entry.Number, entry.Graphic, reqExceptional, mat)
    {
    }

    [Constructible]
    public SmallTailorBOD()
    {
      bool useMaterials = Utility.RandomBool();
      SmallBulkEntry[] entries = useMaterials ? SmallBulkEntry.TailorLeather : SmallBulkEntry.TailorCloth;

      if (entries.Length <= 0)
        return;

      int hue = 0x483;
      int amountMax = Utility.RandomList(10, 15, 20);

      BulkMaterialType material = useMaterials ? GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances)
        : BulkMaterialType.None;

      bool reqExceptional = Utility.RandomBool() || material == BulkMaterialType.None;
      SmallBulkEntry entry = entries[Utility.Random(entries.Length)];

      Hue = hue;
      AmountMax = amountMax;
      Type = entry.Type;
      Number = entry.Number;
      Graphic = entry.Graphic;
      RequireExceptional = reqExceptional;
      Material = material;
    }

    public SmallTailorBOD(int amountCur, int amountMax, Type type, int number, int graphic, bool reqExceptional,
      BulkMaterialType mat) : base(0x483, amountCur, amountMax, type, number, graphic, reqExceptional, mat)
    {
    }

    public SmallTailorBOD(Serial serial) : base(serial)
    {
    }

    public override int ComputeFame() => TailorRewardCalculator.Instance.ComputeFame(this);

    public override int ComputeGold() => TailorRewardCalculator.Instance.ComputeGold(this);

    public override RewardGroup GetRewardGroup() =>
      TailorRewardCalculator.Instance.LookupRewards(TailorRewardCalculator.Instance.ComputePoints(this));

    public static SmallTailorBOD CreateRandomFor(Mobile m)
    {
      SmallBulkEntry[] entries;
      bool useMaterials = Utility.RandomBool();

      int theirSkill = m.Skills.Tailoring.BaseFixedPoint;

      // Ugly, but the easiest leather BOD is Leather Cap which requires at least 6.2 skill.
      entries = useMaterials && theirSkill >= 62 ? SmallBulkEntry.TailorLeather : SmallBulkEntry.TailorCloth;

      if (entries.Length > 0)
      {
        int amountMax;

        if (theirSkill >= 701)
          amountMax = Utility.RandomList(10, 15, 20, 20);
        else if (theirSkill >= 501)
          amountMax = Utility.RandomList(10, 15, 15, 20);
        else
          amountMax = Utility.RandomList(10, 10, 15, 20);

        BulkMaterialType material = BulkMaterialType.None;

        if (useMaterials && theirSkill >= 701)
          for (int i = 0; i < 20; ++i)
          {
            BulkMaterialType check = GetRandomMaterial(BulkMaterialType.Spined, m_TailoringMaterialChances);
            int skillReq = 0;

            switch (check)
            {
              case BulkMaterialType.DullCopper:
                skillReq = 650;
                break;
              case BulkMaterialType.Bronze:
                skillReq = 800;
                break;
              case BulkMaterialType.Gold:
                skillReq = 850;
                break;
              case BulkMaterialType.Agapite:
                skillReq = 900;
                break;
              case BulkMaterialType.Verite:
                skillReq = 950;
                break;
              case BulkMaterialType.Valorite:
                skillReq = 1000;
                break;
              case BulkMaterialType.Spined:
                skillReq = 650;
                break;
              case BulkMaterialType.Horned:
                skillReq = 800;
                break;
              case BulkMaterialType.Barbed:
                skillReq = 990;
                break;
            }

            if (theirSkill >= skillReq)
            {
              material = check;
              break;
            }
          }

        double excChance = theirSkill >= 701 ? (theirSkill + 800) / 2000.0 : 0.0;

        bool reqExceptional = excChance > Utility.RandomDouble();

        CraftSystem system = DefTailoring.CraftSystem;

        List<SmallBulkEntry> validEntries = new List<SmallBulkEntry>();

        for (int i = 0; i < entries.Length; ++i)
        {
          CraftItem item = system.CraftItems.SearchFor(entries[i].Type);

          if (item != null)
          {
            bool allRequiredSkills = true;
            double chance = item.GetSuccessChance(m, null, system, false, ref allRequiredSkills);

            if (allRequiredSkills && chance >= 0.0)
            {
              if (reqExceptional)
                chance = item.GetExceptionalChance(system, chance, m);

              if (chance > 0.0)
                validEntries.Add(entries[i]);
            }
          }
        }

        if (validEntries.Count > 0)
        {
          SmallBulkEntry entry = validEntries[Utility.Random(validEntries.Count)];
          return new SmallTailorBOD(entry, material, amountMax, reqExceptional);
        }
      }

      return null;
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
