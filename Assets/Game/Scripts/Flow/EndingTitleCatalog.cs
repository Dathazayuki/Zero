using System;

namespace Zero.AnomalyPrototype.Flow
{
    public static class EndingTitleCatalog
    {
        public static string GetTitle(EndingIdentity ending)
        {
            switch (ending)
            {
                case EndingIdentity.TheCast:
                    return "ENDING I — THE CAST";
                case EndingIdentity.AnotherShift:
                    return "ENDING II — ANOTHER SHIFT";
                case EndingIdentity.CurtainCall:
                    return "TRUE ENDING — CURTAIN CALL";
                default:
                    throw new ArgumentOutOfRangeException(nameof(ending), ending, "Unknown ending identity.");
            }
        }
    }
}
