class Program
{
    static unsafe void Main(string[] args)
    {
        var thread = new Thread(() =>
        {
            // Создаем переменную в главном потоке до вызова метода
            int intMain1 = 123456789;
            Console.WriteLine($"Main: Address of varMain1: 0x{(long)&intMain1:X} {(long)&intMain1:D}");

            // Вызываем метод, который создает новый стекфрейм
            DemonstrateStackFrame();

            // Создаем переменную в главном потоке после возврата из метода
            int intMain2 = 987654321;
            Console.WriteLine($"\nMain: Address of varMain2: 0x{(long)&intMain2:X} {(long)&intMain2:D}");
        });
        thread.Start();
        Console.ReadLine();
    }

    static unsafe void DemonstrateStackFrame()
    {
        Console.WriteLine("\nInside DemonstrateStackFrame:");

        // Выделяем на стеке большой блок ~900 КБ
        Span<byte> largeBlock = stackalloc byte[900 * 1024];

        // Инициализируем часть выделенной памяти, чтобы предотвратить оптимизации
        for (int i = 0; i < 900 * 1024; i += 4096)
        {
            largeBlock[i] = (byte)(i % 256);
        }

        // Создаем переменную после выделения большого блока в этом методе
        int varInMethod = 55555555;
        Console.WriteLine($"Inside method: Address of varInMethod: 0x{(long)&varInMethod:X} {(long)&varInMethod:D}");
    }
}