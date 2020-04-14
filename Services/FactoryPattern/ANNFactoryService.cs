using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNProductSync.Services.FactoryPattern
{
    public class ANNFactoryService
    {
        private static Dictionary<string, IANNService> _instance = new Dictionary<string, IANNService>();

        public static T getInstance<T>() where T : new()
        {
            IANNService instance;
            bool exist = _instance.TryGetValue(typeof(T).FullName, out instance);

            if (!exist)
            {
                instance = (IANNService)new T();
                _instance.TryAdd(typeof(T).FullName, instance);
            }

            return (T)instance;
        }
    }
}
