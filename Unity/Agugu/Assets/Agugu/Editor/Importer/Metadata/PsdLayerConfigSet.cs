using System.Collections.Generic;

namespace Agugu.Editor
{
    public class PsdLayerConfigSet
    {
        private readonly Dictionary<int, PsdLayerConfig> _layerConfigList =
            new Dictionary<int, PsdLayerConfig>();

        public void SetLayerConfig(int id, PsdLayerConfig config)
        {
            _layerConfigList[id] = config;
        }

        public bool HasLayerConfig(int id)
        {
            return _layerConfigList.ContainsKey(id);
        }

        public PsdLayerConfig GetLayerConfig(int id)
        {
            PsdLayerConfig config;
            bool hasConfig = _layerConfigList.TryGetValue(id, out config);
            return hasConfig ? config : new PsdLayerConfig();
        }
    }
}