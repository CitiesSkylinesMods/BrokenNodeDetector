using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.BrokenPathTool {
    public class BrokenPaths : Detector {
        private const string RESULT_ROW = "ResultRow";
        private const string JUMP_TO = "JumpTo";
        private const string ITEM_LABEL = "ItemLabel";
        private const string SELECTION_PULSE_ANIM = "SelectionPulseAnim";
        private const string SELECTION_PULSE_ANIM_INV = "SelectionPulseAnimInv";
        public override string Name => "Detect broken paths (experimental)";

        public override string Tooltip => "Detects broken paths outgoing from selected building\n" +
                                          "Shows where vehicle wanted to go and despawned because of broken path\n" +
                                          "Useful for searching the source of issue with spawning service vehicles";

        public override bool UsePrepareStep => true;

        private static UIPanel _rowTemplate;
        private HighlightData _highlightData = new HighlightData() { BuildingID = 0, Type = HighlightType.Building, AnimatedColor = true, CurrentColor = Color.green};
        private List<ushort> _results;
        private ushort _selectedBuildingId;

        public BrokenPaths() {
            BuildPrepareTemplate();
            BuildResultTemplate();
            BuildRowTemplate();
        }

        public override IEnumerable<bool> Prepare() {
            CustomYieldInstruction = new WaitForSeconds(0.5f);
            ProgressMessage = "Click at a building to select";
            _selectedBuildingId = 0;
            _highlightData.BuildingID = 0;
            ModService.Instance.StartSelection(_highlightData, HighlightType.Building);
            while (ModService.Instance.SelectedBuilding == 0) {
                yield return true;
            }

            ModService.Instance.StopSelection();
            _selectedBuildingId = ModService.Instance.SelectedBuilding;
            if (_selectedBuildingId == 0) {
                yield break;
            }

            _highlightData.BuildingID = _selectedBuildingId;
            ProgressMessage = $"Selected building {_selectedBuildingId}.\nPreparing listener...";
            BndResultHighlightManager.instance.Highlight(_highlightData);
            yield return true;
        }

        public override IEnumerable<float> Process() {
            if (_selectedBuildingId == 0) {
                yield break;
            }

            IsProcessing = true;
            CustomYieldInstruction = new WaitForSeconds(0.25f);
            AnimateSelection(true);
            ProgressMessage = "Listening for failed path(s)...\nDo not stop simulation";
            Singleton<SimulationManager>.instance.AddAction(() => Singleton<SimulationManager>.instance.SelectedSimulationSpeed = 1);
            float time = 50f;
            float step = 1 / time;
            ModService.Instance.StartPathFailedDetector();
            for (int i = 0; i < time; i++) {
                ProgressMessage = $"Listening for failed path(s)...{i * step * 100:F0}%";
                yield return i / time;
            }

            ModService.Instance.StopPathFailedDetector();
            ProgressMessage = "Processing results...";
            CustomYieldInstruction = new WaitForSeconds(0.5f);
            yield return 1.0f;
            _results = ModService.Instance.ResultsPf;
            if (_results.Count > 0) {
                ProgressMessage = $"Done. Found {_results.Count} broken path(s)";
            } else {
                ProgressMessage = $"Done. Did not detect any broken path";
            }

            yield return 1.0f;
            BndColorAnimator.Cancel(SELECTION_PULSE_ANIM);
            BndColorAnimator.Cancel(SELECTION_PULSE_ANIM_INV);
            IsProcessing = false;
        }

        private void AnimateSelection(bool forward) {
            Color startColor = Color.yellow;
            Color endColor = Color.red;

            BndColorAnimator.Animate(
                forward? SELECTION_PULSE_ANIM : SELECTION_PULSE_ANIM_INV,
                (col) => { _highlightData.CurrentColor = col; },
                new AnimatedColor(forward ? startColor : endColor, forward ? endColor : startColor, 1f),
                () => { AnimateSelection(!forward); });
        }
        
        public override void InitPrepareView(UIComponent component) { }

        public override void InitResultsView(UIComponent component) {
            if (component is UIPanel panel) {
                UILabel label = panel.Find<UILabel>("Label");
                if (_results.Count > 0) {
                    label.text = $"Found {_results.Count} unreachable building(s)\n" +
                                 "1. Click on 'Jump' to show building location\n" +
                                 "2. Check intersections near the building:\n" +
                                 "- lane arrows,\n" +
                                 "- lane connections,\n" +
                                 "- vehicle restrictions\n" +
                                 "- make sure that building is connected to road.\n" +
                                 "3. Try Disconnected Buildings Detector.";
                    component.height = label.height + InitSourceBuildingResultRow(component, label.height, out float height1) + GenerateResultRows(component, height1);
                } else {
                    component.height = 40;
                    label.textScale = 1.2f;
                    label.text = "No broken paths found";
                    Debug.Log("[BND] Detect Broken Paths: Nothing found.");
                }
            }
        }

        private float GenerateResultRows(UIComponent component, float marginTop) {
            float result = 0;
            for (int i = 0; i < _results.Count; i++) {
                ushort buildingId = _results[i];
                UIPanel panel = Object.Instantiate(_rowTemplate);
                component.AttachUIComponent(panel.gameObject);
                AttachCallbacks(panel, buildingId);
                UILabel label = panel.Find<UILabel>(ITEM_LABEL);
                label.prefix = "Selected Building: ";
                label.text = buildingId.ToString();
                panel.relativePosition = new Vector3(0, result + marginTop);
                result += panel.height;
            }

            return result;
        }

        private float InitSourceBuildingResultRow(UIComponent component, float marginTop, out float absoluteHeight) {
            ushort buildingId = _selectedBuildingId;
            UIPanel panel = component.Find<UIPanel>(RESULT_ROW);
            panel.Show();
            AttachCallbacks(panel, buildingId);
            UILabel label = panel.Find<UILabel>(ITEM_LABEL);
            label.prefix = "In/Out Target: ";
            label.text = buildingId.ToString();
            panel.relativePosition = new Vector3(0, marginTop);
            UIButton button = panel.Find<UIButton>(JUMP_TO);
            button.Show();
            absoluteHeight = marginTop + panel.height;
            return panel.height;
        }

        private void AttachCallbacks(UIComponent templateInstance, ushort buildingId) {
            UIButton button = templateInstance.Find<UIButton>(JUMP_TO);
            button.eventClick += JumpToBuildingClicked;
            button.objectUserData = buildingId;
        }

        private void BuildResultTemplate() {
            _template = new GameObject("BrokenPathsTemplate").AddComponent<UIPanel>();
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
                JumpToBuildingClicked);
            moveNextButton.name = JUMP_TO;
            moveNextButton.Hide();
        }

        private void BuildPrepareTemplate() {
            _templatePrepare = new GameObject("BrokenPathsPrepareTemplate").AddComponent<UIPanel>();
            _templatePrepare.transform.SetParent(_defaultGameObject.transform, true);
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
            UIPanel resultRow = new GameObject("BrokenPathsRowTemplate").AddComponent<UIPanel>();
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
                JumpToBuildingClicked);
            moveNextButton.name = JUMP_TO;
            _rowTemplate = resultRow;
        }

        private void JumpToBuildingClicked(UIComponent component, UIMouseEventParameter eventparam) {
            ushort buildingId = (ushort)component.objectUserData;
            InstanceID instanceId = default;
            instanceId.Building = buildingId;
            if (InstanceManager.IsValid(instanceId)) {
                bool unlimitedCamera = ToolsModifierControl.cameraController.m_unlimitedCamera;
                ToolsModifierControl.cameraController.m_unlimitedCamera = true;
                ToolsModifierControl.cameraController.SetTarget(instanceId, ToolsModifierControl.cameraController.transform.position, true);
                BndResultHighlightManager.instance.Highlight(new HighlightData { BuildingID = buildingId, Type = HighlightType.Building });
                ToolsModifierControl.cameraController.m_unlimitedCamera = unlimitedCamera;
                ToolsModifierControl.cameraController.ClearTarget();
            }
        }

        public override void Dispose() {
            ModService.Instance.StopSelection();
            ModService.Instance.StopPathFailedDetector();
            _highlightData?.Reset(HighlightType.Building);
            _highlightData = null;
            if (_rowTemplate) {
                Object.Destroy(_rowTemplate.gameObject);
                _rowTemplate = null;
            }

            base.Dispose();
        }
    }
}