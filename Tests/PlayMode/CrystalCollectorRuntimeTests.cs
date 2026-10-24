using System;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;

namespace CrystalCollector.Tests
{
    public sealed class CrystalCollectorRuntimeTests
    {
        [UnityTest]
        public IEnumerator PhysicsTriggerGraphCollectsAllCrystalsAndPreservesPlayerCollider()
        {
            yield return SceneManager.LoadSceneAsync("MainGame");
            yield return null;

            var flow = FindComponentByTypeName("GameFlowController");
            var player = GameObject.FindWithTag("Player");
            var playerCollider = player.GetComponent<Collider>();
            var playerBody = player.GetComponent<Rigidbody>();
            var sceneVariables = GameObject.Find("Scene Variables")?.GetComponent("SceneVariables");
            Assert.That(flow, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(sceneVariables, Is.Not.Null);

            var waitStart = 0f;
            while (ReadState(flow) != "Playing" && waitStart < 5f)
            {
                waitStart += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.That(ReadState(flow), Is.EqualTo("Playing"));

            var crystals = FindComponentsByTypeName("CollectiblePresentation");
            Array.Sort(crystals, (a, b) => string.CompareOrdinal(a.name, b.name));
            Assert.That(crystals.Length, Is.EqualTo(8));
            Assert.That(ReadSceneCount(sceneVariables), Is.EqualTo(0f));

            for (var i = 0; i < crystals.Length; i++)
            {
                var crystal = crystals[i];
                var crystalCollider = crystal.GetComponent<Collider>();
                playerBody.position = crystal.transform.position;
                playerBody.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                Assert.That(ReadSceneCount(sceneVariables), Is.EqualTo(i + 1));
                Assert.That(playerCollider.enabled, Is.True, $"Player collider disabled after {crystal.name}");
                Assert.That(crystalCollider.enabled, Is.False, $"Crystal collider was not disabled by the graph for {crystal.name}");

                playerBody.position = crystal.transform.position;
                Physics.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.That(ReadSceneCount(sceneVariables), Is.EqualTo(i + 1));
            }

            Assert.That(ReadState(flow), Is.EqualTo("Victory"));
        }

        private static Component FindComponentByTypeName(string typeName)
        {
            foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component.GetType().Name == typeName) return component;
            return null;
        }

        private static Component[] FindComponentsByTypeName(string typeName)
        {
            var matches = new System.Collections.Generic.List<Component>();
            foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component.GetType().Name == typeName) matches.Add(component);
            return matches.ToArray();
        }

        private static string ReadState(Component flow)
        {
            return flow.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance)?.GetValue(flow)?.ToString();
        }

        private static float ReadSceneCount(Component sceneVariables)
        {
            var variablesField = sceneVariables.GetType().GetField("variables");
            var variables = variablesField?.GetValue(sceneVariables);
            var declarationsProperty = variables?.GetType().GetProperty("declarations");
            var declarations = declarationsProperty != null
                ? declarationsProperty.GetValue(variables)
                : variables?.GetType().GetField("declarations")?.GetValue(variables);
            var getMethod = declarations?.GetType().GetMethod("Get", new[] { typeof(string) });
            return Convert.ToSingle(getMethod?.Invoke(declarations, new object[] { "CollectedCount" }));
        }
    }
}
