/***************************************************************************
 *                                Packets.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Server.Accounting;
using Server.Gumps;

namespace Server.Network
{
  public static partial class Packets
  {
    public static readonly int MaxPacketSize = 0x10000;
  }

  public sealed class DisplayGumpPacked : Packet, IGumpWriter
  {
    private static byte[] m_True = Gump.StringToBuffer(" 1");
    private static byte[] m_False = Gump.StringToBuffer(" 0");

    private static byte[] m_BeginTextSeparator = Gump.StringToBuffer(" @");
    private static byte[] m_EndTextSeparator = Gump.StringToBuffer("@");

    private static byte[] m_Buffer = new byte[48];

    private Gump m_Gump;

    private PacketWriter m_Layout;

    private int m_StringCount;
    private PacketWriter m_Strings;

    static DisplayGumpPacked()
    {
      m_Buffer[0] = (byte)' ';
    }

    public DisplayGumpPacked(Gump gump)
      : base(0xDD)
    {
      m_Gump = gump;

      m_Layout = PacketWriter.CreateInstance(8192);
      m_Strings = PacketWriter.CreateInstance(8192);
    }

    public int TextEntries{ get; set; }

    public int Switches{ get; set; }

    public void AppendLayout(bool val)
    {
      AppendLayout(val ? m_True : m_False);
    }

    public void AppendLayout(int val)
    {
      string toString = val.ToString();
      int bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      m_Layout.Write(m_Buffer, 0, bytes);
    }

    public void AppendLayout(uint val)
    {
      string toString = val.ToString();
      int bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      m_Layout.Write(m_Buffer, 0, bytes);
    }

    public void AppendLayoutNS(int val)
    {
      string toString = val.ToString();
      int bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1);

      m_Layout.Write(m_Buffer, 1, bytes);
    }

    public void AppendLayout(string text)
    {
      AppendLayout(m_BeginTextSeparator);

      m_Layout.WriteAsciiFixed(text, text.Length);

      AppendLayout(m_EndTextSeparator);
    }

    public void AppendLayout(byte[] buffer)
    {
      m_Layout.Write(buffer, 0, buffer.Length);
    }

    public void WriteStrings(List<string> strings)
    {
      m_StringCount = strings.Count;

      for (int i = 0; i < strings.Count; ++i)
      {
        string v = strings[i] ?? "";

        m_Strings.Write((ushort)v.Length);
        m_Strings.WriteBigUniFixed(v, v.Length);
      }
    }

    public void Flush()
    {
      EnsureCapacity(28 + (int)m_Layout.Length + (int)m_Strings.Length);

      m_Stream.Write(m_Gump.Serial);
      m_Stream.Write(m_Gump.TypeID);
      m_Stream.Write(m_Gump.X);
      m_Stream.Write(m_Gump.Y);

      // Note: layout MUST be null terminated (don't listen to krrios)
      m_Layout.Write((byte)0);
      WritePacked(m_Layout);

      m_Stream.Write(m_StringCount);

      WritePacked(m_Strings);

      PacketWriter.ReleaseInstance(m_Layout);
      PacketWriter.ReleaseInstance(m_Strings);
    }

    private void WritePacked(PacketWriter src)
    {
      byte[] buffer = src.UnderlyingStream.GetBuffer();
      int length = (int)src.Length;

      if (length == 0)
      {
        m_Stream.Write(0);
        return;
      }

      int wantLength = 1 + buffer.Length * 1024 / 1000;

      wantLength += 4095;
      wantLength &= ~4095;

      byte[] packBuffer = ArrayPool<byte>.Shared.Rent(wantLength);

      int packLength = wantLength;

      Compression.Pack(packBuffer, ref packLength, buffer, length, ZLibQuality.Default);

      m_Stream.Write(4 + packLength);
      m_Stream.Write(length);
      m_Stream.Write(packBuffer, 0, packLength);

      ArrayPool<byte>.Shared.Return(packBuffer, true);
    }
  }

  public sealed class DisplayGumpFast : Packet, IGumpWriter
  {
    private static byte[] m_True = Gump.StringToBuffer(" 1");
    private static byte[] m_False = Gump.StringToBuffer(" 0");

    private static byte[] m_BeginTextSeparator = Gump.StringToBuffer(" @");
    private static byte[] m_EndTextSeparator = Gump.StringToBuffer("@");

    private byte[] m_Buffer = new byte[48];
    private int m_LayoutLength;

    public DisplayGumpFast(Gump g) : base(0xB0)
    {
      m_Buffer[0] = (byte)' ';

      EnsureCapacity(4096);

      m_Stream.Write(g.Serial);
      m_Stream.Write(g.TypeID);
      m_Stream.Write(g.X);
      m_Stream.Write(g.Y);
      m_Stream.Write((ushort)0xFFFF);
    }

    public int TextEntries{ get; set; }

    public int Switches{ get; set; }

    public void AppendLayout(bool val)
    {
      AppendLayout(val ? m_True : m_False);
    }

    public void AppendLayout(int val)
    {
      string toString = val.ToString();
      int bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      m_Stream.Write(m_Buffer, 0, bytes);
      m_LayoutLength += bytes;
    }

    public void AppendLayout(uint val)
    {
      string toString = val.ToString();
      int bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      m_Stream.Write(m_Buffer, 0, bytes);
      m_LayoutLength += bytes;
    }

    public void AppendLayoutNS(int val)
    {
      string toString = val.ToString();
      int bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1);

      m_Stream.Write(m_Buffer, 1, bytes);
      m_LayoutLength += bytes;
    }

    public void AppendLayout(string text)
    {
      AppendLayout(m_BeginTextSeparator);

      int length = text.Length;
      m_Stream.WriteAsciiFixed(text, length);
      m_LayoutLength += length;

      AppendLayout(m_EndTextSeparator);
    }

    public void AppendLayout(byte[] buffer)
    {
      int length = buffer.Length;
      m_Stream.Write(buffer, 0, length);
      m_LayoutLength += length;
    }

    public void WriteStrings(List<string> text)
    {
      m_Stream.Seek(19, SeekOrigin.Begin);
      m_Stream.Write((ushort)m_LayoutLength);
      m_Stream.Seek(0, SeekOrigin.End);

      m_Stream.Write((ushort)text.Count);

      for (int i = 0; i < text.Count; ++i)
      {
        string v = text[i] ?? "";

        int length = (ushort)v.Length;

        m_Stream.Write((ushort)length);
        m_Stream.WriteBigUniFixed(v, length);
      }
    }

    public void Flush()
    {
    }
  }

  public sealed class DisplayGump : Packet
  {
    public DisplayGump(Gump g, string layout, string[] text) : base(0xB0)
    {
      if (layout == null) layout = "";

      EnsureCapacity(256);

      m_Stream.Write(g.Serial);
      m_Stream.Write(g.TypeID);
      m_Stream.Write(g.X);
      m_Stream.Write(g.Y);
      m_Stream.Write((ushort)(layout.Length + 1));
      m_Stream.WriteAsciiNull(layout);

      m_Stream.Write((ushort)text.Length);

      for (int i = 0; i < text.Length; ++i)
      {
        string v = text[i] ?? "";

        ushort length = (ushort)v.Length;

        m_Stream.Write(length);
        m_Stream.WriteBigUniFixed(v, length);
      }
    }
  }

  public sealed class CityInfo
  {
    private Point3D m_Location;

    public CityInfo(string city, string building, int description, int x, int y, int z, Map m)
    {
      City = city;
      Building = building;
      Description = description;
      m_Location = new Point3D(x, y, z);
      Map = m;
    }

    public CityInfo(string city, string building, int x, int y, int z, Map m) : this(city, building, 0, x, y, z, m)
    {
    }

    public CityInfo(string city, string building, int description, int x, int y, int z) : this(city, building,
      description, x, y, z, Map.Trammel)
    {
    }

    public CityInfo(string city, string building, int x, int y, int z) : this(city, building, 0, x, y, z, Map.Trammel)
    {
    }

    public string City{ get; set; }

    public string Building{ get; set; }

    public int Description{ get; set; }

    public int X
    {
      get => m_Location.X;
      set => m_Location.X = value;
    }

    public int Y
    {
      get => m_Location.Y;
      set => m_Location.Y = value;
    }

    public int Z
    {
      get => m_Location.Z;
      set => m_Location.Z = value;
    }

    public Point3D Location
    {
      get => m_Location;
      set => m_Location = value;
    }

    public Map Map{ get; set; }
  }

  public sealed class CharacterListUpdate : Packet
  {
    public CharacterListUpdate(IAccount a) : base(0x86)
    {
      EnsureCapacity(4 + a.Length * 60);

      int highSlot = -1;

      for (int i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      int count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      m_Stream.Write((byte)count);

      for (int i = 0; i < count; ++i)
      {
        Mobile m = a[i];

        if (m != null)
        {
          m_Stream.WriteAsciiFixed(m.Name, 30);
          m_Stream.Fill(30); // password
        }
        else
        {
          m_Stream.Fill(60);
        }
      }
    }
  }

  [Flags]
  public enum ThirdPartyFeature : ulong
  {
    FilterWeather = 1 << 0,
    FilterLight = 1 << 1,

    SmartTarget = 1 << 2,
    RangedTarget = 1 << 3,

    AutoOpenDoors = 1 << 4,

    DequipOnCast = 1 << 5,
    AutoPotionEquip = 1 << 6,

    ProtectHeals = 1 << 7,

    LoopedMacros = 1 << 8,

    UseOnceAgent = 1 << 9,
    RestockAgent = 1 << 10,
    SellAgent = 1 << 11,
    BuyAgent = 1 << 12,

    PotionHotkeys = 1 << 13,

    RandomTargets = 1 << 14,
    ClosestTargets = 1 << 15, // All closest target hotkeys
    OverheadHealth = 1 << 16, // Health and Mana/Stam messages shown over player's heads

    AutolootAgent = 1 << 17,
    BoneCutterAgent = 1 << 18,
    AdvancedMacros = 1 << 19,
    AutoRemount = 1 << 20,
    AutoBandage = 1 << 21,
    EnemyTargetShare = 1 << 22,
    FilterSeason = 1 << 23,
    SpellTargetShare = 1 << 24,

    All = ulong.MaxValue
  }

  public static class FeatureProtection
  {
    public static ThirdPartyFeature DisabledFeatures{ get; private set; } = 0;

    public static void Disable(ThirdPartyFeature feature)
    {
      SetDisabled(feature, true);
    }

    public static void Enable(ThirdPartyFeature feature)
    {
      SetDisabled(feature, false);
    }

    public static void SetDisabled(ThirdPartyFeature feature, bool value)
    {
      if (value)
        DisabledFeatures |= feature;
      else
        DisabledFeatures &= ~feature;
    }
  }

  public sealed class CharacterList : Packet
  {
    //private static MD5CryptoServiceProvider m_MD5Provider;

    public CharacterList(IAccount a, CityInfo[] info) : base(0xA9)
    {
      EnsureCapacity(11 + a.Length * 60 + info.Length * 89);

      int highSlot = -1;

      for (int i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      int count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      m_Stream.Write((byte)count);

      for (int i = 0; i < count; ++i)
        if (a[i] != null)
        {
          m_Stream.WriteAsciiFixed(a[i].Name, 30);
          m_Stream.Fill(30); // password
        }
        else
        {
          m_Stream.Fill(60);
        }

      m_Stream.Write((byte)info.Length);

      for (int i = 0; i < info.Length; ++i)
      {
        CityInfo ci = info[i];

        m_Stream.Write((byte)i);
        m_Stream.WriteAsciiFixed(ci.City, 32);
        m_Stream.WriteAsciiFixed(ci.Building, 32);
        m_Stream.Write(ci.X);
        m_Stream.Write(ci.Y);
        m_Stream.Write(ci.Z);
        m_Stream.Write(ci.Map.MapID);
        m_Stream.Write(ci.Description);
        m_Stream.Write(0);
      }

      CharacterListFlags flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (a.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      m_Stream.Write((int)(flags | AdditionalFlags)); // Additional Flags

      m_Stream.Write((short)-1);

      /*ThirdPartyFeature disabled = FeatureProtection.DisabledFeatures;

      if (disabled != 0)
      {
        if (m_MD5Provider == null)
          m_MD5Provider = new MD5CryptoServiceProvider();

        m_Stream.UnderlyingStream.Flush();

        byte[] hashCode = m_MD5Provider.ComputeHash(m_Stream.UnderlyingStream.GetBuffer(), 0,
          (int)m_Stream.UnderlyingStream.Length);
        byte[] buffer = new byte[28];

        for (int i = 0; i < count; ++i)
        {
          Utility.RandomBytes(buffer);

          m_Stream.Seek(35 + i * 60, SeekOrigin.Begin);
          m_Stream.Write(buffer, 0, buffer.Length);
        }

        m_Stream.Seek(35, SeekOrigin.Begin);
        m_Stream.Write((int)((long)disabled >> 32));
        m_Stream.Write((int)disabled);

        m_Stream.Seek(95, SeekOrigin.Begin);
        m_Stream.Write(hashCode, 0, hashCode.Length);
      }*/
    }

    public static CharacterListFlags AdditionalFlags{ get; set; }
  }

  public sealed class CharacterListOld : Packet
  {
    // private static MD5CryptoServiceProvider m_MD5Provider;

    public CharacterListOld(IAccount a, CityInfo[] info) : base(0xA9)
    {
      EnsureCapacity(9 + a.Length * 60 + info.Length * 63);

      int highSlot = -1;

      for (int i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      int count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      m_Stream.Write((byte)count);

      for (int i = 0; i < count; ++i)
        if (a[i] != null)
        {
          m_Stream.WriteAsciiFixed(a[i].Name, 30);
          m_Stream.Fill(30); // password
        }
        else
        {
          m_Stream.Fill(60);
        }

      m_Stream.Write((byte)info.Length);

      for (int i = 0; i < info.Length; ++i)
      {
        CityInfo ci = info[i];

        m_Stream.Write((byte)i);
        m_Stream.WriteAsciiFixed(ci.City, 31);
        m_Stream.WriteAsciiFixed(ci.Building, 31);
      }

      CharacterListFlags flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (a.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      m_Stream.Write((int)(flags | CharacterList.AdditionalFlags)); // Additional Flags

/*      ThirdPartyFeature disabled = FeatureProtection.DisabledFeatures;

      if (disabled != 0)
      {
        if (m_MD5Provider == null)
          m_MD5Provider = new MD5CryptoServiceProvider();

        m_Stream.UnderlyingStream.Flush();

        byte[] hashCode = m_MD5Provider.ComputeHash(m_Stream.UnderlyingStream.GetBuffer(), 0,
          (int)m_Stream.UnderlyingStream.Length);
        byte[] buffer = new byte[28];

        for (int i = 0; i < count; ++i)
        {
          Utility.RandomBytes(buffer);

          m_Stream.Seek(35 + i * 60, SeekOrigin.Begin);
          m_Stream.Write(buffer, 0, buffer.Length);
        }

        m_Stream.Seek(35, SeekOrigin.Begin);
        m_Stream.Write((int)((long)disabled >> 32));
        m_Stream.Write((int)disabled);

        m_Stream.Seek(95, SeekOrigin.Begin);
        m_Stream.Write(hashCode, 0, hashCode.Length);
      }*/
    }
  }

  public sealed class ClearWeaponAbility : Packet
  {
    public static readonly Packet Instance = SetStatic(new ClearWeaponAbility());

    public ClearWeaponAbility() : base(0xBF)
    {
      EnsureCapacity(5);

      m_Stream.Write((short)0x21);
    }
  }

  public enum ALRReason : byte
  {
    Invalid = 0x00,
    InUse = 0x01,
    Blocked = 0x02,
    BadPass = 0x03,
    Idle = 0xFE,
    BadComm = 0xFF
  }

  public sealed class AccountLoginRej : Packet
  {
    public AccountLoginRej(ALRReason reason) : base(0x82, 2)
    {
      m_Stream.Write((byte)reason);
    }
  }

  [Flags]
  public enum AffixType : byte
  {
    Append = 0x00,
    Prepend = 0x01,
    System = 0x02
  }

  public sealed class MessageLocalizedAffix : Packet
  {
    public MessageLocalizedAffix(Serial serial, int graphic, MessageType messageType, int hue, int font, int number,
      string name, AffixType affixType, string affix, string args) : base(0xCC)
    {
      if (affix == null) affix = "";
      if (args == null) args = "";

      if (hue == 0)
        hue = 0x3B2;

      EnsureCapacity(52 + affix.Length + args.Length * 2);

      m_Stream.Write(serial);
      m_Stream.Write((short)graphic);
      m_Stream.Write((byte)messageType);
      m_Stream.Write((short)hue);
      m_Stream.Write((short)font);
      m_Stream.Write(number);
      m_Stream.Write((byte)affixType);
      m_Stream.WriteAsciiFixed(name ?? "", 30);
      m_Stream.WriteAsciiNull(affix);
      m_Stream.WriteBigUniNull(args);
    }
  }

  public sealed class ServerInfo
  {
    public ServerInfo(string name, int fullPercent, TimeZoneInfo tz, IPEndPoint address)
    {
      Name = name;
      FullPercent = fullPercent;
      TimeZone = tz.GetUtcOffset(DateTime.Now).Hours;
      Address = address;
    }

    public string Name{ get; set; }

    public int FullPercent{ get; set; }

    public int TimeZone{ get; set; }

    public IPEndPoint Address{ get; set; }
  }

  public sealed class FollowMessage : Packet
  {
    public FollowMessage(Serial serial1, Serial serial2) : base(0x15, 9)
    {
      m_Stream.Write(serial1);
      m_Stream.Write(serial2);
    }
  }

  public sealed class AccountLoginAck : Packet
  {
    public AccountLoginAck(ServerInfo[] info) : base(0xA8)
    {
      EnsureCapacity(6 + info.Length * 40);

      m_Stream.Write((byte)0x5D); // Unknown

      m_Stream.Write((ushort)info.Length);

      for (int i = 0; i < info.Length; ++i)
      {
        ServerInfo si = info[i];

        m_Stream.Write((ushort)i);
        m_Stream.WriteAsciiFixed(si.Name, 32);
        m_Stream.Write((byte)si.FullPercent);
        m_Stream.Write((sbyte)si.TimeZone);
        m_Stream.Write(Utility.GetAddressValue(si.Address.Address));
      }
    }
  }

  public sealed class DisplaySignGump : Packet
  {
    public DisplaySignGump(Serial serial, int gumpID, string unknown, string caption) : base(0x8B)
    {
      if (unknown == null) unknown = "";
      if (caption == null) caption = "";

      EnsureCapacity(16 + unknown.Length + caption.Length);

      m_Stream.Write(serial);
      m_Stream.Write((short)gumpID);
      m_Stream.Write((short)unknown.Length);
      m_Stream.WriteAsciiFixed(unknown, unknown.Length);
      m_Stream.Write((short)(caption.Length + 1));
      m_Stream.WriteAsciiFixed(caption, caption.Length + 1);
    }
  }

  public sealed class GodModeReply : Packet
  {
    public GodModeReply(bool reply) : base(0x2B, 2)
    {
      m_Stream.Write(reply);
    }
  }

  public sealed class PlayServerAck : Packet
  {
    internal static int m_AuthID = -1;

    public PlayServerAck(ServerInfo si) : base(0x8C, 11)
    {
      int addr = Utility.GetAddressValue(si.Address.Address);

      m_Stream.Write((byte)addr);
      m_Stream.Write((byte)(addr >> 8));
      m_Stream.Write((byte)(addr >> 16));
      m_Stream.Write((byte)(addr >> 24));

      m_Stream.Write((short)si.Address.Port);
      m_Stream.Write(m_AuthID);
    }
  }
}
