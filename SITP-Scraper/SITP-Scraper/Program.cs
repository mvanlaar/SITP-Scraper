﻿using CsvHelper;
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
using SharpKml;
using SharpKml.Engine;
using SharpKml.Dom;
using SharpKml.Base;

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
            string[] start_urls = new string[] { "http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=6", "http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=7", "http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=8", "http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=9", "http://www.sitp.gov.co/loader.php?lServicio=Rutas&lTipo=busqueda&lFuncion=mostrarRuta&tipoRuta=10" };
            string downloadsite = "http://www.sitp.gov.co";
            const string ua = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            
            string downloadDir = AppDomain.CurrentDomain.BaseDirectory + "\\Download";
            System.IO.Directory.CreateDirectory(downloadDir);
            string ExportDir = AppDomain.CurrentDomain.BaseDirectory + "\\Export";
            System.IO.Directory.CreateDirectory(ExportDir);

            ServicePointManager.DefaultConnectionLimit = 20;

            List<Route> Rutas = new List<Route> { };
            List<Horario> Horarios = new List<Horario> { };
            List<RouteParada> RouteParadas = new List<RouteParada> { };
            List<Parada> Paradas = new List<Parada> { };
            List<ParadaSITP> ParadasSITP = new List<ParadaSITP> { };

                        // Parse the .kml files 
            // This will read a Kml file into memory.            
            KmlFile file = KmlFile.Load(new StreamReader("kml//Paraderos SITP.kml"));
            Kml kml = file.Root as Kml;
            if (kml != null)
            {
                Console.WriteLine("Parsing Paraderos SITP...");
                foreach (var placemark in kml.Flatten().OfType<Placemark>())
                {                                    
                    Vector coord = ((Point)placemark.Geometry).Coordinate;                    
                    ParadasSITP.Add(new ParadaSITP { name = placemark.Name, latitude = coord.Latitude.ToString(), longtitude = coord.Longitude.ToString() });                                
                }
            }

            //KmlFile filerutas = KmlFile.Load(new StreamReader("kml//Rutas SITP.kml"));
            //Kml kmlrutas = filerutas.Root as Kml;
            //if (kmlrutas != null)
            //{
            //    foreach (var placemark in kmlrutas.Flatten().OfType<Placemark>())
            //    {
            //        Console.WriteLine(placemark.Name);
            //        foreach (var linepart in ((LineString)placemark.Geometry).Coordinates)
            //        {
            //            Vector coord = linepart;
            //            Console.WriteLine(coord.Latitude);
            //            Console.WriteLine(coord.Longitude);
            //        }                    
            //        ParadasSITP.Add(new ParadaSITP { name = placemark.Name, latitude = coord.Latitude.ToString(), longtitude = coord.Longitude.ToString() });
            //    }
            //}
            Console.WriteLine("Downloading files...");
            foreach (string address in start_urls)
            {
                HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;
                request.Method = "GET";
                request.Proxy = null;
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
                    if (tipoRuta == "6")
                    {
                        // Parsing Troncales Routes
                        foreach (HtmlNode row in AP.DocumentNode.SelectNodes("//table[@id='tblPagine']//tbody//tr"))
                        {
                            // PArsing route
                            string codigoRuta = row.SelectSingleNode(".//p[@class='letraTroncal']").InnerText;
                            string rutaNombre = row.SelectSingleNode(".//a[@class='nombreTroncal']").InnerText;
                            rutaNombre = rutaNombre.Trim();
                            rutaNombre = HttpUtility.HtmlDecode(rutaNombre);
                            string rutaLink = row.SelectSingleNode(".//a[@class='nombreTroncal']").Attributes["href"].Value.ToString();
                            rutaLink = HttpUtility.HtmlDecode(rutaLink);
                            var urllink = new Uri(rutaLink);
                            string idRuta = HttpUtility.ParseQueryString(urllink.Query).Get("troncal");
                            // List<string> horario = new List<string>();
                            Console.WriteLine("Parsing {0}", rutaNombre);
                            string linkPlegableRuta = null;
                            linkPlegableRuta = row.SelectSingleNode("//div[@class='esquemaTroncal'] //a").Attributes["href"].Value.ToString();
                            // Download IMG
                            //if (Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DownloadPDF")))
                            //{
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
                                        client.Headers.Add("Referer", address);
                                        client.Proxy = null;
                                        client.DownloadFile(fullurl, fullpath);
                                    }
                                }
                                catch { }
                            //} 

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
                    }
                    if (tipoRuta == "7")
                    {
                        // Parsing Alimentadors Routes
                        foreach (HtmlNode row in AP.DocumentNode.SelectNodes("//table[@id='tblPagine']//tbody//tr"))
                        {
                            // PArsing route
                            string codigoRuta = row.SelectSingleNode(".//div[@class='codigoRuta']").InnerText;
                            string rutaNombre = row.SelectSingleNode(".//a[@class='rutaEstacionesNombre']").InnerText;
                            rutaNombre = rutaNombre.Trim();
                            rutaNombre = HttpUtility.HtmlDecode(rutaNombre);
                            string rutaLink = row.SelectSingleNode(".//a[@class='rutaEstacionesNombre']").Attributes["href"].Value.ToString();
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
                    }
                    //else
                    //{
                    //    // Parsing Routes
                    //    foreach (HtmlNode row in AP.DocumentNode.SelectNodes("//table[@id='tblRutaTroncal']//tbody//tr"))
                    //    {
                    //        // PArsing route
                    //        string codigoRuta = row.SelectSingleNode(".//div[@class='codigoRuta']").InnerText;
                    //        string rutaNombre = row.SelectSingleNode(".//a[@class='rutaNombre']").InnerText;
                    //        rutaNombre = rutaNombre.Trim();
                    //        rutaNombre = HttpUtility.HtmlDecode(rutaNombre);
                    //        string rutaLink = row.SelectSingleNode(".//a[@class='rutaNombre']").Attributes["href"].Value.ToString();
                    //        rutaLink = HttpUtility.HtmlDecode(rutaLink);
                    //        var urllink = new Uri(rutaLink);
                    //        string idRuta = HttpUtility.ParseQueryString(urllink.Query).Get("idRuta");
                    //        // List<string> horario = new List<string>();
                    //        Console.WriteLine("Parsing {0}", rutaNombre);
                    //        var ListHorarios = row.SelectNodes(".//p[@class='label label-horario']");
                    //        if (ListHorarios != null)
                    //        {
                    //            foreach (var itemhorario in ListHorarios)
                    //            {
                    //                // Add Running time.
                    //                Horarios.Add(new Horario
                    //                {
                    //                    idRuta = idRuta,
                    //                    horario = itemhorario.InnerText.Replace("               ", "")
                    //                }
                    //                );
                    //            }
                    //        }

                    //        // Add route to list 
                    //        Rutas.Add(new Route
                    //        {
                    //            codigoRuta = codigoRuta,
                    //            rutaNombre = rutaNombre,
                    //            rutaLink = rutaLink,
                    //            idRuta = idRuta,
                    //            tipoRuta = tipoRuta
                    //        });
                    //        // End route parsing
                    //    }
                    //}
                // End Webrequest.
                }
            // End url list parsing.
            }
            // Looping through pages with Route information.
            for (int i = 0; i < Rutas.Count; i++) // Loop through List with for)
            {
                Console.WriteLine("Parsing Route {0} of {1}", i, Rutas.Count);
                //Console.WriteLine("Parsing Route page: {0}", Rutas[i].rutaLink);
                HttpWebRequest requestdetail = WebRequest.Create(Rutas[i].rutaLink) as HttpWebRequest;
                requestdetail.Method = "GET";
                requestdetail.Proxy = null;
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
                 // End Route Parsing    
                }
            }
            // Get Official Paradero number so its possible to match it to 
            for (int i = 0; i < Paradas.Count; i++) // Loop through List with for)
            {
                Console.WriteLine("Parsing Parada {0} of {1}", i, Paradas.Count);
                //Console.WriteLine("Parsing Parada page: {0}", Paradas[i].estLink);
                HttpWebRequest requestparada = WebRequest.Create(Paradas[i].estLink) as HttpWebRequest;
                requestparada.Method = "GET";
                requestparada.Proxy = null;
                using (HttpWebResponse responsedetail = requestparada.GetResponse() as HttpWebResponse)
                {
                    HtmlDocument RutaParada = new HtmlDocument();
                    StreamReader readerdetail = new StreamReader(responsedetail.GetResponseStream());
                    RutaParada.LoadHtml(readerdetail.ReadToEnd());
                    string savefile = String.Format("Download\\Parada-{0}.html", Paradas[i].estId);
                    if (Convert.ToBoolean(ConfigurationManager.AppSettings.Get("SaveHTML")))
                    {
                        RutaParada.Save(savefile);
                    }
                    // 
                    string estSITPNumber = RutaParada.DocumentNode.SelectSingleNode("//div[@id='zonaBloqueContent']//h3").InnerText;
                    
                    estSITPNumber = estSITPNumber.Replace("Paradero ", "");
                    int index = estSITPNumber.IndexOf(" - ",0);
                    estSITPNumber = estSITPNumber.Substring(0, index);
                    estSITPNumber = estSITPNumber.Trim();
                    Paradas[i].estSITPNumber = estSITPNumber;
                    var item = ParadasSITP.Find(q => q.name == estSITPNumber);                   
                    if (item != null)
                    {
                        //Do stuff
                        Paradas[i].estlatitude = item.latitude;
                        Paradas[i].estlongtitude = item.longtitude;
                    }
                    else
                    {
                        // Ok this can be also a multiple paradas site
                        // Check for Letter A in posistion 4 and retry search
                        // http://www.sitp.gov.co/Publicaciones/paraderos_multiples
                        string sitpnumbermultiple = estSITPNumber;
                        string letter = sitpnumbermultiple.Substring(3, 1);
                        if (letter != "A")
                        {
                            // Ok postition 4 is not the letter A
                            // Replace position 4 with the letter A
                            sitpnumbermultiple = sitpnumbermultiple.Remove(3, 1);
                            sitpnumbermultiple = sitpnumbermultiple.Insert(3, "A");

                            var itemmultiple = ParadasSITP.Find(q => q.name == sitpnumbermultiple);
                            if (itemmultiple != null)
                            {
                                //Do stuff
                                Paradas[i].estlatitude = itemmultiple.latitude;
                                Paradas[i].estlongtitude = itemmultiple.longtitude;
                            }
                            else
                            {
                                Paradas[i].estlatitude = "";
                                Paradas[i].estlongtitude = "";
                            }
                        }
                        else
                        {
                            // Ok when we search for A the first time we don't got the lat and lng
                            Paradas[i].estlatitude = "";
                            Paradas[i].estlongtitude = "";
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
                csvroutes.WriteField("estSITPNumber");
                csvroutes.WriteField("estNombre");
                csvroutes.WriteField("estDireccion");
                csvroutes.WriteField("estLink");
                csvroutes.WriteField("estlongtitude");
                csvroutes.WriteField("estlatitude");                               
                csvroutes.NextRecord();
                for (int i = 0; i < Paradas.Count; i++) // Loop through List with for)
                {
                    csvroutes.WriteField(Paradas[i].estId);
                    csvroutes.WriteField(Paradas[i].estSITPNumber);
                    csvroutes.WriteField(Paradas[i].estNombre);
                    csvroutes.WriteField(Paradas[i].estDireccion);
                    csvroutes.WriteField(Paradas[i].estLink);
                    if (!string.IsNullOrEmpty(Paradas[i].estlongtitude))
                    {
                        csvroutes.WriteField(Paradas[i].estlongtitude);
                    }
                    else { csvroutes.WriteField(""); }
                    if (!string.IsNullOrEmpty(Paradas[i].estlatitude))
                    {
                        csvroutes.WriteField(Paradas[i].estlatitude);
                    }
                    else { csvroutes.WriteField(""); }                    
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
        public string estSITPNumber;
        public string estlatitude;
        public string estlongtitude;
    }

    public class Horario
    {
        public string idRuta;
        public string horario;
    }    

    public class ParadaSITP
    {
        public string name;
        public string latitude;
        public string longtitude;
    }
}
