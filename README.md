# RabbitMQ memory test
Note: this repository only works on Windows.

It uses:
- [Release 7.0.0-alpha.2](https://github.com/rabbitmq/rabbitmq-dotnet-client/tree/v7.0.0-alpha.2)
- [Fork: branch `prototype/reduce-memory-usage`](https://github.com/GerardSmit/rabbitmq-dotnet-client/tree/prototype/reduce-memory-usage)

## How to run
1. Run RabbitMQ locally (e.g. using Docker) on port 5672 with the default user `guest` and password `guest`.
2. Clone this repository (`git clone https://github.com/GerardSmit/RabbitMQ.MemoryTest.git`)
3. Run the commands below.

## Commands
You can run the test with `dotnet run -c Release --`. You can provide the following arguments:

| Short | Long | Description | Default |
| --- | --- | --- | --- |
| `-i` | `--iterations` | The number of iterations to run. | _required_ |
|  | `--mb` | The size of the message in MB. | _required_ |
| `-t` | `--tasks` | The number of tasks to run in parallel. | _required_ |
|  | `--nc` | Disable copying the message. | `false` |
| `-q` | `--queue` | The name of the queue to use. | `test-queue` |

To use the fork, run the command with `dotnet run -c Release -p:Fork=true --`.

## Examples
Run the test with 1 MB messages, 100 iterations and 16 tasks.
```bash
dotnet run -c Release -- --mb=1 --iterations=100 --tasks=16
```

Run the test with 1 MB messages, 100 iterations and 16 tasks, using the fork and disabling copying the message.
```bash
dotnet run -c Release -p:Fork=true -- --mb=1 --iterations=100 --tasks=16 --nc
```

## Results
The test has been run with the configuration as described in the examples above. The results are shown below.

### Official client (copying)
```
$ dotnet run -c Release -- --mb=1 --iterations=100 --tasks=16
Official version
Body size       : 1 MB
Iterations      : 100
Tasks           : 16
Non-copying     : False
Startup memory  : 20 MB

---  Start  ---
Memory usage: 22 MB
Memory usage: 1765 MB
Memory usage: 2167 MB
Memory usage: 2632 MB

--- Results ---
Avg time        : 3737 ms
Min time        : 3679 ms
Max time        : 3759 ms
Memory          : 2728 MB
Queue length    : 1600 / 1600
Valid messages  : 100 / 100 (first 100 of 1600)
```

### Fork (copying)
```
$ dotnet run -c Release -p:Fork=true -- --mb=1 --iterations=100 --tasks=16
Forked version
Body size       : 1 MB
Iterations      : 100
Tasks           : 16
Non-copying     : False
Startup memory  : 20 MB

---  Start  ---
Memory usage: 22 MB
Memory usage: 1934 MB
Memory usage: 2398 MB
Memory usage: 2819 MB

--- Results ---
Avg time        : 3447 ms
Min time        : 3384 ms
Max time        : 3489 ms
Memory          : 2854 MB
Queue length    : 1600 / 1600
Valid messages  : 100 / 100 (first 100 of 1600)
```

### Fork (non-copying)
```
$ dotnet run -c Release -p:Fork=true -- --mb=1 --iterations=100 --tasks=16 --nc
Forked version
Body size       : 1 MB
Iterations      : 100
Tasks           : 16
Non-copying     : True
Startup memory  : 20 MB

---  Start  ---
Memory usage: 22 MB
Memory usage: 61 MB
Memory usage: 61 MB
Memory usage: 61 MB

--- Results ---
Avg time        : 3252 ms
Min time        : 3211 ms
Max time        : 3277 ms
Memory          : 61 MB
Queue length    : 1600 / 1600
Valid messages  : 100 / 100 (first 100 of 1600)
```