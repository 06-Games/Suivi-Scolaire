using Home;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public class Local : Provider, Home
    {
        public string Name => "Local";
        public IEnumerator GetHolidays(Action<List<Holiday>> onComplete) { onComplete?.Invoke(new List<Holiday>()); yield break; }
    }
}
