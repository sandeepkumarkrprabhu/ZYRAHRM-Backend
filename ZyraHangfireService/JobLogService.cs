namespace ZyraHangfireService
{
    public class JobLogService
    {
        public void CheckIn()
        {
            Console.WriteLine($"Job executed at {DateTime.Now}");
        }

        public void CheckOut()
        {
            Console.WriteLine($"Job executed at {DateTime.Now}");
        }
    }
}
