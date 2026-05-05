using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace CIVARCH.Pages
{
    [Authorize]
    public class HromadnyExportModel : PageModel
    {
        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            string query = Request.Form["Start"].ToString()?.Trim() ?? "";

            List<Obcan> obcane = await DatabaseHandler.LoadObcaneListQuery(0, int.MaxValue, query);

            if (obcane.Count == 0)
                return new JsonResult(new { ok = false, error = "Žádné záznamy nebyly nalezeny." });

            var bytes = BuildCsv(obcane);
            string fname = "hromadny_export_" + DateTime.Now.ToString("dd.MM.yyyy-HH-mm-ss") + ".csv";
            return File(bytes, "text/csv; charset=utf-8", fname);
        }

        private static byte[] BuildCsv(List<Obcan> obcane)
        {
            const string sep = ";";
            var sb = new StringBuilder();

            string[] headers =
            {
                "ID", "Příjmení", "Jméno", "Titul", "Rodné příjmení", "Rodné číslo",
                "Datum narození", "Adresa"
            };
            sb.Append(string.Join(sep, headers.Select(h => Esc(h, sep))));
            sb.Append("\r\n");

            foreach (var o in obcane)
            {
                string[] row =
                {
                    o.Id.ToString(),
                    o.Prijmeni ?? "",
                    o.Jmeno ?? "",
                    o.Titul ?? "",
                    o.RodneJm ?? "",
                    o.RC ?? "",
                    o.DatumNarozeni ?? "",
                    o.Adresa?.ToString() ?? ""
                };
                sb.Append(string.Join(sep, row.Select(v => Esc(v, sep))));
                sb.Append("\r\n");
            }

            byte[] bom = { 0xEF, 0xBB, 0xBF };
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
