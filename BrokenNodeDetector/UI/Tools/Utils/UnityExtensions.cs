using System;
using System.Collections;
using ColossalFramework.UI;
using JetBrains.Annotations;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.Utils
{
    public static class UnityExtensions
    {
        internal static Action<Exception> DefaultExceptionHandler;

        static UnityExtensions() {
            DefaultExceptionHandler = OnException;
        }

        public static Coroutine StartExceptionHandledIterator(this MonoBehaviour behaviour, IEnumerator routine, Action<Exception> onExceptionThrown) {
            return behaviour.StartCoroutine(ExceptionHandledIterator(routine, onExceptionThrown));
        }

        private static IEnumerator ExceptionHandledIterator(IEnumerator routine, Action<Exception> onExceptionThrown) {
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext())
                    {
                        break;
                    }
                    current = routine.Current;
                }
                catch (Exception e)
                {
                    onExceptionThrown(e);
                    yield break;
                }
                yield return current;
            }
        }

        private static void OnException([CanBeNull] Exception e) {
            if (e != null) {
                UIView.ForwardException(e);
            }
        }
    }
}
