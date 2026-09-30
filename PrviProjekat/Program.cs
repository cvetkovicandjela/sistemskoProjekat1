using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Threading;


namespace prviProjekat
{
    class Program
    {
        static HttpListener listener = new HttpListener();
        static Loger logger = new Loger("log.txt");
        static SlikaKes slikaKes = new SlikaKes(logger);
        static SemaphoreSlim semafor = new SemaphoreSlim(4, 4);

        static void Main(string[] args)
        {
            listener.Prefixes.Add("http://localhost:5050/");   //adresa na kojoj server slusa zahteve
            listener.Start();
            Console.WriteLine("Server je pokrenut na http://localhost:5050/");
            Console.WriteLine("Ceka se na zahteve...");

            while (true)
            {
                
                HttpListenerContext context = listener.GetContext(); 

                ThreadPool.QueueUserWorkItem(ObradiZahtev, context);  //server odmah moze da prima sledeci zahtev
            }
        }

        static void ObradiZahtev(object? state)
        {
            semafor.Wait();
            HttpListenerResponse? response = null;
            try
            {
                HttpListenerContext context = (HttpListenerContext)state!;
                HttpListenerRequest request = context.Request;
                response = context.Response;

                string nazivFajla = request.Url!.AbsolutePath.TrimStart('/');
                string? pragString = request.QueryString["prag"];

                logger.Zabelezi($"Zahtev za fajl: {nazivFajla}, prag: {pragString}");

                if (string.IsNullOrEmpty(nazivFajla) || string.IsNullOrEmpty(pragString) || !int.TryParse(pragString, out int prag))
                {
                    PosaljiGresku(response, "Nedostaje naziv fajla ili prag, ili prag nije validan broj.");
                    return;
                }

                string putanjaDoFajla = Path.Combine(Directory.GetCurrentDirectory(), nazivFajla);

                if (!File.Exists(putanjaDoFajla))
                {
                    PosaljiGresku(response, $"Fajl '{nazivFajla}' ne postoji na serveru.");
                    return;
                }

                string kljucKesa = $"{nazivFajla}_{prag}";

                if (slikaKes.PokusajUzmiIliZauzmi(kljucKesa, out byte[] izKesa))
                {
                    response.ContentType = "image/png";
                    response.ContentLength64 = izKesa.Length;
                    response.OutputStream.Write(izKesa, 0, izKesa.Length);
                    response.OutputStream.Close();
                    return;
                }

                Bitmap binarizovanaSlika = BinarizujSliku(putanjaDoFajla, prag);

                using (MemoryStream memStream = new MemoryStream())
                {
                    binarizovanaSlika.Save(memStream, ImageFormat.Png);
                    byte[] bajtoviSlike = memStream.ToArray();

                    slikaKes.DodajIOslobodi(kljucKesa, bajtoviSlike);

                    response.ContentType = "image/png";
                    response.ContentLength64 = bajtoviSlike.Length;
                    response.OutputStream.Write(bajtoviSlike, 0, bajtoviSlike.Length);
                    response.OutputStream.Close();
                }

                binarizovanaSlika.Dispose();
            }
            catch (Exception e)
            {
                //ovo hvata bas svaku gresku 
                Console.WriteLine($"[GRESKA] Neocekivana greska prilikom obrade zahteva: {e.Message}");

                try
                {
                    if (response != null)
                        PosaljiGresku(response, "Doslo je do neocekivane greske na serveru.");
                }
                catch
                {
                    //ako ni slanje greske ne uspe samo ignorisemo da ne obori server
                }
            }
            finally
            {
                semafor.Release();
            }
        }

        static void PosaljiGresku(HttpListenerResponse response, string poruka)
        {
            response.StatusCode = 404;
            byte[] bajtovi = System.Text.Encoding.UTF8.GetBytes(poruka);
            response.ContentLength64 = bajtovi.Length;
            response.OutputStream.Write(bajtovi, 0, bajtovi.Length);
            response.OutputStream.Close();
        }

        static Bitmap BinarizujSliku(string putanjaDoFajla, int prag)
        {
            using (Bitmap originalna = new Bitmap(putanjaDoFajla))
            {
                Bitmap rezultat = new Bitmap(originalna.Width, originalna.Height);

                for (int x = 0; x < originalna.Width; x++)
                {
                    for (int y = 0; y < originalna.Height; y++)
                    {
                        Color piksel = originalna.GetPixel(x, y);
                        int prosek = (piksel.R + piksel.G + piksel.B) / 3;

                        if (prosek < prag)
                            rezultat.SetPixel(x, y, Color.Black);
                        else
                            rezultat.SetPixel(x, y, Color.White);
                    }
                }

                return rezultat;
            }
        }
    }
}