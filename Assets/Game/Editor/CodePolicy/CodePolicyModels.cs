using System;
using System.Collections.Generic;

namespace Zero.Editor.CodePolicy
{
    internal static class CodePolicyRules
    {
        public const int MaxLogicalLines = 300;
        public const int MaxMethodLines = 40;
        public const int MaxParameters = 5;
        public const int MaxNesting = 3;
        public const int MaxBranches = 10;
        public const int MaxSerializedDependencies = 7;

        public const string FileSize = "ZERO001";
        public const string MethodSize = "ZERO002";
        public const string Parameters = "ZERO003";
        public const string Nesting = "ZERO004";
        public const string Branches = "ZERO005";
        public const string Dependencies = "ZERO006";
        public const string PublicMutableField = "ZERO007";
        public const string HotPathApi = "ZERO008";
        public const string EditorHook = "ZERO009";
        public const string RuntimeEditorReference = "ZERO010";
        public const string AsyncVoid = "ZERO011";
        public const string EmptyCatch = "ZERO012";
        public const string Namespace = "ZERO013";
        public const string PublicTypes = "ZERO014";
        public const string SubscriptionCleanup = "ZERO015";
        public const string MissingPureRuleTest = "ZERO016";
    }

    [Serializable]
    internal class CodePolicyMetrics
    {
        public string path;
        public int logicalLines;
        public int maxMethodLines;
        public int maxParameters;
        public int maxNesting;
        public int maxBranches;
        public int serializedDependencies;
        public int publicMutableFields;
        public int hotPathViolations;
        public int forbiddenEditorHooks;
        public int runtimeEditorReferences;
        public int asyncVoidMethods;
        public int emptyCatchBlocks;
        public int namespaceViolations;
        public int publicTopLevelTypes;
        public int subscriptionCleanupWarnings;
        public int missingPureRuleTests;

        public bool HasDebt => logicalLines > CodePolicyRules.MaxLogicalLines ||
            maxMethodLines > CodePolicyRules.MaxMethodLines ||
            maxParameters > CodePolicyRules.MaxParameters ||
            maxNesting > CodePolicyRules.MaxNesting ||
            maxBranches > CodePolicyRules.MaxBranches ||
            serializedDependencies > CodePolicyRules.MaxSerializedDependencies ||
            publicMutableFields > 0 || hotPathViolations > 0 || forbiddenEditorHooks > 0 ||
            runtimeEditorReferences > 0 || asyncVoidMethods > 0 || emptyCatchBlocks > 0 ||
            namespaceViolations > 0 || publicTopLevelTypes > 1 ||
            subscriptionCleanupWarnings > 0 || missingPureRuleTests > 0;
    }

    [Serializable]
    internal sealed class CodePolicyBaseline
    {
        public int version = 1;
        public string generatedUtc;
        public string policy = "baseline-may-only-decrease";
        public List<CodePolicyBaselineEntry> entries = new List<CodePolicyBaselineEntry>();
    }

    [Serializable]
    internal sealed class CodePolicyBaselineEntry : CodePolicyMetrics
    {
        public string owner = "Zero team";
        public string reason = "Legacy debt present before policy adoption.";
        public string removalPlan = "Reduce when this module is next changed; never raise this ceiling.";
    }

    [Serializable]
    internal sealed class CodePolicyReport
    {
        public string generatedUtc;
        public bool passed;
        public int filesScanned;
        public int errorCount;
        public int warningCount;
        public List<CodePolicyIssue> issues = new List<CodePolicyIssue>();
        public List<CodePolicyMetrics> metrics = new List<CodePolicyMetrics>();
    }

    [Serializable]
    internal sealed class CodePolicyIssue
    {
        public string rule;
        public string severity;
        public string path;
        public string message;
    }
}
