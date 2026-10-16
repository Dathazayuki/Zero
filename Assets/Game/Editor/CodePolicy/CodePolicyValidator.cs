using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Zero.Editor.CodePolicy
{
    public static class CodePolicyValidator
    {
        internal const string BaselinePath = "Assets/Game/Documentation/Engineering/CodePolicyBaseline.json";
        internal const string ReportPath = "Assets/Game/Documentation/Engineering/CodePolicyReport.json";
        private const string ScriptsRoot = "Assets/Game/Scripts";

        [MenuItem("Tools/Zero/Code Policy/Validate Changed Code")]
        private static void ValidateChangedMenu() => Validate(false, true);

        [MenuItem("Tools/Zero/Code Policy/Validate Entire Project")]
        private static void ValidateEntireMenu() => Validate(true, true);

        [MenuItem("Tools/Zero/Code Policy/Export Report")]
        private static void ExportReport()
        {
            CodePolicyReport report = Validate(true, false);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.ImportAsset(ReportPath);
            Debug.Log($"CODE_POLICY_REPORT_EXPORTED: path={ReportPath}, errors={report.errorCount}, warnings={report.warningCount}");
        }

        [MenuItem("Tools/Zero/Code Policy/Create Initial Baseline")]
        private static void CreateInitialBaseline()
        {
            CodePolicyBaseline existing = LoadBaseline();
            if (existing.entries.Count > 0)
            {
                Debug.LogError("CODE_POLICY_BASELINE_CREATE_REJECTED: baseline already contains debt entries.");
                return;
            }
            var baseline = new CodePolicyBaseline { generatedUtc = DateTime.UtcNow.ToString("O") };
            foreach (CodePolicyMetrics metrics in ScanAll())
            {
                if (!metrics.HasDebt) continue;
                baseline.entries.Add(CreateEntry(metrics));
            }
            File.WriteAllText(BaselinePath, JsonUtility.ToJson(baseline, true));
            AssetDatabase.ImportAsset(BaselinePath);
            Debug.Log($"CODE_POLICY_BASELINE_CREATED: entries={baseline.entries.Count}");
        }

        public static void CreateInitialBaselineBatch()
        {
            CreateInitialBaseline();
            CodePolicyReport report = Validate(false, true);
            if (!report.passed)
                throw new InvalidOperationException($"Initial code-policy validation failed with {report.errorCount} error(s).");
        }

        [MenuItem("Tools/Zero/Code Policy/Lower Existing Baseline")]
        private static void LowerExistingBaseline()
        {
            CodePolicyBaseline baseline = LoadBaseline();
            Dictionary<string, CodePolicyMetrics> current = ScanAll().ToDictionary(item => item.path);
            bool changed = false;
            foreach (CodePolicyBaselineEntry entry in baseline.entries)
            {
                if (!current.TryGetValue(entry.path, out CodePolicyMetrics metrics)) continue;
                changed |= Lower(entry, metrics);
            }
            if (!changed) { Debug.Log("CODE_POLICY_BASELINE_UNCHANGED"); return; }
            baseline.generatedUtc = DateTime.UtcNow.ToString("O");
            File.WriteAllText(BaselinePath, JsonUtility.ToJson(baseline, true));
            AssetDatabase.ImportAsset(BaselinePath);
            Debug.Log("CODE_POLICY_BASELINE_LOWERED: debt ceilings were reduced; no new exception was added.");
        }

        public static void LowerExistingBaselineBatch()
        {
            LowerExistingBaseline();
            CodePolicyReport report = Validate(false, true);
            if (!report.passed)
                throw new InvalidOperationException($"Code-policy validation failed with {report.errorCount} regression(s).");
        }

        internal static CodePolicyReport Validate(bool includeLegacyDebt, bool log)
        {
            CodePolicyBaseline baseline = LoadBaseline();
            var ceilings = baseline.entries.ToDictionary(item => item.path, StringComparer.Ordinal);
            var report = new CodePolicyReport { generatedUtc = DateTime.UtcNow.ToString("O") };
            foreach (CodePolicyMetrics metrics in ScanAll())
            {
                report.filesScanned++;
                report.metrics.Add(metrics);
                ceilings.TryGetValue(metrics.path, out CodePolicyBaselineEntry ceiling);
                Compare(metrics, ceiling, report, includeLegacyDebt);
            }
            report.errorCount = report.issues.Count(issue => issue.severity == "Error");
            report.warningCount = report.issues.Count(issue => issue.severity == "Warning");
            report.passed = report.errorCount == 0;
            if (log) Log(report);
            return report;
        }

        private static IEnumerable<CodePolicyMetrics> ScanAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { ScriptsRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.Contains("/Editor/")) continue;
                yield return CodePolicyScanner.AnalyzeFile(Path.GetFullPath(path), path);
            }
        }

        private static CodePolicyBaseline LoadBaseline()
        {
            if (!File.Exists(BaselinePath))
                throw new InvalidOperationException($"CODE_POLICY_BASELINE_MISSING: path={BaselinePath}");
            CodePolicyBaseline baseline = JsonUtility.FromJson<CodePolicyBaseline>(File.ReadAllText(BaselinePath));
            if (baseline == null || baseline.version != 1)
                throw new InvalidOperationException("CODE_POLICY_BASELINE_INVALID: expected version=1");
            return baseline;
        }

        private static void Compare(CodePolicyMetrics current, CodePolicyBaselineEntry baseline,
            CodePolicyReport report, bool includeDebt)
        {
            Check(CodePolicyRules.FileSize, current.logicalLines, CodePolicyRules.MaxLogicalLines,
                baseline?.logicalLines ?? 0, current.path, "logical lines", report, includeDebt);
            Check(CodePolicyRules.MethodSize, current.maxMethodLines, CodePolicyRules.MaxMethodLines,
                baseline?.maxMethodLines ?? 0, current.path, "method lines", report, includeDebt);
            Check(CodePolicyRules.Parameters, current.maxParameters, CodePolicyRules.MaxParameters,
                baseline?.maxParameters ?? 0, current.path, "method parameters", report, includeDebt);
            Check(CodePolicyRules.Nesting, current.maxNesting, CodePolicyRules.MaxNesting,
                baseline?.maxNesting ?? 0, current.path, "nesting depth", report, includeDebt);
            Check(CodePolicyRules.Branches, current.maxBranches, CodePolicyRules.MaxBranches,
                baseline?.maxBranches ?? 0, current.path, "branch points", report, includeDebt);
            Check(CodePolicyRules.Dependencies, current.serializedDependencies, CodePolicyRules.MaxSerializedDependencies,
                baseline?.serializedDependencies ?? 0, current.path, "serialized dependencies", report, includeDebt);
            CheckZero(CodePolicyRules.PublicMutableField, current.publicMutableFields, baseline?.publicMutableFields ?? 0, current.path, report, includeDebt);
            CheckZero(CodePolicyRules.HotPathApi, current.hotPathViolations, baseline?.hotPathViolations ?? 0, current.path, report, includeDebt);
            CheckZero(CodePolicyRules.EditorHook, current.forbiddenEditorHooks, baseline?.forbiddenEditorHooks ?? 0, current.path, report, includeDebt);
            CheckZero(CodePolicyRules.RuntimeEditorReference, current.runtimeEditorReferences, baseline?.runtimeEditorReferences ?? 0, current.path, report, includeDebt);
            CheckZero(CodePolicyRules.AsyncVoid, current.asyncVoidMethods, baseline?.asyncVoidMethods ?? 0, current.path, report, includeDebt);
            CheckZero(CodePolicyRules.EmptyCatch, current.emptyCatchBlocks, baseline?.emptyCatchBlocks ?? 0, current.path, report, includeDebt);
            CheckZero(CodePolicyRules.Namespace, current.namespaceViolations, baseline?.namespaceViolations ?? 0, current.path, report, includeDebt);
            Check(CodePolicyRules.PublicTypes, current.publicTopLevelTypes, 1, baseline?.publicTopLevelTypes ?? 0,
                current.path, "public top-level types", report, includeDebt);
            CheckWarning(CodePolicyRules.SubscriptionCleanup, current.subscriptionCleanupWarnings,
                baseline?.subscriptionCleanupWarnings ?? 0, current.path, "subscription without cleanup", report, includeDebt);
            CheckZero(CodePolicyRules.MissingPureRuleTest, current.missingPureRuleTests,
                baseline?.missingPureRuleTests ?? 0, current.path, report, includeDebt);
        }

        private static void Check(string rule, int value, int strict, int legacy, string path,
            string label, CodePolicyReport report, bool includeDebt)
        {
            int ceiling = Math.Max(strict, legacy);
            if (value > ceiling) Add(report, rule, "Error", path, $"{label}={value}, ceiling={ceiling}");
            else if (includeDebt && value > strict) Add(report, rule, "Warning", path, $"legacy {label}={value}, target={strict}");
        }

        private static void CheckZero(string rule, int value, int legacy, string path,
            CodePolicyReport report, bool includeDebt) => Check(rule, value, 0, legacy, path, "violations", report, includeDebt);

        private static void CheckWarning(string rule, int value, int legacy, string path,
            string label, CodePolicyReport report, bool includeDebt)
        {
            if (value > legacy) Add(report, rule, "Warning", path, $"{label}; count={value}");
            else if (includeDebt && value > 0) Add(report, rule, "Warning", path, $"legacy {label}; count={value}");
        }

        private static void Add(CodePolicyReport report, string rule, string severity, string path, string message) =>
            report.issues.Add(new CodePolicyIssue { rule = rule, severity = severity, path = path, message = message });

        private static void Log(CodePolicyReport report)
        {
            foreach (CodePolicyIssue issue in report.issues)
            {
                string message = $"CODE_POLICY_VALIDATION_{issue.severity.ToUpperInvariant()}: rule={issue.rule}, path={issue.path}, {issue.message}";
                if (issue.severity == "Error") Debug.LogError(message); else Debug.LogWarning(message);
            }
            if (report.passed) Debug.Log($"CODE_POLICY_VALIDATION_PASS: files={report.filesScanned}, warnings={report.warningCount}");
            else Debug.LogError($"CODE_POLICY_VALIDATION_FAIL: files={report.filesScanned}, errors={report.errorCount}, warnings={report.warningCount}");
        }

        private static bool Lower(CodePolicyBaselineEntry entry, CodePolicyMetrics current)
        {
            bool changed = false;
            changed |= LowerValue(ref entry.logicalLines, current.logicalLines);
            changed |= LowerValue(ref entry.maxMethodLines, current.maxMethodLines);
            changed |= LowerValue(ref entry.maxParameters, current.maxParameters);
            changed |= LowerValue(ref entry.maxNesting, current.maxNesting);
            changed |= LowerValue(ref entry.maxBranches, current.maxBranches);
            changed |= LowerValue(ref entry.serializedDependencies, current.serializedDependencies);
            changed |= LowerValue(ref entry.publicMutableFields, current.publicMutableFields);
            changed |= LowerValue(ref entry.hotPathViolations, current.hotPathViolations);
            changed |= LowerValue(ref entry.forbiddenEditorHooks, current.forbiddenEditorHooks);
            changed |= LowerValue(ref entry.runtimeEditorReferences, current.runtimeEditorReferences);
            changed |= LowerValue(ref entry.asyncVoidMethods, current.asyncVoidMethods);
            changed |= LowerValue(ref entry.emptyCatchBlocks, current.emptyCatchBlocks);
            changed |= LowerValue(ref entry.namespaceViolations, current.namespaceViolations);
            changed |= LowerValue(ref entry.publicTopLevelTypes, current.publicTopLevelTypes);
            changed |= LowerValue(ref entry.subscriptionCleanupWarnings, current.subscriptionCleanupWarnings);
            changed |= LowerValue(ref entry.missingPureRuleTests, current.missingPureRuleTests);
            return changed;
        }

        private static CodePolicyBaselineEntry CreateEntry(CodePolicyMetrics source)
        {
            return new CodePolicyBaselineEntry
            {
                path = source.path,
                logicalLines = source.logicalLines,
                maxMethodLines = source.maxMethodLines,
                maxParameters = source.maxParameters,
                maxNesting = source.maxNesting,
                maxBranches = source.maxBranches,
                serializedDependencies = source.serializedDependencies,
                publicMutableFields = source.publicMutableFields,
                hotPathViolations = source.hotPathViolations,
                forbiddenEditorHooks = source.forbiddenEditorHooks,
                runtimeEditorReferences = source.runtimeEditorReferences,
                asyncVoidMethods = source.asyncVoidMethods,
                emptyCatchBlocks = source.emptyCatchBlocks,
                namespaceViolations = source.namespaceViolations,
                publicTopLevelTypes = source.publicTopLevelTypes,
                subscriptionCleanupWarnings = source.subscriptionCleanupWarnings,
                missingPureRuleTests = source.missingPureRuleTests
            };
        }

        private static bool LowerValue(ref int ceiling, int current)
        {
            if (current >= ceiling) return false;
            ceiling = current;
            return true;
        }
    }
}
