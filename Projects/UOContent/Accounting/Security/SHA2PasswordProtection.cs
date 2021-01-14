/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SHA2PasswordProtection.cs                                       *
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
using System.Security.Cryptography;
using System.Text;
using Server.Text;

namespace Server.Accounting.Security
{
    public class SHA2PasswordProtection : IPasswordProtection
    {
        public static IPasswordProtection Instance = new SHA2PasswordProtection();
        private readonly SHA512CryptoServiceProvider m_SHA2HashProvider = new();

        public string EncryptPassword(string plainPassword)
        {
            byte[] bytes = plainPassword.AsSpan(0, Math.Min(256, plainPassword.Length)).GetBytesAscii();
            return m_SHA2HashProvider.ComputeHash(bytes).ToHexString();
        }

        public bool ValidatePassword(string encryptedPassword, string plainPassword) =>
            EncryptPassword(plainPassword) == encryptedPassword;
    }
}
