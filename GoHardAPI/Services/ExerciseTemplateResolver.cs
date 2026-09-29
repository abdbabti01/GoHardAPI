using System.Text.RegularExpressions;
using GoHardAPI.Models;

namespace GoHardAPI.Services
{
    /// <summary>
    /// Conservative AI exercise-name → system <see cref="ExerciseTemplate"/> resolution for plan
    /// creation (Phase 2D spec §4). Exact normalized-name equality or an exact curated alias
    /// only — never substring, prefix stripping or word overlap, because a wrong match silently
    /// attaches history to the wrong movement. Custom templates are never considered. Several
    /// system templates with the same normalized name (seed duplicates) resolve to the lowest
    /// Id. Anything else is null (unresolved identity).
    /// </summary>
    public static class ExerciseTemplateResolver
    {
        // Declared first: static initializers run in textual order and Aliases calls Normalize.
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        // alias (any spelling) -> exact system template name; both sides are normalized at load.
        private static readonly (string Alias, string Template)[] RawAliases =
        {
            ("barbell bench press", "Bench Press"),
            ("flat bench press", "Bench Press"),
            ("barbell flat bench press", "Bench Press"),
            ("back squat", "Squat"),
            ("barbell squat", "Squat"),
            ("barbell back squat", "Squat"),
            ("conventional deadlift", "Deadlift"),
            ("barbell deadlift", "Deadlift"),
            ("rdl", "Romanian Deadlift"),
            ("ohp", "Overhead Press"),
            ("military press", "Overhead Press"),
            ("barbell overhead press", "Overhead Press"),
            ("standing overhead press", "Overhead Press"),
            ("barbell row", "Bent-Over Row"),
            ("barbell bent over row", "Bent-Over Row"),
            ("bent over barbell row", "Bent-Over Row"),
            ("pullup", "Pull-ups"),
            ("pushup", "Push-ups"),
            ("chinup", "Chin-ups"),
        };

        private static readonly Dictionary<string, string> Aliases =
            RawAliases.ToDictionary(a => Normalize(a.Alias), a => Normalize(a.Template), StringComparer.Ordinal);

        public static string Normalize(string name)
        {
            var s = Whitespace.Replace(name.ToLowerInvariant().Replace('-', ' ').Replace('_', ' '), " ").Trim();
            var lastSpace = s.LastIndexOf(' ');
            var last = s[(lastSpace + 1)..];
            if (last.Length >= 3 && last.EndsWith('s') && !last.EndsWith("ss"))
            {
                s = s[..^1];
            }
            return s;
        }

        public static int? Resolve(string? name, IEnumerable<ExerciseTemplate> templates)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var target = Normalize(name);
            var system = templates.Where(t => !t.IsCustom).ToList();

            var direct = LowestId(system, target);
            if (direct is not null)
            {
                return direct;
            }

            return Aliases.TryGetValue(target, out var aliasTarget) ? LowestId(system, aliasTarget) : null;
        }

        private static int? LowestId(List<ExerciseTemplate> system, string normalizedName) =>
            system.Where(t => Normalize(t.Name) == normalizedName)
                .Select(t => (int?)t.Id)
                .Min();
    }
}
