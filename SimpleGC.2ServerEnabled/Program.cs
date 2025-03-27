namespace SimpleGCServerEnabled;

class Program
{
    // Список для хранения выделенных объектов, чтобы они не были собраны GC
    static List<object> allocatedObjects = new List<object>();

    static void Main()
    {
        // Вывод информации о режиме сборщика мусора
        Console.WriteLine("Server GC mode: " + System.Runtime.GCSettings.IsServerGC);

        // Количество процессорных ядер
        int processorCount = Environment.ProcessorCount;
        Console.WriteLine("Количество процессорных ядер: " + processorCount);

        // Создаём и запускаем потоки, равное числу ядер
        List<Thread> threads = new List<Thread>();

        for (int i = 0; i < processorCount; i++)
        {
            int threadIndex = i; // локальная копия для замыкания
            Thread t = new Thread(() => AllocateUniqueArray(threadIndex));
            t.Start();
            threads.Add(t);
        }

        // Ждём завершения всех потоков
        foreach (var t in threads)
        {
            t.Join();
        }

        // Вывод информации о выделенных массивах
        Console.WriteLine("\nВыделенные массивы:");
        for (int i = 0; i < allocatedObjects.Count; i++)
        {
            int[] arr = allocatedObjects[i] as int[];
            Console.WriteLine($"Поток {i}: длина массива = {arr.Length}");
        }

        Console.WriteLine("\nНажмите Enter для выхода...");
        Console.ReadLine();
    }

    static void AllocateUniqueArray(int threadIndex)
    {
        // Определяем размер массива для каждого потока по-разному.
        // Например, поток с индексом 0 получит массив из 100_000 элементов,
        // поток с индексом 1 – из 200_000, и так далее.
        int arraySize = (threadIndex + 1) * 100_000;
        int[] array = new int[arraySize];

        // Заполняем массив значениями, характерными для данного потока
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = threadIndex;
        }

        // Сохраняем выделенный массив в общий список
        lock (allocatedObjects)
        {
            allocatedObjects.Add(array);
        }

        Console.WriteLine($"Поток {threadIndex} выделил массив из {arraySize} элементов");
    }
}