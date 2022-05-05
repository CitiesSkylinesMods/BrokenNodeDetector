using System.Collections.Generic;

namespace BrokenNodeDetector.UI.Tools.BrokenPropsTool {
    public class BrokenProps : Detector {
        private float _processingProgress;
        public override string Name => "Find broken props (experimental)";
        public override string Tooltip => "Detects incorrectly removed props\n" +
                                          "They are usually located below the center of the map\n" +
                                          "High number of broken items may significantly reduce FPS";

        public override IEnumerable<float> Process() {
            /*TODO IMPLEMENT ACTUAL DETECTOR*/
            IsProcessing = true;
            ProgressMessage = "Doing things...";
            _processingProgress = 0.0f;
            yield return _processingProgress;
            _processingProgress = 0.20f;
            yield return _processingProgress;
            _processingProgress = 0.40f;
            yield return _processingProgress;
            _processingProgress = 0.60f;
            yield return _processingProgress;
            _processingProgress = 0.80f;
            yield return _processingProgress;
            ProgressMessage = "Done!";
            _processingProgress = 1.0f;
            yield return _processingProgress;
            IsProcessing = false;
        }
    }
}