using System;
using System.IO;
using System.Threading;

namespace prviProjekat
{
    class Loger
    {
        private object lokLog = new object();
        private string putanjaLogFajla;

        public Loger(string putanjaLogFajla)
        {
            this.putanjaLogFajla = putanjaLogFajla;
        }

        public void Zabelezi(string poruka)
        {
            string linija = $"{DateTime.Now:HH:mm:ss.fff} [nit {Thread.CurrentThread.ManagedThreadId}] {poruka}";

            lock (lokLog)
            {
                Console.WriteLine(linija);

                try
                {
                    File.AppendAllText(putanjaLogFajla, linija + Environment.NewLine);
                }
                catch (Exception)
                {
                    //ako upis u fajl ne uspe iz nekog razloga, preskacemo ga
                    //da ne bismo srusili ceo server zbog problema sa logovanjem
                }
            }
        }
    }
}