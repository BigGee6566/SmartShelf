using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace SmartShelf.Services
{
    /// <summary>
    /// Shared CSV building and download. Lives here so the reports screen and the activity log
    /// produce byte-identical output instead of each keeping its own copy of the escaping rules.
    /// </summary>
    public static class CsvWriter
    {
        public static string Build(string[] headers, IEnumerable<object[]> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(Escape)));

            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", row.Select(v => Escape(Convert.ToString(v)))));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Quotes any field containing a comma, quote or newline, and doubles embedded quotes -
        /// otherwise a product name with a comma in it would silently shift every later column.
        /// </summary>
        public static string Escape(string value)
        {
            value = value ?? string.Empty;

            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Sends the CSV as a download. Ends the response, so nothing may follow it.</summary>
        public static void Send(HttpResponse response, string fileName, string csv)
        {
            response.Clear();
            response.ContentType = "text/csv";
            response.ContentEncoding = Encoding.UTF8;
            response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
            // BOM so Excel opens the file as UTF-8 rather than the local ANSI code page.
            response.BinaryWrite(Encoding.UTF8.GetPreamble());
            response.Write(csv);
            response.End();
        }
    }
}
