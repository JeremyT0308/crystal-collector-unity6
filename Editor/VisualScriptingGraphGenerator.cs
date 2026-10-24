using System;
using System.Reflection;
using CrystalCollector;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;

namespace CrystalCollector.EditorTools
{
    /// <summary>
    /// Builds the shared collection graph with the Visual Scripting 1.9.12 API installed in this project.
    /// </summary>
    public static class VisualScriptingGraphGenerator
    {
        public const string GraphPath = "Assets/Generated/VisualScripting/CollectCrystal.asset";

        public static ScriptGraphAsset GenerateCollectionGraph()
        {
            EnsureFolder("Assets/Generated");
            EnsureFolder("Assets/Generated/VisualScripting");

            if (AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(GraphPath) != null)
            {
                AssetDatabase.DeleteAsset(GraphPath);
            }

            var asset = ScriptableObject.CreateInstance<ScriptGraphAsset>();
            asset.name = "CollectCrystal";

            var graph = asset.graph;
            graph.title = "Collect Crystal";
            graph.summary = "Visual Scripting owns trigger detection, Player validation, duplicate protection, score mutation, notification, and victory threshold evaluation.";
            graph.pan = new Vector2(-420f, -120f);
            graph.zoom = 0.8f;

            var trigger = Add(graph, new OnTriggerEnter(), new Vector2(-950f, -60f));
            var self = Add(graph, new This(), new Vector2(-950f, 220f));
            var gameObject = Add(graph, new GetMember(CreateMember(typeof(Collider), typeof(Component).GetProperty(nameof(Component.gameObject)))), new Vector2(-700f, -180f));
            var compareTag = Add(graph, new InvokeMember(CreateMember(typeof(GameObject), typeof(GameObject).GetMethod(nameof(GameObject.CompareTag), new[] { typeof(string) }))), new Vector2(-420f, -170f));
            compareTag.inputParameters[0].SetDefaultValue("Player");
            var playerBranch = Add(graph, new If(), new Vector2(-160f, -80f));

            var collected = new GetVariable { kind = VariableKind.Object, specifyFallback = true };
            var collectedGet = Add(graph, collected, new Vector2(-410f, 210f));
            collected.name.SetDefaultValue("Collected");
            collected.fallback.SetDefaultValue(false);
            var convertCollected = Add(graph, new InvokeMember(CreateMember(typeof(Convert), typeof(Convert).GetMethod(nameof(Convert.ToBoolean), new[] { typeof(object) }))), new Vector2(-170f, 360f));
            var equalsFalse = Add(graph, new Negate(), new Vector2(90f, 220f));
            var collectedBranch = Add(graph, new If(), new Vector2(90f, 40f));

            var markCollected = new SetVariable { kind = VariableKind.Object };
            var collectedSet = Add(graph, markCollected, new Vector2(330f, 40f));
            markCollected.name.SetDefaultValue("Collected");
            var collectedTrue = Add(graph, new Literal(typeof(bool), true), new Vector2(90f, -120f));

            var ownColliderMember = typeof(GameObject).GetMethod(nameof(GameObject.GetComponent), new[] { typeof(Type) });
            var ownCollider = Add(graph, new InvokeMember(CreateMember(typeof(GameObject), ownColliderMember)), new Vector2(330f, 120f));
            ownCollider.inputParameters[0].SetDefaultValue(typeof(Collider));
            var disableTrigger = Add(graph, new SetMember(CreateMember(typeof(Collider), typeof(Collider).GetProperty(nameof(Collider.enabled)))), new Vector2(560f, 40f));
            disableTrigger.input.SetDefaultValue(false);

            var countGet = new GetVariable { kind = VariableKind.Scene, specifyFallback = true };
            var countRead = Add(graph, countGet, new Vector2(330f, 280f));
            countGet.name.SetDefaultValue("CollectedCount");
            countGet.fallback.SetDefaultValue(0f);
            var convertCount = Add(graph, new InvokeMember(CreateMember(typeof(Convert), typeof(Convert).GetMethod(nameof(Convert.ToSingle), new[] { typeof(object) }))), new Vector2(330f, 430f));
            var addOne = Add(graph, new ScalarSum(), new Vector2(560f, 280f));
            addOne.multiInputs[1].SetDefaultValue(1f);

            var countSet = new SetVariable { kind = VariableKind.Scene };
            var countWrite = Add(graph, countSet, new Vector2(790f, 280f));
            countSet.name.SetDefaultValue("CollectedCount");

            var notify = Add(graph, new InvokeMember(CreateMember(typeof(GameFlowController), typeof(GameFlowController).GetMethod(nameof(GameFlowController.NotifyVisualScriptCrystalCollected), BindingFlags.Public | BindingFlags.Static))), new Vector2(1020f, 280f));

            var totalGet = new GetVariable { kind = VariableKind.Scene, specifyFallback = true };
            var totalRead = Add(graph, totalGet, new Vector2(790f, 560f));
            totalGet.name.SetDefaultValue("TotalCrystals");
            totalGet.fallback.SetDefaultValue(8f);
            var convertTotal = Add(graph, new InvokeMember(CreateMember(typeof(Convert), typeof(Convert).GetMethod(nameof(Convert.ToSingle), new[] { typeof(object) }))), new Vector2(1030f, 700f));
            var threshold = Add(graph, new Comparison { numeric = true }, new Vector2(1260f, 540f));
            threshold.b.SetDefaultValue(8f);
            var completionBranch = Add(graph, new If(), new Vector2(1490f, 280f));
            var victory = Add(graph, new InvokeMember(CreateMember(typeof(GameFlowController), typeof(GameFlowController).GetMethod(nameof(GameFlowController.NotifyVisualScriptVictory), BindingFlags.Public | BindingFlags.Static))), new Vector2(1720f, 210f));

            trigger.trigger.ValidlyConnectTo(compareTag.enter);
            trigger.collider.ValidlyConnectTo(gameObject.target);
            gameObject.value.ValidlyConnectTo(compareTag.target);
            compareTag.result.ValidlyConnectTo(playerBranch.condition);
            compareTag.exit.ValidlyConnectTo(playerBranch.enter);

            collected.value.ValidlyConnectTo(convertCollected.inputParameters[0]);
            convertCollected.result.ValidlyConnectTo(equalsFalse.input);
            equalsFalse.output.ValidlyConnectTo(collectedBranch.condition);
            self.self.ValidlyConnectTo(collectedGet.@object);
            self.self.ValidlyConnectTo(collectedSet.@object);
            playerBranch.ifTrue.ValidlyConnectTo(collectedBranch.enter);
            collectedBranch.ifTrue.ValidlyConnectTo(collectedSet.assign);
            collectedTrue.output.ValidlyConnectTo(collectedSet.input);
            collectedSet.assigned.ValidlyConnectTo(disableTrigger.assign);
            self.self.ValidlyConnectTo(ownCollider.target);
            ownCollider.result.ValidlyConnectTo(disableTrigger.target);
            disableTrigger.assigned.ValidlyConnectTo(countWrite.assign);
            countRead.value.ValidlyConnectTo(convertCount.inputParameters[0]);
            convertCount.result.ValidlyConnectTo(addOne.multiInputs[0]);
            addOne.sum.ValidlyConnectTo(countWrite.input);
            countWrite.assigned.ValidlyConnectTo(notify.enter);
            self.self.ValidlyConnectTo(notify.inputParameters[0]);
            addOne.sum.ValidlyConnectTo(notify.inputParameters[1]);
            addOne.sum.ValidlyConnectTo(threshold.a);
            totalRead.value.ValidlyConnectTo(convertTotal.inputParameters[0]);
            convertTotal.result.ValidlyConnectTo(threshold.b);
            notify.exit.ValidlyConnectTo(completionBranch.enter);
            threshold.aGreaterThanOrEqualToB.ValidlyConnectTo(completionBranch.condition);
            completionBranch.ifTrue.ValidlyConnectTo(victory.enter);

            EditorUtility.SetDirty(asset);
            AssetDatabase.CreateAsset(asset, GraphPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(GraphPath);
        }

        private static T Add<T>(FlowGraph graph, T unit, Vector2 position) where T : Unit
        {
            unit.position = position;
            graph.units.Add(unit);
            return unit;
        }

        private static Member CreateMember(Type targetType, MemberInfo memberInfo)
        {
            if (memberInfo is MethodInfo methodInfo) return new Member(targetType, methodInfo);
            if (memberInfo is PropertyInfo propertyInfo) return new Member(targetType, propertyInfo);
            if (memberInfo is FieldInfo fieldInfo) return new Member(targetType, fieldInfo);
            throw new InvalidOperationException($"Unsupported Visual Scripting member: {memberInfo}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent)) AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
