using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace GoHardAPI.Configuration
{
    /// <summary>
    /// The single definition of how profile-photo files are served, shared by
    /// <c>Program.cs</c> and the serving tests so the two can never drift.
    /// Only <c>*.jpg</c>/<c>*.jpeg</c>/<c>*.png</c> are mapped and unknown types
    /// are refused, so nothing else in the directory is exposed; the
    /// <see cref="PhysicalFileProvider"/> root plus ASP.NET path normalisation
    /// block traversal.
    /// </summary>
    public static class ProfilePhotoStaticFiles
    {
        public static StaticFileOptions BuildOptions(string directory) => new()
        {
            FileProvider = new PhysicalFileProvider(directory),
            RequestPath = ProfilePhotoStorageOptions.PublicPathPrefix,
            ServeUnknownFileTypes = false,
            ContentTypeProvider = new FileExtensionContentTypeProvider(
                new Dictionary<string, string>
                {
                    [".jpg"] = "image/jpeg",
                    [".jpeg"] = "image/jpeg",
                    [".png"] = "image/png",
                }),
            OnPrepareResponse = ctx =>
            {
                var headers = ctx.Context.Response.Headers;
                // Filenames are unique per upload so the bytes under a URL never
                // change - cache hard.
                headers.CacheControl = "public,max-age=86400,immutable";
                // Defence in depth for user-uploaded bytes served same-origin:
                // never sniff to another type, always render inline as the
                // declared image, and neutralise a would-be document polyglot.
                headers.XContentTypeOptions = "nosniff";
                headers.ContentDisposition = "inline";
                headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            },
        };
    }
}
