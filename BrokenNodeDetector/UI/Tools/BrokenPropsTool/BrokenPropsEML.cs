using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using EManagersLib.API;
using HarmonyLib;
using UnityEngine;

namespace BrokenNodeDetector.UI.Tools.BrokenPropsTool {
    public class BrokenPropsEML : Detector {
        public override string Name => "Find broken props (EML - experimental)";
        public override string Tooltip => "Detects incorrectly removed props\n" +
                                          "They are usually located below the center of the map\n" +
                                          "High number of broken items may significantly reduce FPS";

        private int _brokenGrid;
        private int _removedProps;
        private int _brokenGrid2;
        private volatile float _progress;
        private uint[] _propGrid;
        private object _internalArray;

        private uint size;
        private MethodInfo _itemCountMethod;
        
        public override IEnumerable<float> Process() {
            IsProcessing = true;
            Type type = Type.GetType("EManagersLib.EPropManager");
            _propGrid = (uint[])AccessTools.Field(type, "m_propGrid").GetValue(null);
            _internalArray = AccessTools.Field(type, "m_props").GetValue(null);
            _itemCountMethod = _internalArray.GetType().GetMethod(nameof(Array32<EPropInstance>.ItemCount));
            size = (uint)AccessTools.Field(_internalArray.GetType(), "m_size").GetValue(_internalArray);

            if (_propGrid == null || _internalArray == null) {
                yield break;
            }
            
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
            ProgressMessage = $"Is prop collection valid?: {(uint)_itemCountMethod.Invoke(_internalArray, null) - 1 == PropWrapper.pmInstance.m_propCount}";
            CustomYieldInstruction = new WaitForSeconds(1f);
            yield return 1.0f;
            ResultMessage = $"Incorrectly removed grid props: {_brokenGrid}\n" +
                            $"{(_removedProps >= 0 ? $"Released: {_removedProps} broken props!" :"")}\n" +
                            $"Is prop collection valid?: {(uint)_itemCountMethod.Invoke(_internalArray, null) - 1 == PropWrapper.pmInstance.m_propCount}";
            IsProcessing = false;
        }

        private IEnumerator TestGrid() {
            float searchStep = 1.0f / _propGrid.Length;
            _brokenGrid = 0; 
            Debug.Log("[BND] Testing Prop Grid...");
            for (var i = 0; i < _propGrid.Length; i++) {
                uint propId = _propGrid[i];
                var flags = (PropInstance.Flags)PropAPI.Wrapper.GetFlags((uint)i);
                if (propId != 0 &&
                    flags != 0 &&
                    (flags & PropInstance.Flags.Deleted) == 0 &&
                    (flags & PropInstance.Flags.Created) == 0) {
                    Debug.Log($"[BND] Found Invalid prop at gridIdx: {i}, id: {propId}, propName: [{PropAPI.Wrapper.GetInfo(propId)?.name}]");
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
            
            float searchStep = 1.0f / size;
            for (var i = 0; i < size; i++) {
                PropInstance.Flags flags = (PropInstance.Flags)PropAPI.Wrapper.GetFlags((uint)i);
                if (flags != 0 &&
                    (flags & PropInstance.Flags.Created) == 0 &&
                    PropAPI.Wrapper.GetNextGridProp((uint)i) != 0 && 
                    Vector3.zero == PropAPI.Wrapper.GetPosition((uint)i)) {
                    _removedProps++;
                    counter++;
                    PropAPI.Wrapper.ReleaseProp((uint)i);
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
            float searchStep = 1.0f / _propGrid.Length;
            _brokenGrid2 = 0;
            Debug.Log("[BND] Testing and fixing Prop Grid");
            for (var i = 0; i < _propGrid.Length; i++) {
                uint propId = _propGrid[i];
                PropInstance.Flags flags = (PropInstance.Flags)PropAPI.Wrapper.GetFlags((uint)i);
                if (propId != 0 &&
                    flags != 0 &&
                    (flags & PropInstance.Flags.Deleted) == 0 &&
                    (flags & PropInstance.Flags.Created) == 0) {
                    PropAPI.Wrapper.SetFixedHeight(propId, true);
                    PropAPI.Wrapper.ReleaseProp(propId);
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