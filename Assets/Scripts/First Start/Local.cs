using Marks;
using System.Collections;
using System.Collections.Generic;

namespace Integrations
{
    public class Local : ModelClass, Model
    {
        public string Name => "Local";
        public bool NeedAuth => false;

        public IEnumerator Connect(Account account, bool save) { FirstStart.OnComplete?.Invoke(new List<Period>(), new List<Subject>(), new List<Mark>()); yield break; }
    }
}
