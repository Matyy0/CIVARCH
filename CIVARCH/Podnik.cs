namespace CIVARCH
{
    /// <summary>
    /// Trida obsahuje informace o podniku
    /// </summary>
    public class Podnik
    {
        private string id = "";
        private string nazev1 = "";
        private string nazev2 = "";
        private string adresaUlice = "";
        private string adresaObec = "";
        private string adresaPSC = "";
        private string predmetCinn = "";
        private string okrOrp = "";
        private string cisOrp = "";


        public string Id { get => id; set => id = value; }
        public string Nazev1 { get => nazev1; set => nazev1 = value; }
        public string Nazev2 { get => nazev2; set => nazev2 = value; }
        public string AdresaUlice { get => adresaUlice; set => adresaUlice = value; }
        public string AdresaObec { get => adresaObec; set => adresaObec = value; }
        public string AdresaPSC { get => adresaPSC; set => adresaPSC = value; }
        public string PredmetCinn { get => predmetCinn; set => predmetCinn = value; }
        public string OkrOrp { get => okrOrp; set => okrOrp = value; }
        public string CisOrp { get => cisOrp; set => cisOrp = value; }

        public Podnik(string _id, string _nazev1, string _nazev2, string _adresaUlice, string _adresaObec, string _adresaPSC, string _predmetCinn, string _okrOrp, string _cisOrp)
        {
            Id = _id;
            Nazev1 = _nazev1;
            Nazev2 = _nazev2;
            AdresaUlice = _adresaUlice;
            AdresaObec = _adresaObec;
            AdresaPSC = _adresaPSC;
            PredmetCinn = _predmetCinn;
            OkrOrp = _okrOrp;
            CisOrp = _cisOrp;
        }
    }
}
