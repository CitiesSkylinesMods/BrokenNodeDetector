using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools {
    public class BndColorAnimator : Singleton<BndColorAnimator> {
        private readonly List<AnimationInfo> _animatorInfos = new List<AnimationInfo>();

        public static void Animate(Action<Color> target, AnimatedColor color) {
            instance.AnimateInternal(null, target, color, null);
        }

        public static void Animate(string name,
                                   Action<Color> target,
                                   AnimatedColor color) {
            instance.AnimateInternal(name, target, color, null);
        }

        public static void Animate(string name,
                                   Action<Color> target,
                                   AnimatedColor color,
                                   Action completed) {
            instance.AnimateInternal(name, target, color, completed);
        }

        public static void Cancel(string name) {
            instance.CancelInternal(name);
        }

        public static bool IsAnimating(string name) {
            return instance.InternalIsAnimating(name);
        }

        private bool InternalIsAnimating(string animationName) {
            AnimationInfo animationInfo =
                this._animatorInfos.Find(info => info.AnimationName == animationName);
            return animationInfo != null;
        }

        private void AnimateInternal(string animationName,
                                     Action<Color> target,
                                     AnimatedColor v,
                                     Action completed) {
            AnimationInfo animationInfo = _animatorInfos.Find(info => info.AnimationName == animationName);
            if (animationName != null && animationInfo != null) {
                animationInfo.Value = v;
                animationInfo.Completed = completed;
                animationInfo.Target(v);
                animationInfo.Target = target;
                return;
            }

            target(v);
            this._animatorInfos.Add(
                new AnimationInfo {
                    AnimationName = animationName,
                    Target = target,
                    Value = v,
                    Completed = completed
                });
        }

        private void CancelInternal(string animationName) {
            for (int i = this._animatorInfos.Count - 1; i >= 0; i--) {
                if (this._animatorInfos[i].AnimationName == animationName) {
                    this._animatorInfos.RemoveAt(i);
                    return;
                }
            }
        }

        private void Update() {
            AnimationInfo info;
            for (int i = this._animatorInfos.Count - 1; i >= 0; i--) {
                info = this._animatorInfos[i];
                try {
                    info.Target(info.Value);
                    if (info.Value.isDone) {
                        info.Target(info.Value.endValue);
                        info.Completed?.Invoke();

                        this._animatorInfos.RemoveAt(i);
                    }
                } catch (Exception ex) {
                    Debug.LogError($"BndColorAnimator {info.AnimationName} {ex.GetType()} {ex.Message}\n{ex.StackTrace}");
                    try {
                        info.Completed?.Invoke();
                    } catch (Exception ex2) {
                        Debug.LogError($"BndColorAnimator {info.AnimationName} {ex2.GetType()} {ex2.Message}\n{ex2.StackTrace}");
                    } finally {
                        _animatorInfos.RemoveAt(i);
                    }
                }
            }
        }

        private void OnDestroy() {
            GetType().GetField("sInstance", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, null);
        }

        private class AnimationInfo {
            public string AnimationName;

            public Action<Color> Target;

            public AnimatedColor Value;

            public Action Completed;
        }
    }
}