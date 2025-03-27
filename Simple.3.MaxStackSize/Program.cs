using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    [DllImport("libc")]
    private static extern IntPtr pthread_self();

    [DllImport("libc")]
    private static extern IntPtr pthread_get_stackaddr_np(IntPtr thread);

    [DllImport("libc")]
    private static extern IntPtr pthread_get_stacksize_np(IntPtr thread);

    static void Main()
    {
        IntPtr mainThreadSelf = pthread_self();
        IntPtr mainThreadStackAddr = pthread_get_stackaddr_np(mainThreadSelf);
        IntPtr mainThreadStackSize = pthread_get_stacksize_np(mainThreadSelf);

        Console.WriteLine($"Верхняя граница стека главного потока: 0x{mainThreadStackAddr:X}");
        Console.WriteLine($"Размер стека главного потока: {mainThreadStackSize.ToInt64()/1024/1024} Мб");
        
        ManualResetEventSlim mre = new ManualResetEventSlim(false);
        Span<long> largeArrayOnStack = stackalloc long[7*1024 * 1024 / sizeof(long)];
        Thread thread = new Thread(o =>
        {
            IntPtr localThreadSelf = pthread_self();
            IntPtr localThreadStackAddr = pthread_get_stackaddr_np(localThreadSelf);
            IntPtr localThreadStackSize = pthread_get_stacksize_np(localThreadSelf);
            
            // занимаем 1 мегабайт на стеке
            Span<long> largeArrayOnLocalStack = stackalloc long[1024 * 1024 / sizeof(long)];

            Console.WriteLine($"Верхняя граница стека: 0x{localThreadStackAddr:X}");
            Console.WriteLine($"Размер стека: {localThreadStackSize.ToInt64()/1024/1024} Мб");
            mre.Set();
        });
        
        thread.Start();
        mre.Wait();
        
        Process current = Process.GetCurrentProcess();
        Console.WriteLine($"Working Set: {current.WorkingSet64/1024/1024} Мб");
        Console.WriteLine($"Virtual Memory Size: {current.VirtualMemorySize64/1024/1024} Мб");
        Console.ReadLine();
    }
}