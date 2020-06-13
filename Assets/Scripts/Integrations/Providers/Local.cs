using System;
using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public class Local : Provider
    {
        public string Name => "Local";

        public IEnumerator Connect(Account account, Action<Data.Data> onComplete, Action<string> onError)
        {
            var data = new Data.Data { Children = new[] { new Data.Child { id = "0", name = account.username = "Local profile" } } };
            onComplete?.Invoke(data);
            yield break;
        }
    }
}
