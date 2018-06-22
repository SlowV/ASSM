using System;

namespace Vbank.error
{
    public class VbankError : Exception
    {
        public VbankError(string message) : base(message)
        {
            
        }
    }
}