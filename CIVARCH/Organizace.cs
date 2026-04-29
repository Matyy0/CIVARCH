namespace CIVARCH
{
    /// <summary>
    /// Trida obsahuje informace o organizaci
    /// </summary>
    public class Organizace
    {
        private string cisOrp = "";
        private string name = "";
        private string sidloNazev = "";
        private string sidloUlice = "";
        private string sidloPSC = "";
        private string sidloObec = "";
        private string odbor = "";

        public string CisOrp { get => cisOrp; set => cisOrp = value; }
        public string Name { get => name; set => name = value; }
        public string SidloNazev { get => sidloNazev; set => sidloNazev = value; }
        public string SidloUlice { get => sidloUlice; set => sidloUlice = value; }
        public string SidloPSC { get => sidloPSC; set => sidloPSC = value; }
        public string SidloObec { get => sidloObec; set => sidloObec = value; }
        public string Odbor { get => odbor; set => odbor = value; }

        public Organizace(string _cisOrp, string _name, string _sidloNazev, string _sidloUlice, string _sidloPsc, string _sidloObec, string _odbor)
        {
            CisOrp = _cisOrp;
            Name = _name;
            SidloNazev = _sidloNazev;
            SidloUlice = _sidloUlice;
            SidloPSC = _sidloPsc;
            SidloObec = _sidloObec;
            Odbor = _odbor;
        }

        public override string ToString()
        {
            return SidloNazev + " - " + Odbor + ", " + SidloUlice + ", " + SidloObec + ", " + SidloPSC;
        }
    }
}
