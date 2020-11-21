/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OutgoingMovementPackets.cs                                      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers;

namespace Server.Network
{
    public enum SpeedControlSetting
    {
        Disable,
        Mount,
        Walk
    }

    public static class OutgoingMovementPackets
    {
        public static void SendSpeedControl(this NetState ns, SpeedControlSetting speedControl)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)06);
            writer.Write((ushort)0x26); // Subpacket
            writer.Write((byte)speedControl);

            ns.Send(ref buffer, writer.Position);
        }

        public static void SendMovePlayer(this NetState ns, Direction d)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            buffer[0] = 0x97; // Packet ID
            buffer[1] = (byte)d;

            ns.Send(ref buffer, 2);
        }

        public static void SendMovementAck(this NetState ns, int seq, Mobile m) =>
            ns.SendMovementAck(seq, Notoriety.Compute(m, m));

        public static void SendMovementAck(this NetState ns, int seq, int noto)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            buffer[0] = 0x22; // Packet ID
            buffer[1] = (byte)seq;
            buffer[2] = (byte)noto;

            ns.Send(ref buffer, 3);
        }

        public static void SendMovementRej(this NetState ns, int seq, Mobile m)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x21); // Packet ID
            writer.Write((byte)seq);
            writer.Write((short)m.X);
            writer.Write((short)m.Y);
            writer.Write((byte)m.Direction);
            writer.Write((sbyte)m.Z);

            ns.Send(ref buffer, 8);
        }

        public static void SendInitialFaswalkStack(
            this NetState ns, int k1 = 0, int k2 = 0, int k3 = 0, int k4 = 0, int k5 = 0, int k6 = 0
        )
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)0x1); // Subpacket
            writer.Write(k1);
            writer.Write(k2);
            writer.Write(k3);
            writer.Write(k4);
            writer.Write(k5);
            writer.Write(k6);
        }

        public static void SendAddToFastwalkStack(this NetState ns, int k1 = 0)
        {
            if (ns == null || !ns.GetSendBuffer(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xBF);  // Packet ID
            writer.Write((ushort)0x2); // Subpacket
            writer.Write(k1);
        }
    }
}
