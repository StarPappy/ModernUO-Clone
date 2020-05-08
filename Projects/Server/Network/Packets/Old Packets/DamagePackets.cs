/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: DamagePackets.cs - Created: 2020/05/03 - Updated: 2020/05/03    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
  public sealed class DamagePacketOld : Packet
  {
    public DamagePacketOld(Mobile m, int amount) : base(0xBF)
    {
      EnsureCapacity(11);

      Stream.Write((short)0x22);
      Stream.Write((byte)1);
      Stream.Write(m.Serial);

      if (amount > 255)
        amount = 255;
      else if (amount < 0)
        amount = 0;

      Stream.Write((byte)amount);
    }
  }

  public sealed class DamagePacket : Packet
  {
    public DamagePacket(Mobile m, int amount) : base(0x0B, 7)
    {
      Stream.Write(m.Serial);

      if (amount > 0xFFFF)
        amount = 0xFFFF;
      else if (amount < 0)
        amount = 0;

      Stream.Write((ushort)amount);
    }
  }
}
