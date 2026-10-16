using System;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class CallingCorridorRules
    {
        public enum Choice { Rejected, Wrong, Correct }
        public enum Hand { Hour, Minute }
        private static readonly string[] HandPatterns =
        {
            "\u25cf\u25cb\u25cf", "\u25cb\u25cf\u25cb", "\u25cf\u25cf\u25cb", "\u25cb\u25cb\u25cf",
            "\u25cf\u25cb\u25cb", "\u25cb\u25cf\u25cf", "\u25cf\u25cf\u25cf"
        };
        private static readonly string[][] Codes =
        {
            new[] { "K7-134", "K7-143", "K7-314", "K7-341", "K7-413", "K7-431" },
            new[] { "A4-281", "A4-219", "A4-812", "A4-218", "A4-182", "A4-821" },
            new[] { "R3-124", "R3-142", "R3-214", "R3-241", "R3-421", "R3-412" },
            new[] { "B6-153", "B6-135", "B6-315", "B6-351", "B6-513", "B6-531" }
        };
        private static readonly int[] RoundStarts = { 0, 1, 2, 4, 7 };
        private static readonly int[] ExpectedOptions = { 0, 3, 1, 4, 2, 5, 0 };
        private bool awaitingPassage;
        private bool awaitingRetry;
        private bool encounterConsumed;
        public int Round { get; private set; }
        public int Decision { get; private set; }
        public int WrongChoices { get; private set; }
        public bool IsComplete => Decision == 7;
        public bool AwaitingPassage => awaitingPassage;
        public bool AwaitingRetry => awaitingRetry;
        public bool CanStartMannequin => Decision >= 3 && !IsComplete && !encounterConsumed;
        public static int DoorCount(int decision) => Codes[RoundForDecision(decision)].Length;
        public static string DoorCode(int decision, int option) => Codes[RoundForDecision(decision)][option];
        public static string ExpectedCode(int decision) => DoorCode(decision, ExpectedOptions[decision]);
        public static string CodePrefix(int decision)
        {
            string code = ExpectedCode(decision);
            return code.Substring(0, code.IndexOf('-'));
        }

        public static int ClockDigit(int decision, int clockIndex)
        {
            if (clockIndex < 0 || clockIndex > 2) throw new ArgumentOutOfRangeException(nameof(clockIndex));
            if (decision == 7) return -1;
            string code = ExpectedCode(decision);
            int start = code.IndexOf('-') + 1;
            return code[start + clockIndex] - '0';
        }

        public static Hand ClockHand(int decision, int clockIndex)
        {
            if (clockIndex < 0 || clockIndex > 2) throw new ArgumentOutOfRangeException(nameof(clockIndex));
            return HandPatterns[decision][clockIndex] == '\u25cf' ? Hand.Hour : Hand.Minute;
        }

        public static string ClockSymbols(int decision) => decision == 7 ? string.Empty :
            HandPatterns[decision][0] + "   " + HandPatterns[decision][1] + "   " + HandPatterns[decision][2];

        public static int ClockHandDigit(int decision, int clockIndex, Hand hand)
        {
            int digit = ClockDigit(decision, clockIndex);
            if (decision == 7 || hand == ClockHand(decision, clockIndex)) return digit;
            string code = ClockDecoyCode(decision);
            return code[code.IndexOf('-') + 1 + clockIndex] - '0';
        }

        private static string ClockDecoyCode(int decision)
        {
            string expected = ExpectedCode(decision);
            int start = expected.IndexOf('-') + 1;
            for (int option = 0; option < DoorCount(decision); option++)
            {
                string code = DoorCode(decision, option);
                if (code[start] != expected[start] && code[start + 1] != expected[start + 1] &&
                    code[start + 2] != expected[start + 2]) return code;
            }
            throw new InvalidOperationException("Calling Corridor requires a decoy with three different clock digits.");
        }
        public static int RoundStart(int round) => RoundStarts[round];

        private static int RoundForDecision(int decision)
        {
            for (int round = 0; round < 4; round++)
                if (decision < RoundStarts[round + 1]) return round;
            throw new ArgumentOutOfRangeException(nameof(decision));
        }

        public Choice Select(int decision, string code)
        {
            if (IsComplete || awaitingPassage || awaitingRetry || decision != Decision) return Choice.Rejected;
            if (!IsListedCode(decision, code)) return Choice.Rejected;
            if (string.Equals(code, ExpectedCode(decision), StringComparison.Ordinal))
            {
                awaitingPassage = true;
                return Choice.Correct;
            }
            WrongChoices++;
            awaitingRetry = true;
            return Choice.Wrong;
        }

        public Choice EnterRoom(int decision, string code)
        {
            Choice result = Select(decision, code);
            if (result == Choice.Correct) ConfirmPassage(decision);
            return result;
        }

        private static bool IsListedCode(int decision, string code)
        {
            for (int i = 0; i < DoorCount(decision); i++)
                if (string.Equals(DoorCode(decision, i), code, StringComparison.Ordinal)) return true;
            return false;
        }

        public bool ConfirmPassage(int decision)
        {
            if (!awaitingPassage || decision != Decision || IsComplete) return false;
            awaitingPassage = false;
            Decision++;
            encounterConsumed = false;
            if (Decision != RoundStarts[Round + 1]) return true;
            if (!IsComplete) Round++;
            WrongChoices = 0;
            return true;
        }

        public bool TryConsumeEncounter(bool scripted)
        {
            if (Round == 0 || encounterConsumed || IsComplete) return false;
            if (!scripted && WrongChoices < 2) return false;
            encounterConsumed = true;
            return true;
        }

        public void RetryRound()
        {
            Decision = RoundStarts[Round];
            awaitingPassage = false;
            awaitingRetry = false;
        }

        public void Reset()
        {
            Round = 0;
            Decision = 0;
            WrongChoices = 0;
            encounterConsumed = false;
            awaitingPassage = false;
            awaitingRetry = false;
        }
    }
}
