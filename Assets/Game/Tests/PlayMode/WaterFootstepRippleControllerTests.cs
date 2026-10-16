using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Utilities;

namespace Zero.Tests.PlayMode
{
    public sealed class WaterFootstepRippleControllerTests
    {
        [UnityTest]
        public IEnumerator MovementEmitsAndDisableClearsPooledRipples()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            GameObject actor = new GameObject("Ripple Test Player");
            GameObject host = new GameObject("Ripple Test Effects");
            Material material = new Material(Shader.Find("Zero/Water Footstep Ripple URP"));
            try
            {
                floor.transform.SetPositionAndRotation(new Vector3(0, -0.1f, 0), Quaternion.identity);
                floor.transform.localScale = new Vector3(20, 0.2f, 20);
                water.transform.position = Vector3.up * 0.24f;
                water.GetComponent<Collider>().enabled = false;
                CharacterController player = actor.AddComponent<CharacterController>();
                player.center = Vector3.up;
                var effects = host.AddComponent<WaterFootstepRippleController>();
                effects.Configure(player, water.GetComponent<MeshRenderer>(), material);
                Physics.SyncTransforms();
                player.Move(Vector3.down * 0.1f);
                yield return null;
                Assert.That(effects.ActiveRippleCount, Is.Zero);
                player.Move(new Vector3(1, -0.1f, 0));
                yield return null;
                Assert.That(effects.ActiveRippleCount, Is.GreaterThan(0));
                effects.enabled = false;
                effects.ClearRipples();
                Assert.That(effects.ActiveRippleCount, Is.Zero);
                foreach (MeshRenderer renderer in host.GetComponentsInChildren<MeshRenderer>())
                    Assert.That(renderer.enabled, Is.False);
            }
            finally
            {
                Object.Destroy(host); Object.Destroy(actor);
                Object.Destroy(water); Object.Destroy(floor); Object.Destroy(material);
            }
        }

        [UnityTest]
        public IEnumerator MissingReferencesFailSafeWithoutCreatingPool()
        {
            GameObject host = new GameObject("Missing Ripple Configuration");
            LogAssert.Expect(LogType.Error,
                "WATER_RIPPLE_CONFIGURATION_INVALID: player, surface or material missing");
            host.AddComponent<WaterFootstepRippleController>();
            yield return null;
            Assert.That(host.transform.childCount, Is.Zero);
            Object.Destroy(host);
        }
    }
}
