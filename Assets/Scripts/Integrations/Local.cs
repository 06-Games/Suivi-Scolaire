using System.Collections;

namespace Integrations
{
    public class Local : ModelClass, Model
    {
        public string Name => "Local";
        public bool NeedAuth => false;

        public IEnumerator Connect(Account account, bool save) { OnComplete?.Invoke(new Note[0]); yield break; }
    }
}
