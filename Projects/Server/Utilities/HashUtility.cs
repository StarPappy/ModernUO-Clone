﻿/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: HashUtility.cs                                                  *
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
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Standart.Hash.xxHash;

namespace Server;

/// <summary>
/// Represents supported non-cryptographic fast hash algorithms.
/// </summary>
public enum FastHashAlgorithm
{
    None, // Used for collisions where full-data is serialized instead
    XXHash3_64, // xxHash3 64bit
    XXHash_32, // xxHash 32bit
}

public static class HashUtility
{
    // *************** DO NOT CHANGE THIS NUMBER ****************
    // * Computed hashes might be serialized against this seed! *
    // **********************************************************
    private const ulong xxHash3Seed = 9609125370673258709ul; // Randomly generated 64-bit prime number
    private const uint xxHashSeed = 665738807u; // Randomly generated 32-bit prime number

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ComputeHash64(string? data, FastHashAlgorithm algorithm = FastHashAlgorithm.XXHash3_64) =>
        algorithm switch
        {
            FastHashAlgorithm.XXHash3_64 => ComputeXXHash3_64(data),
            _                            => throw new NotSupportedException($"Hash {algorithm} is not supported.")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ComputeXXHash3_64(string? data) => data == null ? 0 : xxHash3.ComputeHash(data, xxHash3Seed);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ComputeHash32(string? data, FastHashAlgorithm algorithm = FastHashAlgorithm.XXHash_32) =>
        algorithm switch
        {
            FastHashAlgorithm.XXHash_32 => ComputeXXHash3_32(data),
            _                            => throw new NotSupportedException($"Hash {algorithm} is not supported.")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ComputeXXHash3_32(string? data) => data == null ? 0 : xxHash32.ComputeHash(data, xxHashSeed);

    public static unsafe int GetNetFrameworkHashCode(this string? str)
    {
        if (str == null)
        {
            return 0;
        }

        fixed (char* src = &str.GetPinnableReference())
        {
            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            uint* ptr = (uint*)src;
            int length = str.Length;

            while (length > 2)
            {
                length -= 4;
                // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
                hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                ptr += 2;
            }

            if (length > 0)
            {
                // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
                hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[0];
            }

            return (int)(hash1 + hash2 * 1566083941);
        }
    }
}
