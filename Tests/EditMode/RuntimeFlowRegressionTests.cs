using NUnit.Framework;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace CrystalCollector.Tests
{
    public sealed class RuntimeFlowRegressionTests
    {
        [Test]
        public void ShowingPanelRestoresCanvasGroupInteractionBeforeAnimation()
        {
            var panel = new GameObject("Panel", typeof(RectTransform));
            var group = panel.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var routine = InvokeAnimator(group, panel.GetComponent<RectTransform>(), true);
            routine.MoveNext();

            Assert.That(panel.activeSelf, Is.True);
            Assert.That(group.interactable, Is.True);
            Assert.That(group.blocksRaycasts, Is.True);
            UnityEngine.Object.DestroyImmediate(panel);
        }

        [Test]
        public void HidingPanelReleasesCanvasGroupInteractionBeforeAnimation()
        {
            var panel = new GameObject("Panel", typeof(RectTransform));
            var group = panel.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            var routine = InvokeAnimator(group, panel.GetComponent<RectTransform>(), false);
            routine.MoveNext();

            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
            UnityEngine.Object.DestroyImmediate(panel);
        }

        private static IEnumerator InvokeAnimator(CanvasGroup group, RectTransform target, bool visible)
        {
            var type = Type.GetType("CrystalCollector.UIAnimator, Assembly-CSharp");
            var method = type.GetMethod("FadeAndScale", BindingFlags.Public | BindingFlags.Static);
            return (IEnumerator)method.Invoke(null, new object[] { group, target, visible, 0f });
        }
    }
}
