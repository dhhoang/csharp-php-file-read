using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MyCode;

class Program
{
    const string filePath = "/home/ec2-user/test.txt";
    // const string filePath = "/home/ec2-user/ubuntu-20.04.4-desktop-amd64.iso";

    public static int Main()
    {
        for (var i = 0; i < 10; i++)
        {
            Test_FileStream_Vectorized();
        }

        return 0;
    }

    private static void Test_StreamReader()
    {
        using var file = File.OpenText(filePath);
        var counter = 0;
        var sw = Stopwatch.StartNew();
        while (!file.EndOfStream)
        {
            if (file.Read() == '1')
            {
                counter++;
            }
        }
        sw.Stop();
        Console.WriteLine($"Counted {counter} 1s in {sw.ElapsedMilliseconds} milliseconds");
    }

    private static void Test_FileStream_Linq()
    {
        using var file = File.OpenRead(filePath);
        var counter = 0;
        var buffer = new byte[4096];
        var numRead = 0;
        var sw = Stopwatch.StartNew();
        while ((numRead = file.Read(buffer, 0, buffer.Length)) != 0)
        {
            counter += buffer.Take(numRead).Count((x) => x == '1');
        }
        sw.Stop();
        Console.WriteLine($"Counted {counter} 1s in {sw.ElapsedMilliseconds} milliseconds");
    }

    private static void Test_FileStream_NoLinq()
    {
        using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);
        var counter = 0;
        var buffer = new byte[4096];
        var numRead = 0;
        var sw = Stopwatch.StartNew();
        while ((numRead = file.Read(buffer, 0, buffer.Length)) != 0)
        {
            for (var c = 0; c < numRead; c++)
            {
                if (buffer[c] == '1')
                {
                    counter++;
                }
            }
        }
        sw.Stop();
        Console.WriteLine($"Counted {counter} 1s in {sw.ElapsedMilliseconds} milliseconds");
    }

    private static void Test_FileStream_Vectorized()
    {
        using var file = File.OpenRead(filePath);
        var counter = 0;
        var buffer = new byte[4096];
        var numRead = 0;
        var sw = Stopwatch.StartNew();
        while ((numRead = file.Read(buffer, 0, buffer.Length)) != 0)
        {
            counter += buffer.AsSpan().Slice(0, numRead).OccurrencesOf((byte)'1');
        }
        sw.Stop();
        Console.WriteLine($"Counted {counter} 1s in {sw.ElapsedMilliseconds} milliseconds");
    }
}
