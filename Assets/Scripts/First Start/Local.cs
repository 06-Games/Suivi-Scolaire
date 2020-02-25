using System.Collections;

namespace Integrations
{
    public class Local : ModelClass, Model
    {
        public string Name => "Local";
        public bool NeedAuth => false;

        public IEnumerator Connect(Account account, bool save) { FirstStart.OnComplete?.Invoke(new Marks.Period[0], new Marks.Subject[0], new Marks.Mark[0]); yield break; }
    }
}
