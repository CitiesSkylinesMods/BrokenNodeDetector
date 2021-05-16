namespace BrokenNodeDetector.Highlighter {
    public interface IHighlightable {
        bool IsValid { get; }

        HighlightType Type { get; }

        void SetHighlightData(HighlightData data);

        void Render( RenderManager.CameraInfo cameraInfo);
    }
    
}