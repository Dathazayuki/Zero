using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Game.Tests.EditMode
{
    public sealed class EndingScriptReferenceMigrationTests
    {
        [TestCase("Assets/Game/Scripts/Flow/EndingOneCutsceneController.cs",
            "Assets/Game/Scenes/InfiniteStairwayScene.unity", typeof(EndingOneCutsceneController))]
        [TestCase("Assets/Game/Scripts/Flow/TrueEndingGameplayController.cs",
            "Assets/Game/Scenes/BackRoomScene.unity", typeof(TrueEndingGameplayController))]
        [TestCase("Assets/Game/Scripts/Flow/TrueEndingTriggerRelay.cs",
            "Assets/Game/Scenes/BackRoomScene.unity", typeof(TrueEndingTriggerRelay))]
        public void RenamedScriptRetainsItsSceneReference(string scriptPath, string scenePath, Type expectedType)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            Assert.That(script, Is.Not.Null, $"Missing script: {scriptPath}");
            Assert.That(script.GetClass(), Is.EqualTo(expectedType));

            string guid = AssetDatabase.AssetPathToGUID(scriptPath);
            Assert.That(guid, Is.Not.Empty);
            Assert.That(File.ReadAllText(scenePath), Does.Contain($"guid: {guid}"));
        }
    }
}
