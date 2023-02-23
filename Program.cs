using System.Diagnostics;

namespace TestParallelHttpRequests
{
    public class Program
    {
        private const int RequestsCount = 999;
        private const int ThreadsCount = 100;

        private const string BaseAddr = "https://limiter.moontrader.com";  // "https://moontrader.com";
        private const string SiteUrl = "/index10.bin";  // "/"

        private readonly HttpClient _client = new HttpClient()
        {
            BaseAddress = new Uri(BaseAddr)
        };

        private readonly ManualResetEvent _completeEvent = new(false);

        private volatile int _requestNum = 0;  // First request is #1
        private volatile int _threadsCount = ThreadsCount;

        public static void Main()
        {
            new Program().RunParallelRequests();
        }

        private void RunParallelRequests()
        {
            new Thread(WatcherLoop).Start();

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < ThreadsCount; i++)
            {
                new Thread(ThreadLoop).Start();
            }

            _completeEvent.WaitOne();

            Log($"Complete: {sw.Elapsed:mm':'ss}");
        }

        private void ThreadLoop()
        {
            while (true)
            {
                int num = Interlocked.Increment(ref _requestNum);
                if (num > RequestsCount)
                {
                    break;
                }

                SendRequest(num);
            }

            if (Interlocked.Decrement(ref _threadsCount) == 0)
            {
                _completeEvent.Set();
            }
        }

        private void SendRequest(int num)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, SiteUrl);

            Log($"#{num,-3} Before Send");
            var sw = Stopwatch.StartNew();

            using var response = _client.Send(request);
            var content = response.Content.ReadAsStringAsync().Result;

            sw.Stop();
            Log($"#{num,-3} After Send: {sw.ElapsedMilliseconds:#,##0} ms");
        }

        private static volatile int _loggerId;

        private static void Log(string msg)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | {Environment.CurrentManagedThreadId,3} | {msg}");
            Interlocked.Increment(ref _loggerId);
        }

        private void WatcherLoop()
        {
            var prevLoggerId = _loggerId;

            while (!_completeEvent.WaitOne(3_000))
            {
                if (_loggerId == prevLoggerId)
                {
                    Log("!!! No logger activity in last 3 seconds");
                }
                prevLoggerId = _loggerId;
            }
        }

    }
}