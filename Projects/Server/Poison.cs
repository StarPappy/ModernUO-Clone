/***************************************************************************
 *                                 Poison.cs
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

namespace Server
{
  [Parsable]
  public abstract class Poison
  {
    /*public abstract TimeSpan Interval{ get; }
      public abstract TimeSpan Duration{ get; }*/
    public abstract string Name{ get; }
    public abstract int Level{ get; }

    public static Poison Lesser => GetPoison("Lesser");
    public static Poison Regular => GetPoison("Regular");
    public static Poison Greater => GetPoison("Greater");
    public static Poison Deadly => GetPoison("Deadly");
    public static Poison Lethal => GetPoison("Lethal");

    public static List<Poison> Poisons{ get; } = new List<Poison>();

    public abstract Timer ConstructTimer(Mobile m);
    /*public abstract void OnDamage( Mobile m, ref object state );*/

    public override string ToString() => Name;


    public static void Register(Poison reg)
    {
      string regName = reg.Name.ToLower();

      for (int i = 0; i < Poisons.Count; i++)
      {
        if (reg.Level == Poisons[i].Level)
          throw new Exception("A poison with that level already exists.");
        if (regName == Poisons[i].Name.ToLower())
          throw new Exception("A poison with that name already exists.");
      }

      Poisons.Add(reg);
    }

    public static Poison Parse(string value) => (int.TryParse(value, out int plevel) ? GetPoison(plevel) : null) ?? GetPoison(value);

    public static Poison GetPoison(int level)
    {
      for (int i = 0; i < Poisons.Count; ++i)
      {
        Poison p = Poisons[i];

        if (p.Level == level)
          return p;
      }

      return null;
    }

    public static Poison GetPoison(string name)
    {
      for (int i = 0; i < Poisons.Count; ++i)
      {
        Poison p = Poisons[i];

        if (Utility.InsensitiveCompare(p.Name, name) == 0)
          return p;
      }

      return null;
    }

    public static void Serialize(Poison p, GenericWriter writer)
    {
      if (p == null)
      {
        writer.Write((byte)0);
      }
      else
      {
        writer.Write((byte)1);
        writer.Write((byte)p.Level);
      }
    }

    public static Poison Deserialize(GenericReader reader)
    {
      switch (reader.ReadByte())
      {
        case 1: return GetPoison(reader.ReadByte());
        case 2:
          //no longer used, safe to remove?
          reader.ReadInt();
          reader.ReadDouble();
          reader.ReadInt();
          reader.ReadTimeSpan();
          break;
      }

      return null;
    }
  }
}