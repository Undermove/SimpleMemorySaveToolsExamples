using System.Diagnostics;


internal class Program
{
    static void Main()
    {
        Process current = Process.GetCurrentProcess();
        // Принудительная сборка мусора для чистоты замеров
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        current.Refresh();
        Console.WriteLine("Начальное использование памяти (WorkingSet): {0:F2} МБ", GetMemoryMb(current));

        // Создаем большую строку. Например, 5 млн символов ~ 10 МБ (каждый char занимает 2 байта).
        int largeStringLength = 5 * 1024 * 1024;
        string largeString = new string('A', largeStringLength);

        // Метод, который аллоцирует в куче:
        // MethodWithHeapAllocation(largeString);

        // Метод, который использует Span
        MethodWithStackAllocation(largeString);


        // Принудительная сборка мусора и обновление информации
        current.Refresh();
        Console.WriteLine("Память после выполнения метода: {0:F2} МБ", GetMemoryMb(current));
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Console.WriteLine("Нажмите Enter для завершения...");
        Console.ReadLine();
    }

    // Метод, который аллоцирует 10 МБ памяти в куче через new
    static void MethodWithHeapAllocation(string largeString)
    {
        // 7 МБ
        for (int i = 0; i < 100; i++)
        {
            // Извлекаем подстроку длиной 100000 символов, начиная с позиции 1000
            string part = largeString.Substring(1000, 100000);
            // Используем часть, чтобы избежать оптимизаций
            if (part[0] != 'A')
                throw new Exception("Неверное значение");
        }
    }

    // Метод, который использует stackalloc и Span<T> для выделения 10 МБ памяти на стеке
    static void MethodWithStackAllocation(string largeString)
    {
        for (int i = 0; i < 100; i++)
        {
            ReadOnlySpan<char> spanPart = largeString.AsSpan().Slice(1000, 100000);
            if (spanPart[0] != 'A')
                throw new Exception("Неверное значение");
        }
    }

    // Метод для получения использования памяти процесса в мегабайтах.
    private static double GetMemoryMb(Process process)
    {
        return process.WorkingSet64 / (1024.0 * 1024.0);
    }
}