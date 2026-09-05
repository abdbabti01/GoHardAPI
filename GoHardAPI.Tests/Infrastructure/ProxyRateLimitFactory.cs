using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// Boots the real <c>Program.cs</c> pipeline (environment <c>"Testing"</c>) with an
    /// isolated in-memory <see cref="TrainingContext"/> and a known JWT key, so the
    /// <c>auth</c> / <c>GlobalLimiter</c> partitioning and the middleware order can be
    /// exercised end to end over HTTP.
    ///
    /// <para>The <see cref="TestServer"/> never populates a real socket peer address, so
    /// a front <see cref="IStartupFilter"/> middleware (which runs before everything in
    /// <c>Program.cs</c>, hence before <c>UseRateLimiter</c>) copies the
    /// <see cref="PeerIpHeader"/> request header into
    /// <c>HttpContext.Connection.RemoteIpAddress</c> — simulating what Kestrel sets from
    /// the TCP connection. Forwarding headers (<c>X-Real-IP</c> / <c>X-Forwarded-For</c>)
    /// can be sent as ordinary request headers; the app never reads them.</para>
    /// </summary>
    public class ProxyRateLimitFactory : WebApplicationFactory<Program>
    {
        /// <summary>Request header the front middleware maps to <c>Connection.RemoteIpAddress</c>.</summary>
        public const string PeerIpHeader = "X-Test-Peer-Ip";

        public const string JwtSecret = "proxy-rate-limit-tests-signing-key-0123456789abcd";
        public const string JwtIssuer = "GoHardAPI";
        public const string JwtAudience = "GoHardApp";

        private readonly string _databaseName = "prl-" + Guid.NewGuid().ToString("N");

        /// <summary>Extra configuration. Set before the first client is created.</summary>
        public Dictionary<string, string?> ConfigOverrides { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseSetting("JwtSettings:Secret", JwtSecret);
            builder.UseSetting("JwtSettings:Issuer", JwtIssuer);
            builder.UseSetting("JwtSettings:Audience", JwtAudience);
            builder.UseSetting("JwtSettings:ExpirationHours", "24");
            foreach (var kv in ConfigOverrides)
            {
                builder.UseSetting(kv.Key, kv.Value);
            }

            builder.ConfigureServices(services =>
            {
                foreach (var d in services.Where(s =>
                             s.ServiceType == typeof(DbContextOptions<TrainingContext>) ||
                             s.ServiceType == typeof(DbContextOptions) ||
                             s.ServiceType == typeof(TrainingContext)).ToList())
                {
                    services.Remove(d);
                }

                services.AddDbContext<TrainingContext>(o => o.UseInMemoryDatabase(_databaseName));

                foreach (var d in services
                             .Where(s => s.ImplementationType == typeof(DraftSessionCleanupService))
                             .ToList())
                {
                    services.Remove(d);
                }

                services.AddSingleton<IStartupFilter, PeerIpStartupFilter>();
            });
        }

        /// <summary>A client whose simulated socket peer address is <paramref name="peerIp"/>.</summary>
        public HttpClient CreateClientWithPeer(string peerIp)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add(PeerIpHeader, peerIp);
            return client;
        }

        /// <summary>A client with an authenticated JWT for <paramref name="userId"/> and the given peer IP.</summary>
        public HttpClient CreateClientForUser(int userId, string? peerIp = null)
        {
            SeedUser(userId);
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", MintToken(userId));
            if (peerIp is not null)
            {
                client.DefaultRequestHeaders.Add(PeerIpHeader, peerIp);
            }
            return client;
        }

        public void SeedUser(int userId)
        {
            using var scope = Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<TrainingContext>();
            if (!ctx.Users.Any(u => u.Id == userId))
            {
                ctx.Users.Add(new User
                {
                    Id = userId,
                    Name = $"user{userId}",
                    Email = $"user{userId}@example.com",
                    PasswordHash = "x",
                });
                ctx.SaveChanges();
            }
        }

        /// <summary>Seeds a user whose password actually verifies, so a real login can 200.</summary>
        public void SeedUserWithCredentials(int userId, string email, string password)
        {
            using var scope = Services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<TrainingContext>();
            if (!ctx.Users.Any(u => u.Email == email))
            {
                ctx.Users.Add(new User
                {
                    Id = userId,
                    Name = $"user{userId}",
                    Username = $"user{userId}",
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    IsActive = true,
                });
                ctx.SaveChanges();
            }
        }

        public static string MintToken(int userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
            var token = new JwtSecurityToken(
                issuer: JwtIssuer,
                audience: JwtAudience,
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, $"user{userId}"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                },
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private sealed class PeerIpStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use(async (context, nextMw) =>
                {
                    if (context.Request.Headers.TryGetValue(PeerIpHeader, out var raw)
                        && raw.Count == 1
                        && IPAddress.TryParse(raw[0], out var ip))
                    {
                        context.Connection.RemoteIpAddress = ip;
                    }

                    await nextMw();
                });

                next(app);
            };
        }
    }
}
