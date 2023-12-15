using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Channels;
using CommandLine;
using RabbitMQ.Client;

var parser = Parser.Default.ParseArguments<Options>(args);

if (parser.Value is not {} options)
{
	return;
}

#if !FORK
if (options.NonCopying)
{
	Console.WriteLine("Non-copying is only supported in the forked version");
	return;
}
#endif

var messageBody = RandomNumberGenerator.GetBytes(options.MessageBodySize * 1024 * 1024);

var process = Process.GetCurrentProcess();
var counter = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName);

var expectedCount = options.Iterations * options.Tasks;
var elapsedTimes = new List<TimeSpan>();
var tasks = new List<Task>();

var cancellationTokenSource = new CancellationTokenSource();

#if FORK
Console.WriteLine("Forked version");
#else
Console.WriteLine("Official version");
#endif

Console.WriteLine($"Body size	: {options.MessageBodySize} MB");
Console.WriteLine($"Iterations	: {options.Iterations}");
Console.WriteLine($"Tasks		: {options.Tasks}");
Console.WriteLine($"Non-copying	: {options.NonCopying}");
Console.WriteLine($"Startup memory	: {(int)(counter.NextValue() / 1024 / 1024)} MB");

Console.WriteLine();
Console.WriteLine("---  Start  ---");

var thread = new Thread(() =>
{
	while (!cancellationTokenSource.IsCancellationRequested)
	{
		var mb = (int)(counter.NextValue() / 1024 / 1024);
		Console.WriteLine($"Memory usage: {mb} MB");
		Thread.Sleep(1000);
	}
});

thread.Start();

var connectionFactory = new ConnectionFactory();

using (var connection = await connectionFactory.CreateConnectionAsync())
using (var channel = await connection.CreateChannelAsync())
{
	channel.QueueDelete(options.QueueName);
}

for (var i = 0; i < options.Tasks; i++)
{
	tasks.Add(Task.Run(async () =>
	{
		var stopwatch = Stopwatch.StartNew();

		using (var connection = await connectionFactory.CreateConnectionAsync())
		using (var channel = await connection.CreateChannelAsync())
		{
			channel.QueueDeclare(options.QueueName, durable: false, exclusive: false, autoDelete: true, arguments: null);

			for (var j = 0; j < options.Iterations; j++)
			{
				await channel.BasicPublishAsync(
					string.Empty,
					options.QueueName,
					messageBody
#if FORK
					, copyBody: !options.NonCopying
#endif
				);
			}
		}

		stopwatch.Stop();
		elapsedTimes.Add(stopwatch.Elapsed);
	}));
}

await Task.WhenAll(tasks);

cancellationTokenSource.Cancel();
thread.Join();

Console.WriteLine();
Console.WriteLine("--- Results ---");
if (elapsedTimes.Count > 1)
{
	Console.WriteLine($"Avg time	: {(int)elapsedTimes.Average(x => x.TotalMilliseconds)} ms");
	Console.WriteLine($"Min time	: {(int)elapsedTimes.Min(x => x.TotalMilliseconds)} ms");
	Console.WriteLine($"Max time	: {(int)elapsedTimes.Max(x => x.TotalMilliseconds)} ms");
}
else
{
	Console.WriteLine($"Time		: {(int)elapsedTimes[0].TotalMilliseconds} ms");
}

Console.WriteLine($"Memory		: {(int)(counter.NextValue() / 1024 / 1024)} MB");

using (var connection = await connectionFactory.CreateConnectionAsync())
using (var channel = await connection.CreateChannelAsync())
{
	var count = channel.MessageCount(options.QueueName);

	Console.WriteLine($"Queue length	: {count} / {expectedCount}");

	var checkCount = Math.Min(count, 100);
	var correctCount = 0;

	Console.Write("Valid messages	: ");

	for (var i = 0; i < checkCount; i++)
	{
		var result = await channel.BasicGetAsync(options.QueueName, true);

		if (result.Body.Span.SequenceEqual(messageBody))
		{
			correctCount++;
		}
	}

	Console.Write($"{correctCount} / {checkCount}");

	if (checkCount == 100)
	{
		Console.Write($" (first 100 of {count})");
	}

	Console.WriteLine();

	channel.QueueDelete(options.QueueName);
}

class Options
{
	[Option('i', "iterations", Required = true, HelpText = "Number of iterations")]
	public int Iterations { get; set; }

	[Option("mb", Required = true, HelpText = "Message body size in megabytes")]
	public int MessageBodySize { get; set; }

	[Option('t', "tasks", Required = true, HelpText = "Number of tasks to run")]
	public int Tasks { get; set; }

	[Option("nc", HelpText = "Don't copy the message body")]
	public bool NonCopying { get; set; }

	[Option('q', "queue", Default = "test-queue", HelpText = "Queue name")]
	public string QueueName { get; set; } = "test-queue";
}