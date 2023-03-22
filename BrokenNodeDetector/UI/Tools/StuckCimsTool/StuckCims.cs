using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.StuckCimsTool {
    public class StuckCims : Detector {
        private const string RESULT_ROW = "ResultRow";
        private const string JUMP_TO = "JumpTo";
        private const string ITEM_LABEL = "ItemLabel";
        public override string Name => "Detect stuck citizens (experimental)";

        public override string Tooltip => "Detects for stuck citizens. Shows where they stuck, suggests possible solution.\n" +
                                          "Useful for searching the source of issue with slow simulation or constantly\n" +
                                          "raising path-find counter when TM:PE mod is enabled";

        public override bool UsePrepareStep => true;

        private static UIPanel _rowTemplate;
        private Dictionary<uint, int> _results;
        private int _resultCount;

        public StuckCims() {
            BuildPrepareTemplate();
            BuildResultTemplate();
            BuildRowTemplate();
        }

        public override IEnumerable<bool> Prepare() {
            ProgressMessage = "Searching for stuck citizens...\nDo not stop simulation";
            yield return true;
        }

        public override IEnumerable<float> Process() {

            IsProcessing = true;
            CustomYieldInstruction = new WaitForSeconds(0.25f);
            ProgressMessage = "Searching for stuck citizens...\nDo not pause the simulation";
            Singleton<SimulationManager>.instance.AddAction(() => Singleton<SimulationManager>.instance.SelectedSimulationSpeed = 1);
            float time = 50f;
            float step = 1 / time;
            ModService.Instance.StartCimPathFailedDetector();
            for (int i = 0; i < time; i++) {
                ProgressMessage = $"Searching for stuck citizen(s)...{i * step * 100:F0}%";
                yield return i / time;
            }

            ModService.Instance.StopCimPathFailedDetector();
            ProgressMessage = "Processing results...";
            CustomYieldInstruction = new WaitForSeconds(0.5f);
            yield return 1.0f;
            _results = ModService.Instance.ResultsPfPerInstance;
            _resultCount = ModService.Instance.ResultsPfPerInstanceCount; 
            if (_resultCount > 0) {
                ProgressMessage = $"Done. Found {_resultCount} stuck citizen(s)";
                Debug.Log($"[BND] Stuck citizen results [ CitizenID: number of invalidated paths ]\n{string.Join("\n", _results.Select(p => $"[{p.Key}: {p.Value}]").ToArray())}");
            } else {
                ProgressMessage = $"Done. Did not detect any stuck citizens";
            }

            yield return 1.0f;
            IsProcessing = false;
        }

        public override void InitPrepareView(UIComponent component) { }

        public override void InitResultsView(UIComponent component) {
            if (component is UIPanel panel) {
                UILabel label = panel.Find<UILabel>("Label");
                label.processMarkup = true;
                if (_resultCount > 0) {
                    label.text = $"Found <color #E59000>{_resultCount}</color> stuck citizen(s)\n\n" +
                                 "Only <color #FFEB04>15</color> most failing will be displayed below\n" +
                                 "1. Click on 'Jump to' to move to citizen location\n" +
                                 "2. If it stuck at junction:\n" +
                                 "   - change Node Controller settings\n" +
                                 "   - when near elevated or underground station\n" +
                                 "     try moving or removing the stop\n" +
                                 "3. If bike or pedestrian path:\n" +
                                 "   - move it or rebuilt\n";
                    component.height = label.height + GenerateResultRows(component, label.height);
                } else {
                    component.height = 40;
                    label.textScale = 1.2f;
                    label.text = "No stuck citizens found";
                    Debug.Log("[BND] Detect stuck sitizens: Nothing found.");
                }
            }
        }

        private float GenerateResultRows(UIComponent component, float marginTop) {
            float result = 0;
            foreach (KeyValuePair<uint,int> pair in _results) {
                
                uint citizenInstanceId = pair.Key;
                UIPanel panel = Object.Instantiate(_rowTemplate);
                component.AttachUIComponent(panel.gameObject);
                AttachCallbacks(panel, citizenInstanceId);
                UILabel label = panel.Find<UILabel>(ITEM_LABEL);
                label.prefix = "Stuck Citizen: ";
                label.text = citizenInstanceId.ToString();
                label.suffix = $" failed {pair.Value}x";
                panel.relativePosition = new Vector3(0, result + marginTop);
                result += panel.height;
            }

            return result;
        }

        private void AttachCallbacks(UIComponent templateInstance, uint citizenInstance) {
            UIButton button = templateInstance.Find<UIButton>(JUMP_TO);
            button.eventClick += JumpToCitizenClicked;
            button.objectUserData = citizenInstance;
        }

        private void BuildResultTemplate() {
            _template = new GameObject("StuckCimsTemplate").AddComponent<UIPanel>();
            _template.transform.SetParent(_defaultGameObject.transform, true);
            _template.width = 400;
            _template.height = 50;
            UILabel label = _template.AddUIComponent<UILabel>();
            label.autoSize = true;
            label.padding = new RectOffset(15, 10, 15, 15);
            label.relativePosition = new Vector3(0, 0);
            label.name = "Label";
            UIPanel resultRow = _template.AddUIComponent<UIPanel>();
            resultRow.name = RESULT_ROW;
            resultRow.width = 400;
            resultRow.height = 50;
            resultRow.relativePosition = new Vector3(0, 30f);
            UILabel itemLabel = resultRow.AddUIComponent<UILabel>();
            itemLabel.autoSize = true;
            itemLabel.padding = new RectOffset(10, 10, 5, 5);
            itemLabel.relativePosition = new Vector3(0, 0);
            itemLabel.name = ITEM_LABEL;
            UIButton moveNextButton = UIHelpers.CreateButton(
                resultRow,
                "Jump to",
                new Rect(new Vector2(400 - 100, 10),
                    new Vector2(90, 30)),
                JumpToCitizenClicked);
            moveNextButton.name = JUMP_TO;
            moveNextButton.Hide();
        }

        private void BuildPrepareTemplate() {
            _templatePrepare = new GameObject("StuckCimsPrepareTemplate").AddComponent<UIPanel>();
            _templatePrepare.gameObject.transform.SetParent(_defaultGameObject.transform, true);
            _templatePrepare.width = 400;
            _templatePrepare.height = 50;
            UILabel label = _templatePrepare.AddUIComponent<UILabel>();
            label.textScale = 1.1f;
            label.autoSize = true;
            label.padding = new RectOffset(15, 10, 15, 15);
            label.relativePosition = new Vector3(0, 0);
            label.text = "Preparing...";
            label.name = "Label";
        }

        private void BuildRowTemplate() {
            UIPanel resultRow = new GameObject("StuckCimsRowTemplate").AddComponent<UIPanel>();
            resultRow.gameObject.transform.SetParent(_defaultGameObject.transform, true);
            resultRow.name = RESULT_ROW;
            resultRow.width = 400;
            resultRow.height = 40;
            UILabel itemLabel = resultRow.AddUIComponent<UILabel>();
            itemLabel.autoSize = true;
            itemLabel.padding = new RectOffset(10, 10, 5, 5);
            itemLabel.relativePosition = new Vector3(0, 0);
            itemLabel.name = ITEM_LABEL;
            UIButton moveNextButton = UIHelpers.CreateButton(
                resultRow,
                "Jump to",
                new Rect(new Vector2(400 - 100, 5),
                    new Vector2(90, 30)),
                JumpToCitizenClicked);
            moveNextButton.name = JUMP_TO;
            _rowTemplate = resultRow;
        }

        private void JumpToCitizenClicked(UIComponent component, UIMouseEventParameter eventparam) {
            uint citizenId = (uint)component.objectUserData;
            InstanceID instanceId = default;
            instanceId.Citizen = citizenId;
            if (InstanceManager.IsValid(instanceId)) {
                bool unlimitedCamera = ToolsModifierControl.cameraController.m_unlimitedCamera;
                ToolsModifierControl.cameraController.m_unlimitedCamera = true;
                ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
                BndResultHighlightManager.instance.Highlight(new HighlightData {
                    CitizenInstanceID = CitizenManager.instance.m_citizens.m_buffer[citizenId].m_instance, 
                    Type = HighlightType.Citizen
                });
                ToolsModifierControl.cameraController.m_unlimitedCamera = unlimitedCamera;
            }
        }

        public override void Dispose() {
            ModService.Instance.StopSelection();
            ModService.Instance.StopCimPathFailedDetector();
            if (_rowTemplate) {
                Object.Destroy(_rowTemplate.gameObject);
                _rowTemplate = null;
            }

            base.Dispose();
        }
    }
}