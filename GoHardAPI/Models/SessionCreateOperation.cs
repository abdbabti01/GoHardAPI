using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Durable source of truth for a keyed Session CREATE operation
    /// (POST /api/v1/sessions with a <c>ClientOperationId</c>).
    ///
    /// Exactly one row per <c>(UserId, ClientOperationId)</c> pair — enforced by a
    /// composite unique index. Every lookup the POST state machine performs is
    /// scoped by the JWT-derived <see cref="UserId"/>, so a key belonging to one
    /// user can never disclose or mutate another user's data.
    ///
    /// The row is deliberately hard to destroy: the <see cref="SessionId"/> foreign
    /// key is <c>ON DELETE SET NULL</c>, so neither a cascade delete nor the draft
    /// reaper can remove the operation record. A replay whose Session has since
    /// vanished is answered with <c>410 Gone</c> and the Session is never recreated.
    /// </summary>
    public class SessionCreateOperation
    {
        public int Id { get; set; }

        /// <summary>Authenticated owner (JWT <c>sub</c>). Part of the composite identity.</summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>Client-supplied idempotency key. Part of the composite identity.</summary>
        [Required]
        public Guid ClientOperationId { get; set; }

        /// <summary>
        /// The Session this operation created. Null before the operation completes,
        /// and null again if that Session was later deleted (<c>ON DELETE SET NULL</c>).
        /// </summary>
        public int? SessionId { get; set; }

        /// <summary>
        /// Set — in the same transaction that assigns <see cref="SessionId"/> — once
        /// the Session exists. This is the flag that tells a completed operation whose
        /// Session is now gone (=&gt; 410) apart from an operation that never finished
        /// (=&gt; fail closed, never create a second Session).
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Set when the operation was canceled out of band. A replay of a canceled
        /// operation creates nothing and returns <c>409 operation_canceled</c>.
        ///
        /// DORMANT in P1: no code path writes this column yet. The producer is the P2
        /// DELETE-by-operation-key endpoint; until it lands, the <c>operation_canceled</c>
        /// branch of <see cref="Services.SessionCreateService"/> is unreachable in
        /// production (exercised only by tests that seed the row directly).
        /// </summary>
        public DateTime? CanceledAt { get; set; }

        /// <summary>When the operation row was first inserted (UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
        public Session? Session { get; set; }
    }
}
