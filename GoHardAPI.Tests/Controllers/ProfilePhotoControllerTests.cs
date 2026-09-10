using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// <c>POST/DELETE /profile/photo</c> replacement and removal ordering:
    /// validate then write the new file first, commit the DB reference (compare
    /// -and-set) before deleting the old file, and never let a failed upload or a
    /// cleanup failure destroy the previous photo. Real SQLite so the
    /// <c>ExecuteUpdateAsync</c> compare-and-set actually runs.
    /// </summary>
    public sealed class ProfilePhotoControllerTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly TestProfilePhotoStorage _storage = new();

        public ProfilePhotoControllerTests()
        {
            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();
            using var ctx = NewContext();
            ctx.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _conn.Dispose();
            _storage.Dispose();
        }

        private TrainingContext NewContext(IInterceptor? interceptor = null)
        {
            var b = new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_conn);
            if (interceptor is not null) b.AddInterceptors(interceptor);
            return new TrainingContext(b.Options);
        }

        private ProfileController Controller(TrainingContext ctx, int userId)
        {
            var c = new ProfileController(
                ctx, _storage.Service, new UserRepository(ctx), new CurrentMeasurementsService(ctx),
                // Reconciliation reads a committed value on a fresh context over
                // the same in-memory database (a new connection is not possible
                // for SQLite :memory:, but a fresh context/transaction scope is
                // enough to observe the committed row).
                new TestScopeFactory(() => NewContext()));
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

        private async Task SeedUser(int id, string? photoUrl = null)
        {
            await using var ctx = NewContext();
            ctx.Users.Add(new User
            {
                Id = id,
                Name = $"U{id}",
                Username = $"u{id}",
                Email = $"u{id}@x.com",
                PasswordHash = "h",
                DateCreated = DateTime.UtcNow,
                UnitPreference = "Metric",
                ProfilePhotoUrl = photoUrl,
            });
            await ctx.SaveChangesAsync();
        }

        private async Task<string?> PhotoUrl(int id)
        {
            await using var ctx = NewContext();
            return (await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == id)).ProfilePhotoUrl;
        }

        private static IFormFile Jpeg(string name = "p.jpg")
        {
            var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.Concat(new byte[128]).ToArray();
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "photo", name)
            { Headers = new HeaderDictionary(), ContentType = "image/jpeg" };
        }

        private static IFormFile NotAnImage() =>
            new FormFile(new MemoryStream(Encoding.ASCII.GetBytes("nope")), 0, 4, "photo", "x.jpg")
            { Headers = new HeaderDictionary() };

        // ---- happy path -----------------------------------------------------

        [Fact]
        public async Task upload_stores_the_file_and_persists_a_matching_reference()
        {
            await SeedUser(1);
            await using var ctx = NewContext();

            var body = Assert.IsType<PhotoUploadResponse>(
                Assert.IsType<OkObjectResult>((await Controller(ctx, 1).UploadPhoto(Jpeg())).Result).Value);

            Assert.StartsWith("/uploads/profiles/user_1_", body.PhotoUrl);
            Assert.Equal(body.PhotoUrl, await PhotoUrl(1));
            Assert.NotNull(_storage.Service.ResolveOwnedPath(body.PhotoUrl));
            Assert.True(File.Exists(_storage.Service.ResolveOwnedPath(body.PhotoUrl)!));
        }

        [Fact]
        public async Task replacing_a_photo_deletes_the_old_file_after_committing_the_new_reference()
        {
            await SeedUser(1);
            string firstUrl;
            await using (var ctx = NewContext())
                firstUrl = ((PhotoUploadResponse)((OkObjectResult)(await Controller(ctx, 1).UploadPhoto(Jpeg())).Result!).Value!).PhotoUrl;
            var firstPath = _storage.Service.ResolveOwnedPath(firstUrl)!;
            Assert.True(File.Exists(firstPath));

            string secondUrl;
            await using (var ctx = NewContext())
                secondUrl = ((PhotoUploadResponse)((OkObjectResult)(await Controller(ctx, 1).UploadPhoto(Jpeg())).Result!).Value!).PhotoUrl;

            Assert.NotEqual(firstUrl, secondUrl);
            Assert.Equal(secondUrl, await PhotoUrl(1));
            Assert.True(File.Exists(_storage.Service.ResolveOwnedPath(secondUrl)!));
            Assert.False(File.Exists(firstPath)); // old file cleaned only after the swap committed
        }

        // ---- failed replacement preserves the previous photo ---------------

        [Fact]
        public async Task an_invalid_replacement_returns_400_and_leaves_the_previous_photo_intact()
        {
            await SeedUser(1);
            string url;
            await using (var ctx = NewContext())
                url = ((PhotoUploadResponse)((OkObjectResult)(await Controller(ctx, 1).UploadPhoto(Jpeg())).Result!).Value!).PhotoUrl;
            var path = _storage.Service.ResolveOwnedPath(url)!;

            await using (var ctx = NewContext())
                Assert.IsType<BadRequestObjectResult>((await Controller(ctx, 1).UploadPhoto(NotAnImage())).Result);

            Assert.Equal(url, await PhotoUrl(1)); // reference unchanged
            Assert.True(File.Exists(path));       // file unchanged
            Assert.Single(_storage.Files());      // no orphan written
        }

        [Fact]
        public async Task an_exception_then_a_read_of_the_previous_url_preserves_both_files()
        {
            await SeedUser(1, photoUrl: "/uploads/profiles/user_1_" + new string('a', 32) + ".jpg");
            var oldPath = Path.Combine(_storage.Directory, "user_1_" + new string('a', 32) + ".jpg");
            await File.WriteAllBytesAsync(oldPath, new byte[] { 1, 2, 3 });

            // The interceptor throws around the UPDATE command. Reconciliation
            // then reads the row and sees the PREVIOUS url - not the one this
            // request wrote. That is inconclusive: a client-side exception is not
            // proof the server write finished, so the controller must NOT infer a
            // rollback and must NOT delete the new file. Both files are kept and
            // the failure propagates.
            await using (var ctx = NewContext(new ThrowOnUsersUpdateInterceptor()))
            {
                await Assert.ThrowsAnyAsync<Exception>(() => Controller(ctx, 1).UploadPhoto(Jpeg()));
            }

            Assert.Equal("/uploads/profiles/user_1_" + new string('a', 32) + ".jpg", await PhotoUrl(1));
            Assert.True(File.Exists(oldPath));            // previous file intact
            Assert.Equal(2, _storage.Files().Length);     // new file kept as a recoverable orphan
        }

        [Fact]
        public async Task a_lost_confirmation_after_a_committed_swap_keeps_the_new_photo_and_returns_success()
        {
            // The compare-and-set really commits; confirmation is then lost
            // (transient error surfacing after the row was already updated).
            await SeedUser(1, photoUrl: "/uploads/profiles/user_1_" + new string('b', 32) + ".jpg");
            var oldPath = Path.Combine(_storage.Directory, "user_1_" + new string('b', 32) + ".jpg");
            await File.WriteAllBytesAsync(oldPath, new byte[] { 1, 2, 3 });

            string newUrl;
            await using (var ctx = NewContext())
            {
                var controller = Controller(ctx, 1);
                controller.AfterPhotoCompareAndSetForTests =
                    () => throw new InvalidOperationException("connection dropped after commit");

                var result = (await controller.UploadPhoto(Jpeg())).Result;
                newUrl = ((PhotoUploadResponse)((OkObjectResult)result!).Value!).PhotoUrl;
            }

            // Persisted reference points at the fully written new file...
            Assert.Equal(newUrl, await PhotoUrl(1));
            var newPath = _storage.Service.ResolveOwnedPath(newUrl)!;
            Assert.True(File.Exists(newPath));
            // ...the new file was NOT deleted just because the call threw...
            Assert.NotEqual(oldPath, newPath);
            // ...and the superseded old file was still cleaned up (success path).
            Assert.False(File.Exists(oldPath));
            Assert.Single(_storage.Files());
        }

        [Fact]
        public async Task a_cancellation_after_a_committed_swap_keeps_the_new_photo_and_returns_success()
        {
            // Same boundary as above, but the lost confirmation arrives as a
            // cancellation (client disconnect landing after the row was updated).
            await SeedUser(1, photoUrl: "/uploads/profiles/user_1_" + new string('c', 32) + ".jpg");
            var oldPath = Path.Combine(_storage.Directory, "user_1_" + new string('c', 32) + ".jpg");
            await File.WriteAllBytesAsync(oldPath, new byte[] { 1, 2, 3 });

            string newUrl;
            await using (var ctx = NewContext())
            {
                var controller = Controller(ctx, 1);
                controller.AfterPhotoCompareAndSetForTests =
                    () => throw new OperationCanceledException("request aborted after commit");

                var result = (await controller.UploadPhoto(Jpeg())).Result;
                newUrl = ((PhotoUploadResponse)((OkObjectResult)result!).Value!).PhotoUrl;
            }

            Assert.Equal(newUrl, await PhotoUrl(1));
            Assert.True(File.Exists(_storage.Service.ResolveOwnedPath(newUrl)!));
            Assert.False(File.Exists(oldPath));
            Assert.Single(_storage.Files());
        }

        [Fact]
        public async Task an_uncertain_outcome_when_reconciliation_also_fails_preserves_both_files()
        {
            // The swap call throws AND the reconciliation read cannot run: the
            // outcome is genuinely unknown, so neither file may be deleted.
            await SeedUser(1, photoUrl: "/uploads/profiles/user_1_" + new string('d', 32) + ".jpg");
            var oldPath = Path.Combine(_storage.Directory, "user_1_" + new string('d', 32) + ".jpg");
            await File.WriteAllBytesAsync(oldPath, new byte[] { 1, 2, 3 });

            await using (var ctx = NewContext(new ThrowOnUsersUpdateInterceptor()))
            {
                var controller = new ProfileController(
                    ctx, _storage.Service, new UserRepository(ctx), new CurrentMeasurementsService(ctx),
                    TestScopeFactory.Unused()); // reconciliation read throws -> outcome unknown
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, "1") }, "TestAuth"))
                    }
                };

                await Assert.ThrowsAnyAsync<Exception>(() => controller.UploadPhoto(Jpeg()));
            }

            Assert.True(File.Exists(oldPath));   // previous file preserved
            Assert.Equal(2, _storage.Files().Length); // new file kept as a recoverable orphan
        }

        [Fact]
        public async Task a_confirmed_cas_conflict_deletes_only_its_own_new_file_and_returns_409()
        {
            await SeedUser(1, photoUrl: "/uploads/profiles/user_1_" + new string('e', 32) + ".jpg");
            var oldPath = Path.Combine(_storage.Directory, "user_1_" + new string('e', 32) + ".jpg");
            await File.WriteAllBytesAsync(oldPath, new byte[] { 1, 2, 3 });

            // The UPDATE runs and definitively reports 0 rows affected (someone
            // else changed the row first). This is a CONFIRMED outcome - not an
            // exception - so the request cleans up the file it just wrote and
            // leaves the previous photo untouched.
            await using (var ctx = NewContext(new ZeroRowsUsersUpdateInterceptor()))
            {
                Assert.IsType<ConflictObjectResult>((await Controller(ctx, 1).UploadPhoto(Jpeg())).Result);
            }

            Assert.Equal("/uploads/profiles/user_1_" + new string('e', 32) + ".jpg", await PhotoUrl(1));
            Assert.True(File.Exists(oldPath));       // previous file untouched
            Assert.Single(_storage.Files());         // this request's new file was removed
        }

        [Fact]
        public async Task an_old_file_cleanup_failure_does_not_fail_an_otherwise_successful_replacement()
        {
            await SeedUser(1);
            string firstUrl;
            await using (var ctx = NewContext())
                firstUrl = ((PhotoUploadResponse)((OkObjectResult)(await Controller(ctx, 1).UploadPhoto(Jpeg())).Result!).Value!).PhotoUrl;
            var firstPath = _storage.Service.ResolveOwnedPath(firstUrl)!;

            // Portably force the cleanup delete to fail (POSIX unlink ignores
            // file locks, so a FileShare.None handle would not simulate this on
            // Linux). Restored after so the temp dir can be removed.
            var deleted = new System.Collections.Generic.List<string>();
            _storage.Service.DeleteFileForTests = p =>
            {
                deleted.Add(p);
                throw new IOException("simulated filesystem cleanup failure");
            };
            try
            {
                string secondUrl;
                await using (var ctx = NewContext())
                {
                    var result = (await Controller(ctx, 1).UploadPhoto(Jpeg())).Result;
                    secondUrl = ((PhotoUploadResponse)((OkObjectResult)result!).Value!).PhotoUrl;
                }

                Assert.Contains(firstPath, deleted);                 // cleanup was attempted...
                Assert.Equal(secondUrl, await PhotoUrl(1));           // ...and failed, but the replacement still succeeded
                Assert.True(File.Exists(_storage.Service.ResolveOwnedPath(secondUrl)!));
                Assert.True(File.Exists(firstPath));                  // old file survives as a recoverable orphan
            }
            finally
            {
                _storage.Service.DeleteFileForTests = null;
            }
        }

        // ---- removal ------------------------------------------------------

        [Fact]
        public async Task remove_clears_the_reference_before_deleting_the_file()
        {
            await SeedUser(1);
            string url;
            await using (var ctx = NewContext())
                url = ((PhotoUploadResponse)((OkObjectResult)(await Controller(ctx, 1).UploadPhoto(Jpeg())).Result!).Value!).PhotoUrl;
            var path = _storage.Service.ResolveOwnedPath(url)!;

            await using (var ctx = NewContext())
                Assert.IsType<NoContentResult>(await Controller(ctx, 1).DeletePhoto());

            Assert.Null(await PhotoUrl(1));
            Assert.False(File.Exists(path));
        }

        [Fact]
        public async Task remove_with_no_photo_is_404()
        {
            await SeedUser(1);
            await using var ctx = NewContext();
            Assert.IsType<NotFoundObjectResult>(await Controller(ctx, 1).DeletePhoto());
        }

        // ---- account isolation ------------------------------------------

        [Fact]
        public async Task removing_one_users_photo_never_touches_another_users_file()
        {
            await SeedUser(1);
            await SeedUser(2);
            string url1, url2;
            await using (var ctx = NewContext())
                url1 = ((PhotoUploadResponse)((OkObjectResult)(await Controller(ctx, 1).UploadPhoto(Jpeg())).Result!).Value!).PhotoUrl;
            await using (var ctx = NewContext())
                url2 = ((PhotoUploadResponse)((OkObjectResult)(await Controller(ctx, 2).UploadPhoto(Jpeg())).Result!).Value!).PhotoUrl;

            await using (var ctx = NewContext())
                Assert.IsType<NoContentResult>(await Controller(ctx, 1).DeletePhoto());

            Assert.Null(await PhotoUrl(1));
            Assert.Equal(url2, await PhotoUrl(2));
            Assert.True(File.Exists(_storage.Service.ResolveOwnedPath(url2)!)); // user 2 untouched
            Assert.False(File.Exists(_storage.Service.ResolveOwnedPath(url1)!));
        }

        private static bool IsUsersPhotoUpdate(string sql) =>
            sql.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)
            && sql.Contains("Users", StringComparison.OrdinalIgnoreCase)
            && sql.Contains("ProfilePhotoUrl", StringComparison.OrdinalIgnoreCase);

        private sealed class ThrowOnUsersUpdateInterceptor : DbCommandInterceptor
        {
            public override InterceptionResult<int> NonQueryExecuting(
                System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
            {
                if (IsUsersPhotoUpdate(command.CommandText))
                    throw new InvalidOperationException("simulated database failure");
                return base.NonQueryExecuting(command, eventData, result);
            }

            public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
                System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                if (IsUsersPhotoUpdate(command.CommandText))
                    throw new InvalidOperationException("simulated database failure");
                return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            }
        }

        /// <summary>
        /// Suppresses the profile-photo <c>UPDATE</c> and reports 0 rows affected
        /// - a deterministic stand-in for "another request already changed the
        /// row", i.e. the confirmed compare-and-set conflict path.
        /// </summary>
        private sealed class ZeroRowsUsersUpdateInterceptor : DbCommandInterceptor
        {
            public override InterceptionResult<int> NonQueryExecuting(
                System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<int> result) =>
                IsUsersPhotoUpdate(command.CommandText)
                    ? InterceptionResult<int>.SuppressWithResult(0)
                    : base.NonQueryExecuting(command, eventData, result);

            public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
                System.Data.Common.DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
                CancellationToken cancellationToken = default) =>
                IsUsersPhotoUpdate(command.CommandText)
                    ? new ValueTask<InterceptionResult<int>>(InterceptionResult<int>.SuppressWithResult(0))
                    : base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
