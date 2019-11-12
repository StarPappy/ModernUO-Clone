using System;
using System.Collections.Generic;
using Server.Items;
using Server.Multis.Deeds;
using Server.Network;
using Server.Targeting;

namespace Server.Mobiles
{
  public class RealEstateBroker : BaseVendor
  {
    private DateTime m_NextCheckPack;
    private List<SBInfo> m_SBInfos = new List<SBInfo>();

    [Constructible]
    public RealEstateBroker() : base("the real estate broker")
    {
    }

    public RealEstateBroker(Serial serial) : base(serial)
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override bool HandlesOnSpeech(Mobile from)
    {
      if (from.Alive && from.InRange(this, 3))
        return true;

      return base.HandlesOnSpeech(from);
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
      if (DateTime.UtcNow > m_NextCheckPack && InRange(m, 4) && !InRange(oldLocation, 4) && m.Player)
      {
        Container pack = m.Backpack;

        if (pack != null)
        {
          m_NextCheckPack = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);

          if (pack.FindItemByType<HouseDeed>(false) != null)
          {
            // If you have a deed, I can appraise it or buy it from you...
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500605, m.NetState);

            // Simply hand me a deed to sell it.
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500606, m.NetState);
          }
        }
      }

      base.OnMovement(m, oldLocation);
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
      if (!e.Handled && e.Mobile.Alive && e.HasKeyword(0x38)) // *appraise*
      {
        PublicOverheadMessage(MessageType.Regular, 0x3B2, 500608); // Which deed would you like appraised?
        e.Mobile.BeginTarget(12, false, TargetFlags.None, Appraise_OnTarget);
        e.Handled = true;
      }

      base.OnSpeech(e);
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
      if (dropped is HouseDeed deed)
      {
        int price = ComputePriceFor(deed);

        if (price > 0)
        {
          if (Banker.Deposit(from, price))
          {
            // For the deed I have placed gold in your bankbox : 
            PublicOverheadMessage(MessageType.Regular, 0x3B2, 1008000, AffixType.Append, price.ToString());

            deed.Delete();
            return true;
          }

          PublicOverheadMessage(MessageType.Regular, 0x3B2, 500390); // Your bank box is full.
          return false;
        }

        PublicOverheadMessage(MessageType.Regular, 0x3B2, 500607); // I'm not interested in that.
        return false;
      }

      return base.OnDragDrop(from, dropped);
    }

    public void Appraise_OnTarget(Mobile from, object obj)
    {
      if (obj is HouseDeed deed)
      {
        int price = ComputePriceFor(deed);

        if (price > 0)
        {
          // I will pay you gold for this deed : 
          PublicOverheadMessage(MessageType.Regular, 0x3B2, 1008001, AffixType.Append, price.ToString());

          PublicOverheadMessage(MessageType.Regular, 0x3B2,
            500610); // Simply hand me the deed if you wish to sell it.
        }
        else
        {
          PublicOverheadMessage(MessageType.Regular, 0x3B2, 500607); // I'm not interested in that.
        }
      }
      else
      {
        PublicOverheadMessage(MessageType.Regular, 0x3B2, 500609); // I can't appraise things I know nothing about...
      }
    }

    public int ComputePriceFor(HouseDeed deed)
    {
      int price = 0;

      if (deed is SmallBrickHouseDeed || deed is StonePlasterHouseDeed || deed is FieldStoneHouseDeed || deed is WoodHouseDeed || deed is WoodPlasterHouseDeed ||
          deed is ThatchedRoofCottageDeed)
        price = 43800;
      else if (deed is BrickHouseDeed)
        price = 144500;
      else if (deed is TwoStoryWoodPlasterHouseDeed || deed is TwoStoryStonePlasterHouseDeed)
        price = 192400;
      else if (deed is TowerDeed)
        price = 433200;
      else if (deed is KeepDeed)
        price = 665200;
      else if (deed is CastleDeed)
        price = 1022800;
      else if (deed is LargePatioDeed)
        price = 152800;
      else if (deed is LargeMarbleDeed)
        price = 192800;
      else if (deed is SmallTowerDeed)
        price = 88500;
      else if (deed is LogCabinDeed)
        price = 97800;
      else if (deed is SandstonePatioDeed)
        price = 90900;
      else if (deed is VillaDeed)
        price = 136500;
      else if (deed is StoneWorkshopDeed)
        price = 60600;
      else if (deed is MarbleWorkshopDeed)
        price = 60300;

      return AOS.Scale(price, 80); // refunds 80% of the purchase price
    }

    public override void InitSBInfo()
    {
      m_SBInfos.Add(new SBRealEstateBroker());
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