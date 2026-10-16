using NUnit.Framework;
using Zero.Editor.CodePolicy;

namespace Zero.Tests.EditMode
{
    public sealed class CodePolicyScannerTests
    {
        [Test]
        public void AnalyzeText_DetectsForbiddenLookupInsideUpdate()
        {
            const string source = @"
namespace Zero.AnomalyPrototype.Flow
{
    public sealed class BadComponent : UnityEngine.MonoBehaviour
    {
        private void Update()
        {
            UnityEngine.GameObject.Find(""Player"");
        }
    }
}";

            CodePolicyMetrics metrics = CodePolicyScanner.AnalyzeText(
                "Assets/Game/Scripts/Flow/BadComponent.cs", source);

            Assert.That(metrics.hotPathViolations, Is.EqualTo(1));
        }

        [Test]
        public void AnalyzeText_IgnoresForbiddenWordsInsideCommentsAndStrings()
        {
            const string source = @"
namespace Zero.AnomalyPrototype.Flow
{
    public sealed class SafeComponent : UnityEngine.MonoBehaviour
    {
        private const string Label = ""GameObject.Find"";
        private void Update()
        {
            // GameObject.Find(""Player"");
            if (!enabled) return;
        }
    }
}";

            CodePolicyMetrics metrics = CodePolicyScanner.AnalyzeText(
                "Assets/Game/Scripts/Flow/SafeComponent.cs", source);

            Assert.That(metrics.hotPathViolations, Is.Zero);
        }

        [Test]
        public void AnalyzeText_DetectsPublicMutableField()
        {
            const string source = @"
namespace Zero.AnomalyPrototype.Core
{
    public sealed class Rule
    {
        public int MutableValue;
        public int ReadOnlyValue { get; private set; }
    }
}";

            CodePolicyMetrics metrics = CodePolicyScanner.AnalyzeText(
                "Assets/Game/Scripts/Core/Rule.cs", source);

            Assert.That(metrics.publicMutableFields, Is.EqualTo(1));
        }

        [Test]
        public void AnalyzeText_NestedFolderUsesOwningModuleNamespace()
        {
            const string source = "namespace Zero.AnomalyPrototype.Flow { public sealed class AudioRule { } }";
            CodePolicyMetrics metrics = CodePolicyScanner.AnalyzeText(
                "Assets/Game/Scripts/Flow/Audio/AudioRule.cs", source);
            Assert.That(metrics.namespaceViolations, Is.Zero);
        }
    }
}
