using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework.UI;
using JetBrains.Annotations;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools {
    public abstract class Detector : IDetector, IDisposable {
        internal static GameObject _defaultGameObject = new GameObject("BND_Detectors");
        protected static UIComponent _defaultTemplate;
        protected UIComponent _template;
        protected UIComponent _templatePrepare;

        public abstract string Name { get; }
        public abstract string Tooltip { get; }

        public virtual bool UsePrepareStep { get; protected set; }
        public virtual IEnumerable<bool> Prepare() {
            yield return true;
        }
        
        public abstract IEnumerable<float> Process();
        public bool IsProcessing { get; protected set; }
        public bool ShowResultsPanel { get; protected set; } = true;
        public string ProgressMessage { get; protected set; }
        public string ResultMessage { get; protected set; } = String.Empty;
        public UIComponent UITemplatePrepare => _templatePrepare;
        public UIComponent UITemplateResults => _template ? _template : _defaultTemplate;
        public YieldInstruction CustomYieldInstruction { get; protected set; } = null;

        public virtual void InitResultsView(UIComponent component) {
            if (component is UILabel label) {
                label.textScale = 1.2f;
                label.text = ResultMessage;
            }
        }

        public virtual void InitPrepareView(UIComponent component) {
            
        }

        protected void SetTaskProgress(AsyncTaskBase task, int steps) {
            typeof(AsyncTaskBase)
                .GetField("m_ProgressSteps", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(task, steps);
            typeof(AsyncTaskBase)
                .GetField("m_CachedStepCount", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(task, 1f / steps);
        }

        protected static void GenerateDefaultTemplate() {
            if (_defaultTemplate) return;

            UILabel label = new GameObject("Result Default UI").AddComponent<UILabel>();
            label.gameObject.transform.SetParent(_defaultGameObject.transform, true);
            label.gameObject.transform.SetParent(_defaultGameObject.transform, true);
            label.autoSize = true;
            label.padding = new RectOffset(10, 10, 15, 15);
            _defaultTemplate = label;
        }

        public virtual void Dispose() {
            if (_template) {
                UnityEngine.Object.Destroy(_template.gameObject);
                _template = null;
            }
            
            if (_templatePrepare) {
                UnityEngine.Object.Destroy(_templatePrepare.gameObject);
                _templatePrepare = null;
            }

            if (_defaultTemplate) {
                UnityEngine.Object.Destroy(_defaultTemplate.gameObject);
                _defaultTemplate = null;
            }
        }

        internal static void DisposeDefaultGameObject() {
            if (_defaultGameObject) {
                UnityEngine.Object.Destroy(_defaultGameObject);
                _defaultGameObject = null;
            }
        }
    }

    public interface IDetector {
        string Name { get; }
        string Tooltip { get; }
        bool UsePrepareStep { get; }
        IEnumerable<bool> Prepare();
        IEnumerable<float> Process();
        bool IsProcessing { get; }
        bool ShowResultsPanel { get; }
        string ProgressMessage { get; }
        string ResultMessage { get; }
        [CanBeNull] UIComponent UITemplateResults { get; }
        void InitResultsView(UIComponent component);
        [CanBeNull] UIComponent UITemplatePrepare { get; }
        void InitPrepareView(UIComponent component);

        [CanBeNull] YieldInstruction CustomYieldInstruction { get; }
    }

    public class DetectionResult {
        public string Text;
    }
}