
using System.Threading;

namespace Cortex
{
    /// <summary>
    /// Simple wrapper for a CancellationTokenSource that safely encapsulates the somewhat fragile dispose
    /// With this, you can cancel an operation and generate a new token for a new operation
    /// </summary>
    public class ResetCancellationToken
    {
        /// <summary>
        /// The current CancellationToken
        /// </summary>
        public CancellationToken Token { get; private set; }

        public ResetCancellationToken()
        {
            cts = new CancellationTokenSource();
            Token = cts.Token;
        }

        /// <summary>
        /// Will cancel the current token and create a new one
        /// </summary>
        public void Reset()
        {
            lock (objLock)
            {
                // while other methods are thread-safe, disposing is not, so we synchronize the source here
                // the token itself should be safe to passe even if the source gets disposed, so we don't synchronize the getter
                // TODO might need more involved implementation to handle all circumstances
                cts.Cancel();
                cts.Dispose();
                cts = new CancellationTokenSource();
                Token = cts.Token;
            }
        }
        private CancellationTokenSource cts;
        private readonly object objLock = new();

    }
} // end namespace Cortex