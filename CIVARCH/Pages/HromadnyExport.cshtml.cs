using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;

namespace CIVARCH.Pages
{
    public class HromadnyExportModel : PageModel
    {
        public void OnGet()
        {
        }

        /// <summary>
        /// Export vyhledávaného roèníku
        /// </summary>
        /// <returns>Funkce funguje na principu zadání dvouèíslí hledaného roku. Napøíklad vyhledávání roku 1960, staèí zadat 60 a program z Rè vezme první hodnoty a udìlá seznam/// </returns>
        public async Task<ActionResult> OnPost()
        {

            string start = Request.Form["Start"].ToString() ?? "";
            DataHandler dh = new DataHandler();

            List<Obcan> _obcane = await DatabaseHandler.LoadObcaneListQuery(0, int.MaxValue, start);

            DataTable table = dh.ExportDataToDataTable(_obcane);

            var output = ToCsvByteArray(table, ";");


            string filename = "hromadny_export_" + DateTime.Now.ToString("dd.MM.yyyy-HH:mm:ss") + ".csv";

            return new FileContentResult(output, "text/csv")
            {
                FileDownloadName = filename
            };
        }

        // Metoda pro export do CSV
        private byte[] ToCsvByteArray(DataTable input, string delimeter = ";")
        {
            var stream = new MemoryStream();
            using (StreamWriter sw = new StreamWriter(stream, Encoding.UTF8)) // Použití UTF-8
            {
                for (int i = 0; i < input.Columns.Count; i++)
                {
                    sw.Write(input.Columns[i]);
                    if (i < input.Columns.Count - 1)
                    {
                        sw.Write(delimeter);
                    }
                }
                sw.Write(sw.NewLine);

                foreach (DataRow row in input.Rows)
                {
                    for (int i = 0; i < input.Columns.Count; i++)
                    {
                        if (!Convert.IsDBNull(row[i]))
                        {
                            string value = row[i].ToString();
                            if (value.Contains(delimeter))
                            {
                                value = String.Format("\"{0}\"", value);
                            }
                            sw.Write(value);
                        }
                        else
                        {
                            sw.Write(row[i].ToString());
                        }

                        if (i < input.Columns.Count - 1)
                        {
                            sw.Write(delimeter);
                        }
                    }
                    sw.Write(sw.NewLine);
                }
            }
            return stream.ToArray();
        }
    }
}
