/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BufferedFileWriter.cs                                           *
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Guilds;

namespace Server
{
    public class BufferWriter : IGenericWriter
    {
        private const int LargeByteBufferSize = 256;

        private readonly Encoding m_Encoding;
        private readonly bool m_PrefixStrings;

        protected byte[] m_Buffer;
        protected long m_Index;

        private readonly char[] m_SingleCharBuffer = new char[1];

        private byte[] m_CharacterBuffer;

        private int m_MaxBufferChars;

        public BufferWriter(byte[] buffer, bool prefixStr)
        {
            m_PrefixStrings = prefixStr;
            m_Encoding = Utility.UTF8;
            m_Buffer = buffer;
        }

        public BufferWriter(bool prefixStr)
        {
            m_PrefixStrings = prefixStr;
            m_Encoding = Utility.UTF8;
            m_Buffer = new byte[BufferSize];
        }

        public virtual long Position => m_Index;

        protected virtual int BufferSize => 256;

        public byte[] Data => m_Buffer;

        public virtual void Close()
        {
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int size)
        {
            var copy = new byte[size];
            Buffer.BlockCopy(m_Buffer, 0, copy, 0, Math.Min(size, m_Buffer.Length));
            m_Buffer = copy;
        }

        public virtual void Flush()
        {
            Resize(m_Buffer.Length * 2);
        }

        public void Reset()
        {
            m_Index = 0;
        }

        public virtual long Seek(long offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Begin   => m_Index = offset,
                SeekOrigin.Current => m_Index += offset,
                SeekOrigin.End     => m_Index = BufferSize - offset,
                _                  => m_Index
            };
        }

        public void WriteEncodedInt(int value)
        {
            var v = (uint)value;

            while (v >= 0x80)
            {
                if (m_Index + 1 > m_Buffer.Length)
                {
                    Flush();
                }

                m_Buffer[m_Index++] = (byte)(v | 0x80);
                v >>= 7;
            }

            if (m_Index + 1 > m_Buffer.Length)
            {
                Flush();
            }

            m_Buffer[m_Index++] = (byte)v;
        }

        public void Write(string value)
        {
            if (m_PrefixStrings)
            {
                if (value == null)
                {
                    if (m_Index + 1 > m_Buffer.Length)
                    {
                        Flush();
                    }

                    m_Buffer[m_Index++] = 0;
                }
                else
                {
                    if (m_Index + 1 > m_Buffer.Length)
                    {
                        Flush();
                    }

                    m_Buffer[m_Index++] = 1;

                    InternalWriteString(value);
                }
            }
            else
            {
                InternalWriteString(value);
            }
        }

        public void Write(DateTime value)
        {
            Write(value.Ticks);
        }

        public void Write(DateTimeOffset value)
        {
            Write(value.Ticks);
            Write(value.Offset.Ticks);
        }

        public void WriteDeltaTime(DateTime value)
        {
            var ticks = value.Ticks;
            var now = DateTime.UtcNow.Ticks;

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

        public void Write(IPAddress value)
        {
            Write(Utility.GetLongAddressValue(value));
        }

        public void Write(TimeSpan value)
        {
            Write(value.Ticks);
        }

        public void Write(decimal value)
        {
            var bits = decimal.GetBits(value);

            for (var i = 0; i < bits.Length; ++i)
            {
                Write(bits[i]);
            }
        }

        public void Write(long value)
        {
            if (m_Index + 8 > m_Buffer.Length)
            {
                Flush();
            }

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

        public void Write(ulong value)
        {
            if (m_Index + 8 > m_Buffer.Length)
            {
                Flush();
            }

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

        public void Write(int value)
        {
            if (m_Index + 4 > m_Buffer.Length)
            {
                Flush();
            }

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Buffer[m_Index + 2] = (byte)(value >> 16);
            m_Buffer[m_Index + 3] = (byte)(value >> 24);
            m_Index += 4;
        }

        public void Write(uint value)
        {
            if (m_Index + 4 > m_Buffer.Length)
            {
                Flush();
            }

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Buffer[m_Index + 2] = (byte)(value >> 16);
            m_Buffer[m_Index + 3] = (byte)(value >> 24);
            m_Index += 4;
        }

        public void Write(short value)
        {
            if (m_Index + 2 > m_Buffer.Length)
            {
                Flush();
            }

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Index += 2;
        }

        public void Write(ushort value)
        {
            if (m_Index + 2 > m_Buffer.Length)
            {
                Flush();
            }

            m_Buffer[m_Index] = (byte)value;
            m_Buffer[m_Index + 1] = (byte)(value >> 8);
            m_Index += 2;
        }

        public unsafe void Write(double value)
        {
            if (m_Index + 8 > m_Buffer.Length)
            {
                Flush();
            }

            fixed (byte* pBuffer = m_Buffer)
            {
                *(double*)(pBuffer + m_Index) = value;
            }

            m_Index += 8;
        }

        public unsafe void Write(float value)
        {
            if (m_Index + 4 > m_Buffer.Length)
            {
                Flush();
            }

            fixed (byte* pBuffer = m_Buffer)
            {
                *(float*)(pBuffer + m_Index) = value;
            }

            m_Index += 4;
        }

        public void Write(char value)
        {
            if (m_Index + 8 > m_Buffer.Length)
            {
                Flush();
            }

            m_SingleCharBuffer[0] = value;

            var byteCount = m_Encoding.GetBytes(m_SingleCharBuffer, 0, 1, m_Buffer, (int)m_Index);
            m_Index += byteCount;
        }

        public void Write(byte value)
        {
            if (m_Index + 1 > m_Buffer.Length)
            {
                Flush();
            }

            m_Buffer[m_Index++] = value;
        }

        public void Write(byte[] value, int length)
        {
            var remaining = length;
            var idx = 0;

            while (remaining > 0)
            {
                int size = Math.Min(m_Buffer.Length - (int)m_Index, remaining);
                Buffer.BlockCopy(value, idx, m_Buffer, (int)m_Index, size);
                // value.Slice(idx).CopyTo(m_Buffer.AsSpan(m_Index, size));

                remaining -= size;
                m_Index += size;
                idx += size;

                if (m_Index == m_Buffer.Length)
                {
                    Flush();
                }
            }
        }

        public void Write(sbyte value)
        {
            if (m_Index + 1 > m_Buffer.Length)
            {
                Flush();
            }

            m_Buffer[m_Index++] = (byte)value;
        }

        public void Write(bool value)
        {
            if (m_Index + 1 > m_Buffer.Length)
            {
                Flush();
            }

            m_Buffer[m_Index++] = (byte)(value ? 1 : 0);
        }

        public void Write(Point3D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
            Write(value.m_Z);
        }

        public void Write(Point2D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
        }

        public void Write(Rectangle2D value)
        {
            Write(value.Start);
            Write(value.End);
        }

        public void Write(Rectangle3D value)
        {
            Write(value.Start);
            Write(value.End);
        }

        public void Write(Map value)
        {
            if (value != null)
            {
                Write((byte)value.MapIndex);
            }
            else
            {
                Write((byte)0xFF);
            }
        }

        public void Write(Race value)
        {
            if (value != null)
            {
                Write((byte)value.RaceIndex);
            }
            else
            {
                Write((byte)0xFF);
            }
        }

        public void WriteEntity(IEntity value)
        {
            Write(value?.Deleted != false ? Serial.MinusOne : value.Serial);
        }

        public void Write(Item value)
        {
            Write(value?.Deleted != false ? Serial.MinusOne : value.Serial);
        }

        public void Write(Mobile value)
        {
            Write(value?.Deleted != false ? Serial.MinusOne : value.Serial);
        }

        public void Write(BaseGuild value)
        {
            if (value == null)
            {
                Write(0);
            }
            else
            {
                Write(value.Serial);
            }
        }

        public void WriteItem<T>(T value) where T : Item
        {
            Write(value);
        }

        public void WriteMobile<T>(T value) where T : Mobile
        {
            Write(value);
        }

        public void WriteGuild<T>(T value) where T : BaseGuild
        {
            Write(value);
        }

        public void Write(List<Item> list)
        {
            WriteItemList(list);
        }

        public void Write(List<Item> list, bool tidy)
        {
            WriteItemList(list, tidy);
        }

        public void WriteItemList<T>(List<T> list) where T : Item
        {
            WriteItemList(list, false);
        }

        public void WriteItemList<T>(List<T> list, bool tidy) where T : Item
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Deleted)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void Write(HashSet<Item> set)
        {
            Write(set, false);
        }

        public void Write(HashSet<Item> set, bool tidy)
        {
            if (tidy)
            {
                set.RemoveWhere(item => item.Deleted);
            }

            Write(set.Count);

            foreach (var item in set)
            {
                Write(item);
            }
        }

        public void WriteItemSet<T>(HashSet<T> set) where T : Item
        {
            WriteItemSet(set, false);
        }

        public void WriteItemSet<T>(HashSet<T> set, bool tidy) where T : Item
        {
            if (tidy)
            {
                set.RemoveWhere(item => item.Deleted);
            }

            Write(set.Count);

            foreach (var item in set)
            {
                Write(item);
            }
        }

        public void Write(List<Mobile> list)
        {
            Write(list, false);
        }

        public void Write(List<Mobile> list, bool tidy)
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Deleted)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void WriteMobileList<T>(List<T> list) where T : Mobile
        {
            WriteMobileList(list, false);
        }

        public void WriteMobileList<T>(List<T> list, bool tidy) where T : Mobile
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Deleted)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void Write(HashSet<Mobile> set)
        {
            Write(set, false);
        }

        public void Write(HashSet<Mobile> set, bool tidy)
        {
            if (tidy)
            {
                set.RemoveWhere(mobile => mobile.Deleted);
            }

            Write(set.Count);

            foreach (var mob in set)
            {
                Write(mob);
            }
        }

        public void WriteMobileSet<T>(HashSet<T> set) where T : Mobile
        {
            WriteMobileSet(set, false);
        }

        public void WriteMobileSet<T>(HashSet<T> set, bool tidy) where T : Mobile
        {
            if (tidy)
            {
                set.RemoveWhere(mob => mob.Deleted);
            }

            Write(set.Count);

            foreach (var mob in set)
            {
                Write(mob);
            }
        }

        public void Write(List<BaseGuild> list)
        {
            Write(list, false);
        }

        public void Write(List<BaseGuild> list, bool tidy)
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Disbanded)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void WriteGuildList<T>(List<T> list) where T : BaseGuild
        {
            WriteGuildList(list, false);
        }

        public void WriteGuildList<T>(List<T> list, bool tidy) where T : BaseGuild
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Disbanded)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void Write(HashSet<BaseGuild> set)
        {
            Write(set, false);
        }

        public void Write(HashSet<BaseGuild> set, bool tidy)
        {
            if (tidy)
            {
                set.RemoveWhere(guild => guild.Disbanded);
            }

            Write(set.Count);

            foreach (var guild in set)
            {
                Write(guild);
            }
        }

        public void WriteGuildSet<T>(HashSet<T> set) where T : BaseGuild
        {
            WriteGuildSet(set, false);
        }

        public void WriteGuildSet<T>(HashSet<T> set, bool tidy) where T : BaseGuild
        {
            if (tidy)
            {
                set.RemoveWhere(guild => guild.Disbanded);
            }

            Write(set.Count);

            foreach (var guild in set)
            {
                Write(guild);
            }
        }

        internal void InternalWriteString(string value)
        {
            var length = m_Encoding.GetByteCount(value);

            WriteEncodedInt(length);

            if (m_CharacterBuffer == null)
            {
                m_CharacterBuffer = new byte[LargeByteBufferSize];
                m_MaxBufferChars = LargeByteBufferSize / m_Encoding.GetMaxByteCount(1);
            }

            if (length > LargeByteBufferSize)
            {
                var current = 0;
                var charsLeft = value.Length;

                while (charsLeft > 0)
                {
                    var charCount = charsLeft > m_MaxBufferChars ? m_MaxBufferChars : charsLeft;
                    var byteLength = m_Encoding.GetBytes(value, current, charCount, m_CharacterBuffer, 0);

                    if (m_Index + byteLength > m_Buffer.Length)
                    {
                        Flush();
                    }

                    Buffer.BlockCopy(m_CharacterBuffer, 0, m_Buffer, (int)m_Index, byteLength);
                    m_Index += byteLength;

                    current += charCount;
                    charsLeft -= charCount;
                }
            }
            else
            {
                var byteLength = m_Encoding.GetBytes(value, 0, value.Length, m_CharacterBuffer, 0);

                if (m_Index + byteLength > m_Buffer.Length)
                {
                    Flush();
                }

                Buffer.BlockCopy(m_CharacterBuffer, 0, m_Buffer, (int)m_Index, byteLength);
                m_Index += byteLength;
            }
        }
    }
}
