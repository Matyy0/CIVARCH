namespace CIVARCH
{
    /// <summary>
    /// Trida obcan definuje obcana a slouzi pouze k ukladani dat o jednotlivci.
    /// </summary>
    public class Obcan
    {
        private int id;

        private string rc = "";
        private string prijmeni = "";
        private string jmeno = "";
        private string titul = "";
        private string rodneJm = "";
        private string datumNarozeni = "";
        private string podnikNazev1 = "";
        private string podnikNazev2 = "";
        private string organizaceCislo = "";

        private KontaktniAdresa? adresa;

        public int Id { get => id; set => id = value; }

        public string Prijmeni { get => prijmeni; set => prijmeni = value; }
        public string Jmeno { get => jmeno; set => jmeno = value; }
        public string Titul { get => titul; set => titul = value; }
        public string RC { get => rc; set => rc = value; }
        public string RodneJm { get => rodneJm; set => rodneJm = value; }
        public string DatumNarozeni { get => datumNarozeni; set => datumNarozeni = value; }
        public string DatumNarozeniFormatovano { get; private set; }
        public string PodnikNazev1 { get => podnikNazev1; set => podnikNazev1 = value; }
        public string PodnikNazev2 { get => podnikNazev2; set => podnikNazev2 = value; }
        public string OrganizaceCislo { get => organizaceCislo; set => organizaceCislo = value; }

        public KontaktniAdresa? Adresa { get => adresa; set => adresa = value; }

        public Obcan() : this("", "", "", "1111111111", "", new("", "", "", "")) { }

        public Obcan(string _prijmeni, string _jmeno, string _titul, string _rc,
                     string _rodneJm, KontaktniAdresa _adresa, string _organizaceCislo = "", int _id = -1)
        {
            Id = _id;
            Prijmeni = _prijmeni;
            Jmeno = _jmeno;
            Titul = _titul;
            RC = _rc;
            Adresa = _adresa;
            RodneJm = _rodneJm;
            OrganizaceCislo = _organizaceCislo;

            Adresa ??= new("", "", "", "");

            // datum narozeni
            if(RC != null)
            {
                DatumNarozeni = DataHandler.ConvertRCToDatumNarozeni(RC);
                DatumNarozeniFormatovano = DataHandler.ConvertRCToDatumNarozeni(RC, "-", "ymd");
            }    
            
        }
        public int GetRokNarozeni(string rc)
        {
            // Rodné číslo má formát XXXXXX/XXXX, takže vybereme první část
            string rcBezLomitka = rc.Split('/')[0];

            // Poslední dvě číslice jsou rok narození
            int rokNarozeni = int.Parse(rcBezLomitka.Substring(0, 2));

            // Pokud je rok menší než 54, jedná se o rok 2000-2053, jinak o rok 1900-1999
            if (rokNarozeni < 54)
            {
                rokNarozeni += 2000;
            }
            else
            {
                rokNarozeni += 1900;
            }

            return rokNarozeni;
        }
    }
}
