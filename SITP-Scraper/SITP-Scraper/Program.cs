using CsvHelper;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;


namespace SITP_Scraper
{
    class Program
    {
        static void Main(string[] args)
        {
            // Troncales has other html layout
            //"http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=6", 
            // Alimentadors has other html layout
            // "http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=7",
            string[] start_urls = new string[] { "http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=8", "http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=9", "http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=10" };

            string downloadDir = AppDomain.CurrentDomain.BaseDirectory + "\\Download";
            System.IO.Directory.CreateDirectory(downloadDir);
            string ExportDir = AppDomain.CurrentDomain.BaseDirectory + "\\Export";
            System.IO.Directory.CreateDirectory(ExportDir);

            List<Route> Rutas = new List<Route> { };
            List<Horario> Horarios = new List<Horario> { };
            Console.WriteLine("Downloading files...")
            foreach (string address in start_urls)
            {
                HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;
                request.Method = "GET";
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    var uri = new Uri(address);
                    string tipoRuta = HttpUtility.ParseQueryString(uri.Query).Get("tipoRuta");
                    
                    HtmlDocument AP = new HtmlDocument();
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    AP.LoadHtml(reader.ReadToEnd());
                    string savefile = String.Format("Download\\{0}.html", tipoRuta);
                    AP.Save(savefile);
                    // Parsing Routes
                    foreach (HtmlNode row in AP.DocumentNode.SelectNodes("//table[@id='tblRutaTroncal']//tbody//tr"))
                    {
                        // PArsing route
                        string codigoRuta = row.SelectSingleNode(".//div[@class='codigoRuta']").InnerText;
                        string rutaNombre = row.SelectSingleNode(".//a[@class='rutaNombre']").InnerText;                        
                        rutaNombre = rutaNombre.Trim();
                        rutaNombre = HttpUtility.HtmlDecode(rutaNombre);
                        string rutaLink = row.SelectSingleNode(".//a[@class='rutaNombre']").Attributes["href"].Value.ToString();
                        var urllink = new Uri(HttpUtility.HtmlDecode(rutaLink));
                        string idRuta = HttpUtility.ParseQueryString(urllink.Query).Get("idRuta");
                        // List<string> horario = new List<string>();
                        Console.WriteLine("Parsing {0}", rutaNombre);
                        var ListHorarios = row.SelectNodes(".//p[@class='label label-horario']");
                        if (ListHorarios != null)
                        {
                            foreach (var itemhorario in ListHorarios)
                            {
                                // Add Running time.
                                Horarios.Add(new Horario
                                {
                                    idRuta = idRuta,
                                    horario = itemhorario.InnerText.Replace("               ", "")
                                }
                                );
                            }
                        }
                        
                        // Add route to list 
                        Rutas.Add(new Route
                        {
                            codigoRuta = codigoRuta,
                            rutaNombre = rutaNombre,
                            rutaLink = rutaLink,
                            idRuta = idRuta,
                            tipoRuta = tipoRuta
                        });
                    // End route parsing
                    }
                // End Webrequest.
                }
            // End url list parsing.
            }
            // Export to CSV.
            string exportroutesfile = ExportDir + "\\routes.txt";
            Console.WriteLine("Creating Export File routes.txt ...");
            using (var exportroutes = new StreamWriter(exportroutesfile))
            {
                // Route record
                var csvroutes = new CsvWriter(exportroutes);
                csvroutes.Configuration.Delimiter = ",";
                csvroutes.Configuration.Encoding = Encoding.UTF8;
                csvroutes.Configuration.TrimFields = true;
                // header 
                csvroutes.WriteField("idRuta");
                csvroutes.WriteField("tipoRuta");  
                csvroutes.WriteField("codigoRuta");
                csvroutes.WriteField("rutaNombre");
                csvroutes.WriteField("rutaLink");           
                csvroutes.NextRecord();
                for (int i = 0; i < Rutas.Count; i++) // Loop through List with for)
                {
                    csvroutes.WriteField(Rutas[i].idRuta);
                    csvroutes.WriteField(Rutas[i].tipoRuta);
                    csvroutes.WriteField(Rutas[i].codigoRuta);
                    csvroutes.WriteField(Rutas[i].rutaNombre);
                    csvroutes.WriteField(Rutas[i].rutaLink);
                    csvroutes.NextRecord();
                }
            }
            string exporthorariofile = ExportDir + "\\horario.txt";
            Console.WriteLine("Creating Export File horario.txt ...");
            using (var exporthorario = new StreamWriter(exporthorariofile))
            {
                // Route record
                var csvroutes = new CsvWriter(exporthorario);
                csvroutes.Configuration.Delimiter = ",";
                csvroutes.Configuration.Encoding = Encoding.UTF8;
                csvroutes.Configuration.TrimFields = true;
                // header 
                csvroutes.WriteField("codigoRuta");
                csvroutes.WriteField("horario");               
                csvroutes.NextRecord();
                for (int i = 0; i < Horarios.Count; i++) // Loop through List with for)
                {
                    csvroutes.WriteField(Horarios[i].idRuta);
                    csvroutes.WriteField(Horarios[i].horario);
                    csvroutes.NextRecord();
                }

            }

        }
    }

    public class Route
    {
        public string codigoRuta;
        public string rutaNombre;
        public string rutaLink;
        public string idRuta;
        public string tipoRuta;
    }

    public class Horario
    {
        public string idRuta;
        public string horario;
    }
}
