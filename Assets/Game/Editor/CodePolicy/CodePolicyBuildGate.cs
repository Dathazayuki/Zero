using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Zero.Editor.CodePolicy
{
    internal sealed class CodePolicyBuildGate : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            CodePolicyReport policyReport = CodePolicyValidator.Validate(false, true);
            if (!policyReport.passed)
                throw new BuildFailedException($"Code policy rejected the build with {policyReport.errorCount} regression(s).");
        }
    }
}
