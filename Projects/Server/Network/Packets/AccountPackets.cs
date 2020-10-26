/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AccountPackets.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.IO;
using Server.Accounting;
using System.Buffers;

namespace Server.Network
{
    public enum ALRReason : byte
    {
        Invalid = 0,
        InUse = 1,
        Blocked = 2,
        BadPass = 3,
        Idle = 254,
        BadComm = 255
    }

    public enum PMMessage : byte
    {
        None = 0,
        CharNoExist = 1,
        CharExists = 2,
        CharInWorld = 5,
        LoginSyncError = 6,
        IdleWarning = 7
    }

    public enum DeleteResultType
    {
        PasswordInvalid,
        CharNotExist,
        CharBeingPlayed,
        CharTooYoung,
        CharQueued,
        BadRequest
    }

    public static partial class Packets
    {
        /**
         * Packet: 0x81
         * Length: Up to 425 bytes
         *
         * Displays the list of characters during the login process.
         * Note: Currently Unused
         */
        public static void SendChangeCharacter(NetState ns, IAccount a)
        {
            if (ns == null || a == null || !ns.GetAvailableSendPipe(out var buffer))
            {
                return;
            }

            var length = 5 + a.Length * 60;
            var writer = new CircularBufferWriter(buffer);

            writer.Write((byte)0x81); // Packet ID
            writer.Write((ushort)length);
            writer.Write((ushort)0); // Count & Placeholder

            int count = 0;

            for (var i = 0; i < a.Length; ++i)
            {
                var m = a[i];

                if (m == null)
                {
                    writer.Clear(60);
                }
                else
                {
                    var name = (m.RawName?.Trim()).DefaultIfNullOrEmpty("-no name-");

                    count++;
                    writer.WriteAscii(name, 30);
                    writer.Clear(30); // Password (empty)
                }
            }

            var position = writer.Position;
            writer.Seek(3, SeekOrigin.Begin);
            writer.Write((byte)count);
            writer.Seek(position, SeekOrigin.Begin);

            ns.Send(ref buffer, writer.Position);
        }

        /**
         * Packet: 0xBD
         * Length: 3 bytes
         *
         * Sends a requests for the client version
         */
        public static void SendClientVersionRequest(NetState ns)
        {
            if (ns != null && ns.GetAvailableSendPipe(out var buffer))
            {
                buffer[0] = 0xBD; // Packet ID
                buffer[1] = 0x00;
                buffer[2] = 0x03; // Length

                ns.Send(ref buffer, 3);
            }
        }

        /**
         * Packet: 0x85
         * Length: 2 bytes
         *
         * Sends the result of a deletion request
         */
        public static void SendCharacterDeleteResult(NetState ns, DeleteResultType res)
        {
            if (ns != null && ns.GetAvailableSendPipe(out var buffer))
            {
                buffer[0] = 0x85; // Packet ID
                buffer[1] = (byte)res;

                ns.Send(ref buffer, 2);
            }
        }

        /**
         * Packet: 0x53
         * Length: 2 bytes
         *
         * Sends a PopupMessage with a predetermined message
         */
        public static void SendPopupMessage(NetState ns, PMMessage msg)
        {
            if (ns != null && ns.GetAvailableSendPipe(out var buffer))
            {
                buffer[0] = 0x53; // Packet ID
                buffer[1] = (byte)msg;

                ns.Send(ref buffer, 2);
            }
        }

        /**
         * Packet: 0xB9
         * Length: 3 or 5 bytes
         *
         * Sends support features based on the client version
         */
        public static void SendSupportedFeature(NetState ns)
        {
            if (ns == null || !ns.GetAvailableSendPipe(out var buffer))
            {
                return;
            }

            var flags = ExpansionInfo.CoreExpansion.SupportedFeatures;

            if (ns.Account.Limit >= 6)
            {
                flags |= FeatureFlags.LiveAccount;
                flags &= ~FeatureFlags.UOTD;

                if (ns.Account.Limit > 6)
                {
                    flags |= FeatureFlags.SeventhCharacterSlot;
                }
                else
                {
                    flags |= FeatureFlags.SixthCharacterSlot;
                }
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xB9); // Packet ID

            if (ns.ExtendedSupportedFeatures)
            {
                writer.Write((uint)flags);
            }
            else
            {
                writer.Write((ushort)flags);
            }

            ns.Send(ref buffer, ns.ExtendedSupportedFeatures ? 5 : 3);
        }

        /**
         * Packet: 0x1B
         * Length: 37 bytes
         *
         * Sends login confirmation
         */
        public static void SendLoginConfirmation(NetState ns, Mobile m)
        {
            if (ns == null || !ns.GetAvailableSendPipe(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x1B); // PacketID
            writer.Write(m.Serial);
            writer.Write(0);
            writer.Write((short)m.Body);
            writer.Write((short)m.X);
            writer.Write((short)m.Y);
            writer.Write((short)m.Z);
            writer.Write((byte)m.Direction);
            writer.Write((byte)0);
            writer.Write(-1);

            writer.Write(0);

            var map = m.Map;

            if (map == null || map == Map.Internal)
            {
                map = m.LogoutMap;
            }

            writer.Write((short)(map?.Width ?? Map.Felucca.Width));
            writer.Write((short)(map?.Height ?? Map.Felucca.Height));
            writer.Clear();

            ns.Send(ref buffer, 37);
        }

        /**
         * Packet: 0x55
         * Length: 1 byte
         *
         * Sends login completion
         */
        public static void SendLoginComplete(NetState ns)
        {
            if (ns != null && ns.GetAvailableSendPipe(out var buffer))
            {
                buffer[0] = 0x55; // Packet ID

                ns.Send(ref buffer, 1);
            }
        }

        /**
         * Packet: 0x86
         * Length: Up to 424 bytes
         *
         * Sends updated character list
         */
        public static void SendCharacterListUpdate(NetState ns, IAccount a)
        {
            if (ns == null || a == null || !ns.GetAvailableSendPipe(out var buffer))
            {
                return;
            }

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0x86); // Packet ID
            writer.Seek(2, SeekOrigin.Current); // Length

            var highSlot = -1;

            for (var i = a.Length - 1; i >= 0; i--)
            {
                if (a[i] != null)
                {
                    highSlot = i;
                    break;
                }
            }

            var count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);
            writer.Write((byte)count);

            for (int i = 0; i < count; i++)
            {
                var m = a[i];

                if (m == null)
                {
                    writer.Clear(60);
                }
                else
                {
                    var name = (m.RawName?.Trim()).DefaultIfNullOrEmpty("-no name-");
                    writer.WriteAscii(name, 30);
                    writer.Clear(30); // password
                }
            }

            writer.WritePacketLength();
            ns.Send(ref buffer, writer.Position);
        }

        /**
         * Packet: 0xA9
         * Length: 1410 or more bytes
         *
         * Sends list of characters and starting cities
         */
        public static void SendCharacterList(NetState ns)
        {
            var acct = ns?.Account;

            if (acct == null || !ns.GetAvailableSendPipe(out var buffer))
            {
                return;
            }

            var cityInfo = ns.CityInfo;

            var writer = new CircularBufferWriter(buffer);
            writer.Write((byte)0xA9); // Packet ID
            writer.Seek(2, SeekOrigin.Current); // Length

            var highSlot = -1;

            for (var i = acct.Length - 1; i >= 0; i--)
            {
                if (acct[i] != null)
                {
                    highSlot = i;
                    break;
                }
            }

            var count = Math.Max(Math.Max(highSlot + 1, acct.Limit), 5);
            writer.Write((byte)count);

            for (int i = 0; i < count; i++)
            {
                var m = acct[i];

                if (m == null)
                {
                    writer.Clear(60);
                }
                else
                {
                    var name = (m.RawName?.Trim()).DefaultIfNullOrEmpty("-no name-");
                    writer.WriteAscii(name, 30);
                    writer.Clear(30); // password
                }
            }

            writer.Write((byte)cityInfo.Length);

            for (int i = 0; i < cityInfo.Length; ++i)
            {
                var ci = cityInfo[i];

                writer.Write((byte)i);
                writer.WriteAscii(ci.City, 32);
                writer.WriteAscii(ci.Building, 32);
                writer.Write(ci.X);
                writer.Write(ci.Y);
                writer.Write(ci.Z);
                writer.Write(ci.Map.MapID);
                writer.Write(ci.Description);
                writer.Write(0);
            }

            var flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

            if (count > 6)
            {
                flags |= CharacterListFlags.SeventhCharacterSlot |
                         CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
            }
            else if (count == 6)
            {
                flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
            }
            else if (acct.Limit == 1)
            {
                flags |= CharacterListFlags.SlotLimit &
                         CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character
            }

            writer.Write((int)flags);
            writer.Write((short)-1);

            writer.WritePacketLength();
            ns.Send(ref buffer, writer.Position);
        }
    }
}
