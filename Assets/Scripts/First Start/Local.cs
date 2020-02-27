using Marks;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public class Local : Provider
    {
        public string Name => "Local";
        public bool NeedAuth => false;
        
        public IEnumerator GetMarks(Action<List<Period>, List<Subject>, List<Mark>> onComplete) { onComplete?.Invoke(new List<Period>(), new List<Subject>(), new List<Mark>()); yield break; }
    }
}
