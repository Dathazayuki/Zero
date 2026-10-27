using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Anomalies;
using Object = UnityEngine.Object;

internal static class CabaretBartenderRepair
{
    [MenuItem("Tools/Zero/Temporary/Repair Bartender Water")]
    private static void Repair()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "InfinityRoom5_CrimsonCabaret_Demo")
            throw new InvalidOperationException("Open Cabaret demo in Edit Mode.");
        var anomaly = GameObject.Find("IR5_Anomalies/IR5_08_InvisibleBartender").GetComponent<CabaretBartenderAnomaly>();
        var serialized = new SerializedObject(anomaly);
        var liquid = (Transform)serialized.FindProperty("liquid").objectReferenceValue;
        var observer = (Transform)serialized.FindProperty("observer").objectReferenceValue;
        var glass = (Transform)serialized.FindProperty("glass").objectReferenceValue;
        anomaly.ResetAnomaly();
        Undo.RecordObject(liquid.gameObject, "Keep normal bartender glass empty");
        liquid.gameObject.SetActive(false);
        Vector3 observerStart = observer.localPosition;
        var report = new StringBuilder();
        report.AppendLine("Water parent=" + liquid.parent.name + "; glass=" + glass.name);
        try { VerifyScene(anomaly, serialized, report); }
        finally { anomaly.ResetAnomaly(); observer.localPosition = observerStart; }
        RunTests(report);
        var policy = Zero.Editor.CodePolicy.CodePolicyValidator.Validate(false, false);
        if (policy.errorCount > 0) throw new InvalidOperationException("Code Policy failed.");
        report.AppendLine("Code Policy PASS: errors=0; warnings=" + policy.warningCount);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "cabaret-bartender-repair.txt"), report + "Scene NOT saved.");
    }

    private static void VerifyScene(CabaretBartenderAnomaly anomaly, SerializedObject serialized, StringBuilder report)
    {
        var observer = (Transform)serialized.FindProperty("observer").objectReferenceValue;
        var surface = (Transform)serialized.FindProperty("servingSurface").objectReferenceValue;
        var glass = (Transform)serialized.FindProperty("glass").objectReferenceValue;
        var liquid = (Transform)serialized.FindProperty("liquid").objectReferenceValue;
        var cap = (Transform)serialized.FindProperty("cap").objectReferenceValue;
        observer.position = surface.TransformPoint(new Vector3(-.1f, .6f, 1.2f));
        anomaly.Activate();
        for (int i = 0; i < 220; i++) anomaly.Advance(.05f);
        if (!liquid.gameObject.activeInHierarchy || Vector3.Distance(liquid.position, glass.position) > .001f)
            throw new InvalidOperationException("Water not visible/aligned after serving.");
        report.AppendLine("Finished: Water active, aligned with served glass; " + liquid.position);
        Render(new Vector3(-2.6f, 1.7f, -.4f), glass.position + Vector3.up * .08f, "cabaret-filled-glass.png");
        anomaly.ResetAnomaly(); anomaly.Activate();
        for (int i = 0; i < 94; i++) anomaly.Advance(.05f);
        Vector3 parked = cap.position;
        for (int i = 0; i < 15; i++) anomaly.Advance(.05f);
        if (Vector3.Distance(cap.position, parked) > .001f)
            throw new InvalidOperationException("Cap moves with the tilted bottle instead of remaining parked.");
        report.AppendLine("Pour: cap remains parked in world space while bottle tilts.");
        anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
        if (liquid.gameObject.activeSelf) throw new InvalidOperationException("Water should hide on F2.");
    }

    private static void RunTests(StringBuilder report)
    {
        Type type = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType("Zero.Tests.EditMode.CabaretBartenderAnomalyTests");
            if (type != null) break;
        }
        var instance = Activator.CreateInstance(type);
        foreach (bool sibling in new[] { true, false })
            type.GetMethod("FilledWaterFollowsGlassWithEitherSiblingOrChildHierarchy").Invoke(instance, new object[] { sibling });
        type.GetMethod("CapArcLiftsAboveStraightPathAndReturnsToExactMouthPosition").Invoke(instance, null);
        type.GetMethod("RearOfCounterCannotTriggerServiceButCustomerSideCan").Invoke(instance, null);
        foreach (float seconds in new[] { 1f, 2.8f, 4.8f, 7f, 8.7f })
            type.GetMethod("InterruptedServiceRestoresPropsAndHidesWine").Invoke(instance, new object[] { seconds });
        report.AppendLine("9/9 component regression cases passed in-process (not Test Runner).");
    }

    private static void Render(Vector3 position, Vector3 target, string filename)
    {
        var holder = new GameObject("BartenderRepairPreview");
        var camera = holder.AddComponent<Camera>();
        camera.transform.position = position; camera.transform.LookAt(target);
        camera.fieldOfView = 38f; camera.nearClipPlane = .03f;
        var texture = new RenderTexture(960, 720, 24);
        var previous = RenderTexture.active;
        Texture2D image = null;
        try
        {
            camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
            image = new Texture2D(960, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), filename), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous; texture.Release();
            Object.DestroyImmediate(texture); Object.DestroyImmediate(holder);
            if (image != null) Object.DestroyImmediate(image);
        }
    }
}
