using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
                    id           = z.Obcan.Id,
                    jmeno        = z.Obcan.Jmeno,
                    prijmeni     = z.Obcan.Prijmeni,
                    titul        = z.Obcan.Titul,
                    rc           = z.Obcan.RC,
                    datumNarozeni= z.Obcan.DatumNarozeni,
                    rodneJm      = z.Obcan.RodneJm,
                    adresa       = z.Obcan.Adresa?.ToString() ?? "–",
                    ulice        = z.Obcan.Adresa?.Ulice ?? "",
                    cp           = z.Obcan.Adresa?.CisloPopisne ?? "",
                    adresaObec   = z.Obcan.Adresa?.Obec ?? "",
                    psc          = z.Obcan.Adresa?.Psc ?? "",
                    organizace   = z.Organizace,
                    organizaceCislo = z.Obcan.OrganizaceCislo,
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

        public async Task<IActionResult> OnPostNovyPodnikAsync()
        {
            string? okrOrp    = Request.Form["np_OkrOrp"];
            string? cisOrp    = Request.Form["np_CisOrp"];
            string? orgNazev1 = Request.Form["np_OrgNazev1"];
            string? ulice     = Request.Form["np_Ulice"];
            string? obec      = Request.Form["np_Obec"];
            string? psc       = Request.Form["np_Psc"];
            string? predmet   = Request.Form["np_PredmetCinn"];

            try
            {
                int highestId = await DatabaseHandler.GetHighestColumnIndexAsync("tbPODNIK", "PODNIK_Id");
                var podnik = new Podnik(
                    (highestId + 1).ToString(),
                    orgNazev1 ?? "", "",
                    ulice ?? "", obec ?? "", psc ?? "",
                    predmet ?? "", okrOrp ?? "", cisOrp ?? "");
                DatabaseHandler.SavePodnik(podnik);
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostUpravitZaznamAsync()
        {
            string? id       = Request.Form["uz_Id"];
            string? prijmeni = Request.Form["uz_Prijmeni"];
            string? jmeno    = Request.Form["uz_Jmeno"];
            string? titul    = Request.Form["uz_Titul"];
            string? rc       = Request.Form["uz_RC"];
            string? rodneJm  = Request.Form["uz_RodneJm"];
            string? ulice    = Request.Form["uz_Adresa_Ulice"];
            string? cp       = Request.Form["uz_Adresa_CP"];
            string? obec     = Request.Form["uz_Adresa_Obec"];
            string? psc      = Request.Form["uz_Adresa_PSC"];
            string? podnik   = Request.Form["uz_Rizeni_Podnik"];
            string? dat1     = Request.Form["uz_Rizeni_DatNastupu1"];
            string? dat2     = Request.Form["uz_Rizeni_DatNastupu2"];
            string? datum    = Request.Form["uz_Rizeni_Datum"];
            string? datV     = Request.Form["uz_Rizeni_DatVystaveno"];
            string? stav     = Request.Form["uz_Rizeni_Stav"];

            try
            {
                string orgCislo = await DatabaseHandler.GetCisoOrp(podnik ?? "");
                var adresa   = new KontaktniAdresa(ulice, cp, obec, psc);
                var rizeni   = new Rizeni(podnik, dat1, dat2, datum, datV, stav);
                var obcanObj = new Obcan(prijmeni, jmeno, titul, rc, rodneJm, adresa, orgCislo, int.Parse(id ?? "0"));
                var zaznam   = new Zaznam(obcanObj, rizeni, orgCislo, User.Identity?.Name ?? "");
                DatabaseHandler.UpdateZaznam(zaznam);
                DatabaseHandler.UpdateRizeniTable(rc ?? "");
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, error = ex.Message });
            }
        }

    }
}
