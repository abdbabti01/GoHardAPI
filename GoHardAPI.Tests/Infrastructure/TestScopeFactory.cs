using System;
using GoHardAPI.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GoHardAPI.Tests.Infrastructure
{
    /// <summary>
    /// Minimal <see cref="IServiceScopeFactory"/> for controller tests. Each
    /// created scope resolves a FRESH <see cref="TrainingContext"/> from the
    /// supplied factory - mirroring how <c>ProfileController</c> obtains an
    /// independent context/connection for its post-exception reconciliation read.
    /// The context is disposed when the scope is disposed.
    /// </summary>
    public sealed class TestScopeFactory : IServiceScopeFactory
    {
        private readonly Func<TrainingContext> _contextFactory;

        public TestScopeFactory(Func<TrainingContext> contextFactory) =>
            _contextFactory = contextFactory;

        /// <summary>
        /// A factory whose scope creation throws if used. For controller tests
        /// that never reach the profile-photo reconciliation path.
        /// </summary>
        public static TestScopeFactory Unused() => new(() =>
            throw new InvalidOperationException(
                "TestScopeFactory.Unused() was resolved - this test was not expected to hit reconciliation."));

        public IServiceScope CreateScope() => new Scope(_contextFactory());

        private sealed class Scope : IServiceScope, IServiceProvider
        {
            private readonly TrainingContext _context;

            public Scope(TrainingContext context) => _context = context;

            public IServiceProvider ServiceProvider => this;

            public object? GetService(Type serviceType) =>
                serviceType == typeof(TrainingContext) ? _context : null;

            public void Dispose() => _context.Dispose();
        }
    }
}
