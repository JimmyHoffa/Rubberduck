using System;

namespace Rubberduck.VBA
{
    /// <summary>
    /// An exception thrown by an <c>IParseTreeListener</c> implementation 
    /// that does not need to traverse an entire parse tree.
    /// </summary>
    [Serializable]
    public class WalkerCancelledException : Exception
    {
        public WalkerCancelledException(Exception exception)
            : base("Tree walker was cancelled by listener.", exception)
        { }
    }
}