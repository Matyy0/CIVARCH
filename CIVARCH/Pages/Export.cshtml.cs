using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace CIVARCH.Pages
{
    [Authorize]
    public class ExportModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return RedirectToPage("/Index");

            var zaznamy = new List<Zaznam>();
            foreach (var id in ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var z = await DatabaseHandler.LoadSingleZaznamById(id.Trim());
                    zaznamy.Add(z);
                }
                catch { }
            }

            if (zaznamy.Count == 0)
                return RedirectToPage("/Index");

            var bytes  = BuildCsv(zaznamy);
            var fname  = "export_" + DateTime.Now.ToString("dd.MM.yyyy-HH-mm-ss") + ".csv";
            return File(bytes, "text/csv; charset=utf-8", fname);
        }

        private static byte[] BuildCsv(List<Zaznam> zaznamy)
        {
            const string sep = ";";
            var sb = new StringBuilder();

            string[] headers =
            {
                "ID", "Příjmení", "Jméno", "Titul", "Rodné příjmení", "Rodné číslo",
                "Datum narození", "Adresa", "Datum nástupu 1", "Datum nástupu 2",
                "Datum ukončení", "Datum vystavení", "Organizace", "Podnik", "Stav",
                "Datum změny", "Autor změny"
            };
            sb.Append(string.Join(sep, headers.Select(h => Esc(h, sep))));
            sb.Append("\r\n");

            foreach (var z in zaznamy)
            {
                string[] row =
                {
                    z.Obcan.Id.ToString(),
                    z.Obcan.Prijmeni,
                    z.Obcan.Jmeno,
                    z.Obcan.Titul,
                    z.Obcan.RodneJm,
                    z.Obcan.RC,
                    z.Obcan.DatumNarozeni,
                    z.Obcan.Adresa?.ToString() ?? "",
                    z.Rizeni.DatNastupu1,
                    z.Rizeni.DatNastupu2,
                    z.Rizeni.Datum,
                    z.Rizeni.DatVystaveno,
                    z.Organizace,
                    z.Obcan.PodnikNazev1,
                    z.Rizeni.Stav,
                    z.ZmenaDatum,
                    z.ZmenaAutor
                };
                sb.Append(string.Join(sep, row.Select(v => Esc(v ?? "", sep))));
                sb.Append("\r\n");
            }

            byte[] bom  = { 0xEF, 0xBB, 0xBF };
            byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] result = new byte[bom.Length + data.Length];
            bom.CopyTo(result, 0);
            data.CopyTo(result, bom.Length);
            return result;
        }

        private static string Esc(string v, string sep) =>
            (v.Contains(sep) || v.Contains('"') || v.Contains('\n') || v.Contains('\r'))
                ? '"' + v.Replace("\"", "\"\"") + '"'
                : v;
    }
}
