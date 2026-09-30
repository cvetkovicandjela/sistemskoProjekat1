using System;
using System.Net.Http;
using System.Threading.Tasks;

Console.WriteLine("Saljem 10 zahteva istovremeno...");

HttpClient client = new HttpClient();
Task[] taskovi = new Task[10];

for (int i = 0; i < 10; i++)
{
    int brojZahteva = i;
    taskovi[i] = Task.Run(async () =>
    {
        var odgovor = await client.GetAsync("http://localhost:5050/slika.jpg?prag=128");
        Console.WriteLine($"Zahtev {brojZahteva} zavrsen, status: {odgovor.StatusCode}");
    });
}

Task.WaitAll(taskovi);
Console.WriteLine("Svi zahtevi zavrseni.");