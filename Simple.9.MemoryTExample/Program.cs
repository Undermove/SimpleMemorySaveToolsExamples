using System.Diagnostics;
using System.Text;

internal class Program
{
    static void Main()
    {
        // Генерируем большой текст: например, 100 000 строк, разделённых символом новой строки.
        int lineCount = 100_000;
        string delimiter = "\n";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lineCount; i++)
        {
            sb.Append("Message number " + i);
            sb.Append(delimiter);
        }
        string data = sb.ToString();
        byte[] buffer = Encoding.UTF8.GetBytes(data);

        
        Process current = Process.GetCurrentProcess();
        // Принудительная сборка мусора для чистоты замеров
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        current.Refresh();
        Console.WriteLine("Начальное использование памяти (WorkingSet): {0:F2} МБ", GetMemoryMb(current));
        
        
        // Вариант 1: Обработка с копированием – для каждого сообщения создается новый массив.
        // int count = ProcessMessagesByCopying(buffer, delimiter);

        // Вариант 2: Обработка с использованием Memory<T> – срез без копирования.
        int count = ProcessMessagesByMemory(buffer, delimiter);

        current.Refresh();
        Console.WriteLine("Память после выполнения метода: {0:F2} МБ", GetMemoryMb(current));
        Console.ReadLine();
    }

    // Метод, обрабатывающий сообщения с копированием данных.
    static int ProcessMessagesByCopying(byte[] buffer, string delimiter)
    {
        int count = 0;
        int start = 0;
        byte delim = (byte)'\n';
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == delim)
            {
                int length = i - start;
                // Создаем новый массив для сообщения (копирование)
                byte[] message = new byte[length];
                Array.Copy(buffer, start, message, 0, length);
                // Можно обработать сообщение, например, декодировать его:
                string msg = Encoding.UTF8.GetString(message);
                count++;
                start = i + 1;
            }
        }
        return count;
    }

    // Метод, обрабатывающий сообщения без копирования с использованием Memory<T>.
    static int ProcessMessagesByMemory(byte[] buffer, string delimiter)
    {
        int count = 0;
        int start = 0;
        byte delim = (byte)'\n';
        Memory<byte> memoryBuffer = buffer; // Оборачиваем массив в Memory<byte>
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == delim)
            {
                int length = i - start;
                // Получаем срез без копирования
                Memory<byte> message = memoryBuffer.Slice(start, length);
                // Для обработки можно, например, декодировать: 
                string msg = Encoding.UTF8.GetString(message.Span);
                count++;
                start = i + 1;
            }
        }
        return count;
    }
    
    // Метод для получения использования памяти процесса в мегабайтах.
    private static double GetMemoryMb(Process process)
    {
        return process.WorkingSet64 / (1024.0 * 1024.0);
    }
}
