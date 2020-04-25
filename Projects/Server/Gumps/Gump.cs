/***************************************************************************
 *                                 Gump.cs
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
using System.Text;
using Server.Network;

namespace Server.Gumps
{
  public class Gump
  {
    private static uint m_NextSerial = 1;

    private static readonly byte[] m_BeginLayout = StringToBuffer("{ ");
    private static readonly byte[] m_EndLayout = StringToBuffer(" }");

    private static readonly byte[] m_NoMove = StringToBuffer("{ nomove }");
    private static readonly byte[] m_NoClose = StringToBuffer("{ noclose }");
    private static readonly byte[] m_NoDispose = StringToBuffer("{ nodispose }");
    private static readonly byte[] m_NoResize = StringToBuffer("{ noresize }");

    private readonly List<string> m_Strings;

    internal int m_TextEntries, m_Switches;

    public Gump(int x, int y)
    {
      do
      {
        Serial = m_NextSerial++;
      } while (Serial == 0); // standard client apparently doesn't send a gump response packet if serial == 0

      X = x;
      Y = y;

      TypeID = GetTypeID(GetType());

      Entries = new List<GumpEntry>();
      m_Strings = new List<string>();
    }

    public int TypeID { get; }

    public List<GumpEntry> Entries { get; }

    public uint Serial { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public bool Disposable { get; set; } = true;

    public bool Resizable { get; set; } = true;

    public bool Draggable { get; set; } = true;

    public bool Closable { get; set; } = true;

    public static int GetTypeID(Type type) => type?.FullName?.GetHashCode() ?? -1;

    public void AddPage(int page)
    {
      Add(new GumpPage(page));
    }

    public void AddAlphaRegion(int x, int y, int width, int height)
    {
      Add(new GumpAlphaRegion(x, y, width, height));
    }

    public void AddBackground(int x, int y, int width, int height, int gumpID)
    {
      Add(new GumpBackground(x, y, width, height, gumpID));
    }

    public void AddButton(int x, int y, int normalID, int pressedID, int buttonID,
      GumpButtonType type = GumpButtonType.Reply, int param = 0)
    {
      Add(new GumpButton(x, y, normalID, pressedID, buttonID, type, param));
    }

    public void AddCheck(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
    {
      Add(new GumpCheck(x, y, inactiveID, activeID, initialState, switchID));
    }

    public void AddGroup(int group)
    {
      Add(new GumpGroup(group));
    }

    public void AddTooltip(int number)
    {
      Add(new GumpTooltip(number));
    }

    public void AddHtml(int x, int y, int width, int height, string text, bool background = false, bool scrollbar = false)
    {
      Add(new GumpHtml(x, y, width, height, text, background, scrollbar));
    }

    public void AddHtmlLocalized(int x, int y, int width, int height, int number, bool background = false,
      bool scrollbar = false)
    {
      Add(new GumpHtmlLocalized(x, y, width, height, number, background, scrollbar));
    }

    public void AddHtmlLocalized(int x, int y, int width, int height, int number, int color, bool background = false,
      bool scrollbar = false)
    {
      Add(new GumpHtmlLocalized(x, y, width, height, number, color, background, scrollbar));
    }

    public void AddHtmlLocalized(int x, int y, int width, int height, int number, string args, int color,
      bool background = false, bool scrollbar = false)
    {
      Add(new GumpHtmlLocalized(x, y, width, height, number, args, color, background, scrollbar));
    }

    public void AddImage(int x, int y, int gumpID, int hue = 0)
    {
      Add(new GumpImage(x, y, gumpID, hue));
    }

    public void AddImageTiled(int x, int y, int width, int height, int gumpID)
    {
      Add(new GumpImageTiled(x, y, width, height, gumpID));
    }

    public void AddImageTiledButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type,
      int param, int itemID, int hue, int width, int height, int localizedTooltip = -1)
    {
      Add(new GumpImageTileButton(x, y, normalID, pressedID, buttonID, type, param, itemID, hue, width, height,
        localizedTooltip));
    }

    public void AddItem(int x, int y, int itemID, int hue = 0)
    {
      Add(new GumpItem(x, y, itemID, hue));
    }

    public void AddLabel(int x, int y, int hue, string text)
    {
      Add(new GumpLabel(x, y, hue, text));
    }

    public void AddLabelCropped(int x, int y, int width, int height, int hue, string text)
    {
      Add(new GumpLabelCropped(x, y, width, height, hue, text));
    }

    public void AddRadio(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
    {
      Add(new GumpRadio(x, y, inactiveID, activeID, initialState, switchID));
    }

    public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText)
    {
      Add(new GumpTextEntry(x, y, width, height, hue, entryID, initialText));
    }

    public void AddTextEntry(int x, int y, int width, int height, int hue, int entryID, string initialText, int size)
    {
      Add(new GumpTextEntryLimited(x, y, width, height, hue, entryID, initialText, size));
    }

    public void AddItemProperty(uint serial)
    {
      Add(new GumpItemProperty(serial));
    }

    public void AddSpriteImage(int x, int y, int gumpID, int width, int height, int sx, int sy)
    {
      Add(new GumpSpriteImage(x, y, gumpID, width, height, sx, sy));
    }

    public void AddECHandleInput()
    {
      Add(new GumpECHandleInput());
    }

    public void AddTooltip(int number, params TextDefinition[] args)
    {
      Add(new GumpTooltip(number, args));
    }

    public void AddGumpIDOverride(int gumpID)
    {
      Add(new GumpMasterGump(gumpID));
    }

    public void Add(GumpEntry g)
    {
      if (g.Parent != this)
      {
        g.Parent = this;
      }
      else if (!Entries.Contains(g))
      {
        Entries.Add(g);
      }
    }

    public void Remove(GumpEntry g)
    {
      if (g == null || !Entries.Contains(g))
        return;

      Entries.Remove(g);
      g.Parent = null;
    }

    public int Intern(string value)
    {
      var indexOf = m_Strings.IndexOf(value);

      if (indexOf >= 0) return indexOf;

      m_Strings.Add(value);
      return m_Strings.Count - 1;
    }

    public void SendTo(NetState state)
    {
      state.AddGump(this);
      state.Send(Compile(state));
    }

    public static byte[] StringToBuffer(string str) => Encoding.ASCII.GetBytes(str);

    private Packet Compile(NetState ns = null)
    {
      IGumpWriter disp;

      if (ns?.Unpack == true)
        disp = new DisplayGumpPacked(this);
      else
        disp = new DisplayGumpFast(this);

      if (!Draggable)
        disp.AppendLayout(m_NoMove);

      if (!Closable)
        disp.AppendLayout(m_NoClose);

      if (!Disposable)
        disp.AppendLayout(m_NoDispose);

      if (!Resizable)
        disp.AppendLayout(m_NoResize);

      var count = Entries.Count;

      for (var i = 0; i < count; ++i)
      {
        var e = Entries[i];

        disp.AppendLayout(m_BeginLayout);
        e.AppendTo(ns, disp);
        disp.AppendLayout(m_EndLayout);
      }

      disp.WriteStrings(m_Strings);

      disp.Flush();

      m_TextEntries = disp.TextEntries;
      m_Switches = disp.Switches;

      return (Packet)disp;
    }

    public virtual void OnResponse(NetState sender, RelayInfo info)
    {
    }

    public virtual void OnServerClose(NetState owner)
    {
    }
  }
}
