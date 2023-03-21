using System.Reflection;
using ColossalFramework;
using ColossalFramework.Plugins;

namespace BrokenNodeDetector.UI.Tools.Utils {
#if BROKEN_PROPS_SCANNER
    public class EmlUtils {
        internal static bool IsEmlInstalled() {
            foreach (PluginManager.PluginInfo pluginInfo in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                try {
                    foreach (Assembly assembly in pluginInfo.GetAssemblies()) {
                        bool flag = assembly.GetName().Name.ToLower().Equals("emanagerslib");
                        if (flag) {
                            return pluginInfo.isEnabled;
                        }
                    }
                } catch {
                    // ignore
                }
            }
            return false;
        }
    }
#endif
}