using Server.Multis;
using Server.Prompts;
using Server.Regions;

namespace Server.Items
{
  [Flippable(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
  public class RecallRune : Item
  {
    private const string RuneFormat = "a recall rune for {0}";
    private string m_Description;
    private BaseHouse m_House;
    private bool m_Marked;
    private Map m_TargetMap;

    [Constructible]
    public RecallRune() : base(0x1F14)
    {
      Weight = 1.0;
      CalculateHue();
    }

    public RecallRune(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public BaseHouse House
    {
      get
      {
        if (m_House?.Deleted == true)
          House = null;

        return m_House;
      }
      set
      {
        m_House = value;
        CalculateHue();
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public string Description
    {
      get => m_Description;
      set
      {
        m_Description = value;
        InvalidateProperties();
      }
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public bool Marked
    {
      get => m_Marked;
      set
      {
        if (m_Marked != value)
        {
          m_Marked = value;
          CalculateHue();
          InvalidateProperties();
        }
      }
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public Point3D Target{ get; set; }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public Map TargetMap
    {
      get => m_TargetMap;
      set
      {
        if (m_TargetMap != value)
        {
          m_TargetMap = value;
          CalculateHue();
          InvalidateProperties();
        }
      }
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      if (m_House?.Deleted == false)
      {
        writer.Write(1); // version

        writer.Write(m_House);
      }
      else
      {
        writer.Write(0); // version
      }

      writer.Write(m_Description);
      writer.Write(m_Marked);
      writer.Write(Target);
      writer.Write(m_TargetMap);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 1:
        {
          m_House = reader.ReadItem() as BaseHouse;
          goto case 0;
        }
        case 0:
        {
          m_Description = reader.ReadString();
          m_Marked = reader.ReadBool();
          Target = reader.ReadPoint3D();
          m_TargetMap = reader.ReadMap();

          CalculateHue();

          break;
        }
      }
    }

    private void CalculateHue()
    {
      if (!m_Marked)
        Hue = 0;
      else if (m_TargetMap == Map.Trammel)
        Hue = House != null ? 0x47F : 50;
      else if (m_TargetMap == Map.Felucca)
        Hue = House != null ? 0x66D : 0;
      else if (m_TargetMap == Map.Ilshenar)
        Hue = House != null ? 0x55F : 1102;
      else if (m_TargetMap == Map.Malas)
        Hue = House != null ? 0x55F : 1102;
      else if (m_TargetMap == Map.Tokuno)
        Hue = House != null ? 0x47F : 1154;
    }

    public void Mark(Mobile m)
    {
      m_Marked = true;

      bool setDesc = false;
      if (Core.AOS)
      {
        m_House = BaseHouse.FindHouseAt(m);

        if (m_House == null)
        {
          Target = m.Location;
          m_TargetMap = m.Map;
        }
        else
        {
          HouseSign sign = m_House.Sign;

          if (sign != null)
            m_Description = sign.Name;
          else
            m_Description = null;

          if (m_Description == null || (m_Description = m_Description.Trim()).Length == 0)
            m_Description = "an unnamed house";

          setDesc = true;

          int x = m_House.BanLocation.X;
          int y = m_House.BanLocation.Y + 2;
          int z = m_House.BanLocation.Z;

          Map map = m_House.Map;

          if (map?.CanFit(x, y, z, 16, false, false) == false)
            z = map.GetAverageZ(x, y);

          Target = new Point3D(x, y, z);
          m_TargetMap = map;
        }
      }
      else
      {
        m_House = null;
        Target = m.Location;
        m_TargetMap = m.Map;
      }

      if (!setDesc)
        m_Description = BaseRegion.GetRuneNameFor(Region.Find(Target, m_TargetMap));

      CalculateHue();
      InvalidateProperties();
    }

    public override void GetProperties(ObjectPropertyList list)
    {
      base.GetProperties(list);

      if (m_Marked)
      {
        string desc;

        if ((desc = m_Description) == null || (desc = desc.Trim()).Length == 0)
          desc = "an unknown location";

        if (m_TargetMap == Map.Tokuno)
          list.Add(House != null ? 1063260 : 1063259, RuneFormat, desc); // ~1_val~ (Tokuno Islands)[(House)]
        else if (m_TargetMap == Map.Malas)
          list.Add(House != null ? 1062454 : 1060804, RuneFormat, desc); // ~1_val~ (Malas)[(House)]
        else if (m_TargetMap == Map.Felucca)
          list.Add(House != null ? 1062452 : 1060805, RuneFormat, desc); // ~1_val~ (Felucca)[(House)]
        else if (m_TargetMap == Map.Trammel)
          list.Add(House != null ? 1062453 : 1060806, RuneFormat, desc); // ~1_val~ (Trammel)[(House)]
        else
          list.Add(House != null ? "{0} ({1})(House)" : "{0} ({1})", string.Format(RuneFormat, desc), m_TargetMap);
      }
    }

    public override void OnSingleClick(Mobile from)
    {
      if (m_Marked)
      {
        string desc;

        if ((desc = m_Description) == null || (desc = desc.Trim()).Length == 0)
          desc = "an unknown location";

        if (m_TargetMap == Map.Tokuno)
          LabelTo(from, House != null ? 1063260 : 1063259,
            string.Format(RuneFormat, desc)); // ~1_val~ (Tokuno Islands)[(House)]
        else if (m_TargetMap == Map.Malas)
          LabelTo(from, House != null ? 1062454 : 1060804,
            string.Format(RuneFormat, desc)); // ~1_val~ (Malas)[(House)]
        else if (m_TargetMap == Map.Felucca)
          LabelTo(from, House != null ? 1062452 : 1060805,
            string.Format(RuneFormat, desc)); // ~1_val~ (Felucca)[(House)]
        else if (m_TargetMap == Map.Trammel)
          LabelTo(from, House != null ? 1062453 : 1060806,
            string.Format(RuneFormat, desc)); // ~1_val~ (Trammel)[(House)]
        else
          LabelTo(from, House != null ? "{0} ({1})(House)" : "{0} ({1})", string.Format(RuneFormat, desc),
            m_TargetMap);
      }
      else
      {
        LabelTo(from, "an unmarked recall rune");
      }
    }

    public override void OnDoubleClick(Mobile from)
    {
      int number;

      if (!IsChildOf(from.Backpack))
      {
        number = 1042001; // That must be in your pack for you to use it.
      }
      else if (House != null)
      {
        number = 1062399; // You cannot edit the description for this rune.
      }
      else if (m_Marked)
      {
        number = 501804; // Please enter a description for this marked object.

        from.Prompt = new RenamePrompt(this);
      }
      else
      {
        number = 501805; // That rune is not yet marked.
      }

      from.SendLocalizedMessage(number);
    }

    private class RenamePrompt : Prompt
    {
      private RecallRune m_Rune;

      public RenamePrompt(RecallRune rune)
      {
        m_Rune = rune;
      }

      public override void OnResponse(Mobile from, string text)
      {
        if (m_Rune.House == null && m_Rune.Marked)
        {
          m_Rune.Description = text;
          from.SendLocalizedMessage(1010474); // The etching on the rune has been changed.
        }
      }
    }
  }
}
