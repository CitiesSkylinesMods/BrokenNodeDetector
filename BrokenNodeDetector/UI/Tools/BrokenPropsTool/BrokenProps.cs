#if BROKEN_PROPS_SCANNER
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.BrokenPropsTool {
    public class BrokenProps : Detector {
        public override string Name => "Find broken props (experimental)";
        public override string Tooltip => "Detects incorrectly removed props\n" +
                                          "They are usually located below the center of the map\n" +
                                          "High number of broken items may significantly reduce FPS";

        private int _brokenGrid;
        private int _removedProps;
        private int _brokenGrid2;
        private volatile float _progress;
        
        public override IEnumerable<float> Process() {
            IsProcessing = true;
            ProgressMessage = "Testing prop grid...";
            CustomYieldInstruction = new WaitForSeconds(0.2f);
            _progress = 0f;
            AsyncTask asyncTask = SimulationManager.instance.AddAction(TestGrid());
            while (!asyncTask.completed) {
                yield return _progress;
            }
            ProgressMessage = $"Grid testing done! Found {_brokenGrid} props";
            CustomYieldInstruction = new WaitForSeconds(1f);
            yield return 1.0f;
            CustomYieldInstruction = new WaitForSeconds(0.1f);
            yield return 0.0f;
            _progress = 0f;
            AsyncTask asyncTask2 = SimulationManager.instance.AddAction(RemoveBrokenProps());
            while (!asyncTask2.completed) {              
                yield return _progress;
            }
            ProgressMessage = $"Removing finished! Removed {_removedProps} props";
            CustomYieldInstruction = new WaitForSeconds(1f);
            yield return 1.0f;
            ProgressMessage = $"Preparing Grid fixing process...";
            CustomYieldInstruction = new WaitForSeconds(0.2f);
            yield return 0.0f;
            _progress = 0f;
            AsyncTask asyncTask3 = SimulationManager.instance.AddAction(FixPropGrid());
            while (!asyncTask3.completed) {
                yield return _progress;
            }
            ProgressMessage = $"Grid fixing done! Found {_brokenGrid2} props";
            CustomYieldInstruction = new WaitForSeconds(1f);
            yield return 1.0f;
            ProgressMessage = $"Is prop collection valid?: {PropManager.instance.m_props.ItemCount() - 1 == PropManager.instance.m_propCount}";
            CustomYieldInstruction = new WaitForSeconds(1f);
            yield return 1.0f;
            ResultMessage = $"Incorrectly removed grid props: {_brokenGrid}\n" +
                            $"{(_removedProps >= 0 ? $"Released: {_removedProps} broken props!" :"")}\n" +
                            $"Is prop collection valid?: {PropManager.instance.m_props.ItemCount() - 1 == PropManager.instance.m_propCount}";
            IsProcessing = false;
        }

        private IEnumerator TestGrid() {
            float searchStep = 1.0f / PropManager.instance.m_propGrid.Length;
            _brokenGrid = 0; 
            Debug.Log("[BND] Testing Prop Grid...");
            for (var i = 0; i < PropManager.instance.m_propGrid.Length; i++) {
                ushort propId = PropManager.instance.m_propGrid[i];
                if (propId != 0 &&
                    PropManager.instance.m_props.m_buffer[propId].m_flags != 0 &&
                    ((PropInstance.Flags)PropManager.instance.m_props.m_buffer[propId].m_flags & PropInstance.Flags.Deleted) == 0 &&
                    ((PropInstance.Flags)PropManager.instance.m_props.m_buffer[propId].m_flags & PropInstance.Flags.Created) == 0) {
                    Debug.Log($"[BND] Found Invalid prop at gridIdx: {i}, id: {propId}, propName: [{PropManager.instance.m_props.m_buffer[propId].Info?.name}]");
                    _brokenGrid++;
                }
                
                float searchProgress = searchStep * i;
                if (i % 32 == 0) {
                    ProgressMessage = $"Testing Prop Grid...{searchProgress * 100:F0}% | Found {_brokenGrid}";
                    Thread.Sleep(1);
                    _progress = searchProgress;
                }
            }
            Debug.Log($"[BND] Grid testing done! Found {_brokenGrid} props");
            _progress = 1.0f;

            yield return null;
        }

        private IEnumerator RemoveBrokenProps() {
            int counter = 0;
            int counter2 = 0;
            _removedProps = 0;
            Array16<PropInstance> props = PropManager.instance.m_props;
            float searchStep = 1.0f / props.m_size;
            for (var i = 0; i < props.m_size; i++) {
                PropInstance p = props.m_buffer[i];
                if (p.m_flags != 0 && ((PropInstance.Flags)p.m_flags & PropInstance.Flags.Created) == 0 && p.m_nextGridProp != 0 && Vector3.zero == p.Position) {
                    _removedProps++;
                    counter++;
                    PropManager.instance.ReleaseProp((ushort)i);
                    if (counter % 256 == 0) {
                        Debug.Log($"[BND] Released {++counter2 * 256} props");
                    }
                }
                
                float searchProgress = searchStep * i;
                if (i % 32 == 0) {
                    ProgressMessage = $"Removing props...{searchProgress * 100:F0}% | Found & removed: {_removedProps}";
                    Thread.Sleep(1);
                    _progress = searchProgress;
                }
            }

            Debug.Log($"[BND] Potentially broken props: {counter}");
            _progress = 1.0f;
 
            yield return null;
        }
        
        private IEnumerator FixPropGrid() {
            float searchStep = 1.0f / PropManager.instance.m_propGrid.Length;
            _brokenGrid2 = 0;
            Debug.Log("[BND] Testing and fixing Prop Grid");
            for (var i = 0; i < PropManager.instance.m_propGrid.Length; i++) {
                ushort propId = PropManager.instance.m_propGrid[i];
                if (propId != 0 && 
                    PropManager.instance.m_props.m_buffer[propId].m_flags != 0 &&
                    ((PropInstance.Flags)PropManager.instance.m_props.m_buffer[propId].m_flags & PropInstance.Flags.Deleted) == 0 &&
                    ((PropInstance.Flags)PropManager.instance.m_props.m_buffer[propId].m_flags & PropInstance.Flags.Created) == 0) {
                    PropManager.instance.m_props.m_buffer[propId].FixedHeight = true;
                    PropManager.instance.ReleaseProp(propId);
                    Debug.Log($"[BND] Released prop {propId}");
                    _brokenGrid2++;
                }
                
                float searchProgress = searchStep * i;
                if (i % 32 == 0) {
                    ProgressMessage = $"Testing and fixing Prop Grid...{searchProgress * 100:F0}% | Fixed: {_brokenGrid2}";
                    Thread.Sleep(1);
                    _progress = searchProgress;
                }
            }
            
            Debug.Log($"[BND] Grid testing&fixing done! Found {_brokenGrid2} props");
            _progress = 1.0f;

            yield return null;
        }
    }
}
#endif