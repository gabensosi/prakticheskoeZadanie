using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace Michalev
{ 
internal class Data
{
    public string Error { get; set; }
    public IList<Logs> Logs { get; set; }
    public void QuickSort()
    {
        this.Logs = QuickSort(this.Logs, 0, this.Logs.Count - 1);
    }
    private IList<Logs> QuickSort(IList<Logs> logs, int minIndex, int maxIndex)
    {
        if (minIndex >= maxIndex)
            return logs;
        var pivotIndex = Split(logs, minIndex, maxIndex);
        QuickSort(logs, minIndex, pivotIndex - 1);
        QuickSort(logs, pivotIndex + 1, maxIndex);

        return logs;
    }
    private int Split(IList<Logs> logs, int minIndex, int maxIndex)
    {
        var pivot = minIndex - 1;
        for (var i = minIndex; i < maxIndex; i++)
        {
            if (logs[i].Created_at.Ticks < logs[maxIndex].Created_at.Ticks)
            {
                pivot++;
                Swap(pivot, i);
            }
        }
        pivot++;
        Swap(pivot, maxIndex);
        return pivot;
    }
    private void Swap(int x, int y)
    {
        var temp = this.Logs[x];
        this.Logs[x] = this.Logs[y];
        this.Logs[y] = temp;
    }
}

internal class Logs
{
    public DateTime Created_at { get; set; }
    public string First_name { get; set; }
    public string Message { get; set; }
    public string Second_name { get; set; }
    public string User_id { get; set; }
}

internal class Program
{
    private static readonly string sqliteConnectionString = @"Logs.db";
    public static string date = "";

    private static string GetData(string uri)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
        request.ContentType = @"application/json;charset=""utf-8""";
        request.Method = "GET";
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
        {
            return reader.ReadToEnd();
        }
    }

    private static void Main(string[] args)
    {


        if (args.Length == 0)
        {
            Console.WriteLine("Define date:");
            date = Console.ReadLine();
        }
        else date = args[0];
        if (date.Equals(""))
            throw new ArgumentException("No date argument");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine("Argument: " + date);
        Console.WriteLine("Sending HTTP request..");
        string data = GetData("http://www.dsdev.tech/logs/" + date);
        Console.WriteLine("Done.\nParsing HTTP response..");
        Data myData = new Data();
        myData = JsonSerializer.Deserialize<Data>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (!myData.Error.Equals(""))
            throw new ArgumentException("Wrong date argument");
        Console.WriteLine("Done.\nStarting sort..");
#if DEBUG
        var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
        myData.QuickSort();
#if DEBUG
        sw.Stop();
        Console.WriteLine("My quickort took {0} milliseconds", sw.Elapsed.TotalMilliseconds.ToString("#,##0.00"));
        Data myData2 = new Data();
        myData2 = JsonSerializer.Deserialize<Data>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        sw = System.Diagnostics.Stopwatch.StartNew();
        myData2.Logs = myData.Logs.OrderBy(x => x.Created_at).ToArray();
        sw.Stop();
        Console.WriteLine("Internal sort took {0} milliseconds", sw.Elapsed.TotalMilliseconds.ToString("#,##0.00"));

#endif
        Console.WriteLine("Done\nInserting into db..");
        SQLiteConnection myDb = new SQLiteConnection(sqliteConnectionString, false);
        myDb.InsertAll(myData.Logs);
        myDb.Close();
        stopwatch.Stop();

        Console.WriteLine("Done.\nFinished all Jobs.\nFull run taked {0} milliseconds\nPress Enter to exit", stopwatch.Elapsed.TotalMilliseconds.ToString("#,##0.00"));
        _ = Console.ReadLine();
        return;
    }
}
}
