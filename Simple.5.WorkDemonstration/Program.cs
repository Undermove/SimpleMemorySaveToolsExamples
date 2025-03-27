class Program
{
    static void Main()
    {
        // Шаг 0: Вывод начального использования памяти
        Console.WriteLine("Начальное использование памяти: {0} МБ", GetMemoryUsageMB());

        // Шаг 1: Выделение 10 МБ для объекта, который затем выйдет из области видимости
        AllocateAndReleaseObject();
        // После выхода из метода объект больше не достижим
        Console.WriteLine("После вызова метода (объект вышел из стека): {0} МБ", GetMemoryUsageMB());

        // Шаг 2: Выделение 10 МБ для объекта, который остается в памяти (ссылка не теряется)
        byte[] persistentObject = new byte[10 * 1024 * 1024]; // 10 МБ
        // Записываем данные, чтобы избежать оптимизации
        persistentObject[0] = 1;
        persistentObject[^1] = 2;
        Console.WriteLine("После постоянной аллокации (объект остается в памяти): {0} МБ", GetMemoryUsageMB());

        // Шаг 3: Вызов сборщика мусора
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Console.WriteLine("После GC.Collect(): {0} МБ", GetMemoryUsageMB());

        Console.WriteLine("Нажмите Enter для завершения...");
        Console.ReadLine();
    }

    // Метод, который выделяет 10 МБ и затем выходит,
    // делая объект недостижимым для GC.
    static void AllocateAndReleaseObject()
    {
        byte[] tempObject = new byte[10 * 1024 * 1024]; // 10 МБ
        // Запись в начало и конец массива, чтобы объект точно был аллоцирован
        tempObject[0] = 1;
        tempObject[^1] = 2;
        // После завершения метода переменная tempObject выходит из области видимости,
        // и объект становится кандидатом для сборки мусора.
    }

    // Метод для получения общего объёма управляемой памяти в МБ
    static string GetMemoryUsageMB()
    {
        long bytes = GC.GetTotalMemory(false);
        double mb = bytes / (1024.0 * 1024.0);
        return mb.ToString("F2");
    }
}
