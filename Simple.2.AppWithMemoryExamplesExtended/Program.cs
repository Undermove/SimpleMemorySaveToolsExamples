internal class Program
{
    public static void Main(string[] args)
    {
        int varInStack = 123456789;
        int[] varInHeap = new int[1];
        varInHeap[0] = 42;

// Работа с переменной на стеке.
        unsafe
        {
            int* p = &varInStack;
            long addrStack = (long)p;
            Console.WriteLine($"Address of {nameof(varInStack)}: 0x{addrStack:X} ({addrStack:D})");
    
            // Вычисляем компоненты виртуального адреса для 4-уровневой таблицы страниц:
            long pml4 = (addrStack >> 39) & 0x1FF;
            long pdpt = (addrStack >> 30) & 0x1FF;
            long pd   = (addrStack >> 21) & 0x1FF;
            long pt   = (addrStack >> 12) & 0x1FF;
            long offset = addrStack & 0xFFF;
    
            Console.WriteLine("Partitions for varInStack:");
            Console.WriteLine($"  PML4:   0x{pml4:X} ({pml4:D})");
            Console.WriteLine($"  PDPT:   0x{pdpt:X} ({pdpt:D})");
            Console.WriteLine($"  PD:     0x{pd:X} ({pd:D})");
            Console.WriteLine($"  PT:     0x{pt:X} ({pt:D})");
            Console.WriteLine($"  Offset: 0x{offset:X} ({offset:D})");
    
            // Работа с переменной на куче.
            fixed (int* q = varInHeap)
            {
                long addrHeap = (long)q;
                Console.WriteLine($"\nAddress of {nameof(varInHeap)}: 0x{addrHeap:X} ({addrHeap:D})");
        
                long pml4_heap = (addrHeap >> 39) & 0x1FF;
                long pdpt_heap = (addrHeap >> 30) & 0x1FF;
                long pd_heap   = (addrHeap >> 21) & 0x1FF;
                long pt_heap   = (addrHeap >> 12) & 0x1FF;
                long offset_heap = addrHeap & 0xFFF;
        
                Console.WriteLine("Partitions for varInHeap:");
                Console.WriteLine($"  PML4:   0x{pml4_heap:X} ({pml4_heap:D})");
                Console.WriteLine($"  PDPT:   0x{pdpt_heap:X} ({pdpt_heap:D})");
                Console.WriteLine($"  PD:     0x{pd_heap:X} ({pd_heap:D})");
                Console.WriteLine($"  PT:     0x{pt_heap:X} ({pt_heap:D})");
                Console.WriteLine($"  Offset: 0x{offset_heap:X} ({offset_heap:D})");
            }
        }

        Console.WriteLine($"\nHello, World! {varInStack}");
    }
}

