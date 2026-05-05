using System.Data;
using System.Globalization;
using System.Text;

namespace CIVARCH
{
    /// <summary>
    /// Trida DataHandler obsahuje data a metody, ktere pracuji s daty.
    /// </summary>
    public class DataHandler
    {
        /// <summary>
        /// Ze tri parametru / retezcu (titul, jmeno, prijmeni) vytvori jeden retezec.
        /// </summary>
        /// <param name="jmeno">Jmeno obcana</param>
        /// <param name="prijmeni">Prijmeni obcana</param>
        /// <param name="titul">Titul obcana</param>
        /// <returns>Retezec, ktery obsahuje vsechny tri informace o obcanovi</returns>
        public static string StripDiacritics(string text)
        {
            var decomposed = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string CreateFullName(string jmeno, string prijmeni, string? titul)
        {
            if (titul != null && titul != "")
            {
                return titul + " " + jmeno + " " + prijmeni;
            }
            else
            {
                return jmeno + " " + prijmeni;
            }
        }

        /// <summary>
        /// Metoda, ktera z rodneho cisla udela datum narozeni. Funguje pouze pro lidi narozene do roku 2000! Metoda nepredpoklada, ze data budou zadana spatne.
        /// </summary>
        /// <param name="RC">Rodne cislo obcana</param>
        /// <param name="delimeter">Oddelovac data (v CR se pouziva tecka '.', ktera je take nastavena jako default hodnota)</param>
        /// <param name="format">Urcuje format data za pomoci tri pismen 'dmy', kde 'd' je den, 'm' je mesic a 'y' je rok.</param>
        /// <returns>Vraci datum narozeni obcana v danem formatu</returns>
        public static string ConvertRCToDatumNarozeni(string RC, string delimeter = ".", string format = "dmy")
        {
            if(string.IsNullOrEmpty(RC))
            {
                return "";
            }
            if (RC.Length < 6)
            {
                Console.WriteLine($"Rodné číslo {RC}, je neplatné");
                return $"Neplatné rodné číslo!";
            }

            string rok = "19" + RC[0].ToString() + RC[1].ToString();
            string mesic = RC[2].ToString() + RC[3].ToString();
            string den = RC[4].ToString() + RC[5].ToString();

            // Ženská RC mají měsíc zvýšený o 50 (51–62 = leden–prosinec)
            if (int.TryParse(mesic, out int mesicInt) && mesicInt > 50)
            {
                mesic = (mesicInt - 50).ToString("D2");
            }

            switch (format)
            {
                case "dmy":
                    return $"{den}{delimeter}{mesic}{delimeter}{rok}";
                case "ymd":
                    return $"{rok}{delimeter}{mesic}{delimeter}{den}";
                case "mdy":
                    return $"{mesic}{delimeter}{den}{delimeter}{rok}";
                default:
                throw new ArgumentException("Neplatný formát data.");
            }
        }

        /// <summary>
        /// Metoda exportuje seznam zaznamu do datove tabulky. Vyuziva se tridy DataTable, pomoci ktere se data usporadaji do tabulky.
        /// </summary>
        /// <param name="z">Seznam zaznamu</param>
        /// <returns>Datova tabulka obsahujici data ze seznamu zaznamu</returns>
        public static DataTable ExportDataToDataTable(List<Zaznam> z)
        {
            DataTable _table = new();

            _table.Columns.Add("ID zaznamu");
            _table.Columns.Add("Prijmení");
            _table.Columns.Add("Jmeno");
            _table.Columns.Add("Titul");
            _table.Columns.Add("Rodne cislo");
            _table.Columns.Add("Datum narozeni");
            _table.Columns.Add("Adresa bydliste");

            _table.Columns.Add("Datum pocatku vykonu CS");
            _table.Columns.Add("Datum preruseni vykonu CS");
            _table.Columns.Add("Datum ukonceni vykonu CS");
            _table.Columns.Add("Organizace");

            _table.Columns.Add("Nazev podniku 1");
            _table.Columns.Add("Nazev podniku 2");

            _table.Columns.Add("Datum zmeny");
            _table.Columns.Add("Autor zmeny");

            foreach (Zaznam _z in z)
            {
                string adresa = "-";

                if (_z.Obcan.Adresa != null)
                {
                    adresa = _z.Obcan.Adresa.ToString();
                }

                _table.Rows.Add(
                                _z.Obcan.Id,
                                _z.Obcan.Prijmeni.Normalize(NormalizationForm.FormD),
                                _z.Obcan.Jmeno.Normalize(NormalizationForm.FormD),
                                _z.Obcan.Titul,
                                _z.Obcan.RC,
                                _z.Obcan.DatumNarozeni,
                                adresa.Normalize(NormalizationForm.FormD),
                                _z.Rizeni.DatNastupu1,
                                _z.Rizeni.DatNastupu2,
                                _z.Rizeni.DatNastupu1Formatovano,
                                _z.Organizace.Normalize(NormalizationForm.FormD),
                                _z.Obcan.PodnikNazev1.Normalize(NormalizationForm.FormD),
                                _z.Obcan.PodnikNazev2.Normalize(NormalizationForm.FormD),
                                _z.ZmenaDatum,
                                _z.ZmenaAutor.Normalize(NormalizationForm.FormD)
                                );
            }

            return _table;
        }

        /// <summary>
        /// Metoda exportuje zaznam do datove tabulky. Vyuziva se tridy DataTable, pomoci ktere se data usporadaji do tabulky.
        /// </summary>
        /// <param name="z">Zaznam</param>
        /// <returns>Datova tabulka obsahujici data zaznamu</returns>
        public static DataTable ExportDataToDataTable(Zaznam z)
        {
            DataTable _table = new();

            _table.Columns.Add("ID zaznamu");
            _table.Columns.Add("Prijmení");
            _table.Columns.Add("Jmeno");
            _table.Columns.Add("Titul");
            _table.Columns.Add("Rodne cislo");
            _table.Columns.Add("Datum narozeni");
            _table.Columns.Add("Adresa bydliste");

            _table.Columns.Add("Datum pocatku vykonu CS");
            _table.Columns.Add("Datum preruseni vykonu CS");
            _table.Columns.Add("Datum ukonceni vykonu CS");
            _table.Columns.Add("Organizace");

            _table.Columns.Add("Nazev podniku 1");
            _table.Columns.Add("Nazev podniku 2");

            _table.Columns.Add("Datum zmeny");
            _table.Columns.Add("Autor zmeny");

            string adresa = "-";

            if (z.Obcan.Adresa != null)
            {
                adresa = z.Obcan.Adresa.ToString();
            }


            _table.Rows.Add(
                            z.Obcan.Id,
                            z.Obcan.Prijmeni.Normalize(NormalizationForm.FormD),
                            z.Obcan.Jmeno.Normalize(NormalizationForm.FormD),
                            z.Obcan.Titul,
                            z.Obcan.RC,
                            z.Obcan.DatumNarozeni,
                            adresa.Normalize(NormalizationForm.FormD),
                            z.Rizeni.DatNastupu1,
                            z.Rizeni.DatNastupu2,
                            z.Rizeni.DatNastupu1Formatovano,
                            z.Organizace.Normalize(NormalizationForm.FormD),
                            z.Obcan.PodnikNazev1.Normalize(NormalizationForm.FormD),
                            z.Obcan.PodnikNazev2.Normalize(NormalizationForm.FormD),
                            z.ZmenaDatum
                            );

            return _table;
        }
        public DataTable ExportDataToDataTable(List<Obcan> obcane)
        {
            DataTable table = new DataTable();

            // Přidání sloupců podle vlastností třídy Obcan
            table.Columns.Add("ID");
            table.Columns.Add("Příjmení");
            table.Columns.Add("Jméno");
            table.Columns.Add("Titul");
            table.Columns.Add("Rodné číslo");
            table.Columns.Add("Datum narození");
            table.Columns.Add("Adresa bydliště");
            table.Columns.Add("Datum počátku výkonu CS");
            table.Columns.Add("Datum přerušení výkonu CS");
            table.Columns.Add("Datum ukončení vykonu CS");
            table.Columns.Add("Organizace");
            table.Columns.Add("Nazev podniku 1");
            table.Columns.Add("Nazev podniku 2");
            table.Columns.Add("Datum změny");
            table.Columns.Add("Autor změny");


            // Přidání řádků do tabulky
            foreach (var obcan in obcane)
            {
                table.Rows.Add(obcan.Id, obcan.Prijmeni, obcan.Jmeno, obcan.Titul, obcan.RC, obcan.DatumNarozeni, obcan.Adresa);
            }

            return table;
        }
    }
}
