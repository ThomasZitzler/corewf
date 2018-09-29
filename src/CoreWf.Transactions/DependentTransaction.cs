// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace CoreWf.Transactions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229", Justification = "Serialization not yet supported and will be done using DistributedTransaction")]
    [Serializable]
    public sealed class DependentTransaction : Transaction
    {
        private readonly bool _blocking;

        // Create a transaction with the given settings
        //
        internal DependentTransaction(IsolationLevel isoLevel, InternalTransaction internalTransaction, bool blocking) :
            base(isoLevel, internalTransaction)
        {
            _blocking = blocking;
            lock (_internalTransaction)
            {
                if (blocking)
                {
                    _internalTransaction.State.CreateBlockingClone(_internalTransaction);
                }
                else
                {
                    _internalTransaction.State.CreateAbortingClone(_internalTransaction);
                }
            }
        }

        public void Complete()
        {
            TransactionsEtwProvider etwLog = TransactionsEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.MethodEnter(TraceSourceType.TraceSourceLtm, this);
            }

            lock (_internalTransaction)
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(DependentTransaction));
                }

                if (_complete)
                {
                    throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
                }

                _complete = true;

                if (_blocking)
                {
                    _internalTransaction.State.CompleteBlockingClone(_internalTransaction);
                }
                else
                {
                    _internalTransaction.State.CompleteAbortingClone(_internalTransaction);
                }
            }

            if (etwLog.IsEnabled())
            {
                etwLog.TransactionDependentCloneComplete(this, "DependentTransaction");
                etwLog.MethodExit(TraceSourceType.TraceSourceLtm, this);
            }
        }
    }
}