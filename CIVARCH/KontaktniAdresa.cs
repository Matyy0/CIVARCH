namespace CIVARCH
{
    /// <summary>
    /// Trida obsahuje informace o kontaktni adrese
    /// </summary>
    public class KontaktniAdresa
    {
        private string? ulice;
        private string? cisloPopisne;
        private string? obec;
        private string? psc;

        public string? Ulice { get => ulice; set => ulice = value; }
        public string? CisloPopisne { get => cisloPopisne; set => cisloPopisne = value; }
        public string? Obec { get => obec; set => obec = value; }
        public string? Psc { get => psc; set => psc = value; }

        public KontaktniAdresa(string? _ulice, string? _cisloPopisne, string? _obec, string? _psc)
        {
            Ulice = _ulice;
            CisloPopisne = _cisloPopisne;
            Obec = _obec;
            Psc = _psc;
        }

        public override string ToString()
        {
            return Ulice + " " + CisloPopisne + ", " + Obec + ", " + Psc;
        }
    }
}
