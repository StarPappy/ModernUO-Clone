/***************************************************************************
 *                          GumpTextEntryLimited.cs
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

using Server.Network;

namespace Server.Gumps
{
  public class GumpTextEntryLimited : GumpEntry
  {
    private static byte[] m_LayoutName = Gump.StringToBuffer("textentrylimited");
    private int m_EntryID;
    private int m_Hue;
    private string m_InitialText;
    private int m_Size;
    private int m_Width, m_Height;
    private int m_X, m_Y;

    public GumpTextEntryLimited(int x, int y, int width, int height, int hue, int entryID, string initialText, int size = 0)
    {
      m_X = x;
      m_Y = y;
      m_Width = width;
      m_Height = height;
      m_Hue = hue;
      m_EntryID = entryID;
      m_InitialText = initialText;
      m_Size = size;
    }

    public int X
    {
      get => m_X;
      set => Delta(ref m_X, value);
    }

    public int Y
    {
      get => m_Y;
      set => Delta(ref m_Y, value);
    }

    public int Width
    {
      get => m_Width;
      set => Delta(ref m_Width, value);
    }

    public int Height
    {
      get => m_Height;
      set => Delta(ref m_Height, value);
    }

    public int Hue
    {
      get => m_Hue;
      set => Delta(ref m_Hue, value);
    }

    public int EntryID
    {
      get => m_EntryID;
      set => Delta(ref m_EntryID, value);
    }

    public string InitialText
    {
      get => m_InitialText;
      set => Delta(ref m_InitialText, value);
    }

    public int Size
    {
      get => m_Size;
      set => Delta(ref m_Size, value);
    }

    public override string Compile(NetState ns)
    {
      return
        $"{{ textentrylimited {m_X} {m_Y} {m_Width} {m_Height} {m_Hue} {m_EntryID} {Parent.Intern(m_InitialText)} {m_Size} }}";
    }

    public override void AppendTo(NetState ns, IGumpWriter disp)
    {
      disp.AppendLayout(m_LayoutName);
      disp.AppendLayout(m_X);
      disp.AppendLayout(m_Y);
      disp.AppendLayout(m_Width);
      disp.AppendLayout(m_Height);
      disp.AppendLayout(m_Hue);
      disp.AppendLayout(m_EntryID);
      disp.AppendLayout(Parent.Intern(m_InitialText));
      disp.AppendLayout(m_Size);

      disp.TextEntries++;
    }
  }
}
