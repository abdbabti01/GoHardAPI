using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Repositories;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Real-PostgreSQL evidence for the cross-instance guarantees: the
    /// <c>ProfilePhotoUrl</c> compare-and-set (an <c>UPDATE ... WHERE
    /// ProfilePhotoUrl = @previous</c>) serialises overlapping upload/upload and
    /// upload/remove for one user via genuine row locking - exactly one write
    /// wins, the loser cleans up only its own new file, and the winner's file is
    /// never deleted by a concurrent operation.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    [Collection(ProfilePostgresCollection.Name)]
    public sealed class ProfilePhotoConcurrencyPostgresTests : IDisposable
    {
        private readonly ProfilePostgresFixture _pg;
        private readonly TestProfilePhotoStorage _storage = new();

        public ProfilePhotoConcurrencyPostgresTests(ProfilePostgresFixture pg) => _pg = pg;

        public void Dispose() => _storage.Dispose();

        private ProfileController Controller(TrainingContext ctx, int userId)
        {
            var c = new ProfileController(
                ctx, _storage.Service, new UserRepository(ctx), new CurrentMeasurementsService(ctx),
                new TestScopeFactory(() => _pg.NewContext()));
            c.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return c;
        }

        private static IFormFile Jpeg()
        {
            var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.Concat(new byte[200]).ToArray();
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "photo", "p.jpg")
            { Headers = new HeaderDictionary(), ContentType = "image/jpeg" };
        }

        private async Task<(int userId, string startUrl, string startPath)> SeedUserWithPhoto()
        {
            await using var seed = _pg.NewContext();
            await seed.Database.ExecuteSqlRawAsync("TRUNCATE \"Users\" RESTART IDENTITY CASCADE;");
            var u = new User
            {
                Name = "U",
                Username = "u" + Guid.NewGuid().ToString("N")[..12],
                Email = $"u{Guid.NewGuid():N}@x.com",
                PasswordHash = "h",
                DateCreated = DateTime.UtcNow,
                UnitPreference = "Metric",
            };
            seed.Users.Add(u);
            await seed.SaveChangesAsync();

            // A start file named for the real user id so it matches the service's
            // owned-file pattern and the winner's step-3 cleanup can remove it.
            var startFile = $"user_{u.Id}_{Guid.NewGuid():N}.jpg";
            var startUrl = "/uploads/profiles/" + startFile;
            var startPath = Path.Combine(_storage.Directory, startFile);
            await File.WriteAllBytesAsync(startPath, new byte[] { 9, 9, 9 });

            await using (var ctx = _pg.NewContext())
                await ctx.Users.Where(x => x.Id == u.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProfilePhotoUrl, startUrl));

            return (u.Id, startUrl, startPath);
        }

        [DockerRequiredFact]
        public async Task overlapping_uploads_leave_exactly_one_winner_and_the_losers_file_removed()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var (userId, _, startPath) = await SeedUserWithPhoto();

            async Task<IActionResult> Upload()
            {
                await using var ctx = _pg.NewContext();
                return (await Controller(ctx, userId).UploadPhoto(Jpeg())).Result!;
            }

            var results = await Task.WhenAll(Upload(), Upload());

            // Two legitimate interleavings, both safe:
            //  * genuine overlap  -> one 200, one 409 (loser deleted its own new file);
            //  * no real overlap  -> the second request sees the first's photo as
            //    its `previousUrl` and performs a valid REPLACEMENT (two 200s).
            // The invariant that must hold either way: no 5xx, every result is
            // 200/409, at least one 200, and the persisted URL points at the one
            // and only `user_<id>_*.jpg` file left on disk (no orphan, and no
            // concurrent op deleted the surviving winner's file).
            var oks = results.OfType<OkObjectResult>().ToList();
            Assert.NotEmpty(oks);
            Assert.All(results, r => Assert.True(r is OkObjectResult or ConflictObjectResult));
            Assert.DoesNotContain(results, r => r is ObjectResult o && o.StatusCode >= 500);

            var okUrls = oks.Select(o => ((PhotoUploadResponse)o.Value!).PhotoUrl).ToList();

            await using var verify = _pg.NewContext();
            var finalUrl = (await verify.Users.AsNoTracking().FirstAsync(u => u.Id == userId)).ProfilePhotoUrl;
            Assert.Contains(finalUrl, okUrls);

            var finalPath = _storage.Service.ResolveOwnedPath(finalUrl!)!;
            Assert.True(File.Exists(finalPath));                // surviving photo present...
            Assert.False(File.Exists(startPath));               // ...previous file cleaned up

            var onDisk = Directory.GetFiles(_storage.Directory, $"user_{userId}_*.jpg");
            Assert.Single(onDisk);
            Assert.Equal(Path.GetFullPath(finalPath), Path.GetFullPath(onDisk[0]));
        }

        [DockerRequiredFact]
        public async Task two_first_time_uploads_race_on_the_is_null_compare_and_set()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");

            int userId;
            await using (var seed = _pg.NewContext())
            {
                await seed.Database.ExecuteSqlRawAsync("TRUNCATE \"Users\" RESTART IDENTITY CASCADE;");
                var u = new User
                {
                    Name = "U",
                    Username = "u" + Guid.NewGuid().ToString("N")[..12],
                    Email = $"u{Guid.NewGuid():N}@x.com",
                    PasswordHash = "h",
                    DateCreated = DateTime.UtcNow,
                    UnitPreference = "Metric",
                    // No photo: the CAS predicate is `ProfilePhotoUrl IS NULL`.
                };
                seed.Users.Add(u);
                await seed.SaveChangesAsync();
                userId = u.Id;
            }

            async Task<IActionResult> Upload()
            {
                await using var ctx = _pg.NewContext();
                return (await Controller(ctx, userId).UploadPhoto(Jpeg())).Result!;
            }

            var results = await Task.WhenAll(Upload(), Upload());

            // As in overlapping_uploads_...: genuine overlap yields one 200 + one
            // 409; if the two do not actually overlap, the second is a valid
            // replacement and both are 200. Either way the CAS prevents a lost
            // update - the surviving row points at the sole file left on disk.
            var oks = results.OfType<OkObjectResult>().ToList();
            Assert.NotEmpty(oks);
            Assert.All(results, r => Assert.True(r is OkObjectResult or ConflictObjectResult));
            Assert.DoesNotContain(results, r => r is ObjectResult o && o.StatusCode >= 500);

            var okUrls = oks.Select(o => ((PhotoUploadResponse)o.Value!).PhotoUrl).ToList();

            await using var verify = _pg.NewContext();
            var finalUrl = (await verify.Users.AsNoTracking().FirstAsync(u => u.Id == userId)).ProfilePhotoUrl;
            Assert.NotNull(finalUrl);
            Assert.Contains(finalUrl, okUrls);
            Assert.True(File.Exists(_storage.Service.ResolveOwnedPath(finalUrl!)!));
            Assert.Single(Directory.GetFiles(_storage.Directory, $"user_{userId}_*.jpg")); // no orphan left
        }

        [DockerRequiredFact]
        public async Task a_lost_confirmation_after_a_committed_swap_is_reconciled_on_a_fresh_connection()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var (userId, _, startPath) = await SeedUserWithPhoto();

            string newUrl;
            await using (var ctx = _pg.NewContext())
            {
                var controller = Controller(ctx, userId);
                // The compare-and-set really commits against Postgres; then the
                // caller's confirmation is lost. Reconciliation must open a
                // genuinely separate connection, observe the committed new URL,
                // and complete as success - without deleting the new file.
                controller.AfterPhotoCompareAndSetForTests =
                    () => throw new InvalidOperationException("connection reset after commit");

                var result = (await controller.UploadPhoto(Jpeg())).Result;
                newUrl = ((PhotoUploadResponse)((OkObjectResult)result!).Value!).PhotoUrl;
            }

            await using var verify = _pg.NewContext();
            var finalUrl = (await verify.Users.AsNoTracking().FirstAsync(u => u.Id == userId)).ProfilePhotoUrl;
            Assert.Equal(newUrl, finalUrl);

            var newPath = _storage.Service.ResolveOwnedPath(newUrl)!;
            Assert.True(File.Exists(newPath));                  // new file kept
            Assert.False(File.Exists(startPath));               // superseded file still cleaned up
            Assert.Single(Directory.GetFiles(_storage.Directory, $"user_{userId}_*.jpg"));
        }

        [DockerRequiredFact]
        public async Task overlapping_upload_and_remove_end_in_a_consistent_state_with_no_broken_reference()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var (userId, _, startPath) = await SeedUserWithPhoto();

            async Task<IActionResult> Upload()
            {
                await using var ctx = _pg.NewContext();
                return (await Controller(ctx, userId).UploadPhoto(Jpeg())).Result!;
            }
            async Task<IActionResult> Remove()
            {
                await using var ctx = _pg.NewContext();
                return await Controller(ctx, userId).DeletePhoto();
            }

            var results = await Task.WhenAll(Upload(), Remove());
            Assert.DoesNotContain(results, r => r is ObjectResult o && o.StatusCode >= 500);

            await using var verify = _pg.NewContext();
            var finalUrl = (await verify.Users.AsNoTracking().FirstAsync(u => u.Id == userId)).ProfilePhotoUrl;

            // The previous photo is always gone (whoever won removed/replaced it).
            Assert.False(File.Exists(startPath));

            if (finalUrl is null)
            {
                // Remove won: the upload got 409 and cleaned its own new file.
                Assert.Contains(results, r => r is ConflictObjectResult);
                Assert.Empty(Directory.GetFiles(_storage.Directory, $"user_{userId}_*.jpg"));
            }
            else
            {
                // Upload won: the reference points at a file that actually exists.
                Assert.Contains(results, r => r is OkObjectResult);
                var path = _storage.Service.ResolveOwnedPath(finalUrl);
                Assert.NotNull(path);
                Assert.True(File.Exists(path));
                Assert.Single(Directory.GetFiles(_storage.Directory, $"user_{userId}_*.jpg"));
            }
        }
    }
}
