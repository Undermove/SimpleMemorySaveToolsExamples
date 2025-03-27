internal class Program
{
    public static void Main(string[] args)
    {
        int varInStack = 123456789;
        int[] varInHeap = [42];

        unsafe
        {
            int* p = &varInStack;
            Console.WriteLine($"Address of {nameof(varInStack)}: 0x{(long)p:X} {(long)p:D}");

            fixed (int* q = varInHeap)
            {
                Console.WriteLine($"Address of {nameof(varInHeap)} : 0x{(long)q:X}    {(long)q:D} ");
            }
        }
    }
}