using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Core;

public static class HashHelper
{
    // Stable across runs and machines. string.GetHashCode() is randomized per process, so never use it here.
    public static uint Hash(string input)
    {
        byte[] digest = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return BinaryPrimitives.ReadUInt32BigEndian(digest); // first 4 bytes -> 0..2^32-1
    }
}