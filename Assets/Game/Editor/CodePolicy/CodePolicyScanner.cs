using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Zero.Editor.CodePolicy
{
    internal static class CodePolicyScanner
    {
        private static readonly Regex MethodPattern = new Regex(
            @"(?m)^\s*(?:public|private|protected|internal)\s+(?:(?:static|virtual|override|abstract|async|sealed|new|extern|unsafe|partial)\s+)*(?:[\w.<>,?\[\]]+\s+)?(?<name>\w+)\s*\((?<parameters>[^)]*)\)\s*(?:where[^\{]+)?\s*\{",
            RegexOptions.Compiled);
        private static readonly Regex SerializedFieldPattern = new Regex(
            @"\[SerializeField[^\]]*\](?:\s*\[[^\]]+\])*\s*private\s+(?<type>[\w.<>,?\[\]]+)\s+\w+",
            RegexOptions.Compiled);
        private static readonly Regex PublicFieldPattern = new Regex(
            @"(?m)^\s*public\s+(?!(?:const|readonly|class|struct|enum|interface|delegate|event)\b)[\w.<>,?\[\]]+\s+\w+\s*(?:=(?!>)[^;]*)?;",
            RegexOptions.Compiled);
        private static readonly Regex PublicTypePattern = new Regex(
            @"(?m)^\s*public\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|struct|interface|enum)\s+",
            RegexOptions.Compiled);
        private static readonly Regex BranchPattern = new Regex(
            @"\b(?:if|else\s+if|for|foreach|while|case|catch)\b|&&|\|\|", RegexOptions.Compiled);
        private static readonly Regex HotPathForbiddenPattern = new Regex(
            @"\b(?:GameObject\s*\.\s*Find|Find(?:First|Any)?Object(?:s)?ByType|FindObjectOfType|Resources\s*\.\s*Load|GetComponent(?:InChildren|InParent)?|Enumerable\s*\.|\.\s*(?:Where|Select|OrderBy|ToList|ToArray)\s*\()",
            RegexOptions.Compiled);
        private static readonly string[] DependencyTokens =
        {
            "GameObject", "Transform", "Camera", "Animator", "AudioSource", "AudioClip",
            "Renderer", "Collider", "Light", "Material", "RenderTexture", "Canvas", "Image",
            "ParticleSystem", "NavMesh", "Cinemachine", "PlayableDirector", "Controller",
            "Manager", "Behaviour", "Trigger", "Prefab", "Sprite", "Texture", "Volume"
        };

        public static CodePolicyMetrics AnalyzeFile(string absolutePath, string assetPath)
        {
            return AnalyzeText(assetPath, File.ReadAllText(absolutePath));
        }

        public static CodePolicyMetrics AnalyzeText(string assetPath, string source)
        {
            string clean = CodePolicySource.Sanitize(source);
            var metrics = new CodePolicyMetrics
            {
                path = assetPath.Replace('\\', '/'),
                logicalLines = CodePolicySource.CountLogicalLines(clean),
                publicMutableFields = PublicFieldPattern.Matches(clean).Count,
                publicTopLevelTypes = CountTopLevelPublicTypes(clean),
                forbiddenEditorHooks = Count(clean, @"\[InitializeOnLoad(?:Method)?\]|EditorApplication\s*\.\s*(?:update|delayCall)"),
                runtimeEditorReferences = Count(clean, @"\b(?:using\s+UnityEditor|UnityEditor\s*\.)"),
                asyncVoidMethods = Count(clean, @"\basync\s+void\s+\w+\s*\("),
                emptyCatchBlocks = Count(clean, @"\bcatch\s*(?:\([^)]*\))?\s*\{\s*\}"),
                subscriptionCleanupWarnings = HasUnbalancedSubscription(clean) ? 1 : 0,
                namespaceViolations = HasExpectedNamespace(metricsPath: assetPath, source: clean) ? 0 : 1,
                serializedDependencies = CountSerializedDependencies(clean)
            };

            bool hasPlainClass = Regex.IsMatch(clean, @"\bclass\s+\w+") &&
                !Regex.IsMatch(clean, @":\s*(?:MonoBehaviour|ScriptableObject|Editor|EditorWindow)\b");
            metrics.missingPureRuleTests = hasPlainClass && !HasMatchingTest(assetPath) ? 1 : 0;

            foreach (Match method in MethodPattern.Matches(clean))
            {
                int open = clean.IndexOf('{', method.Index + method.Length - 1);
                if (open < 0) continue;
                int close = CodePolicySource.FindClosingBrace(clean, open);
                string body = clean.Substring(open, close - open + 1);
                int lines = CodePolicySource.CountLogicalLines(clean, method.Index, close + 1);
                metrics.maxMethodLines = Math.Max(metrics.maxMethodLines, lines);
                metrics.maxParameters = Math.Max(metrics.maxParameters, CountParameters(method.Groups["parameters"].Value));
                metrics.maxBranches = Math.Max(metrics.maxBranches, BranchPattern.Matches(body).Count);
                metrics.maxNesting = Math.Max(metrics.maxNesting, CalculateNesting(body));
                string name = method.Groups["name"].Value;
                if (name == "Update" || name == "LateUpdate" || name == "FixedUpdate")
                    metrics.hotPathViolations += HotPathForbiddenPattern.Matches(body).Count;
            }
            return metrics;
        }

        private static int Count(string source, string pattern) => Regex.Matches(source, pattern).Count;

        private static int CountParameters(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters)) return 0;
            int genericDepth = 0;
            int count = 1;
            foreach (char c in parameters)
            {
                if (c == '<' || c == '(' || c == '[') genericDepth++;
                else if (c == '>' || c == ')' || c == ']') genericDepth--;
                else if (c == ',' && genericDepth == 0) count++;
            }
            return count;
        }

        private static int CalculateNesting(string body)
        {
            int depth = 0;
            int maximum = 0;
            foreach (char c in body)
            {
                if (c == '{') maximum = Math.Max(maximum, ++depth - 1);
                else if (c == '}') depth--;
            }
            return maximum;
        }

        private static int CountSerializedDependencies(string source)
        {
            int count = 0;
            foreach (Match match in SerializedFieldPattern.Matches(source))
            {
                string type = match.Groups["type"].Value;
                if (DependencyTokens.Any(type.Contains)) count++;
            }
            return count;
        }

        private static int CountTopLevelPublicTypes(string source)
        {
            int count = 0;
            foreach (Match match in PublicTypePattern.Matches(source))
            {
                int depth = 0;
                for (int i = 0; i < match.Index; i++)
                {
                    if (source[i] == '{') depth++;
                    else if (source[i] == '}') depth--;
                }
                if (depth <= 1) count++;
            }
            return count;
        }

        private static bool HasUnbalancedSubscription(string source)
        {
            bool subscribes = Regex.IsMatch(source, @"\b\w+(?:\.\w+)+\s*\+=\s*\w+\s*;");
            bool unsubscribes = Regex.IsMatch(source, @"\b\w+(?:\.\w+)+\s*-=\s*\w+\s*;");
            return subscribes && !unsubscribes;
        }

        private static bool HasExpectedNamespace(string metricsPath, string source)
        {
            string path = metricsPath.Replace('\\', '/');
            string[] parts = path.Split('/');
            int scriptsIndex = Array.IndexOf(parts, "Scripts");
            string folder = scriptsIndex >= 0 && scriptsIndex + 1 < parts.Length - 1
                ? parts[scriptsIndex + 1]
                : parts.Reverse().Skip(1).FirstOrDefault() ?? string.Empty;
            if (folder == "EnvironmentLoop") return Regex.IsMatch(source, @"namespace\s+Zero\.EnvironmentLoop\b");
            if (folder == "Utilities" || folder == "Debug" || folder == "Editor") return true;
            string expected = "Zero.AnomalyPrototype." + folder;
            return Regex.IsMatch(source, @"namespace\s+" + Regex.Escape(expected) + @"\b");
        }

        private static bool HasMatchingTest(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath);
            string[] tests = Directory.Exists("Assets/Game/Tests")
                ? Directory.GetFiles("Assets/Game/Tests", "*Tests.cs", SearchOption.AllDirectories)
                : Array.Empty<string>();
            return tests.Any(path => Path.GetFileNameWithoutExtension(path).StartsWith(name, StringComparison.Ordinal));
        }
    }
}
