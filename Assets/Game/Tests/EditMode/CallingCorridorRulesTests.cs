using NUnit.Framework;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class CallingCorridorRulesTests
    {
        [TestCase(0, "K7", 1, 3, 4)]
        [TestCase(1, "A4", 2, 1, 8)]
        [TestCase(2, "R3", 1, 4, 2)]
        [TestCase(3, "R3", 4, 2, 1)]
        [TestCase(4, "B6", 3, 1, 5)]
        [TestCase(5, "B6", 5, 3, 1)]
        [TestCase(6, "B6", 1, 5, 3)]
        public void ClockHintsMatchEachAuthoredAnswer(int decision, string prefix, int first, int middle, int last)
        {
            Assert.AreEqual(prefix, CallingCorridorRules.CodePrefix(decision));
            Assert.AreEqual(first, CallingCorridorRules.ClockDigit(decision, 0));
            Assert.AreEqual(middle, CallingCorridorRules.ClockDigit(decision, 1));
            Assert.AreEqual(last, CallingCorridorRules.ClockDigit(decision, 2));
        }

        [Test]
        public void CompletedAttemptTurnsAllClockHintsOff()
        {
            for (int i = 0; i < 3; i++) Assert.AreEqual(-1, CallingCorridorRules.ClockDigit(7, i));
            Assert.That(CallingCorridorRules.ClockSymbols(7), Is.Empty);
            foreach (CallingCorridorRules.Hand hand in new[] { CallingCorridorRules.Hand.Hour, CallingCorridorRules.Hand.Minute })
                for (int i = 0; i < 3; i++) Assert.AreEqual(-1, CallingCorridorRules.ClockHandDigit(7, i, hand));
        }

        [TestCase(0, "\u25cf   \u25cb   \u25cf")]
        [TestCase(1, "\u25cb   \u25cf   \u25cb")]
        [TestCase(2, "\u25cf   \u25cf   \u25cb")]
        [TestCase(3, "\u25cb   \u25cb   \u25cf")]
        [TestCase(4, "\u25cf   \u25cb   \u25cb")]
        [TestCase(5, "\u25cb   \u25cf   \u25cf")]
        [TestCase(6, "\u25cf   \u25cf   \u25cf")]
        public void BoardSelectsHandsThatEncodeOneRabbitAndOneListedSpiderCode(int decision, string symbols)
        {
            Assert.That(CallingCorridorRules.ClockSymbols(decision), Is.EqualTo(symbols));
            string correct = CallingCorridorRules.CodePrefix(decision) + "-";
            string decoy = correct;
            for (int clock = 0; clock < 3; clock++)
            {
                var hand = CallingCorridorRules.ClockHand(decision, clock);
                Assert.That(hand == CallingCorridorRules.Hand.Hour, Is.EqualTo(symbols[clock * 4] == '\u25cf'));
                var other = hand == CallingCorridorRules.Hand.Hour ? CallingCorridorRules.Hand.Minute : CallingCorridorRules.Hand.Hour;
                int digit = CallingCorridorRules.ClockHandDigit(decision, clock, hand);
                int distraction = CallingCorridorRules.ClockHandDigit(decision, clock, other);
                Assert.That(distraction, Is.InRange(1, 9).And.Not.EqualTo(digit));
                correct += digit;
                decoy += distraction;
            }
            Assert.That(correct, Is.EqualTo(CallingCorridorRules.ExpectedCode(decision)));
            Assert.That(AtDecision(decision).Select(decision, correct), Is.EqualTo(CallingCorridorRules.Choice.Correct));
            Assert.That(AtDecision(decision).Select(decision, decoy), Is.EqualTo(CallingCorridorRules.Choice.Wrong));
        }

        [Test]
        public void RoomEntryAdvancesImmediatelyAndDuplicateEntryIsRejected()
        {
            var rules = new CallingCorridorRules();
            Assert.AreEqual(CallingCorridorRules.Choice.Correct, rules.EnterRoom(0, "K7-134"));
            Assert.AreEqual(1, rules.Decision);
            Assert.IsFalse(rules.AwaitingPassage);
            Assert.AreEqual(CallingCorridorRules.Choice.Rejected, rules.EnterRoom(0, "K7-134"));
            Assert.AreEqual(1, rules.Decision);
        }

        [Test]
        public void SpiderRoomDoesNotAdvanceAndDeathReturnsToBeginning()
        {
            var rules = AtDecision(5);
            Assert.AreEqual(CallingCorridorRules.Choice.Wrong,
                rules.EnterRoom(5, CallingCorridorRules.DoorCode(5, 1)));
            Assert.AreEqual(5, rules.Decision);
            Assert.AreEqual(CallingCorridorRules.Choice.Rejected,
                rules.EnterRoom(5, CallingCorridorRules.DoorCode(5, 1)));
            rules.Reset();
            Assert.AreEqual(0, rules.Decision);
            Assert.AreEqual(0, rules.Round);
            Assert.IsFalse(rules.AwaitingRetry);
        }

        [Test]
        public void SevenDecisionsReuseSixChoicesWithExactlyOneExactMatch()
        {
            int total = 0;
            for (int point = 0; point < 7; point++)
            {
                int matches = 0;
                Assert.AreEqual(6, CallingCorridorRules.DoorCount(point));
                for (int option = 0; option < CallingCorridorRules.DoorCount(point); option++)
                {
                    var rules = AtDecision(point);
                    var choice = rules.Select(point, CallingCorridorRules.DoorCode(point, option));
                    if (choice == CallingCorridorRules.Choice.Correct) matches++;
                    else Assert.AreEqual(CallingCorridorRules.Choice.Wrong, choice);
                    total++;
                }
                Assert.AreEqual(1, matches);
            }
            Assert.AreEqual(42, total);
        }

        [Test]
        public void FirstDecisionUsesOnlyK7AndSevenCorrectRoomsAreRequired()
        {
            for (int option = 0; option < 6; option++)
            {
                StringAssert.StartsWith("K7-", CallingCorridorRules.DoorCode(0, option));
            }
            var final = AtDecision(6);
            Assert.AreEqual(CallingCorridorRules.Choice.Rejected, new CallingCorridorRules().EnterRoom(0, "EXIT"));
            Assert.IsFalse(final.IsComplete);
            Assert.AreEqual(CallingCorridorRules.Choice.Correct, final.EnterRoom(6, "B6-153"));
            Assert.IsTrue(final.IsComplete);
            Assert.AreEqual(CallingCorridorRules.Choice.Rejected, final.EnterRoom(6, "B6-153"));
        }

        [Test]
        public void CodesStayFixedWithinSequenceAndCorrectDoorIsNotAlwaysFirst()
        {
            for (int option = 0; option < 6; option++)
            {
                Assert.AreEqual(CallingCorridorRules.DoorCode(2, option), CallingCorridorRules.DoorCode(3, option));
                Assert.AreEqual(CallingCorridorRules.DoorCode(4, option), CallingCorridorRules.DoorCode(6, option));
            }
            Assert.AreNotEqual(CallingCorridorRules.DoorCode(1, 0), CallingCorridorRules.ExpectedCode(1));
            Assert.AreNotEqual(CallingCorridorRules.ExpectedCode(2), CallingCorridorRules.ExpectedCode(3));
        }

        [Test]
        public void ExactCaseAndSequenceAreRequiredAndDuplicateCallbacksDoNotAdvance()
        {
            var rules = new CallingCorridorRules();
            Assert.AreEqual(CallingCorridorRules.Choice.Rejected, rules.Select(0, "k7-134"));
            Assert.AreEqual(CallingCorridorRules.Choice.Rejected, rules.Select(1, "A4-218"));
            Assert.IsFalse(rules.ConfirmPassage(0));
            Assert.AreEqual(CallingCorridorRules.Choice.Correct, rules.Select(0, "K7-134"));
            Assert.AreEqual(CallingCorridorRules.Choice.Rejected, rules.Select(0, "K7-134"));
            Assert.AreEqual(0, rules.Decision);
            Assert.IsTrue(rules.ConfirmPassage(0));
            Assert.IsFalse(rules.ConfirmPassage(0));
            Assert.AreEqual(1, rules.Decision);
        }

        [Test]
        public void WrongDoorResetsWholeCurrentSequenceButPreservesEarlierRounds()
        {
            var rules = AtDecision(5);
            Assert.AreEqual(3, rules.Round);
            Assert.AreEqual(CallingCorridorRules.Choice.Wrong, rules.Select(5, "B6-513"));
            Assert.AreEqual(CallingCorridorRules.Choice.Rejected, rules.Select(5, "B6-513"));
            Assert.AreEqual(1, rules.WrongChoices);
            rules.RetryRound();
            Assert.AreEqual(4, rules.Decision);
            Assert.AreEqual(3, rules.Round);
            Assert.AreEqual(1, rules.WrongChoices);
        }

        [Test]
        public void ThreatIsSafeInRoundOneAndConsumedOncePerLaterRound()
        {
            var rules = new CallingCorridorRules();
            Assert.IsFalse(rules.TryConsumeEncounter(true));
            Pass(rules);
            Assert.IsFalse(rules.TryConsumeEncounter(false));
            Wrong(rules);
            Assert.IsFalse(rules.TryConsumeEncounter(false));
            rules.RetryRound();
            Wrong(rules);
            Assert.IsTrue(rules.TryConsumeEncounter(false));
            rules.RetryRound();
            Assert.IsFalse(rules.TryConsumeEncounter(true));
            Pass(rules);
            Assert.IsTrue(rules.TryConsumeEncounter(true));
            Assert.IsFalse(rules.TryConsumeEncounter(true));
        }

        [Test]
        public void DeathAndRepeatedResetClearEntireAttemptAndCompletionIsOneShot()
        {
            var rules = AtDecision(6);
            Pass(rules);
            Assert.IsTrue(rules.IsComplete);
            Assert.IsFalse(rules.ConfirmPassage(6));
            rules.Reset();
            rules.Reset();
            Assert.AreEqual(0, rules.Round);
            Assert.AreEqual(0, rules.Decision);
            Assert.AreEqual(0, rules.WrongChoices);
            Assert.IsFalse(rules.AwaitingPassage);
            Assert.IsFalse(rules.AwaitingRetry);
        }

        private static CallingCorridorRules AtDecision(int point)
        {
            var rules = new CallingCorridorRules();
            while (rules.Decision < point) Pass(rules);
            return rules;
        }

        [Test]
        public void AllChoicesHaveThreeClockReadableDigitsIncludingFirstDecision()
        {
            for (int decision = 0; decision < 7; decision++)
            {
                for (int option = 0; option < 6; option++)
                {
                    string suffix = CallingCorridorRules.DoorCode(decision, option).Split('-')[1];
                    Assert.That(suffix.Length, Is.EqualTo(3));
                    foreach (char digit in suffix) Assert.That(digit - '0', Is.InRange(1, 9));
                }
                for (int clock = 0; clock < 3; clock++)
                    Assert.That(CallingCorridorRules.ClockDigit(decision, clock), Is.GreaterThan(0));
            }
        }

        [Test]
        public void FirstDecisionRejectsOldTwoDigitAnswerAndResetRestores134()
        {
            var rules = new CallingCorridorRules();
            Assert.AreEqual(CallingCorridorRules.Choice.Rejected, rules.EnterRoom(0, "K7-34"));
            Assert.AreEqual(CallingCorridorRules.Choice.Correct, rules.EnterRoom(0, "K7-134"));
            rules.Reset();
            Assert.AreEqual("K7-134", CallingCorridorRules.ExpectedCode(rules.Decision));
            Assert.AreEqual(1, CallingCorridorRules.ClockDigit(rules.Decision, 0));
        }

        [Test]
        public void MannequinCanStartAgainAfterEachCorrectChoiceFromFourthOnward()
        {
            var rules = AtDecision(2);
            Assert.IsFalse(rules.CanStartMannequin);
            Pass(rules);
            Assert.IsTrue(rules.CanStartMannequin);
            Assert.IsTrue(rules.TryConsumeEncounter(true));
            Assert.IsFalse(rules.CanStartMannequin);
            Pass(rules);
            Assert.IsTrue(rules.CanStartMannequin);
            Assert.IsTrue(rules.TryConsumeEncounter(true));
            Pass(rules);
            Assert.IsTrue(rules.CanStartMannequin);
            Assert.IsTrue(rules.TryConsumeEncounter(true));
            Pass(rules);
            Assert.IsTrue(rules.CanStartMannequin);
            Assert.IsTrue(rules.TryConsumeEncounter(true));
            while (!rules.IsComplete) Pass(rules);
            Assert.IsFalse(rules.CanStartMannequin);
            rules.Reset();
            Assert.IsFalse(rules.CanStartMannequin);
        }

        private static void Pass(CallingCorridorRules rules)
        {
            int point = rules.Decision;
            Assert.AreEqual(CallingCorridorRules.Choice.Correct, rules.Select(point, CallingCorridorRules.ExpectedCode(point)));
            Assert.IsTrue(rules.ConfirmPassage(point));
        }

        private static void Wrong(CallingCorridorRules rules) =>
            Assert.AreEqual(CallingCorridorRules.Choice.Wrong,
                rules.Select(rules.Decision, CallingCorridorRules.DoorCode(rules.Decision, 1)));
    }
}
