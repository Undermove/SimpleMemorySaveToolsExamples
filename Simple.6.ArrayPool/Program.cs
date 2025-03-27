using System.Buffers;
using System.Diagnostics;

namespace Simple._6.ArrayPool;

internal class Program
{
    static void Main()
    {
        int arraySize = 1024;        // размер каждого массива: 1 КБ
        int iterations = 10240;      // количество итераций для теста

        // Инициализируем два массива с начальными данными.
        byte[] first = new byte[arraySize];
        byte[] second = new byte[arraySize];
        for (int i = 0; i < arraySize; i++)
        {
            first[i] = 1;
            second[i] = 2;
        }

        Process current = Process.GetCurrentProcess();
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        current.Refresh();
        Console.WriteLine("Начальное использование памяти: {0:F2} МБ", GetMemoryMb(current));

        // Вариант 2: С использованием ArrayPool
        // ConcatExample(iterations, first, second, arraySize);
        ConcatWithArrayPoolExample(iterations, first, second, arraySize);
        current.Refresh();
        Console.WriteLine("Память после склеивания: {0:F2} МБ", GetMemoryMb(current));
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        

        Console.WriteLine("Нажмите Enter для завершения...");
        Console.ReadLine();
    }

    private static void ConcatExample(int iterations, byte[] first, byte[] second, int arraySize)
    {
        for (int i = 0; i < iterations; i++)
        {
            byte[] result = first.Concat(second).ToArray();

            if (result[0] != 1 || result[arraySize] != 2)
                throw new Exception("Неверное значение");
            if(i == result.Length - 1) {Console.WriteLine(result[^1]);}
        }
    }
    
    private static void ConcatWithArrayPoolExample(int iterations, byte[] first, byte[] second, int arraySize)
    {
        for (int i = 0; i < iterations; i++)
        {
            int totalLength = first.Length + second.Length;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(totalLength);
            Buffer.BlockCopy(first, 0, buffer, 0, first.Length);
            Buffer.BlockCopy(second, 0, buffer, first.Length, second.Length);

            if (i == totalLength - 1)
            {
                Console.WriteLine(buffer[^1]);
            }
            
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static double GetMemoryMb(Process process)
    {
        return process.WorkingSet64 / (1024.0 * 1024.0);
    }
}