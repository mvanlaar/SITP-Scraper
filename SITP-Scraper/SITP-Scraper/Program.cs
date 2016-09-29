using CsvHelper;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
            string downloadsite = "http://www.sitp.gov.co";
            const string ua = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";



            string downloadDir = AppDomain.CurrentDomain.BaseDirectory + "\\Download";
            System.IO.Directory.CreateDirectory(downloadDir);
            string ExportDir = AppDomain.CurrentDomain.BaseDirectory + "\\Export";
            System.IO.Directory.CreateDirectory(ExportDir);

            List<Route> Rutas = new List<Route> { };
            List<Horario> Horarios = new List<Horario> { };
            List<RouteParada> RouteParadas = new List<RouteParada> { };
            List<Parada> Paradas = new List<Parada> { };
            Console.WriteLine("Downloading files...");
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
                    if (Convert.ToBoolean(ConfigurationManager.AppSettings.Get("SaveHTML")))
                    {
                        AP.Save(savefile);
                    }
                    // Parsing Routes
                    foreach (HtmlNode row in AP.DocumentNode.SelectNodes("//table[@id='tblRutaTroncal']//tbody//tr"))
                    {
                        // PArsing route
                        string codigoRuta = row.SelectSingleNode(".//div[@class='codigoRuta']").InnerText;
                        string rutaNombre = row.SelectSingleNode(".//a[@class='rutaNombre']").InnerText;                        
                        rutaNombre = rutaNombre.Trim();
                        rutaNombre = HttpUtility.HtmlDecode(rutaNombre);
                        string rutaLink = row.SelectSingleNode(".//a[@class='rutaNombre']").Attributes["href"].Value.ToString();
                        rutaLink = HttpUtility.HtmlDecode(rutaLink);
                        var urllink = new Uri(rutaLink);
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
            // Looping through pages with Route information.
            for (int i = 0; i < Rutas.Count; i++) // Loop through List with for)
            {
                Console.WriteLine("Parsing Route page: {0}", Rutas[i].rutaLink);
                HttpWebRequest requestdetail = WebRequest.Create(Rutas[i].rutaLink) as HttpWebRequest;
                requestdetail.Method = "GET";
                using (HttpWebResponse responsedetail = requestdetail.GetResponse() as HttpWebResponse)
                {
                    HtmlDocument RutaDetail = new HtmlDocument();
                    StreamReader readerdetail = new StreamReader(responsedetail.GetResponseStream());
                    RutaDetail.LoadHtml(readerdetail.ReadToEnd());
                    string savefile = String.Format("Download\\{0}-{1}.html", Rutas[i].tipoRuta, Rutas[i].idRuta);
                    if (Convert.ToBoolean(ConfigurationManager.AppSettings.Get("SaveHTML")))
                    {
                        RutaDetail.Save(savefile);
                    }
                    // Downloadable Files
                    var tmplinkMapaRuta = RutaDetail.DocumentNode.SelectSingleNode("//a[@class='linkMapaRuta']");
                    string linkMapaRuta = null;
                    if (tmplinkMapaRuta != null)
                    {
                        linkMapaRuta = RutaDetail.DocumentNode.SelectSingleNode("//a[@class='linkMapaRuta']").Attributes["href"].Value.ToString();
                        // Download IMG
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DownloadIMG")))
                        {
                            string fullurl = downloadsite + linkMapaRuta;
                            string filename = Path.GetFileName(new Uri(fullurl).AbsolutePath);
                            string downloadDirIMG = downloadDir + "\\IMG";
                            System.IO.Directory.CreateDirectory(downloadDirIMG);
                            string fullpath = downloadDirIMG + "\\" + filename;
                            try
                            {
                                using (var client = new WebClient())
                                {
                                    client.Headers.Add("user-agent", ua);
                                    client.Headers.Add("Referer", Rutas[i].rutaLink);
                                    client.Proxy = null;
                                    client.DownloadFile(fullurl, fullpath);
                                }
                            }
                            catch { }
                        } 

                    }                    
                    var tmplinkPuntoParada = RutaDetail.DocumentNode.SelectSingleNode("//a[@class='linkPuntoParada']");
                    string linkPuntoParada = null;
                    if (tmplinkPuntoParada != null)
                    {
                        linkPuntoParada = RutaDetail.DocumentNode.SelectSingleNode("//a[@class='linkPuntoParada']").Attributes["href"].Value.ToString();
                        // Download IMG
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DownloadIMG")))
                        {
                            string fullurl = downloadsite + linkPuntoParada;
                            string filename = Path.GetFileName(new Uri(fullurl).AbsolutePath);
                            string downloadDirIMG = downloadDir + "\\IMG";
                            System.IO.Directory.CreateDirectory(downloadDirIMG);
                            string fullpath = downloadDirIMG + "\\" + filename;
                            try
                            {
                                using (var client = new WebClient())
                                {
                                    client.Headers.Add("user-agent", ua);
                                    client.Headers.Add("Referer", Rutas[i].rutaLink);
                                    client.Proxy = null;
                                    client.DownloadFile(fullurl, fullpath);
                                }
                            }
                            catch { }
                        } 
                    }
                    var tmplinkPlegableRuta = RutaDetail.DocumentNode.SelectSingleNode("//a[@class='linkPlegableRuta']");
                    string linkPlegableRuta = null;
                    if (tmplinkPlegableRuta != null)
                    {
                        linkPlegableRuta = RutaDetail.DocumentNode.SelectSingleNode("//a[@class='linkPlegableRuta']").Attributes["href"].Value.ToString();
                        // Download PDF
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DownloadPDF")))
                        {
                            string fullurl = downloadsite + linkPlegableRuta;
                            string filename = Path.GetFileName(new Uri(fullurl).AbsolutePath);
                            string downloadDirPDF = downloadDir + "\\PDF";
                            System.IO.Directory.CreateDirectory(downloadDirPDF);
                            string fullpath = downloadDirPDF + "\\" + filename;
                            try
                            {
                                using (var client = new WebClient())
                                {
                                    client.Headers.Add("user-agent", ua);
                                    client.Headers.Add("Referer", Rutas[i].rutaLink);
                                    client.Proxy = null;
                                    client.DownloadFile(fullurl, fullpath);
                                }
                            }
                            catch { }
                        } 
                    }
                    

                    // Route Stations
                    // Parsing Route information Direction A - Because a and b can be different.
                    HtmlNode routea = RutaDetail.DocumentNode.SelectSingleNode("//div[@class='recorrido1']");
                    {
                        string direction = "A";
                        foreach (HtmlNode NodeParada in routea.SelectNodes(".//div[@class='infoParada']"))
                        { 
                            string estNombre = NodeParada.SelectSingleNode(".//a").InnerHtml;
                            estNombre = Regex.Replace(estNombre, @"<span class=""ignoreAaa"">.*?</span>", string.Empty);
                            estNombre = HttpUtility.HtmlDecode(estNombre);
                            estNombre = estNombre.Trim();
                            string estNumbre = NodeParada.SelectSingleNode(".//span[@class='ignoreAaa']").InnerText;
                            estNumbre = estNumbre.Replace(".", "").Trim();
                            string estDireccion = NodeParada.SelectSingleNode(".//div[@class='estDireccion']").InnerText;
                            string estLink = NodeParada.SelectSingleNode(".//a").Attributes["href"].Value.ToString();
                            estLink = HttpUtility.HtmlDecode(estLink);
                            var urllink = new Uri(estLink);
                            string estId = HttpUtility.ParseQueryString(urllink.Query).Get("paradero");
                            RouteParadas.Add(new RouteParada
                            {
                                idRuta = Rutas[i].idRuta,
                                rutaDirection = direction,
                                estNumber = estNumbre,
                                estId = estId
                            }
                            );
                            bool alreadyExists = Paradas.Exists(x => x.estId == estId
                                && x.estNombre == estNombre
                                && x.estDireccion == estDireccion
                                && x.estLink == estLink
                               );
                            if (!alreadyExists)
                            {
                                Paradas.Add(new Parada
                                {
                                    estId = estId,
                                    estNombre = estNombre,
                                    estDireccion = estDireccion,
                                    estLink = estLink
                                }
                                );
                            }                            
                        }
                    }
                    // Parsing Route information Direction B
                    HtmlNode routeb = RutaDetail.DocumentNode.SelectSingleNode("//div[@class='recorrido2']");
                    {
                        string direction = "B";
                        foreach (HtmlNode NodeParada in routea.SelectNodes(".//div[@class='infoParada']"))
                        {
                            string estNombre = NodeParada.SelectSingleNode(".//a").InnerHtml;
                            estNombre = Regex.Replace(estNombre, @"<span class=""ignoreAaa"">.*?</span>", string.Empty);
                            estNombre = HttpUtility.HtmlDecode(estNombre);
                            estNombre = estNombre.Trim();
                            string estNumbre = NodeParada.SelectSingleNode(".//span[@class='ignoreAaa']").InnerText;
                            estNumbre = estNumbre.Replace(".", "").Trim();
                            string estDireccion = NodeParada.SelectSingleNode(".//div[@class='estDireccion']").InnerText;
                            string estLink = NodeParada.SelectSingleNode(".//a").Attributes["href"].Value.ToString();
                            estLink = HttpUtility.HtmlDecode(estLink);
                            var urllink = new Uri(estLink);
                            string estId = HttpUtility.ParseQueryString(urllink.Query).Get("paradero");
                            RouteParadas.Add(new RouteParada
                            {
                                idRuta = Rutas[i].idRuta,
                                rutaDirection = direction,
                                estNumber = estNumbre,
                                estId = estId
                            }
                            );
                            bool alreadyExists = Paradas.Exists(x => x.estId == estId
                                && x.estNombre == estNombre
                                && x.estDireccion == estDireccion
                                && x.estLink == estLink
                               );
                            if (!alreadyExists)
                            {
                                Paradas.Add(new Parada
                                {
                                    estId = estId,
                                    estNombre = estNombre,
                                    estDireccion = estDireccion,
                                    estLink = estLink
                                }
                                );
                            }
                        }
                    }
                }
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

            string exportparadasfile = ExportDir + "\\paradas.txt";
            Console.WriteLine("Creating Export File paradas.txt ...");
            using (var exportparada = new StreamWriter(exportparadasfile))
            {
                // Route record
                var csvroutes = new CsvWriter(exportparada);
                csvroutes.Configuration.Delimiter = ",";
                csvroutes.Configuration.Encoding = Encoding.UTF8;
                csvroutes.Configuration.TrimFields = true;
                // header 
                csvroutes.WriteField("estId");
                csvroutes.WriteField("estNombre");
                csvroutes.WriteField("estDireccion");
                csvroutes.WriteField("estLink");
                csvroutes.NextRecord();
                for (int i = 0; i < Paradas.Count; i++) // Loop through List with for)
                {
                    csvroutes.WriteField(Paradas[i].estId);
                    csvroutes.WriteField(Paradas[i].estNombre);
                    csvroutes.WriteField(Paradas[i].estDireccion);
                    csvroutes.WriteField(Paradas[i].estLink);
                    csvroutes.NextRecord();
                }

            }

            string exportrouteparadasfile = ExportDir + "\\routeparada.txt";
            Console.WriteLine("Creating Export File routeparadas.txt ...");
            using (var exportrouteparada = new StreamWriter(exportrouteparadasfile))
            {
                // Route record
                var csvroutes = new CsvWriter(exportrouteparada);
                csvroutes.Configuration.Delimiter = ",";
                csvroutes.Configuration.Encoding = Encoding.UTF8;
                csvroutes.Configuration.TrimFields = true;
                // header 
                csvroutes.WriteField("idRuta");
                csvroutes.WriteField("rutaDirection");
                csvroutes.WriteField("estNumber");
                csvroutes.WriteField("estId");
                csvroutes.NextRecord();
                for (int i = 0; i < RouteParadas.Count; i++) // Loop through List with for)
                {
                    csvroutes.WriteField(RouteParadas[i].idRuta);
                    csvroutes.WriteField(RouteParadas[i].rutaDirection);
                    csvroutes.WriteField(RouteParadas[i].estNumber);
                    csvroutes.WriteField(RouteParadas[i].estId);
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

    public class RouteParada
    {        
        public string idRuta;
        public string rutaDirection;
        public string estNumber;
        public string estId;
    }

    public class Parada
    {        
        public string estId;
        public string estNombre;
        public string estDireccion;
        public string estLink;

    }

    public class Horario
    {
        public string idRuta;
        public string horario;
    }
}
