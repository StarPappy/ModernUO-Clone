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
using System.IO;

namespace Server
{
  public static class MultiData
  {
    private static MultiComponentList[] m_Components;

    private static FileStream m_Index, m_Stream;
    private static BinaryReader m_IndexReader, m_StreamReader;

    static MultiData()
    {
      string idxPath = Core.FindDataFile("multi.idx");
      string mulPath = Core.FindDataFile("multi.mul");

      if (File.Exists(idxPath) && File.Exists(mulPath))
      {
        m_Index = new FileStream(idxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        m_IndexReader = new BinaryReader(m_Index);

        m_Stream = new FileStream(mulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        m_StreamReader = new BinaryReader(m_Stream);

        m_Components = new MultiComponentList[(int)(m_Index.Length / 12)];

        string vdPath = Core.FindDataFile("verdata.mul");

        if (File.Exists(vdPath))
        {
          using FileStream fs = new FileStream(vdPath, FileMode.Open, FileAccess.Read, FileShare.Read);
          BinaryReader bin = new BinaryReader(fs);

          int count = bin.ReadInt32();

          for (int i = 0; i < count; ++i)
          {
            int file = bin.ReadInt32();
            int index = bin.ReadInt32();
            int lookup = bin.ReadInt32();
            int length = bin.ReadInt32();
            int extra = bin.ReadInt32();

            if (file == 14 && index >= 0 && index < m_Components.Length && lookup >= 0 && length > 0)
            {
              bin.BaseStream.Seek(lookup, SeekOrigin.Begin);

              m_Components[index] = new MultiComponentList(bin, length / 12);

              bin.BaseStream.Seek(24 + i * 20, SeekOrigin.Begin);
            }
          }

          bin.Close();
        }
      }
      else
      {
        Console.WriteLine("Warning: Multi data files not found");

        m_Components = new MultiComponentList[0];
      }
    }

    public static MultiComponentList GetComponents(int multiID)
    {
      MultiComponentList mcl;

      if (multiID >= 0 && multiID < m_Components.Length)
      {
        mcl = m_Components[multiID];

        if (mcl == null)
          m_Components[multiID] = mcl = Load(multiID);
      }
      else
      {
        mcl = MultiComponentList.Empty;
      }

      return mcl;
    }

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
    public int m_Flags;

    public MultiTileEntry(ushort itemID, short xOffset, short yOffset, short zOffset, int flags)
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

    public MultiComponentList(GenericReader reader)
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
          allTiles[i].m_Flags = reader.ReadInt();
        }
      else
        for (int i = 0; i < length; ++i)
        {
          allTiles[i].m_ItemID = reader.ReadUShort();
          allTiles[i].m_OffsetX = reader.ReadShort();
          allTiles[i].m_OffsetY = reader.ReadShort();
          allTiles[i].m_OffsetZ = reader.ReadShort();
          allTiles[i].m_Flags = reader.ReadInt();
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
        allTiles[i].m_Flags = reader.ReadInt32();

        if (PostHSFormat)
          reader.ReadInt32(); // ??

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

        newList[oldList.Length] = new MultiTileEntry((ushort)itemID, (short)x, (short)y, (short)z, 1);

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

          List[index++] = new MultiTileEntry((ushort)tile.ID, (short)vx, (short)vy, (short)tile.Z, 1);
        }
      }
    }

    public void Serialize(GenericWriter writer)
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
        writer.Write(ent.m_Flags);
      }
    }
  }
}