namespace Looper
{
    public static class Program
    {
        public static void Main()
        {
            using (Window w = new Window())
                w.Run(60);
        }
    }
}