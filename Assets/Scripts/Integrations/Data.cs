namespace Integrations.Data
{
    public class Data
    {
        //Data file information
        public string Label;
        public string Provider;
        public System.DateTime CreationDate = System.DateTime.Now;
        public System.DateTime LastLogin = System.DateTime.Now;

        public Child[] Children;
    }
}
