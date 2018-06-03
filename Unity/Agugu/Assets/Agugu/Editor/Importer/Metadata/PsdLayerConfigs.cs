using System.Collections.Generic;

namespace Agugu.Editor
{
    public class PsdLayerConfigs
    {
        private readonly Dictionary<int, Dictionary<string, string>> _layerConfigs =
            new Dictionary<int, Dictionary<string, string>>();

        public void SetLayerConfig(int id, Dictionary<string, string> config)
        {
            _layerConfigs[id] = config;
        }

        public bool HasLayerConfig(int id)
        {
            return _layerConfigs.ContainsKey(id);
        }

        public Dictionary<string, string> GetLayerConfig(int id)
        {
            Dictionary<string, string> config;
            bool hasConfig = _layerConfigs.TryGetValue(id, out config);
            return hasConfig ? config : new Dictionary<string, string>();
        }
    }
}