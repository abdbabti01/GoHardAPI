using System.Runtime.CompilerServices;

namespace GoHardAPI.Tests.Infrastructure
{
    internal static class TestModuleInit
    {
        /// <summary>
        /// The running API sets this switch at process start (Program.cs) so Npgsql keeps
        /// mapping <c>DateTime</c> to <c>timestamp without time zone</c> and does not shift
        /// UTC values. The real-PostgreSQL test suites construct <c>TrainingContext</c>
        /// directly, without Program.cs, so they must set the same switch here to exercise
        /// the production Npgsql behavior.
        /// </summary>
        [ModuleInitializer]
        public static void EnableLegacyNpgsqlTimestampBehavior()
        {
            System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}
