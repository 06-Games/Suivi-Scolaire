using Periods;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public class Local : Provider, Periods
    {
        public string Name => "Local";
        public IEnumerator GetPeriods(Action<List<Period>> onComplete) { onComplete?.Invoke(new List<Period>()); yield break; }
    }
}
