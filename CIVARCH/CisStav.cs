namespace CIVARCH
{
    /// <summary>
    /// Trida uchovava stavy rizeni
    /// </summary>
    public class CisStav
    {
        private string kod = "";
        private string name = "";

        public string Kod { get => kod; set => kod = value; }
        public string Name { get => name; set => name = value; }

        public CisStav(string _kod, string _name)
        {
            Kod = _kod;
            Name = _name;
        }
    }
}
