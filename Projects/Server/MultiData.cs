/***************************************************************************
 *                               MultiData.cs
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
using Server.Network;

namespace Server
{
  public static class MultiData
  {
    public static Dictionary<int, MultiComponentList> Components { get; } = new Dictionary<int, MultiComponentList>();

    private static readonly BinaryReader m_IndexReader;
    private static readonly BinaryReader m_StreamReader;

    private static readonly bool UsingUOPFormat;

    static MultiData()
    {
      string multiUOPPath = Core.FindDataFile("MultiCollection.uop");

      if (File.Exists(multiUOPPath))
      {
        LoadUOP(multiUOPPath);
        UsingUOPFormat = true;
        return;
      }

      string idxPath = Core.FindDataFile("multi.idx");
      string mulPath = Core.FindDataFile("multi.mul");

      if (File.Exists(idxPath) && File.Exists(mulPath))
      {
        var idx = new FileStream(idxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        m_IndexReader = new BinaryReader(idx);

        var stream = new FileStream(mulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        m_StreamReader = new BinaryReader(stream);

        string vdPath = Core.FindDataFile("verdata.mul");

        if (!File.Exists(vdPath)) return;

        using FileStream fs = new FileStream(vdPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        BinaryReader bin = new BinaryReader(fs);

        int count = bin.ReadInt32();

        for (int i = 0; i < count; ++i)
        {
          int file = bin.ReadInt32();
          int index = bin.ReadInt32();
          int lookup = bin.ReadInt32();
          int length = bin.ReadInt32();
          bin.ReadInt32(); // extra

          if (file == 14 && index >= 0 && lookup >= 0 && length > 0)
          {
            bin.BaseStream.Seek(lookup, SeekOrigin.Begin);

            Components[index] = new MultiComponentList(bin, length / 12);

            bin.BaseStream.Seek(24 + i * 20, SeekOrigin.Begin);
          }
        }

        bin.Close();
      }
      else
        Console.WriteLine("Warning: Multi data files not found");
    }

    public static MultiComponentList GetComponents(int multiID)
    {
      MultiComponentList mcl;

      multiID &= 0x3FFF;

      if (Components.ContainsKey(multiID))
        mcl = Components[multiID];
      else if (!UsingUOPFormat)
        Components[multiID] = mcl = Load(multiID);
      else
        mcl = MultiComponentList.Empty;

      return mcl;
    }

    public static void LoadUOP(string path)
    {
      var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      BinaryReader streamReader = new BinaryReader(stream);

      // Head Information Start
      if (streamReader.ReadInt32() != 0x0050594D) // Not a UOP Files
        return;

      if (streamReader.ReadInt32() > 5) // Bad Version
        return;

      // Multi ID List Array Start
      UOPHash.BuildChunkIDs(out var chunkIds);
      // Multi ID List Array End

      streamReader.ReadUInt32();                      // format timestamp? 0xFD23EC43
      long startAddress = streamReader.ReadInt64();

      streamReader.ReadInt32();
      streamReader.ReadInt32();

      stream.Seek(startAddress, SeekOrigin.Begin);    // Head Information End

      long nextBlock;

      do
      {
        int blockFileCount = streamReader.ReadInt32();
        nextBlock = streamReader.ReadInt64();

        int index = 0;

        do
        {
          long offset = streamReader.ReadInt64();

          int headerSize = streamReader.ReadInt32();          // header length
          int compressedSize = streamReader.ReadInt32();      // compressed size
          int decompressedSize = streamReader.ReadInt32();    // decompressed size

          ulong filehash = streamReader.ReadUInt64();         // filename hash (HashLittle2)
          streamReader.ReadUInt32();
          short compressionMethod = streamReader.ReadInt16();              // compression method (0 = none, 1 = zlib)

          index++;

          if (offset == 0 || decompressedSize == 0 || filehash == 0x126D1E99DDEDEE0A) // Exclude housing.bin
            continue;

          chunkIds.TryGetValue(filehash, out var chunkID);

          long position = stream.Position;  // save current position

          stream.Seek(offset + headerSize, SeekOrigin.Begin);

          Span<byte> sourceData = new byte[compressedSize];

          if (stream.Read(sourceData) != compressedSize)
            continue;

          Span<byte> data;

          if (compressionMethod == 1)
          {
            data = new byte[decompressedSize];
            Compression.Unpack(data, ref decompressedSize, sourceData, compressedSize);
          }
          else
            data = sourceData;

          var tileList = new List<MultiTileEntry>();

          // Skip the first 4 bytes
          BufferReader<byte> reader = new BufferReader<byte>(data);

          reader.Advance(4); // ???
          reader.TryReadLittleEndian(out uint count);

          for (uint i = 0; i < count; i++)
          {
            reader.TryReadLittleEndian(out ushort itemid);
            reader.TryReadLittleEndian(out short x);
            reader.TryReadLittleEndian(out short y);
            reader.TryReadLittleEndian(out short z);
            reader.TryReadLittleEndian(out ushort flagValue);

            TileFlag tileFlag = flagValue switch
            {
              1 => TileFlag.None,
              257 => TileFlag.Generic,
              _ => TileFlag.Background // 0
            };

            reader.TryReadLittleEndian(out uint clilocsCount);
            reader.Advance(clilocsCount * 4); // bypass binary block

            tileList.Add(new MultiTileEntry(itemid, x, y, z, tileFlag));
          }

          Components[chunkID] = new MultiComponentList(tileList);

          stream.Seek(position, SeekOrigin.Begin); // back to position
        }
        while (index < blockFileCount);
      }
      while (stream.Seek(nextBlock, SeekOrigin.Begin) != 0);
    }

    // TODO: Change this to read the file all during load time
    public static MultiComponentList Load(int multiID)
    {
      try
      {
        m_IndexReader.BaseStream.Seek(multiID * 12, SeekOrigin.Begin);

        int lookup = m_IndexReader.ReadInt32();
        int length = m_IndexReader.ReadInt32();

        if (lookup < 0 || length <= 0)
          return MultiComponentList.Empty;

        m_StreamReader.BaseStream.Seek(lookup, SeekOrigin.Begin);

        return new MultiComponentList(m_StreamReader, length / (MultiComponentList.PostHSFormat ? 16 : 12));
      }
      catch
      {
        return MultiComponentList.Empty;
      }
    }
  }

  public struct MultiTileEntry
  {
    public ushort m_ItemID;
    public short m_OffsetX, m_OffsetY, m_OffsetZ;
    public TileFlag m_Flags;

    public MultiTileEntry(ushort itemID, short xOffset, short yOffset, short zOffset, TileFlag flags)
    {
      m_ItemID = itemID;
      m_OffsetX = xOffset;
      m_OffsetY = yOffset;
      m_OffsetZ = zOffset;
      m_Flags = flags;
    }
  }

  public sealed class MultiComponentList
  {
    public static readonly MultiComponentList Empty = new MultiComponentList();

    private Point2D m_Min, m_Max;

    public MultiComponentList(MultiComponentList toCopy)
    {
      m_Min = toCopy.m_Min;
      m_Max = toCopy.m_Max;

      Center = toCopy.Center;

      Width = toCopy.Width;
      Height = toCopy.Height;

      Tiles = new StaticTile[Width][][];

      for (int x = 0; x < Width; ++x)
      {
        Tiles[x] = new StaticTile[Height][];

        for (int y = 0; y < Height; ++y)
        {
          Tiles[x][y] = new StaticTile[toCopy.Tiles[x][y].Length];

          for (int i = 0; i < Tiles[x][y].Length; ++i)
            Tiles[x][y][i] = toCopy.Tiles[x][y][i];
        }
      }

      List = new MultiTileEntry[toCopy.List.Length];

      for (int i = 0; i < List.Length; ++i)
        List[i] = toCopy.List[i];
    }

    public MultiComponentList(IGenericReader reader)
    {
      int version = reader.ReadInt();

      m_Min = reader.ReadPoint2D();
      m_Max = reader.ReadPoint2D();
      Center = reader.ReadPoint2D();
      Width = reader.ReadInt();
      Height = reader.ReadInt();

      int length = reader.ReadInt();

      MultiTileEntry[] allTiles = List = new MultiTileEntry[length];

      if (version == 0)
        for (int i = 0; i < length; ++i)
        {
          int id = reader.ReadShort();
          if (id >= 0x4000)
            id -= 0x4000;

          allTiles[i].m_ItemID = (ushort)id;
          allTiles[i].m_OffsetX = reader.ReadShort();
          allTiles[i].m_OffsetY = reader.ReadShort();
          allTiles[i].m_OffsetZ = reader.ReadShort();
          allTiles[i].m_Flags = (TileFlag)reader.ReadInt();
        }
      else
        for (int i = 0; i < length; ++i)
        {
          allTiles[i].m_ItemID = reader.ReadUShort();
          allTiles[i].m_OffsetX = reader.ReadShort();
          allTiles[i].m_OffsetY = reader.ReadShort();
          allTiles[i].m_OffsetZ = reader.ReadShort();
          allTiles[i].m_Flags = (TileFlag)reader.ReadInt();
        }

      TileList[][] tiles = new TileList[Width][];
      Tiles = new StaticTile[Width][][];

      for (int x = 0; x < Width; ++x)
      {
        tiles[x] = new TileList[Height];
        Tiles[x] = new StaticTile[Height][];

        for (int y = 0; y < Height; ++y)
          tiles[x][y] = new TileList();
      }

      for (int i = 0; i < allTiles.Length; ++i)
        if (i == 0 || allTiles[i].m_Flags != 0)
        {
          int xOffset = allTiles[i].m_OffsetX + Center.m_X;
          int yOffset = allTiles[i].m_OffsetY + Center.m_Y;

          tiles[xOffset][yOffset].Add(allTiles[i].m_ItemID, (sbyte)allTiles[i].m_OffsetZ);
        }

      for (int x = 0; x < Width; ++x)
      for (int y = 0; y < Height; ++y)
        Tiles[x][y] = tiles[x][y].ToArray();
    }

    public MultiComponentList(BinaryReader reader, int count)
    {
      MultiTileEntry[] allTiles = List = new MultiTileEntry[count];

      for (int i = 0; i < count; ++i)
      {
        allTiles[i].m_ItemID = reader.ReadUInt16();
        allTiles[i].m_OffsetX = reader.ReadInt16();
        allTiles[i].m_OffsetY = reader.ReadInt16();
        allTiles[i].m_OffsetZ = reader.ReadInt16();

        if (PostHSFormat)
          allTiles[i].m_Flags = (TileFlag)reader.ReadUInt64();
        else
          allTiles[i].m_Flags = (TileFlag)reader.ReadUInt32();

        MultiTileEntry e = allTiles[i];

        if (i == 0 || e.m_Flags != 0)
        {
          if (e.m_OffsetX < m_Min.m_X)
            m_Min.m_X = e.m_OffsetX;

          if (e.m_OffsetY < m_Min.m_Y)
            m_Min.m_Y = e.m_OffsetY;

          if (e.m_OffsetX > m_Max.m_X)
            m_Max.m_X = e.m_OffsetX;

          if (e.m_OffsetY > m_Max.m_Y)
            m_Max.m_Y = e.m_OffsetY;
        }
      }

      Center = new Point2D(-m_Min.m_X, -m_Min.m_Y);
      Width = m_Max.m_X - m_Min.m_X + 1;
      Height = m_Max.m_Y - m_Min.m_Y + 1;

      TileList[][] tiles = new TileList[Width][];
      Tiles = new StaticTile[Width][][];

      for (int x = 0; x < Width; ++x)
      {
        tiles[x] = new TileList[Height];
        Tiles[x] = new StaticTile[Height][];

        for (int y = 0; y < Height; ++y)
          tiles[x][y] = new TileList();
      }

      for (int i = 0; i < allTiles.Length; ++i)
        if (i == 0 || allTiles[i].m_Flags != 0)
        {
          int xOffset = allTiles[i].m_OffsetX + Center.m_X;
          int yOffset = allTiles[i].m_OffsetY + Center.m_Y;

          tiles[xOffset][yOffset].Add(allTiles[i].m_ItemID, (sbyte)allTiles[i].m_OffsetZ);
        }

      for (int x = 0; x < Width; ++x)
      for (int y = 0; y < Height; ++y)
        Tiles[x][y] = tiles[x][y].ToArray();
    }

    public MultiComponentList(List<MultiTileEntry> list)
    {
      var allTiles = List = new MultiTileEntry[list.Count];

      for (int i = 0; i < list.Count; ++i)
      {
        allTiles[i].m_ItemID = list[i].m_ItemID;
        allTiles[i].m_OffsetX = list[i].m_OffsetX;
        allTiles[i].m_OffsetY = list[i].m_OffsetY;
        allTiles[i].m_OffsetZ = list[i].m_OffsetZ;

        allTiles[i].m_Flags = list[i].m_Flags;

        MultiTileEntry e = allTiles[i];

        if (i == 0 || e.m_Flags != 0)
        {
          if (e.m_OffsetX < m_Min.m_X) m_Min.m_X = e.m_OffsetX;

          if (e.m_OffsetY < m_Min.m_Y) m_Min.m_Y = e.m_OffsetY;

          if (e.m_OffsetX > m_Max.m_X) m_Max.m_X = e.m_OffsetX;

          if (e.m_OffsetY > m_Max.m_Y) m_Max.m_Y = e.m_OffsetY;
        }
      }

      Center = new Point2D(-m_Min.m_X, -m_Min.m_Y);
      Width = (m_Max.m_X - m_Min.m_X) + 1;
      Height = (m_Max.m_Y - m_Min.m_Y) + 1;

      var tiles = new TileList[Width][];
      Tiles = new StaticTile[Width][][];

      for (int x = 0; x < Width; ++x)
      {
        tiles[x] = new TileList[Height];
        Tiles[x] = new StaticTile[Height][];

        for (int y = 0; y < Height; ++y) tiles[x][y] = new TileList();
      }

      for (int i = 0; i < allTiles.Length; ++i)
        if (i == 0 || allTiles[i].m_Flags != 0)
        {
          int xOffset = allTiles[i].m_OffsetX + Center.m_X;
          int yOffset = allTiles[i].m_OffsetY + Center.m_Y;
          int itemID = ((allTiles[i].m_ItemID & TileData.MaxItemValue) | 0x10000);

          tiles[xOffset][yOffset].Add((ushort)itemID, (sbyte)allTiles[i].m_OffsetZ);
        }

      for (int x = 0; x < Width; ++x)
      for (int y = 0; y < Height; ++y)
        Tiles[x][y] = tiles[x][y].ToArray();
    }

    private MultiComponentList()
    {
      Tiles = new StaticTile[0][][];
      List = new MultiTileEntry[0];
    }

    public static bool PostHSFormat{ get; set; }

    public Point2D Min => m_Min;
    public Point2D Max => m_Max;

    public Point2D Center{ get; }

    public int Width{ get; private set; }

    public int Height{ get; private set; }

    public StaticTile[][][] Tiles{ get; private set; }

    public MultiTileEntry[] List{ get; private set; }

    public void Add(int itemID, int x, int y, int z)
    {
      int vx = x + Center.m_X;
      int vy = y + Center.m_Y;

      if (vx >= 0 && vx < Width && vy >= 0 && vy < Height)
      {
        StaticTile[] oldTiles = Tiles[vx][vy];

        for (int i = oldTiles.Length - 1; i >= 0; --i)
        {
          ItemData data = TileData.ItemTable[itemID & TileData.MaxItemValue];

          if (oldTiles[i].Z == z && oldTiles[i].Height > 0 == data.Height > 0)
          {
            bool newIsRoof = (data.Flags & TileFlag.Roof) != 0;
            bool oldIsRoof =
              (TileData.ItemTable[oldTiles[i].ID & TileData.MaxItemValue].Flags & TileFlag.Roof) != 0;

            if (newIsRoof == oldIsRoof)
              Remove(oldTiles[i].ID, x, y, z);
          }
        }

        oldTiles = Tiles[vx][vy];

        StaticTile[] newTiles = new StaticTile[oldTiles.Length + 1];

        for (int i = 0; i < oldTiles.Length; ++i)
          newTiles[i] = oldTiles[i];

        newTiles[oldTiles.Length] = new StaticTile((ushort)itemID, (sbyte)z);

        Tiles[vx][vy] = newTiles;

        MultiTileEntry[] oldList = List;
        MultiTileEntry[] newList = new MultiTileEntry[oldList.Length + 1];

        for (int i = 0; i < oldList.Length; ++i)
          newList[i] = oldList[i];

        newList[oldList.Length] = new MultiTileEntry((ushort)itemID, (short)x, (short)y, (short)z, TileFlag.Background);

        List = newList;

        if (x < m_Min.m_X)
          m_Min.m_X = x;

        if (y < m_Min.m_Y)
          m_Min.m_Y = y;

        if (x > m_Max.m_X)
          m_Max.m_X = x;

        if (y > m_Max.m_Y)
          m_Max.m_Y = y;
      }
    }

    public void RemoveXYZH(int x, int y, int z, int minHeight)
    {
      int vx = x + Center.m_X;
      int vy = y + Center.m_Y;

      if (vx >= 0 && vx < Width && vy >= 0 && vy < Height)
      {
        StaticTile[] oldTiles = Tiles[vx][vy];

        for (int i = 0; i < oldTiles.Length; ++i)
        {
          StaticTile tile = oldTiles[i];

          if (tile.Z == z && tile.Height >= minHeight)
          {
            StaticTile[] newTiles = new StaticTile[oldTiles.Length - 1];

            for (int j = 0; j < i; ++j)
              newTiles[j] = oldTiles[j];

            for (int j = i + 1; j < oldTiles.Length; ++j)
              newTiles[j - 1] = oldTiles[j];

            Tiles[vx][vy] = newTiles;

            break;
          }
        }

        MultiTileEntry[] oldList = List;

        for (int i = 0; i < oldList.Length; ++i)
        {
          MultiTileEntry tile = oldList[i];

          if (tile.m_OffsetX == (short)x && tile.m_OffsetY == (short)y && tile.m_OffsetZ == (short)z &&
              TileData.ItemTable[tile.m_ItemID & TileData.MaxItemValue].Height >= minHeight)
          {
            MultiTileEntry[] newList = new MultiTileEntry[oldList.Length - 1];

            for (int j = 0; j < i; ++j)
              newList[j] = oldList[j];

            for (int j = i + 1; j < oldList.Length; ++j)
              newList[j - 1] = oldList[j];

            List = newList;

            break;
          }
        }
      }
    }

    public void Remove(int itemID, int x, int y, int z)
    {
      int vx = x + Center.m_X;
      int vy = y + Center.m_Y;

      if (vx >= 0 && vx < Width && vy >= 0 && vy < Height)
      {
        StaticTile[] oldTiles = Tiles[vx][vy];

        for (int i = 0; i < oldTiles.Length; ++i)
        {
          StaticTile tile = oldTiles[i];

          if (tile.ID == itemID && tile.Z == z)
          {
            StaticTile[] newTiles = new StaticTile[oldTiles.Length - 1];

            for (int j = 0; j < i; ++j)
              newTiles[j] = oldTiles[j];

            for (int j = i + 1; j < oldTiles.Length; ++j)
              newTiles[j - 1] = oldTiles[j];

            Tiles[vx][vy] = newTiles;

            break;
          }
        }

        MultiTileEntry[] oldList = List;

        for (int i = 0; i < oldList.Length; ++i)
        {
          MultiTileEntry tile = oldList[i];

          if (tile.m_ItemID == itemID && tile.m_OffsetX == (short)x && tile.m_OffsetY == (short)y &&
              tile.m_OffsetZ == (short)z)
          {
            MultiTileEntry[] newList = new MultiTileEntry[oldList.Length - 1];

            for (int j = 0; j < i; ++j)
              newList[j] = oldList[j];

            for (int j = i + 1; j < oldList.Length; ++j)
              newList[j - 1] = oldList[j];

            List = newList;

            break;
          }
        }
      }
    }

    public void Resize(int newWidth, int newHeight)
    {
      int oldWidth = Width, oldHeight = Height;
      StaticTile[][][] oldTiles = Tiles;

      int totalLength = 0;

      StaticTile[][][] newTiles = new StaticTile[newWidth][][];

      for (int x = 0; x < newWidth; ++x)
      {
        newTiles[x] = new StaticTile[newHeight][];

        for (int y = 0; y < newHeight; ++y)
        {
          if (x < oldWidth && y < oldHeight)
            newTiles[x][y] = oldTiles[x][y];
          else
            newTiles[x][y] = new StaticTile[0];

          totalLength += newTiles[x][y].Length;
        }
      }

      Tiles = newTiles;
      List = new MultiTileEntry[totalLength];
      Width = newWidth;
      Height = newHeight;

      m_Min = Point2D.Zero;
      m_Max = Point2D.Zero;

      int index = 0;

      for (int x = 0; x < newWidth; ++x)
      for (int y = 0; y < newHeight; ++y)
      {
        StaticTile[] tiles = newTiles[x][y];

        for (int i = 0; i < tiles.Length; ++i)
        {
          StaticTile tile = tiles[i];

          int vx = x - Center.X;
          int vy = y - Center.Y;

          if (vx < m_Min.m_X)
            m_Min.m_X = vx;

          if (vy < m_Min.m_Y)
            m_Min.m_Y = vy;

          if (vx > m_Max.m_X)
            m_Max.m_X = vx;

          if (vy > m_Max.m_Y)
            m_Max.m_Y = vy;

          List[index++] = new MultiTileEntry((ushort)tile.ID, (short)vx, (short)vy, (short)tile.Z, TileFlag.Background);
        }
      }
    }

    public void Serialize(IGenericWriter writer)
    {
      writer.Write(1); // version;

      writer.Write(m_Min);
      writer.Write(m_Max);
      writer.Write(Center);

      writer.Write(Width);
      writer.Write(Height);

      writer.Write(List.Length);

      for (int i = 0; i < List.Length; ++i)
      {
        MultiTileEntry ent = List[i];

        writer.Write(ent.m_ItemID);
        writer.Write(ent.m_OffsetX);
        writer.Write(ent.m_OffsetY);
        writer.Write(ent.m_OffsetZ);
        writer.Write((int)ent.m_Flags);
      }
    }
  }

  public static class UOPHash
  {
    public static void BuildChunkIDs(out Dictionary<ulong, int> chunkIds)
    {
      const int maxId = 0x10000;

      chunkIds = new Dictionary<ulong, int>();

      for (int i = 0; i < maxId; ++i)
        chunkIds[HashLittle2($"build/multicollection/{i:000000}.bin")] = i;
    }

    private static ulong HashLittle2(string s)
    {
      int length = s.Length;

      uint b, c;
      uint a = b = c = 0xDEADBEEF + (uint)length;

      int k = 0;

      while (length > 12)
      {
        a += s[k];
        a += (uint)s[k + 1] << 8;
        a += (uint)s[k + 2] << 16;
        a += (uint)s[k + 3] << 24;
        b += s[k + 4];
        b += (uint)s[k + 5] << 8;
        b += (uint)s[k + 6] << 16;
        b += (uint)s[k + 7] << 24;
        c += s[k + 8];
        c += (uint)s[k + 9] << 8;
        c += (uint)s[k + 10] << 16;
        c += (uint)s[k + 11] << 24;

        a -= c; a ^= (c << 4) | (c >> 28); c += b;
        b -= a; b ^= (a << 6) | (a >> 26); a += c;
        c -= b; c ^= (b << 8) | (b >> 24); b += a;
        a -= c; a ^= (c << 16) | (c >> 16); c += b;
        b -= a; b ^= (a << 19) | (a >> 13); a += c;
        c -= b; c ^= (b << 4) | (b >> 28); b += a;

        length -= 12;
        k += 12;
      }

      if (length != 0)
      {
        switch (length)
        {
          case 12: c += (uint)s[k + 11] << 24; goto case 11;
          case 11: c += (uint)s[k + 10] << 16; goto case 10;
          case 10: c += (uint)s[k + 9] << 8; goto case 9;
          case 9: c += s[k + 8]; goto case 8;
          case 8: b += (uint)s[k + 7] << 24; goto case 7;
          case 7: b += (uint)s[k + 6] << 16; goto case 6;
          case 6: b += (uint)s[k + 5] << 8; goto case 5;
          case 5: b += s[k + 4]; goto case 4;
          case 4: a += (uint)s[k + 3] << 24; goto case 3;
          case 3: a += (uint)s[k + 2] << 16; goto case 2;
          case 2: a += (uint)s[k + 1] << 8; goto case 1;
          case 1: a += s[k]; break;
        }

        c ^= b; c -= (b << 14) | (b >> 18);
        a ^= c; a -= (c << 11) | (c >> 21);
        b ^= a; b -= (a << 25) | (a >> 7);
        c ^= b; c -= (b << 16) | (b >> 16);
        a ^= c; a -= (c << 4) | (c >> 28);
        b ^= a; b -= (a << 14) | (a >> 18);
        c ^= b; c -= (b << 24) | (b >> 8);
      }

      return ((ulong)b << 32) | c;
    }
  }
}
