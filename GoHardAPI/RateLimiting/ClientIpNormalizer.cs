using System.Net;
using System.Net.Sockets;

namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Turns an <see cref="IPAddress"/> (the socket peer address) into a stable,
    /// low-cardinality rate-limit partition token.
    ///
    /// <list type="bullet">
    ///   <item>An IPv4-mapped IPv6 address (<c>::ffff:203.0.113.7</c>) and the plain
    ///     IPv4 address (<c>203.0.113.7</c>) produce the <b>same</b> token, so the two
    ///     wire representations of one client cannot be used to double a quota.</item>
    ///   <item>An IPv6 address is collapsed to its <c>/64</c> routing prefix. A single
    ///     end user is routinely assigned a whole <c>/64</c> (or larger), so keying on
    ///     the full 128-bit address would let one client mint up to 2^64 partition
    ///     keys and evade throttling; the prefix bounds that.</item>
    ///   <item>Never throws. An address family other than IPv4/IPv6 falls back to the
    ///     invariant string form.</item>
    /// </list>
    /// </summary>
    public static class ClientIpNormalizer
    {
        /// <summary>
        /// The normalized partition token for <paramref name="address"/>. IPv4 →
        /// dotted quad; IPv6 → <c>prefix/64</c>.
        /// </summary>
        public static string Normalize(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            var ip = address;

            if (ip.AddressFamily == AddressFamily.InterNetworkV6 && ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return Prefix64(ip).ToString() + "/64";
            }

            return ip.ToString();
        }

        /// <summary>Zeroes the low 64 bits of an IPv6 address, yielding its <c>/64</c> prefix.</summary>
        private static IPAddress Prefix64(IPAddress address)
        {
            Span<byte> bytes = stackalloc byte[16];
            if (!address.TryWriteBytes(bytes, out var written) || written != 16)
            {
                // Defensive: an IPv6 address always serializes to 16 bytes.
                return address;
            }

            for (var i = 8; i < 16; i++)
            {
                bytes[i] = 0;
            }

            // Drop any scope id: it is a local artifact, never part of the identity.
            return new IPAddress(bytes);
        }
    }
}
