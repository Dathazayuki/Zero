using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class AnomalyGazeVisibilityTests
    {
        [UnityTest]
        public IEnumerator VisibleTarget_RejectsBackFacingAndOccludedTargets()
        {
            GameObject cameraObject = new GameObject("Test_GazeCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 0f, 5f);
            blocker.transform.position = new Vector3(10f, 0f, 2f);
            Physics.SyncTransforms();

            Renderer renderer = target.GetComponent<Renderer>();
            Assert.That(AnomalyGazeVisibility.IsVisible(camera, target.transform, renderer, ~0, 0.08f), Is.True);
            blocker.transform.position = new Vector3(0f, 0f, 2f);
            Physics.SyncTransforms();
            Assert.That(AnomalyGazeVisibility.IsVisible(camera, target.transform, renderer, ~0, 0.08f), Is.False);
            blocker.transform.position = new Vector3(10f, 0f, 2f);
            camera.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Physics.SyncTransforms();
            Assert.That(AnomalyGazeVisibility.IsVisible(camera, target.transform, renderer, ~0, 0.08f), Is.False);

            Object.Destroy(cameraObject);
            Object.Destroy(target);
            Object.Destroy(blocker);
            yield return null;
        }
    }
}
