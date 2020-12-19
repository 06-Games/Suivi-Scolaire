using System;
using System.Collections;
using System.Collections.Generic;

namespace Integrations.Providers
{
    public class Local : Provider
    {
        public string Name => "Local";

        public IEnumerator GetInfos(Data.Data data, Action<Data.Data> onComplete)
        {
            data.Children = new[] { new Data.Child { id = "0", name = "Local profile" } };
            onComplete?.Invoke(data);
            yield break;
        }
    }
}
