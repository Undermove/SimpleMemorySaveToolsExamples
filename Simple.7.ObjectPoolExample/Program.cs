using System.Diagnostics;
using Microsoft.Extensions.ObjectPool;

namespace Simple._7.ObjectPoolExample;

internal class Program
{
    private static readonly DefaultObjectPool<ReusableObject> Pool = new(new ReusableObjectPolicy(), maximumRetained: 50);
    static void Main()
    {
        int iterations = 10000;

        Process current = Process.GetCurrentProcess();
        // Очистка памяти перед началом теста
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        current.Refresh();
        Console.WriteLine("Начальное использование памяти: {0:F2} МБ", GetMemoryMb(current));
        
        // Вариант 1: Создание нового объекта в каждой итерации (без ObjectPool)
        //DoWorkWithoutPool(iterations);

        // Вариант 2: Переиспользование объектов через ObjectPool
        DoWorkWithPool(iterations);

        // Измеряем память после выполнения
        current.Refresh();
        Console.WriteLine("Память после работы: {0:F2} МБ", GetMemoryMb(current));

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        current.Refresh();
        Console.WriteLine("Память после GC: {0:F2} МБ", GetMemoryMb(current));

        Console.WriteLine("Нажмите Enter для завершения...");
        Console.ReadLine();
    }

    // Вариант без использования ObjectPool: создаётся новый объект для каждой итерации.
    private static void DoWorkWithoutPool(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            var obj = new ReusableObject();
            obj.DoWork();
            // Объект не сохраняется – после итерации он становится кандидатом на сборку мусора.
        }
    }

    // Вариант с использованием ObjectPool: объект арендуется из пула, используется, а затем возвращается.
    private static void DoWorkWithPool(int iterations)
    {
        // Создаем пул объектов с использованием стандартной политики.
        for (int i = 0; i < iterations; i++)
        {
            var obj = Pool.Get();
            obj.DoWork();
            Pool.Return(obj);
        }
    }

    // Метод для получения использования памяти процесса в мегабайтах.
    private static double GetMemoryMb(Process process)
    {
        return process.WorkingSet64 / (1024.0 * 1024.0);
    }
}

// Простой класс, представляющий «тяжёлый» объект, который мы будем использовать для демонстрации.
// Здесь он содержит буфер размером 1 КБ и имитирует некоторую работу.
public class ReusableObject
{
    private byte[] buffer = new byte[1024]; // 1 КБ

    public void DoWork()
    {
        // Имитируем обработку: заполняем массив данными.
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(i % 256);
        }
    }

    // Метод для сброса состояния объекта перед возвращением в пул.
    public void Reset()
    {
        // Например, очищаем буфер.
        Array.Clear(buffer, 0, buffer.Length);
    }
}

// Собственная политика пула, которая вызывает Reset() перед возвратом объекта.
public class ReusableObjectPolicy : PooledObjectPolicy<ReusableObject>
{
    public override ReusableObject Create() => new();

    public override bool Return(ReusableObject obj)
    {
        obj.Reset();
        return true;
    }
}