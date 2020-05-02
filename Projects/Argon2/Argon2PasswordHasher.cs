using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
// using System.Text.RegularExpressions;

namespace Server.Accounting.Security
{
  /// <summary>
  /// PasswordHasher is a class for creating Argon2 hashes and verifying them. This is a wrapper around
  /// Daniel Dinu and Dmitry Khovratovich's Argon2 library.
  /// </summary>
  public class Argon2PasswordHasher
  {
    private static readonly RNGCryptoServiceProvider Rng = new RNGCryptoServiceProvider();

    // private static readonly Regex HashRegex = new Regex(@"^\$argon2([di])\$v=(\d+)$m=(\d+),t=(\d+),p=(\d+)\$([A-Za-z0-9+/=]+)\$([A-Za-z0-9+/=]*)$", RegexOptions.Compiled);


    /// <summary>
    /// How many iterations of the Argon2 hash to perform
    /// </summary>
    public uint TimeCost { get; set; }

    /// <summary>
    /// How much memory to use while hashing in kibibytes (KiB)
    /// </summary>
    public uint MemoryCost { get; set; }

    /// <summary>
    /// How many threads to use while hashing
    /// </summary>
    public uint Parallelism { get; set; }

    /// <summary>
    /// The type of Argon2 hashing algorithm to use
    /// Argon2d - The memory access is dependent upon the hash value (vulnerable to side-channel attacks)
    /// Argon2i - The memory access is independent upon the hash value (safe from side-channel atacks)
    /// </summary>
    public Argon2Type ArgonType { get; set; }

    /// <summary>
    /// Length of the generated raw hash in bytes
    /// </summary>
    public uint HashLength { get; set; }

    /// <summary>
    /// How strings should be decoded when passed to the Hash method.
    /// The default is Encoding.UTF8.
    /// </summary>
    public Encoding StringEncoding { get; set; }


    /// <summary>
    /// Initialize the Argon2 PasswordHasher with default performance and algorithm settings based upon the environment the hashing will be used in.
    /// You should perform your own profiling to determine what the parameters should be for your specific usage; however, this attempts to provide
    /// some reasonable defaults.
    /// </summary>
    public Argon2PasswordHasher()
    {
      TimeCost = 3;
      MemoryCost = 8192;
      Parallelism = 1;
      ArgonType = Argon2Type.Argon2i;
      HashLength = 32;
      StringEncoding = Encoding.UTF8;
    }


    /// <summary>
    /// Initialize the Argon2 PasswordHasher with the performance and algorithm settings to use while hashing
    /// <param name="timeCost">How many iterations of the Argon2 hash to perform (default: 3, must be at least 1)</param>
    /// <param name="memoryCost">How much memory to use while hashing in kibibytes (KiB) (default: 8192 KiB [8 MiB], must be at least 8 KiB)</param>
    /// <param name="parallelism">How many threads to use while hashing (default: 1, must be at least 1)</param>
    /// <param name="argonType">The type of Argon2 hashing algorithm to use (Independent [default] or Dependent)</param>
    /// <param name="hashLength">The length of the resulting hash in bytes (default: 32)</param>
    /// </summary>
    public Argon2PasswordHasher(uint timeCost = 3, uint memoryCost = 8192, uint parallelism = 1, Argon2Type argonType = Argon2Type.Argon2i, uint hashLength = 32)
    {
      TimeCost = timeCost;
      MemoryCost = memoryCost;
      Parallelism = parallelism;
      ArgonType = argonType;
      HashLength = hashLength;
      StringEncoding = Encoding.UTF8;
    }


    /// <summary>
    /// Hash the password using Argon2 with a cryptographically-secure, random, 16-byte salt.
    /// This is the only overload of the Hash method that the typical user will need to use for password storage. The other overloads are provided for interoperability purposes.
    /// Do not compare two Argon2 hashes directly. Instead, use the Verify or VerifyAndUpdate methods.
    /// <param name="password">A string representing the password to be hashed. The password is first decoded into bytes using StringEncoding (default: Encoding.UTF8)</param>
    /// <returns>A formatted string representing the hashed password, encoded with the parameters used to perform the hash</returns>
    /// </summary>
    public string Hash(string password)
    {
      CheckNull("Hash", "password", password);

      return Hash(StringEncoding.GetBytes(password));
    }

    /// <summary>
    /// Hash the raw password bytes using Argon2 with a cryptographically-secure, random, 16-byte salt.
    /// Do not compare two Argon2 hashes directly. Instead, use the Verify or VerifyAndUpdate methods.
    /// <param name="password">The raw bytes of the password to be hashed</param>
    /// <returns>A formatted string representing the hashed password, encoded with the parameters used to perform the hash</returns>
    /// </summary>
    public string Hash(byte[] password)
    {
      CheckNull("Hash", "password", password);

      return Hash(password, GenerateSalt());
    }

    /// <summary>
    /// Hash the password using Argon2 with the specified salt.
    /// Unless you need to specify your own salt for interoperability purposes, prefer the Hash(string password) overload instead.
    /// Do not compare two Argon2 hashes directly. Instead, use the Verify or VerifyAndUpdate methods.
    /// <param name="password">A string representing the password to be hashed. The password is first decoded into bytes using StringEncoding (default: Encoding.UTF8)</param>
    /// <param name="salt">A string representing the salt to be used for the hash. The salt must be at least 8 bytes. The salt is first decoded into bytes using StringEncoding (default: Encoding.UTF8)</param>
    /// <returns>A formatted string representing the hashed password, encoded with the parameters used to perform the hash</returns>
    /// </summary>
    public string Hash(string password, string salt)
    {
      CheckNull("Hash", "password", password, "salt", salt);

      return Hash(StringEncoding.GetBytes(password), StringEncoding.GetBytes(salt));
    }

    /// <summary>
    /// Hash the raw password bytes using Argon2 with the specified salt bytes.
    /// Unless you need to specify your own salt for interoperability purposes, prefer the Hash(byte[] password) overload instead.
    /// Do not compare two Argon2 hashes directly. Instead, use the Verify or VerifyAndUpdate methods.
    /// <param name="password">The raw bytes of the password to be hashed</param>
    /// <param name="salt">The raw salt bytes to be used for the hash. The salt must be at least 8 bytes.</param>
    /// <returns>A formatted string representing the hashed password, encoded with the parameters used to perform the hash</returns>
    /// </summary>
    public string Hash(byte[] password, byte[] salt)
    {
      CheckNull("Hash", "password", password, "salt", salt);

      byte[] hash = new byte[HashLength];
      byte[] encoded = new byte[39 + ((HashLength + salt.Length) * 4 + 3) / 3];
      var result = Argon2.Library.Hash(
        TimeCost,
        MemoryCost,
        Parallelism,
        password,
        salt,
        hash,
        encoded,
        (int)ArgonType,
        0x13
      );

      if (result != Argon2Error.OK)
        throw new Argon2Exception("hashing", result);

      var firstNonNull = encoded.Length - 2;
      while (encoded[firstNonNull] == 0)
        firstNonNull--;

      return Encoding.ASCII.GetString(encoded, 0, firstNonNull + 1);
    }


    /// <summary>
    /// Hash the password using Argon2 with the specified salt. The HashRaw methods may be used for password-based key derivation.
    /// Unless you're using HashRaw for key deriviation or for interoperability purposes, the Hash methods should be used in favor of the HashRaw methods.
    /// <param name="password">A string representing the password to be hashed. The password is first decoded into bytes using StringEncoding (default: Encoding.UTF8)</param>
    /// <param name="salt">A string representing the salt to be used for the hash. The salt must be at least 8 bytes. The salt is first decoded into bytes using StringEncoding (default: Encoding.UTF8)</param>
    /// <returns>A byte array containing only the resulting hash</returns>
    /// </summary>
    public byte[] HashRaw(string password, string salt)
    {
      CheckNull("HashRaw", "password", password, "salt", salt);

      return HashRaw(StringEncoding.GetBytes(password), StringEncoding.GetBytes(salt));
    }

    /// <summary>
    /// Hash the password using Argon2 with the specified salt. The HashRaw methods may be used for password-based key derivation.
    /// Unless you're using HashRaw for key deriviation or for interoperability purposes, the Hash methods should be used in favor of the HashRaw methods.
    /// <param name="password">The raw bytes of the password to be hashed</param>
    /// <param name="salt">The raw salt bytes to be used for the hash. The salt must be at least 8 bytes.</param>
    /// <returns>A byte array containing only the resulting hash</returns>
    /// </summary>
    public byte[] HashRaw(byte[] password, byte[] salt)
    {
      byte[] hash = new byte[(int)HashLength];

      var result = Argon2.Library.Hash(
        TimeCost,
        MemoryCost,
        Parallelism,
        password,
        salt,
        hash,
        null,
        (int)ArgonType,
        0x13
      );

      if (result != Argon2Error.OK)
        throw new Argon2Exception("raw hashing", result);

      return hash;
    }


    /// <summary>
    /// Hashes the password and verifies that the password results in the specified hash.
    /// The ArgonType must of this PasswordHasher object must match what was used to generate expectedHash.
    /// The other parameters (timeCost, etc.) do not need to match and the parameters embedded in the expectedHash will be used.
    /// <param name="expectedHash">Hashing the password should result in this hash</param>
    /// <param name="password">The password to hash and compare its result to expectedHash. The password is first decoded into bytes using StringEncoding (default: Encoding.UTF8)</param>
    /// <returns>Whether the password results in the expectedHash when hashed</returns>
    /// </summary>
    public bool Verify(string expectedHash, string password)
    {
      CheckNull("Verify", "expectedHash", expectedHash, "password", password);

      return Verify(expectedHash, StringEncoding.GetBytes(password));
    }

    /// <summary>
    /// Hashes the raw password bytes and verifies that the password results in the specified hash.
    /// The ArgonType must of this PasswordHasher object must match what was used to generate expectedHash.
    /// The other parameters (timeCost, etc.) do not need to match and the parameters embedded in the expectedHash will be used.
    /// <param name="expectedHash">Hashing the password should result in this hash</param>
    /// <param name="password">The raw password bytes to hash and compare its result to expectedHash</param>
    /// <returns>Whether the password results in the expectedHash when hashed</returns>
    /// </summary>
    public bool Verify(string expectedHash, byte[] password)
    {
      CheckNull("Verify", "expectedHash", expectedHash, "password", password);

      var result = Argon2.Library.Verify(StringEncoding.GetBytes(expectedHash), password, password.Length, (int)ArgonType);

      if (result == Argon2Error.OK || result == Argon2Error.VERIFY_MISMATCH || result == Argon2Error.DECODING_FAIL)
        return result == Argon2Error.OK;

      throw new Argon2Exception("verifying", result);
    }



    /// <summary>
    /// Hashes the password and verifies that the password results in the specified hash. (See Verify method)
    /// If the password verification is successful, this method checks to see if the memory cost, time cost, and parallelism
    /// match the parameters the PasswordHasher object was constructed with. If they do not much, then the password is rehashed
    /// using the new parameters and the result is outputted via the newFormattedHash parameter.
    /// <param name="expectedHash">Hashing the password should result in this hash</param>
    /// <param name="password">The password to hash and compare its result to expectedHash. The password is first decoded into bytes using StringEncoding (default: Encoding.UTF8)</param>
    /// <param name="isUpdated">Whether the cost parameters of expectedHash differ from the PasswordHasher object and if the password was rehashed using th new parameters. This is always false if the password was incorrect.</param>
    /// <param name="newFormattedHash">If isUpdated is true, then newFormattedHash is the password hashed with the new cost parameters. If isUpdated is false, then newFormattedHash is expectedHash.</param>
    /// <returns>Whether the password results in the expectedHash when hashed</returns>
    /// </summary>
    public bool VerifyAndUpdate(string expectedHash, string password, out bool isUpdated, out string newFormattedHash)
    {
      CheckNull("VerifyAndUpdate", "expectedHash", expectedHash, "password", password);

      return VerifyAndUpdate(expectedHash, StringEncoding.GetBytes(password), out isUpdated, out newFormattedHash);
    }

    /// <summary>
    /// Hashes the password and verifies that the password results in the specified hash. (See Verify method)
    /// If the password verification is successful, this method checks to see if the memory cost, time cost, and parallelism
    /// match the parameters the PasswordHasher object was constructed with. If they do not much, then the password is rehashed
    /// using the new parameters and the result is outputted via the newFormattedHash parameter.
    /// <param name="expectedHash">Hashing the password should result in this hash</param>
    /// <param name="password">The raw password bytes to hash and compare its result to expectedHash</param>
    /// <param name="isUpdated">Whether the cost parameters of expectedHash differ from the PasswordHasher object and if the password was rehashed using th new parameters. This is always false if the password was incorrect.</param>
    /// <param name="newFormattedHash">If isUpdated is true, then newFormattedHash is the password hashed with the new cost parameters. If isUpdated is false, then newFormattedHash is expectedHash.</param>
    /// <returns>Whether the password results in the expectedHash when hashed</returns>
    /// </summary>
    public bool VerifyAndUpdate(string expectedHash, byte[] password, out bool isUpdated, out string newFormattedHash)
    {
      CheckNull("VerifyAndUpdate", "expectedHash", expectedHash, "password", password);

      if (Verify(expectedHash, password))
      {
        var hashMetadata = ExtractMetadata(expectedHash);

        if (hashMetadata.MemoryCost != MemoryCost || hashMetadata.TimeCost != TimeCost || hashMetadata.Parallelism != Parallelism)
        {
          isUpdated = true;
          byte[] salt = hashMetadata.Salt;
          newFormattedHash = Hash(password, salt);
        }
        else
        {
          isUpdated = false;
          newFormattedHash = expectedHash;
        }

        return true;
      }

      isUpdated = false;
      newFormattedHash = expectedHash;
      return false;
    }


    /// <summary>
    /// Generate salt using a Cryptographically-Secure Pseudo-Random Number Generator
    /// <param name="byteLength">The number of bytes of salt to generate (default: 16)</param>
    /// <returns>A array of randomly-generated bytes</returns>
    /// </summary>
    public static byte[] GenerateSalt(uint byteLength = 16)
    {
      var salt = new byte[byteLength];
      Rng.GetBytes(salt);
      return salt;
    }


    /// <summary>
    /// Extracts the memory cost, time cost, etc. used to generate the Argon2 hash.
    /// <param name="formattedHash">An encoded Argon2 hash created by the Hash method</param>
    /// <returns>The hash metadata or null if the formattedHash was not a valid encoded Argon2 hash</returns>
    /// </summary>
    public static HashMetadata ExtractMetadata(string formattedHash)
    {
      CheckNull("ExtractMetadata", "formattedHash", formattedHash);

      var context = new Argon2Context
      {
        Out = Marshal.AllocHGlobal(formattedHash.Length),  // ensure the space to hold the hash is long enough
        OutLen = (uint)formattedHash.Length,
        Pwd = Marshal.AllocHGlobal(1),
        PwdLen = 1,
        Salt = Marshal.AllocHGlobal(formattedHash.Length),  // ensure the space to hold the salt is long enough
        SaltLen = (uint)formattedHash.Length,
        Secret = Marshal.AllocHGlobal(1),
        SecretLen = 1,
        AssocData = Marshal.AllocHGlobal(1),
        AssocDataLen = 1,
        TimeCost = 0,
        MemoryCost = 0,
        Lanes = 0,
        Threads = 0
      };

      try
      {
        var type = formattedHash.StartsWith("$argon2i") ? Argon2Type.Argon2i : Argon2Type.Argon2d;
        var result = Argon2.Library.Decode(context, $"{formattedHash}\0", (int)type);

        if (result != Argon2Error.OK)
          return null;

        var salt = new byte[context.SaltLen];
        var hash = new byte[context.OutLen];
        Marshal.Copy(context.Salt, salt, 0, salt.Length);
        Marshal.Copy(context.Out, hash, 0, hash.Length);

        return new HashMetadata
        {
          ArgonType = type,
          MemoryCost = context.MemoryCost,
          TimeCost = context.TimeCost,
          Parallelism = context.Threads,
          Salt = salt,
          Hash = hash
        };
      }
      finally
      {
        Marshal.FreeHGlobal(context.Out);
        Marshal.FreeHGlobal(context.Pwd);
        Marshal.FreeHGlobal(context.Salt);
        Marshal.FreeHGlobal(context.Secret);
        Marshal.FreeHGlobal(context.AssocData);
      }
    }


    private static void CheckNull(string methodName, params object[] arguments)
    {
      for (var i = 0; i < arguments.Length; i += 2)
        if (arguments[i + 1] == null)
          throw new ArgumentNullException(arguments[i].ToString(), string.Format("Argument {0} to method PasswordHasher.{1} is null", arguments[i], methodName));
    }
  }
}
