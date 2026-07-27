using System;
using System.Collections.Generic;
using System.Text;

namespace Cutter4.Domain
{
    internal static class DomainErrors
    {
        public static UserFacingException CutSolve_InsufficientStock(PanelType type)
            => new UserFacingException(
                "Error_Domain_CutSolve_InsufficientStock",
                $"Can not solve, insufficient stock of type {type}",
                type);
    }

    public class UserFacingException : Exception
    {
        public string ErrorKey { get; }
        public object[] Arguments { get; }

        public UserFacingException(string errorKey, string debugMessage, params object[] arguments) : base(debugMessage)
        {
            ErrorKey = errorKey;
            Arguments = arguments;
        }
    }
}
