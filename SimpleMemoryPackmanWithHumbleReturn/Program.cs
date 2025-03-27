// Список для хранения выделенных блоков,
// чтобы они не были собраны сборщиком мусора.
List<byte[]> allocatedBlocks = new List<byte[]>();

// Размер одного блока — 1 МБ.
const int blockSize = 1024 * 1024;

long eatenMemory = GC.GetTotalMemory(false);
Console.WriteLine("Начинается выделение памяти... Память до выделения: " + eatenMemory / (1024 * 1024) + " МБ");

// Выделяем, например, 10 блоков с интервалом.
for (int iteration = 0; iteration < 10; iteration++)
{
    byte[] block = new byte[blockSize];
    allocatedBlocks.Add(block);

    // Заполняем каждую страницу в блоке, чтобы гарантировать коммит
    for (int j = 0; j < block.Length; j += 4096)
    {
        block[j] = 1;
    }

    long totalMemory = GC.GetTotalMemory(false);
    Console.WriteLine($"Итерация {iteration}: выделено блоков: {allocatedBlocks.Count} МБ, общее использование памяти: {totalMemory / (1024 * 1024)} МБ");

    Thread.Sleep(1000); // Задержка для наблюдения
}

Console.WriteLine($"\nВыделение завершено. Нажмите Enter для освобождения памяти...");
Console.ReadLine();

// Освобождаем блоки, убирая ссылки
allocatedBlocks.Clear();

// Запускаем сборку мусора, чтобы попытаться вернуть память ОС
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

long finalMemory = GC.GetTotalMemory(false);
Console.WriteLine($"После освобождения памяти: общее использование памяти: {finalMemory / (1024 * 1024)} МБ");

Console.WriteLine("Нажмите Enter для завершения программы...");
Console.ReadLine();
