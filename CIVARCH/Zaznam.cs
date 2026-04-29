namespace CIVARCH
{
    /// <summary>
    /// Trida uchovava informace o rizeni, o obcanovi a dalsi informace navic. Vsechny tyto informace jsou vypisovany v detailnim zobrazeni zaznamu na webu
    /// </summary>
    public class Zaznam
    {
        private string organizace = "";
        private string zmenaAutor = "";
        private string zmenaDatum = "";

        private Obcan? obcan = null;
        private Rizeni? rizeni = null;

        public string Organizace { get => organizace; set => organizace = value; }
        public string ZmenaAutor { get => zmenaAutor; set => zmenaAutor = value; }
        public string ZmenaDatum { get => zmenaDatum; set => zmenaDatum = value; }

        public Obcan Obcan { get => obcan ?? new(); set => obcan = value; }
        public Rizeni Rizeni { get => rizeni ?? new(); set => rizeni = value; }

        public Zaznam() : this(new Obcan(), new Rizeni(), "", "") { }

        public Zaznam(Obcan _obcan, Rizeni _rizeni, string _organizace, string _zmenaAutor, string _zmenaDatum = "")
        {
            Obcan = _obcan;
            Rizeni = _rizeni;
            Organizace = _organizace;
            ZmenaAutor = _zmenaAutor;

            Obcan ??= new();
            Rizeni ??= new();

            if (_zmenaDatum.Split(' ').Length == 2)
            {
                ZmenaDatum = _zmenaDatum.Split(' ')[0];
            }
            else
            {
                ZmenaDatum = _zmenaDatum;
            }
        }
    }
}
