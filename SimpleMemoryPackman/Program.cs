// Список, чтобы удерживать выделенные блоки в памяти и не дать GC их убрать.
List<byte[]> allocatedBlocks = new List<byte[]>();

// Размер одного блока — 1 МБ.
const int blockSize = 1024 * 1024;

Console.WriteLine("Начинается выделение памяти...");

int iteration = 0;
while (true)
{
    // Выделяем блок размером 1 МБ.
    byte[] block = new byte[blockSize];
    allocatedBlocks.Add(block);

    // Записываем данные в блок с шагом 4096 байт (размер страницы),
    // чтобы гарантировать коммит страницы ОС.
    for (int j = 0; j < block.Length; j += 4096)
    {
        block[j] = 1;
    }

    // Получаем общее количество памяти, выделенной под управляемые объекты.
    long totalMemory = GC.GetTotalMemory(false);
    Console.WriteLine($"Итерация {iteration}: выделено блоков: {allocatedBlocks.Count} МБ, общее использование памяти: {totalMemory / (1024 * 1024)} МБ");

    iteration++;
    // Задержка, чтобы можно было наблюдать изменение
    Thread.Sleep(1000);
}
