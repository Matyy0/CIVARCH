namespace CIVARCH
{
    /// <summary>
    /// Trida uchovava vsechny informace o rizeni
    /// </summary>
    public class Rizeni
    {
        private int id = -1;

        private string podnik = "";
        private string datNastupu1 = "";
        private string datNastupu2 = "";
        private string datum = "";
        private string datVystaveno = "";
        private string stav = "";

        private string datNastupu1Formatovano = "";
        private string datNastupu2Formatovano = "";
        private string datumFormatovano = "";
        private string datVystavenoFormatovano = "";

        public int Id { get => id; set => id = value; }
        public string DatNastupu1 { get => datNastupu1; set => datNastupu1 = value; }
        public string DatNastupu2 { get => datNastupu2; set => datNastupu2 = value; }
        public string Datum { get => datum; set => datum = value; }
        public string DatVystaveno { get => datVystaveno; set => datVystaveno = value; }
        public string Stav { get => stav; set => stav = value; }
        public string DatNastupu1Formatovano { get => datNastupu1Formatovano; set => datNastupu1Formatovano = value; }
        public string DatNastupu2Formatovano { get => datNastupu2Formatovano; set => datNastupu2Formatovano = value; }
        public string DatumFormatovano { get => datumFormatovano; set => datumFormatovano = value; }
        public string DatVystavenoFormatovano { get => datVystavenoFormatovano; set => datVystavenoFormatovano = value; }
        public string Podnik { get => podnik; set => podnik = value; }

        public Rizeni() : this("", "", "", "", "", "") { }

        public Rizeni(string _podnik, string _datNastupu1, string _datNastupu2, string _datum, string _datVystaveno, string _stav, int _id = 0)
        {
            Id = _id;
            Podnik = _podnik;
            Stav = _stav;

            if (_datNastupu1.Split(' ').Length == 2)
            {
                DatNastupu1 = _datNastupu1.Split(' ')[0];
            }
            else
            {
                DatNastupu1 = _datNastupu1;
            }

            if (_datNastupu2.Split(' ').Length == 2)
            {
                DatNastupu2 = _datNastupu2.Split(' ')[0];
            }
            else
            {
                DatNastupu2 = _datNastupu2;
            }

            if (_datum.Split(' ').Length == 2)
            {
                Datum = _datum.Split(' ')[0];
            }
            else
            {
                Datum = _datum;
            }

            if (_datVystaveno.Split(' ').Length == 2)
            {
                DatVystaveno = _datVystaveno.Split(' ')[0];
            }
            else
            {
                DatVystaveno = _datVystaveno;
            }

            DatNastupu1Formatovano = NaformatujDatum(DatNastupu1);
            DatNastupu2Formatovano = NaformatujDatum(DatNastupu2);
            DatumFormatovano = NaformatujDatum(Datum);
            DatVystavenoFormatovano = NaformatujDatum(DatVystaveno);
        }

        private static string NaformatujDatum(string datum)
        {
            string _formatovano = "";

            if (datum.Split('.').Length == 3)
            {
                _formatovano = datum.Split(".")[2] + "-" + datum.Split(".")[1] + "-" + datum.Split(".")[0];
            }

            return _formatovano;
        }
    }
}
