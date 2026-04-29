using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;

namespace CIVARCH.Pages
{
    public class IndexModel : PageModel
    {
        [TempData]
        public int? SortByColumn { get; set; }

        [TempData]
        public string? DeleteError { get; set; }

        public void OnGet() { }

        public async Task<JsonResult> OnGetDetailAsync(int id)
        {
            try
            {
                var z = await DatabaseHandler.LoadSingleZaznamById(id.ToString());
                return new JsonResult(new
                {
                    ok = true,
                    jmeno        = z.Obcan.Jmeno,
                    prijmeni     = z.Obcan.Prijmeni,
                    titul        = z.Obcan.Titul,
                    rc           = z.Obcan.RC,
                    datumNarozeni= z.Obcan.DatumNarozeni,
                    rodneJm      = z.Obcan.RodneJm,
                    adresa       = z.Obcan.Adresa?.ToString() ?? "–",
                    organizace   = z.Organizace,
                    stav         = z.Rizeni.Stav,
                    datNastupu1  = z.Rizeni.DatNastupu1,
                    datNastupu2  = z.Rizeni.DatNastupu2,
                    datum        = z.Rizeni.Datum,
                    datVystaveno = z.Rizeni.DatVystaveno,
                    podnikNazev1 = z.Obcan.PodnikNazev1,
                    podnikNazev2 = z.Obcan.PodnikNazev2,
                    zmenaDatum   = z.ZmenaDatum,
                    zmenaAutor   = z.ZmenaAutor
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message });
            }
        }

        public ActionResult OnPostDeleteObcan(int id, string sortby, string returnUrl, string searchQuery)
        {
            try
            {
                DatabaseHandler.DeleteZaznam(id);
            }
            catch (Exception ex)
            {
                TempData["DeleteError"] = $"Záznam se nepodařilo smazat: {ex.Message}";
            }

            if (int.TryParse(sortby, out int result))
                SortByColumn = result;

            var redirectUrl = (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                ? returnUrl
                : "/";

            return Redirect(redirectUrl);
        }

        public ActionResult OnPostSaveSortPrefs(string sortby, string returnUrl, string? searchQuery)
        {
            if (int.TryParse(sortby, out int result))
                SortByColumn = result;

            var redirectUrl = (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                ? returnUrl
                : "/";

            return Redirect(redirectUrl);
        }

        public async Task<IActionResult> OnPostNovyZaznamAsync()
        {
            string? prijmeni        = Request.Form["nz_Prijmeni"];
            string? jmeno           = Request.Form["nz_Jmeno"];
            string? titul           = Request.Form["nz_Titul"];
            string? rc              = Request.Form["nz_RC"];
            string? rodneJm         = Request.Form["nz_RodneJm"];
            string? adresa_ulice    = Request.Form["nz_Adresa_Ulice"];
            string? adresa_cp       = Request.Form["nz_Adresa_CP"];
            string? adresa_obec     = Request.Form["nz_Adresa_Obec"];
            string? adresa_psc      = Request.Form["nz_Adresa_PSC"];
            string? rizeni_podnik   = Request.Form["nz_Rizeni_Podnik"];
            string? rizeni_dat1     = Request.Form["nz_Rizeni_DatNastupu1"];
            string? rizeni_dat2     = Request.Form["nz_Rizeni_DatNastupu2"];
            string? rizeni_datum    = Request.Form["nz_Rizeni_Datum"];
            string? rizeni_datV     = Request.Form["nz_Rizeni_DatVystaveno"];
            string? rizeni_stav     = Request.Form["nz_Rizeni_Stav"];

            if (DatabaseHandler.CheckRCExists(rc ?? ""))
                return new JsonResult(new { ok = false, error = "Duplicitní rodné číslo – záznam nebyl přidán." });

            try
            {
                string organizace = await DatabaseHandler.GetCisoOrp(rizeni_podnik);
                var adresa = new KontaktniAdresa(adresa_ulice, adresa_cp, adresa_obec, adresa_psc);
                var rizeni = new Rizeni(rizeni_podnik, rizeni_dat1, rizeni_dat2, rizeni_datum, rizeni_datV, rizeni_stav);
                var obcan  = new Obcan(prijmeni, jmeno, titul, rc, rodneJm, adresa);
                var zaznam = new Zaznam(obcan, rizeni, organizace, User.Identity?.Name ?? "");
                DatabaseHandler.SaveNewZaznam(zaznam);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message });
            }
        }

        public async Task<ActionResult> OnPost()
        {
            string id = Request.Form["ExportId"].ToString();
            if (string.IsNullOrEmpty(id)) return new EmptyResult();

            var zaznamy = new List<Zaznam>();
            foreach (string eid in id.Split(','))
                zaznamy.Add(await DatabaseHandler.LoadSingleZaznamById(eid));

            var table = DataHandler.ExportDataToDataTable(zaznamy);
            var output = ToCsvByteArray(table, ";");
            output = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, output);

            string filename = "export_" + DateTime.Now.ToString("dd.MM.yyyy-HH-mm-ss") + ".csv";
            return new FileContentResult(output, "text/csv") { FileDownloadName = filename };
        }

        private byte[] ToCsvByteArray(DataTable input, string delimeter = ",")
        {
            var stream = new MemoryStream();
            var sw = new StreamWriter(stream);

            for (int i = 0; i < input.Columns.Count; i++)
            {
                sw.Write(input.Columns[i]);
                if (i < input.Columns.Count - 1) sw.Write(delimeter);
            }
            sw.Write(sw.NewLine);

            foreach (DataRow row in input.Rows)
            {
                for (int i = 0; i < input.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(row[i]))
                    {
                        string value = row[i].ToString() ?? "";
                        sw.Write(value.Contains(',') ? $"\"{value}\"" : value);
                    }
                    if (i < input.Columns.Count - 1) sw.Write(delimeter);
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
            return stream.ToArray();
        }
    }
}
