using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Text;

namespace CIVARCH.Pages
{
    public class ZaznamModel : PageModel
    {
        public void OnGet()
        {
        }

        public async Task<ActionResult> OnPost()
        {
            string id = "1";

            if (Request.Query["Id"] != "")
            {
                id = Request.Query["Id"];
            }

            Zaznam _zaznam = await DatabaseHandler.LoadSingleZaznamById(id);

            DataTable table = DataHandler.ExportDataToDataTable(_zaznam);

            var output = ToCsvByteArray(table, ";");

            output = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, output);

            string filename = "export_" + _zaznam.Obcan.Id.ToString() + "_" + DateTime.Now.ToString("dd.MM.yyyy-HH:mm:ss") + ".csv";

            return new FileContentResult(output, "text/csv")
            {
                FileDownloadName = filename
            };
        }

        private byte[] ToCsvByteArray(DataTable input, string delimeter = ",")
        {
            var stream = new MemoryStream();
            StreamWriter sw = new StreamWriter(stream);

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
                        if (value.Contains(','))
                        {
                            value = String.Format("\"{0}\"", value);
                            sw.Write(value);
                        }
                        else
                        {
                            sw.Write(row[i].ToString());
                        }
                    }
                    if (i < input.Columns.Count - 1)
                    {
                        sw.Write(delimeter);
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();

            return stream.ToArray();
        }
    }
}
