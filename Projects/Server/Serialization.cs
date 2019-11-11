/***************************************************************************
 *                             Serialization.cs
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Server.Guilds;

namespace Server
{
  public abstract class GenericReader
  {
    public abstract string ReadString();
    public abstract DateTime ReadDateTime();
    public abstract DateTimeOffset ReadDateTimeOffset();
    public abstract TimeSpan ReadTimeSpan();
    public abstract DateTime ReadDeltaTime();
    public abstract decimal ReadDecimal();
    public abstract long ReadLong();
    public abstract ulong ReadULong();
    public abstract int ReadInt();
    public abstract uint ReadUInt();
    public abstract short ReadShort();
    public abstract ushort ReadUShort();
    public abstract double ReadDouble();
    public abstract float ReadFloat();
    public abstract char ReadChar();
    public abstract byte ReadByte();
    public abstract sbyte ReadSByte();
    public abstract bool ReadBool();
    public abstract int ReadEncodedInt();
    public abstract IPAddress ReadIPAddress();

    public abstract Point3D ReadPoint3D();
    public abstract Point2D ReadPoint2D();
    public abstract Rectangle2D ReadRect2D();
    public abstract Rectangle3D ReadRect3D();
    public abstract Map ReadMap();

    public abstract IEntity ReadEntity();
    public abstract Item ReadItem();
    public abstract Mobile ReadMobile();
    public abstract BaseGuild ReadGuild();

    public abstract T ReadItem<T>() where T : Item;
    public abstract T ReadMobile<T>() where T : Mobile;
    public abstract T ReadGuild<T>() where T : BaseGuild;

    public abstract List<Item> ReadStrongItemList();
    public abstract List<T> ReadStrongItemList<T>() where T : Item;

    public abstract List<Mobile> ReadStrongMobileList();
    public abstract List<T> ReadStrongMobileList<T>() where T : Mobile;

    public abstract List<BaseGuild> ReadStrongGuildList();
    public abstract List<T> ReadStrongGuildList<T>() where T : BaseGuild;

    public abstract HashSet<Item> ReadItemSet();
    public abstract HashSet<T> ReadItemSet<T>() where T : Item;

    public abstract HashSet<Mobile> ReadMobileSet();
    public abstract HashSet<T> ReadMobileSet<T>() where T : Mobile;

    public abstract HashSet<BaseGuild> ReadGuildSet();
    public abstract HashSet<T> ReadGuildSet<T>() where T : BaseGuild;

    public abstract Race ReadRace();

    public abstract bool End();
  }

  public abstract class GenericWriter
  {
    public abstract long Position{ get; }

    public abstract void Close();

    public abstract void Write(string value);
    public abstract void Write(DateTime value);
    public abstract void Write(DateTimeOffset value);
    public abstract void Write(TimeSpan value);
    public abstract void Write(decimal value);
    public abstract void Write(long value);
    public abstract void Write(ulong value);
    public abstract void Write(int value);
    public abstract void Write(uint value);
    public abstract void Write(short value);
    public abstract void Write(ushort value);
    public abstract void Write(double value);
    public abstract void Write(float value);
    public abstract void Write(char value);
    public abstract void Write(byte value);
    public abstract void Write(sbyte value);
    public abstract void Write(bool value);
    public abstract void WriteEncodedInt(int value);
    public abstract void Write(IPAddress value);

    public abstract void WriteDeltaTime(DateTime value);

    public abstract void Write(Point3D value);
    public abstract void Write(Point2D value);
    public abstract void Write(Rectangle2D value);
    public abstract void Write(Rectangle3D value);
    public abstract void Write(Map value);

    public abstract void WriteEntity(IEntity value);
    public abstract void Write(Item value);
    public abstract void Write(Mobile value);
    public abstract void Write(BaseGuild value);

    public abstract void WriteItem<T>(T value) where T : Item;
    public abstract void WriteMobile<T>(T value) where T : Mobile;
    public abstract void WriteGuild<T>(T value) where T : BaseGuild;

    public abstract void Write(Race value);

    public abstract void Write(List<Item> list);
    public abstract void Write(List<Item> list, bool tidy);

    public abstract void WriteItemList<T>(List<T> list) where T : Item;
    public abstract void WriteItemList<T>(List<T> list, bool tidy) where T : Item;

    public abstract void Write(HashSet<Item> list);
    public abstract void Write(HashSet<Item> list, bool tidy);

    public abstract void WriteItemSet<T>(HashSet<T> set) where T : Item;
    public abstract void WriteItemSet<T>(HashSet<T> set, bool tidy) where T : Item;

    public abstract void Write(List<Mobile> list);
    public abstract void Write(List<Mobile> list, bool tidy);

    public abstract void WriteMobileList<T>(List<T> list) where T : Mobile;
    public abstract void WriteMobileList<T>(List<T> list, bool tidy) where T : Mobile;

    public abstract void Write(HashSet<Mobile> list);
    public abstract void Write(HashSet<Mobile> list, bool tidy);

    public abstract void WriteMobileSet<T>(HashSet<T> set) where T : Mobile;
    public abstract void WriteMobileSet<T>(HashSet<T> set, bool tidy) where T : Mobile;

    public abstract void Write(List<BaseGuild> list);
    public abstract void Write(List<BaseGuild> list, bool tidy);

    public abstract void WriteGuildList<T>(List<T> list) where T : BaseGuild;
    public abstract void WriteGuildList<T>(List<T> list, bool tidy) where T : BaseGuild;

    public abstract void Write(HashSet<BaseGuild> list);
    public abstract void Write(HashSet<BaseGuild> list, bool tidy);

    public abstract void WriteGuildSet<T>(HashSet<T> set) where T : BaseGuild;
    public abstract void WriteGuildSet<T>(HashSet<T> set, bool tidy) where T : BaseGuild;

    // Compiler won't notice there 'where' to differentiate the generic methods.
  }

  public class BinaryFileWriter : GenericWriter
  {
    private const int LargeByteBufferSize = 256;

    private byte[] m_Buffer;

    private byte[] m_CharacterBuffer;

    private Encoding m_Encoding;
    private Stream m_File;

    private int m_Index;
    private int m_MaxBufferChars;

    private long m_Position;

    private char[] m_SingleCharBuffer = new char[1];
    private bool PrefixStrings;

    public BinaryFileWriter(Stream strm, bool prefixStr)
    {
      PrefixStrings = prefixStr;
      m_Encoding = Utility.UTF8;
      m_Buffer = new byte[BufferSize];
      m_File = strm;
    }

    public BinaryFileWriter(string filename, bool prefixStr)
    {
      PrefixStrings = prefixStr;
      m_Buffer = new byte[BufferSize];
      m_File = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
      m_Encoding = Utility.UTF8WithEncoding;
    }

    protected virtual int BufferSize => 64 * 1024;

    public override long Position => m_Position + m_Index;

    public Stream UnderlyingStream
    {
      get
      {
        if (m_Index > 0)
          Flush();

        return m_File;
      }
    }

    public void Flush()
    {
      if (m_Index > 0)
      {
        m_Position += m_Index;

        m_File.Write(m_Buffer, 0, m_Index);
        m_Index = 0;
      }
    }

    public override void Close()
    {
      if (m_Index > 0)
        Flush();

      m_File.Close();
    }

    public override void WriteEncodedInt(int value)
    {
      uint v = (uint)value;

      while (v >= 0x80)
      {
        if (m_Index + 1 > m_Buffer.Length)
          Flush();

        m_Buffer[m_Index++] = (byte)(v | 0x80);
        v >>= 7;
      }

      if (m_Index + 1 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index++] = (byte)v;
    }

    internal void InternalWriteString(string value)
    {
      int length = m_Encoding.GetByteCount(value);

      WriteEncodedInt(length);

      if (m_CharacterBuffer == null)
      {
        m_CharacterBuffer = new byte[LargeByteBufferSize];
        m_MaxBufferChars = LargeByteBufferSize / m_Encoding.GetMaxByteCount(1);
      }

      if (length > LargeByteBufferSize)
      {
        int current = 0;
        int charsLeft = value.Length;

        while (charsLeft > 0)
        {
          int charCount = charsLeft > m_MaxBufferChars ? m_MaxBufferChars : charsLeft;
          int byteLength = m_Encoding.GetBytes(value, current, charCount, m_CharacterBuffer, 0);

          if (m_Index + byteLength > m_Buffer.Length)
            Flush();

          Buffer.BlockCopy(m_CharacterBuffer, 0, m_Buffer, m_Index, byteLength);
          m_Index += byteLength;

          current += charCount;
          charsLeft -= charCount;
        }
      }
      else
      {
        int byteLength = m_Encoding.GetBytes(value, 0, value.Length, m_CharacterBuffer, 0);

        if (m_Index + byteLength > m_Buffer.Length)
          Flush();

        Buffer.BlockCopy(m_CharacterBuffer, 0, m_Buffer, m_Index, byteLength);
        m_Index += byteLength;
      }
    }

    public override void Write(string value)
    {
      if (PrefixStrings)
      {
        if (value == null)
        {
          if (m_Index + 1 > m_Buffer.Length)
            Flush();

          m_Buffer[m_Index++] = 0;
        }
        else
        {
          if (m_Index + 1 > m_Buffer.Length)
            Flush();

          m_Buffer[m_Index++] = 1;

          InternalWriteString(value);
        }
      }
      else
      {
        InternalWriteString(value);
      }
    }

    public override void Write(DateTime value)
    {
      Write(value.Ticks);
    }

    public override void Write(DateTimeOffset value)
    {
      Write(value.Ticks);
      Write(value.Offset.Ticks);
    }

    public override void WriteDeltaTime(DateTime value)
    {
      long ticks = value.Ticks;
      long now = DateTime.UtcNow.Ticks;

      TimeSpan d;

      try
      {
        d = new TimeSpan(ticks - now);
      }
      catch
      {
        d = TimeSpan.MaxValue;
      }

      Write(d);
    }

    public override void Write(IPAddress value)
    {
      Write(Utility.GetLongAddressValue(value));
    }

    public override void Write(TimeSpan value)
    {
      Write(value.Ticks);
    }

    public override void Write(decimal value)
    {
      int[] bits = decimal.GetBits(value);

      for (int i = 0; i < bits.Length; ++i)
        Write(bits[i]);
    }

    public override void Write(long value)
    {
      if (m_Index + 8 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index] = (byte)value;
      m_Buffer[m_Index + 1] = (byte)(value >> 8);
      m_Buffer[m_Index + 2] = (byte)(value >> 16);
      m_Buffer[m_Index + 3] = (byte)(value >> 24);
      m_Buffer[m_Index + 4] = (byte)(value >> 32);
      m_Buffer[m_Index + 5] = (byte)(value >> 40);
      m_Buffer[m_Index + 6] = (byte)(value >> 48);
      m_Buffer[m_Index + 7] = (byte)(value >> 56);
      m_Index += 8;
    }

    public override void Write(ulong value)
    {
      if (m_Index + 8 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index] = (byte)value;
      m_Buffer[m_Index + 1] = (byte)(value >> 8);
      m_Buffer[m_Index + 2] = (byte)(value >> 16);
      m_Buffer[m_Index + 3] = (byte)(value >> 24);
      m_Buffer[m_Index + 4] = (byte)(value >> 32);
      m_Buffer[m_Index + 5] = (byte)(value >> 40);
      m_Buffer[m_Index + 6] = (byte)(value >> 48);
      m_Buffer[m_Index + 7] = (byte)(value >> 56);
      m_Index += 8;
    }

    public override void Write(int value)
    {
      if (m_Index + 4 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index] = (byte)value;
      m_Buffer[m_Index + 1] = (byte)(value >> 8);
      m_Buffer[m_Index + 2] = (byte)(value >> 16);
      m_Buffer[m_Index + 3] = (byte)(value >> 24);
      m_Index += 4;
    }

    public override void Write(uint value)
    {
      if (m_Index + 4 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index] = (byte)value;
      m_Buffer[m_Index + 1] = (byte)(value >> 8);
      m_Buffer[m_Index + 2] = (byte)(value >> 16);
      m_Buffer[m_Index + 3] = (byte)(value >> 24);
      m_Index += 4;
    }

    public override void Write(short value)
    {
      if (m_Index + 2 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index] = (byte)value;
      m_Buffer[m_Index + 1] = (byte)(value >> 8);
      m_Index += 2;
    }

    public override void Write(ushort value)
    {
      if (m_Index + 2 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index] = (byte)value;
      m_Buffer[m_Index + 1] = (byte)(value >> 8);
      m_Index += 2;
    }

    public override unsafe void Write(double value)
    {
      if (m_Index + 8 > m_Buffer.Length)
        Flush();

      fixed (byte* pBuffer = m_Buffer)
      {
        *(double*)(pBuffer + m_Index) = value;
      }

      m_Index += 8;
    }

    public override unsafe void Write(float value)
    {
      if (m_Index + 4 > m_Buffer.Length)
        Flush();

      fixed (byte* pBuffer = m_Buffer)
      {
        *(float*)(pBuffer + m_Index) = value;
      }

      m_Index += 4;
    }

    public override void Write(char value)
    {
      if (m_Index + 8 > m_Buffer.Length)
        Flush();

      m_SingleCharBuffer[0] = value;

      int byteCount = m_Encoding.GetBytes(m_SingleCharBuffer, 0, 1, m_Buffer, m_Index);
      m_Index += byteCount;
    }

    public override void Write(byte value)
    {
      if (m_Index + 1 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index++] = value;
    }

    public override void Write(sbyte value)
    {
      if (m_Index + 1 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index++] = (byte)value;
    }

    public override void Write(bool value)
    {
      if (m_Index + 1 > m_Buffer.Length)
        Flush();

      m_Buffer[m_Index++] = (byte)(value ? 1 : 0);
    }

    public override void Write(Point3D value)
    {
      Write(value.m_X);
      Write(value.m_Y);
      Write(value.m_Z);
    }

    public override void Write(Point2D value)
    {
      Write(value.m_X);
      Write(value.m_Y);
    }

    public override void Write(Rectangle2D value)
    {
      Write(value.Start);
      Write(value.End);
    }

    public override void Write(Rectangle3D value)
    {
      Write(value.Start);
      Write(value.End);
    }

    public override void Write(Map value)
    {
      if (value != null)
        Write((byte)value.MapIndex);
      else
        Write((byte)0xFF);
    }

    public override void Write(Race value)
    {
      if (value != null)
        Write((byte)value.RaceIndex);
      else
        Write((byte)0xFF);
    }

    public override void WriteEntity(IEntity value)
    {
      if (value?.Deleted != false)
        Write(Serial.MinusOne);
      else
        Write(value.Serial);
    }

    public override void Write(Item value)
    {
      if (value?.Deleted != false)
        Write(Serial.MinusOne);
      else
        Write(value.Serial);
    }

    public override void Write(Mobile value)
    {
      if (value?.Deleted != false)
        Write(Serial.MinusOne);
      else
        Write(value.Serial);
    }

    public override void Write(BaseGuild value)
    {
      if (value == null)
        Write(0);
      else
        Write(value.Id);
    }

    public override void WriteItem<T>(T value)
    {
      Write(value);
    }

    public override void WriteMobile<T>(T value)
    {
      Write(value);
    }

    public override void WriteGuild<T>(T value)
    {
      Write(value);
    }

    public override void Write(List<Item> list)
    {
      Write(list, false);
    }

    public override void Write(List<Item> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void WriteItemList<T>(List<T> list)
    {
      WriteItemList(list, false);
    }

    public override void WriteItemList<T>(List<T> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void Write(HashSet<Item> set)
    {
      Write(set, false);
    }

    public override void Write(HashSet<Item> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(item => item.Deleted);

      Write(set.Count);

      foreach (Item item in set) Write(item);
    }

    public override void WriteItemSet<T>(HashSet<T> set)
    {
      WriteItemSet(set, false);
    }

    public override void WriteItemSet<T>(HashSet<T> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(item => item.Deleted);

      Write(set.Count);

      foreach (T item in set) Write(item);
    }

    public override void Write(List<Mobile> list)
    {
      Write(list, false);
    }

    public override void Write(List<Mobile> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void WriteMobileList<T>(List<T> list)
    {
      WriteMobileList(list, false);
    }

    public override void WriteMobileList<T>(List<T> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void Write(HashSet<Mobile> set)
    {
      Write(set, false);
    }

    public override void Write(HashSet<Mobile> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(mobile => mobile.Deleted);

      Write(set.Count);

      foreach (Mobile mob in set) Write(mob);
    }

    public override void WriteMobileSet<T>(HashSet<T> set)
    {
      WriteMobileSet(set, false);
    }

    public override void WriteMobileSet<T>(HashSet<T> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(mob => mob.Deleted);

      Write(set.Count);

      foreach (T mob in set) Write(mob);
    }

    public override void Write(List<BaseGuild> list)
    {
      Write(list, false);
    }

    public override void Write(List<BaseGuild> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Disbanded)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void WriteGuildList<T>(List<T> list)
    {
      WriteGuildList(list, false);
    }

    public override void WriteGuildList<T>(List<T> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Disbanded)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void Write(HashSet<BaseGuild> set)
    {
      Write(set, false);
    }

    public override void Write(HashSet<BaseGuild> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(guild => guild.Disbanded);

      Write(set.Count);

      foreach (BaseGuild guild in set) Write(guild);
    }

    public override void WriteGuildSet<T>(HashSet<T> set)
    {
      WriteGuildSet(set, false);
    }

    public override void WriteGuildSet<T>(HashSet<T> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(guild => guild.Disbanded);

      Write(set.Count);

      foreach (T guild in set) Write(guild);
    }
  }

  public sealed class BinaryFileReader : GenericReader
  {
    private BinaryReader m_File;

    public BinaryFileReader(BinaryReader br) => m_File = br;

    public long Position => m_File.BaseStream.Position;

    public void Close()
    {
      m_File.Close();
    }

    public long Seek(long offset, SeekOrigin origin) => m_File.BaseStream.Seek(offset, origin);

    public override string ReadString() => ReadByte() != 0 ? m_File.ReadString() : null;

    public override DateTime ReadDeltaTime()
    {
      long ticks = m_File.ReadInt64();
      long now = DateTime.UtcNow.Ticks;

      if (ticks > 0 && ticks + now < 0)
        return DateTime.MaxValue;
      if (ticks < 0 && ticks + now < 0)
        return DateTime.MinValue;

      try
      {
        return new DateTime(now + ticks);
      }
      catch
      {
        if (ticks > 0) return DateTime.MaxValue;
        return DateTime.MinValue;
      }
    }

    public override IPAddress ReadIPAddress() => new IPAddress(m_File.ReadInt64());

    public override int ReadEncodedInt()
    {
      int v = 0, shift = 0;
      byte b;

      do
      {
        b = m_File.ReadByte();
        v |= (b & 0x7F) << shift;
        shift += 7;
      } while (b >= 0x80);

      return v;
    }

    public override DateTime ReadDateTime() => new DateTime(m_File.ReadInt64());

    public override DateTimeOffset ReadDateTimeOffset()
    {
      long ticks = m_File.ReadInt64();
      TimeSpan offset = new TimeSpan(m_File.ReadInt64());

      return new DateTimeOffset(ticks, offset);
    }

    public override TimeSpan ReadTimeSpan() => new TimeSpan(m_File.ReadInt64());

    public override decimal ReadDecimal() => m_File.ReadDecimal();

    public override long ReadLong() => m_File.ReadInt64();

    public override ulong ReadULong() => m_File.ReadUInt64();

    public override int ReadInt() => m_File.ReadInt32();

    public override uint ReadUInt() => m_File.ReadUInt32();

    public override short ReadShort() => m_File.ReadInt16();

    public override ushort ReadUShort() => m_File.ReadUInt16();

    public override double ReadDouble() => m_File.ReadDouble();

    public override float ReadFloat() => m_File.ReadSingle();

    public override char ReadChar() => m_File.ReadChar();

    public override byte ReadByte() => m_File.ReadByte();

    public override sbyte ReadSByte() => m_File.ReadSByte();

    public override bool ReadBool() => m_File.ReadBoolean();

    public override Point3D ReadPoint3D() => new Point3D(ReadInt(), ReadInt(), ReadInt());

    public override Point2D ReadPoint2D() => new Point2D(ReadInt(), ReadInt());

    public override Rectangle2D ReadRect2D() => new Rectangle2D(ReadPoint2D(), ReadPoint2D());

    public override Rectangle3D ReadRect3D() => new Rectangle3D(ReadPoint3D(), ReadPoint3D());

    public override Map ReadMap() => Map.Maps[ReadByte()];

    public override IEntity ReadEntity()
    {
      Serial serial = ReadUInt();
      IEntity entity = World.FindEntity(serial);
      if (entity == null)
        return new Entity(serial, new Point3D(0, 0, 0), Map.Internal);
      return entity;
    }

    public override Item ReadItem() => World.FindItem(ReadUInt());

    public override Mobile ReadMobile() => World.FindMobile(ReadUInt());

    public override BaseGuild ReadGuild() => BaseGuild.Find(ReadUInt());

    public override T ReadItem<T>() => ReadItem() as T;

    public override T ReadMobile<T>() => ReadMobile() as T;

    public override T ReadGuild<T>() => ReadGuild() as T;

    public override List<Item> ReadStrongItemList() => ReadStrongItemList<Item>();

    public override List<T> ReadStrongItemList<T>()
    {
      int count = ReadInt();

      if (count > 0)
      {
        List<T> list = new List<T>(count);

        for (int i = 0; i < count; ++i)
          if (ReadItem() is T item)
            list.Add(item);

        return list;
      }

      return new List<T>();
    }

    public override HashSet<Item> ReadItemSet() => ReadItemSet<Item>();

    public override HashSet<T> ReadItemSet<T>()
    {
      int count = ReadInt();

      if (count > 0)
      {
        HashSet<T> set = new HashSet<T>();

        for (int i = 0; i < count; ++i)
          if (ReadItem() is T item)
            set.Add(item);

        return set;
      }

      return new HashSet<T>();
    }

    public override List<Mobile> ReadStrongMobileList() => ReadStrongMobileList<Mobile>();

    public override List<T> ReadStrongMobileList<T>()
    {
      int count = ReadInt();

      if (count > 0)
      {
        List<T> list = new List<T>(count);

        for (int i = 0; i < count; ++i)
          if (ReadMobile() is T m)
            list.Add(m);

        return list;
      }

      return new List<T>();
    }

    public override HashSet<Mobile> ReadMobileSet() => ReadMobileSet<Mobile>();

    public override HashSet<T> ReadMobileSet<T>()
    {
      int count = ReadInt();

      if (count > 0)
      {
        HashSet<T> set = new HashSet<T>();

        for (int i = 0; i < count; ++i)
          if (ReadMobile() is T item)
            set.Add(item);

        return set;
      }

      return new HashSet<T>();
    }

    public override List<BaseGuild> ReadStrongGuildList() => ReadStrongGuildList<BaseGuild>();

    public override List<T> ReadStrongGuildList<T>()
    {
      int count = ReadInt();

      if (count > 0)
      {
        List<T> list = new List<T>(count);

        for (int i = 0; i < count; ++i)
          if (ReadGuild() is T g)
            list.Add(g);

        return list;
      }

      return new List<T>();
    }

    public override HashSet<BaseGuild> ReadGuildSet() => ReadGuildSet<BaseGuild>();

    public override HashSet<T> ReadGuildSet<T>()
    {
      int count = ReadInt();

      if (count > 0)
      {
        HashSet<T> set = new HashSet<T>();

        for (int i = 0; i < count; ++i)
          if (ReadGuild() is T item)
            set.Add(item);

        return set;
      }

      return new HashSet<T>();
    }

    public override Race ReadRace() => Race.Races[ReadByte()];

    public override bool End() => m_File.PeekChar() == -1;
  }

  public sealed class AsyncWriter : GenericWriter
  {
    private int BufferSize;
    private BinaryWriter m_Bin;
    private bool m_Closed;
    private FileStream m_File;

    private long m_LastPos, m_CurPos;

    private MemoryStream m_Mem;
    private Thread m_WorkerThread;

    private Queue<MemoryStream> m_WriteQueue;
    private bool PrefixStrings;

    public AsyncWriter(string filename, bool prefix)
      : this(filename, 1048576, prefix) //1 mb buffer
    {
    }

    public AsyncWriter(string filename, int buffSize, bool prefix)
    {
      PrefixStrings = prefix;
      m_Closed = false;
      m_WriteQueue = new Queue<MemoryStream>();
      BufferSize = buffSize;

      m_File = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
      m_Mem = new MemoryStream(BufferSize + 1024);
      m_Bin = new BinaryWriter(m_Mem, Utility.UTF8WithEncoding);
    }

    public static int ThreadCount{ get; private set; }

    public MemoryStream MemStream
    {
      get => m_Mem;
      set
      {
        if (m_Mem.Length > 0)
          Enqueue(m_Mem);

        m_Mem = value;
        m_Bin = new BinaryWriter(m_Mem, Utility.UTF8WithEncoding);
        m_LastPos = 0;
        m_CurPos = m_Mem.Length;
        m_Mem.Seek(0, SeekOrigin.End);
      }
    }

    public override long Position => m_CurPos;

    private void Enqueue(MemoryStream mem)
    {
      lock (m_WriteQueue)
      {
        m_WriteQueue.Enqueue(mem);
      }

      if (m_WorkerThread.IsAlive != true)
      {
        m_WorkerThread = new Thread(new WorkerThread(this).Worker) { Priority = ThreadPriority.BelowNormal };
        m_WorkerThread.Start();
      }
    }

    private void OnWrite()
    {
      long curlen = m_Mem.Length;
      m_CurPos += curlen - m_LastPos;
      m_LastPos = curlen;
      if (curlen >= BufferSize)
      {
        Enqueue(m_Mem);
        m_Mem = new MemoryStream(BufferSize + 1024);
        m_Bin = new BinaryWriter(m_Mem, Utility.UTF8WithEncoding);
        m_LastPos = 0;
      }
    }

    public override void Close()
    {
      Enqueue(m_Mem);
      m_Closed = true;
    }

    public override void Write(IPAddress value)
    {
      m_Bin.Write(Utility.GetLongAddressValue(value));
      OnWrite();
    }

    public override void Write(string value)
    {
      if (PrefixStrings)
      {
        if (value == null)
        {
          m_Bin.Write((byte)0);
        }
        else
        {
          m_Bin.Write((byte)1);
          m_Bin.Write(value);
        }
      }
      else
      {
        m_Bin.Write(value);
      }

      OnWrite();
    }

    public override void WriteDeltaTime(DateTime value)
    {
      long ticks = value.Ticks;
      long now = DateTime.UtcNow.Ticks;

      TimeSpan d;

      try
      {
        d = new TimeSpan(ticks - now);
      }
      catch
      {
        d = TimeSpan.MaxValue;
      }

      Write(d);
    }

    public override void Write(DateTime value)
    {
      m_Bin.Write(value.Ticks);
      OnWrite();
    }

    public override void Write(DateTimeOffset value)
    {
      m_Bin.Write(value.Ticks);
      m_Bin.Write(value.Offset.Ticks);
      OnWrite();
    }

    public override void Write(TimeSpan value)
    {
      m_Bin.Write(value.Ticks);
      OnWrite();
    }

    public override void Write(decimal value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(long value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(ulong value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void WriteEncodedInt(int value)
    {
      uint v = (uint)value;

      while (v >= 0x80)
      {
        m_Bin.Write((byte)(v | 0x80));
        v >>= 7;
      }

      m_Bin.Write((byte)v);
      OnWrite();
    }

    public override void Write(int value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(uint value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(short value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(ushort value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(double value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(float value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(char value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(byte value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(sbyte value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(bool value)
    {
      m_Bin.Write(value);
      OnWrite();
    }

    public override void Write(Point3D value)
    {
      Write(value.m_X);
      Write(value.m_Y);
      Write(value.m_Z);
    }

    public override void Write(Point2D value)
    {
      Write(value.m_X);
      Write(value.m_Y);
    }

    public override void Write(Rectangle2D value)
    {
      Write(value.Start);
      Write(value.End);
    }

    public override void Write(Rectangle3D value)
    {
      Write(value.Start);
      Write(value.End);
    }

    public override void Write(Map value)
    {
      if (value != null)
        Write((byte)value.MapIndex);
      else
        Write((byte)0xFF);
    }

    public override void Write(Race value)
    {
      if (value != null)
        Write((byte)value.RaceIndex);
      else
        Write((byte)0xFF);
    }

    public override void WriteEntity(IEntity value)
    {
      if (value?.Deleted != false)
        Write(Serial.MinusOne);
      else
        Write(value.Serial);
    }

    public override void Write(Item value)
    {
      if (value?.Deleted != false)
        Write(Serial.MinusOne);
      else
        Write(value.Serial);
    }

    public override void Write(Mobile value)
    {
      if (value?.Deleted != false)
        Write(Serial.MinusOne);
      else
        Write(value.Serial);
    }

    public override void Write(BaseGuild value)
    {
      if (value == null)
        Write(0);
      else
        Write(value.Id);
    }

    public override void WriteItem<T>(T value)
    {
      Write(value);
    }

    public override void WriteMobile<T>(T value)
    {
      Write(value);
    }

    public override void WriteGuild<T>(T value)
    {
      Write(value);
    }

    public override void Write(List<Item> list)
    {
      Write(list, false);
    }

    public override void Write(List<Item> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void WriteItemList<T>(List<T> list)
    {
      WriteItemList(list, false);
    }

    public override void WriteItemList<T>(List<T> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void Write(HashSet<Item> set)
    {
      Write(set, false);
    }

    public override void Write(HashSet<Item> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(item => item.Deleted);

      Write(set.Count);

      foreach (Item item in set) Write(item);
    }

    public override void WriteItemSet<T>(HashSet<T> set)
    {
      WriteItemSet(set, false);
    }

    public override void WriteItemSet<T>(HashSet<T> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(item => item.Deleted);

      Write(set.Count);

      foreach (T item in set) Write(item);
    }

    public override void Write(List<Mobile> list)
    {
      Write(list, false);
    }

    public override void Write(List<Mobile> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void WriteMobileList<T>(List<T> list)
    {
      WriteMobileList(list, false);
    }

    public override void WriteMobileList<T>(List<T> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Deleted)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void Write(HashSet<Mobile> set)
    {
      Write(set, false);
    }

    public override void Write(HashSet<Mobile> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(mobile => mobile.Deleted);

      Write(set.Count);

      foreach (Mobile mob in set) Write(mob);
    }

    public override void WriteMobileSet<T>(HashSet<T> set)
    {
      WriteMobileSet(set, false);
    }

    public override void WriteMobileSet<T>(HashSet<T> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(mob => mob.Deleted);

      Write(set.Count);

      foreach (T mob in set) Write(mob);
    }

    public override void Write(List<BaseGuild> list)
    {
      Write(list, false);
    }

    public override void Write(List<BaseGuild> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Disbanded)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void WriteGuildList<T>(List<T> list)
    {
      WriteGuildList(list, false);
    }

    public override void WriteGuildList<T>(List<T> list, bool tidy)
    {
      if (tidy)
        for (int i = 0; i < list.Count;)
          if (list[i].Disbanded)
            list.RemoveAt(i);
          else
            ++i;

      Write(list.Count);

      for (int i = 0; i < list.Count; ++i)
        Write(list[i]);
    }

    public override void Write(HashSet<BaseGuild> set)
    {
      Write(set, false);
    }

    public override void Write(HashSet<BaseGuild> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(guild => guild.Disbanded);

      Write(set.Count);

      foreach (BaseGuild guild in set) Write(guild);
    }

    public override void WriteGuildSet<T>(HashSet<T> set)
    {
      WriteGuildSet(set, false);
    }

    public override void WriteGuildSet<T>(HashSet<T> set, bool tidy)
    {
      if (tidy) set.RemoveWhere(guild => guild.Disbanded);

      Write(set.Count);

      foreach (T guild in set) Write(guild);
    }

    private class WorkerThread
    {
      private AsyncWriter m_Owner;

      public WorkerThread(AsyncWriter owner) => m_Owner = owner;

      public void Worker()
      {
        ThreadCount++;

        int lastCount;

        do
        {
          MemoryStream mem = null;

          lock (m_Owner.m_WriteQueue)
          {
            if ((lastCount = m_Owner.m_WriteQueue.Count) > 0)
              mem = m_Owner.m_WriteQueue.Dequeue();
          }

          if (mem?.Length > 0)
            mem.WriteTo(m_Owner.m_File);
        } while (lastCount > 1);

        if (m_Owner.m_Closed)
          m_Owner.m_File.Close();

        ThreadCount--;

        if (ThreadCount <= 0)
          World.NotifyDiskWriteComplete();
      }
    }
  }

  public interface ISerializable
  {
    int TypeReference{ get; }
    uint SerialIdentity{ get; }
    void Serialize(GenericWriter writer);
  }
}
