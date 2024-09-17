# AsyncEvents library
Async Events library helps to implement delagate/event publisher/subscriber pattern using fully `async` methods (delegates).


## Usage
1. Add nuget
	.NET CLI
	```
	dotnet add package AsyncEvents --version 1.0.0
	```
	or via Package Manager
	```
	NuGet\Install-Package AsyncEvents -Version 1.0.0
	```
1. Add `using AsyncEvents;` to the source code
1. Declare `event` in your class `EventHandlerAsync` for events without parameters (except sender) or 
`EventHandlerAsync<TEventArgs>` for events with additional parameters. For example:
    ```
    public event EventHandlerAsync OnLongRunningTaskFinished;
    public event EventHandlerAsync<int>? OnEventCounting;
    ```
    Please note that `TEventArgs` does not need to inherit from `EventArgs` as it. This is the difference compared to synchrounous type `System.EventHandler<System.EventArgs>`
3. In method where event should be sent use method `InvokeAsync`. For example:
```
public async Task RaiseCountingEventOneAwaitsOtherAsync(int value)
{
    // Using library
    await (OnEventCounting?.InvokeAsync(this, value, true) ?? Task.CompletedTask).ConfigureAwait(false);
}
```
Full example:

```
using AsyncEvents;

private class EventPublisherAsync
{
    public event EventHandlerAsync? OnLongRunningTaskFinished;
    public event EventHandlerAsync<int>? OnEventCounting;


    /// <summary>
    /// Raises (invokes) the event OnEventCounting for the specified value.
    /// It calls the subscribers one by one (as normal delegate/event would do), but it awaits each delegate instance (subscriber handler)
    /// </summary>
    /// <param name="value">Value which will be sent to subscriber</param>
    /// <returns></returns>
    public async Task RaiseCountingEventOneAwaitsOtherAsync(int value)
    {
        // Using library
        await (OnEventCounting?.InvokeAsync(this, value, true) ?? Task.CompletedTask).ConfigureAwait(false);
    }

    /// <summary>
    /// Raises (invokes) the event OnLongRunningTaskFinished event ater the task is finished ( duration is defined by input <paramref name="taskDuration"/> )
    /// It calls the subscribers one by one (as normal delegate/event would do).
    /// </summary>
    /// <param name="taskDuration"></param>
    /// <returns></returns>
    public async Task StartLongRunningTaskOneAwaitsOtherAsync(TimeSpan taskDuration)
    {
        Console.WriteLine($"Long Running Task started with duration {taskDuration.TotalSeconds} seconds.");
        // Simulate long running task
        await Task.Delay(taskDuration).ConfigureAwait(false);
        // Notify subscribers
        //Handlers are started sequentionally, but and awaited.
        // Using library
        await (OnLongRunningTaskFinished?.InvokeAsync(this, false) ?? Task.CompletedTask).ConfigureAwait(false);
    }
}
```


# Delegates/Events info
Demonstrating delegate/event with asynchronous programming. If you are interested in final solution, without explanation 
how it works under the hoods and "not to do" with async delagates, refer to library [AsyncEvents](./AsyncEvents/Readme.md)
Information about delegates/events multicast, which inspired this example can be found here:

1. https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/delegates/
1. https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/events/
1. https://stackoverflow.com/questions/73443885/when-adding-an-async-delegate-to-an-event-how-to-make-the-process-continue-afte
1. https://stackoverflow.com/questions/12451609/how-to-await-raising-an-eventhandler-event
1. https://blog.stephencleary.com/2013/02/async-oop-5-events.html
1. https://www.c-sharpcorner.com/UploadFile/vendettamit/delegates-and-async-programming/
1. https://github.com/microsoft/vs-threading/blob/main/src/Microsoft.VisualStudio.Threading/README.md


## Demo0 - synchronous (delegate void)
This demo demonstrates using `System.EventHandler` in synchronous manner.
It demonstrates that handlers/subscribers  subscibed to publisher using `eventPublisher.OnEventCounting +=  (sender, value) => ...`

Main principle is below (one subscriber and on event)
1. `Publisher` enables to subscribe event `OnEventCounting` and for demo purposes enables to trigger the event using `RaiseCountingEvent()`
1. In the `Main()` method we subscribe using `eventPublisher.OnEventCounting +=` and than asks publisher to simulated event by `eventPublisher.RaiseCountingEvent(123)`

```
namespace System
{
    public delegate void EventHandler<TEventArgs>(object? sender, TEventArgs e);
}

class Publisher
{
    public event EventHandler<int>? OnEventCounting;

    public void  RaiseCountingEvent(int value)
    {
        OnEventCounting?.Invoke(this, value);
    }
}

static Main()
{
    var eventPublisher = new EventPublisher();
    eventPublisher.OnEventCounting +=  (sender, value) => Console.WriteLine($"OnEventCounting({value}) received (subscriber 1)");
    eventPublisher.RaiseCountingEvent(123);
}
```

The full demo below is demonstrating multiple events and multiple subscribers and faulty handler(s) which throws an exception.
As described in MS documetation, handlers are called/processed sequentially in the same order as they were subscribed.
Main method triggers via publisher the event OnEventCounting 1 .. 5 with 1 second delay.
In this example second subscriber is faulty (raises exception `throw new Exception("Exception in subscriber 2")`) and it causes that program stops and only first handler finish processing.
Handlers 3,4,5 are not triggered at all.

### Source code
```
namespace EventDemo
{
    internal class ProgramEventDemo
    {
        static void  Main(string[] args)
        {
            try
            {
                Console.WriteLine("App started");

                var eventPublisher = new EventPublisher();

                Console.WriteLine("Subscribing event handlers");

                // Subscribe to the event OnLongRunningTaskFinished
                eventPublisher.OnLongRunningTaskFinished += (sender, args) =>
                {
                    Console.WriteLine("***Long running task finished***");
                };

                // Subscribe to the event OnEventCounting first subscriber
                eventPublisher.OnEventCounting +=  (sender, value) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 1)");

                };

                // Subscribe to the event OnEventCounting second subscriber
                eventPublisher.OnEventCounting +=  (sender, value) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 2) - raising Exception");
                    throw new Exception("Exception in subscriber 2");
                    //Console.WriteLine($"OnEventCounting({value}) processed (subscriber 2)");
                };

                // Subscribe to the event OnEventCounting third subscriber (this handler is performing very time expensive handling). 
                eventPublisher.OnEventCounting +=  (sender, value) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 3) - long running handler");
                    Thread.Sleep(5000);
                };

                // Subscribe to the event OnEventCounting forth subscriber
                eventPublisher.OnEventCounting +=  (sender, value) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 4) - raising Exception");
                    throw new Exception("Exception in subscriber 4");
                    //Console.WriteLine($"OnEventCounting({value}) processed (subscriber 4)");
                    // Task.CompletedTask;
                };

                // Subscribe to the event OnEventCounting fifth subscriber
                eventPublisher.OnEventCounting +=  (sender, value) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 5)");

                };

                // Start/simulate long running task (3 seconds)
                eventPublisher.StartLongRunningTask(TimeSpan.FromSeconds(3));

                // Raise the event OnEventCounting 1 .. 5 with 1 second delay
                for (int i = 1; i <= 5; i++)
                {
                    // Raise the event OnEventCounting i
                    Console.WriteLine($" -- Raising event OnEventCounting {i} -- ");
                     eventPublisher.RaiseCountingEvent(i);
                    Thread.Sleep(1000);
                }

                // Wait indefinitely
                Thread.Sleep(10000);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"App crashed {exc.Message}\n{exc.ToString()}");
            }
            finally
            {
                Console.WriteLine("App finished");
            }
        }

        private class EventPublisher
        {
            public event EventHandler? OnLongRunningTaskFinished;

            public event EventHandler<int>? OnEventCounting;

            /// <summary>
            /// Raises (invokes) the event OnEventCounting for the specified value.
            /// It calls the subscribers one by one (as normal delegate/event would do) 
            /// </summary>
            /// <param name="value">Value which will be sent to subscriber</param>
            /// <returns></returns>
            public void  RaiseCountingEvent(int value)
            {
                //Handlers are started sequentionally and exception in one stops the rest from processing
                 OnEventCounting?.Invoke(this, value);
            }

            /// <summary>
            /// Raises (invokes) the event OnLongRunningTaskFinished event ater the task is finished ( duration is defined by input <paramref name="taskDuration"/> )
            /// It calls the subscribers one by one (as normal delegate/event would do).
            /// </summary>
            /// <param name="taskDuration"></param>
            /// <returns></returns>
            public void StartLongRunningTask(TimeSpan taskDuration)
            {
                Console.WriteLine($"Long Running Task started with duration {taskDuration.TotalSeconds} seconds.");
                // Simulate long running mthod
                Thread.Sleep(taskDuration);
                //Handlers are started sequentionally and exception in one stops the rest from processing
                OnLongRunningTaskFinished?.Invoke(this,EventArgs.Empty);
            }

        }
    }
}

```

### Output
```
App started
Subscribing event handlers
Long Running Task started with duration 3 seconds.
***Long running task finished***
 -- Raising event OnEventCounting 1 --
OnEventCounting(1) received (subscriber 1)
OnEventCounting(1) processed (subscriber 1)
OnEventCounting(1) received (subscriber 2) - raising Exception
App crashed Exception in subscriber 2
System.Exception: Exception in subscriber 2
   at EventDemo.ProgramEventDemo.<>c.<Main>b__0_2(Object sender, Int32 value) in Y:\Personal_Projects\AsyncEvents\AsyncEventDemo\EventDemo\ProgramEventDemo.cs:line 36
   at EventDemo.ProgramEventDemo.EventPublisher.RaiseCountingEvent(Int32 value) in Y:\Personal_Projects\AsyncEvents\AsyncEventDemo\EventDemo\ProgramEventDemo.cs:line 110
   at EventDemo.ProgramEventDemo.Main(String[] args) in Y:\Personal_Projects\AsyncEvents\AsyncEventDemo\EventDemo\ProgramEventDemo.cs:line 77
App finished
```

## Demo1: delegate (async) Task
This demo is trying to solve how to subscibe asynchronous handlers ( for example method with signature `async Task DoSomething(int value)` )
It is possible to declare delagate with return type `Task` which can be later used by publisher. For example (simplified example without null checking):
```
public delegate Task EventHandlerAsync<TEventArgs>(object? sender, TEventArgs e, CancellationToken ct = default);

class Publisher
{
    public event EventHandlerAsync<int>? OnEventCounting;

    public async Task RaiseCountingEventAsync(int value)
    {
        await OnEventCounting.Invoke(this, value);
    }
}
```

Complete demo source code is below.

The code below compiles, runs, events are raised/multicasted (in other words handlers are called) with following in mind:
1. Handlers are started sequentionally (in the same order as subscribed). The same way as synchronous (`void delegate`) would be called (refer to https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/delegates/using-delegates)
1. BUT !!! Handlers are not awaited at all, so result of the previus handler cannot be used as an input to the following handler. Following handler might be even strated before previous handler finishes.
1. Exceptions thrown in hadlers (for example `throw new Exception("Exception in subscriber 2")`) _disseapear_:
    1. For example `try-catch` block in `Program` does not catch such exception(s).
    1. Handlers subscribed after faulty handler are triggered/called ( handlers 3. 4. and 5. are triggered/called). This would not happen with non-async delagate (void delegate):
        ```
        .
        .
        OnEventCounting(1) received (subscriber 2) - raising Exception // no following OnEventCounting(1) processed (subscriber 2)
        OnEventCounting(1) received (subscriber 3)
        OnEventCounting(1) received (subscriber 4)
        OnEventCounting(1) received (subscriber 5)
        ```
        The root cause (TODO confirm in delegate source code) is that exception end up in TaskScheduler as UnobservedTaskException. It can be caught by:
        ```
        TaskScheduler.UnobservedTaskException += (s, e) => Console.WriteLine($"TaskScheduler_UnobservedTaskException: {e.Exception.Message}");
        ```
        The Output would look like this:
        ```
        App started
        Subscribing event handlers
        Long Running Task started with duration 3 seconds.
         -- Raising event OnEventCounting 1 --
        OnEventCounting(1) received (subscriber 1)
        OnEventCounting(1) received (subscriber 2) - raising Exception
        OnEventCounting(1) received (subscriber 3) - long running handler
        OnEventCounting(1) received (subscriber 4) - raising Exception
        OnEventCounting(1) processed (subscriber 1)
        OnEventCounting(1) received (subscriber 5)
        TaskScheduler_UnobservedTaskException: Exception in subscriber 4
        TaskScheduler_UnobservedTaskException: Exception in subscriber 2
        OnEventCounting(1) processed (subscriber 5)
         -- Raising event OnEventCounting 2 --
         .
         .
         .
        ```
### Source code	
Above described example with full source code and output
```
namespace AsyncEventDemo
{	
    public delegate Task EventHandlerAsync(object? sender, CancellationToken ct = default);
    public delegate Task EventHandlerAsync<TEventArgs>(object? sender, TEventArgs e, CancellationToken ct = default);
	
    internal class Program
    {
 
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("App started");
 
                var eventPublisher = new EventPublisherAsync();
 
                Console.WriteLine("Subscribing event handlers");
 
                // Subscribe to the event OnLongRunningTaskFinished
                eventPublisher.OnLongRunningTaskFinished += async (sender, ct) =>
                {
                    await Task.Delay(1);
                    Console.WriteLine("***Long running task finished***");
                };
 
                // Subscribe to the event OnEventCounting first subscriber
                eventPublisher.OnEventCounting += async (sender, value, ct) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 1)");
                    await Task.Delay(1);
                    Console.WriteLine($"OnEventCounting({value}) processed (subscriber 1)");
 
                };
 
                // Subscribe to the event OnEventCounting second subscriber
                eventPublisher.OnEventCounting += async (sender, value, ct) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 2) - raising Exception");
                    await Task.Delay(1);
                    throw new Exception("Exception in subscriber 2");
                    Console.WriteLine($"OnEventCounting({value}) processed (subscriber 2)");
                    await Task.CompletedTask;
                };
 
                // Subscribe to the event OnEventCounting third subscriber (this handler is performing very time expensive handling). 
                eventPublisher.OnEventCounting += async (sender, value, ct) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 3) - long running handler");
                    await Task.Delay(5000);
                    Console.WriteLine($"OnEventCounting({value}) processed (subscriber 3)");
                };
 
                // Subscribe to the event OnEventCounting forth subscriber
                eventPublisher.OnEventCounting += async (sender, value, ct) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 4) - raising Exception");
                    await Task.Delay(1);
                    throw new Exception("Exception in subscriber 4");
                    Console.WriteLine($"OnEventCounting({value}) processed (subscriber 4)");
                    //await Task.CompletedTask;
                };
 
                // Subscribe to the event OnEventCounting fifth subscriber
                eventPublisher.OnEventCounting += async (sender, value, ct) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 5)");
                    await Task.Delay(1);
                    Console.WriteLine($"OnEventCounting({value}) processed (subscriber 5)");
 
                };
                // Start/simulate long running task (3 seconds)
#pragma warning disable CS4014 // Intentionally not waited for the task to finish
                eventPublisher.StartLongRunningTaskAsync(TimeSpan.FromSeconds(3));
#pragma warning restore CS4014
 
                // Raise the event OnEventCounting 1 .. 5 with 1 second delay
                for (int i = 1; i <= 5; i++)
                {
                    // Raise the event OnEventCounting i
                    Console.WriteLine($" -- Raising event OnEventCounting {i} -- ");
                    await eventPublisher.RaiseCountingEventAsync(i).ConfigureAwait(false);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
 
                // Wait indefinitely
                await Task.Delay(10000);
            }
            catch (Exception exc)
            {
                Console.WriteLine($"App crashed {exc.Message}\n{exc.ToString()}");
            }
            finally
            {
                Console.WriteLine("App finished");
            }
        }
 
        private class EventPublisherAsync
        {
            public event EventHandlerAsync? OnLongRunningTaskFinished;
 
            public event EventHandlerAsync<int>? OnEventCounting;
 
            /// <summary>
            /// Raises (invokes) the event OnEventCounting for the specified value.
            /// It calls the subscribers one by one (as normal delegate/event would do) 
            /// </summary>
            /// <param name="value">Value which will be sent to subscriber</param>
            /// <returns></returns>
            public async Task RaiseCountingEventAsync(int value)
            {
                // No exceptions are caught and if any of the subscribers throws an exception, the following subscribers would be notified as well.
                //Handlers are started sequentionally, but not awaited at all.
                await (OnEventCounting?.Invoke(this, value) ?? Task.CompletedTask).ConfigureAwait(false);
            }
 
            /// <summary>
            /// Raises (invokes) the event OnLongRunningTaskFinished event ater the task is finished ( duration is defined by input <paramref name="taskDuration"/> )
            /// It calls the subscribers one by one (as normal delegate/event would do).
            /// </summary>
            /// <param name="taskDuration"></param>
            /// <returns></returns>
            public async Task StartLongRunningTaskAsync(TimeSpan taskDuration)
            {
                Console.WriteLine($"Long Running Task started with duration {taskDuration.TotalSeconds} seconds.");
                // Simulate long running task
                await Task.Delay(taskDuration).ConfigureAwait(false);
                // Notify subscribers
                // No exceptions are caught and if any of the subscribers throws an exception, the following subscribers would be notified as well.
                //Handlers are started sequentionally, but not awaited at all.
	                await (OnLongRunningTaskFinished?.Invoke(this) ?? Task.CompletedTask).ConfigureAwait(false);
            }
        }
    }
}
```	

### Output
Console output
```
	App started
	Long Running Task started with duration 3 seconds.
	 -- Raising event OnEventCounting 1 --
	OnEventCounting(1) received (subscriber 1)
	OnEventCounting(1) received (subscriber 2) - raising Exception // no following OnEventCounting(1) processed (subscriber 2)
	OnEventCounting(1) processed (subscriber 1)
	OnEventCounting(1) received (subscriber 3) - long running handler
	OnEventCounting(1) received (subscriber 4) - raising Exception
	OnEventCounting(1) received (subscriber 5)
	OnEventCounting(1) processed (subscriber 5)
	 -- Raising event OnEventCounting 2 --
	OnEventCounting(2) received (subscriber 1)
	OnEventCounting(2) received (subscriber 2) - raising Exception
	OnEventCounting(2) received (subscriber 3) - long running handler
	OnEventCounting(2) processed (subscriber 1)
	OnEventCounting(2) received (subscriber 4) - raising Exception
	OnEventCounting(2) received (subscriber 5)
	OnEventCounting(2) processed (subscriber 5)
	 -- Raising event OnEventCounting 3 --
	OnEventCounting(3) received (subscriber 1)
	OnEventCounting(3) received (subscriber 2) - raising Exception
	OnEventCounting(3) received (subscriber 3) - long running handler
	OnEventCounting(3) processed (subscriber 1)
	OnEventCounting(3) received (subscriber 4) - raising Exception
	OnEventCounting(3) received (subscriber 5)
	OnEventCounting(3) processed (subscriber 5)
	***Long running task finished***
	 -- Raising event OnEventCounting 4 --
	OnEventCounting(4) received (subscriber 1)
	OnEventCounting(4) received (subscriber 2) - raising Exception
	OnEventCounting(4) processed (subscriber 1)
	OnEventCounting(4) received (subscriber 3) - long running handler
	OnEventCounting(4) received (subscriber 4) - raising Exception
	OnEventCounting(4) received (subscriber 5)
	OnEventCounting(4) processed (subscriber 5)
	 -- Raising event OnEventCounting 5 --
	OnEventCounting(5) received (subscriber 1)
	OnEventCounting(5) received (subscriber 2) - raising Exception
	OnEventCounting(5) processed (subscriber 1)
	OnEventCounting(5) received (subscriber 3) - long running handler
	OnEventCounting(5) received (subscriber 4) - raising Exception
	OnEventCounting(5) received (subscriber 5)
	OnEventCounting(5) processed (subscriber 5)
	OnEventCounting(1) processed (subscriber 3)
	OnEventCounting(2) processed (subscriber 3)
	OnEventCounting(3) processed (subscriber 3)
	OnEventCounting(4) processed (subscriber 3)
	OnEventCounting(5) processed (subscriber 3)
```	

## Demo2: delegate Task - Not awaited
Handlers are not awaited at all, so result of the previus handler cannot be used as an input to the following handler. 
TODO confirm and add example

## Demo3: delegate Task - without async
`Non-async` handlers exceptions are propagated as from non-async and exception breaks hanlers chain and is propagated to Program.Main
Example of such handler (the rest of the code is the same)
```
// Subscribe to the event OnEventCounting second subscriber
eventPublisher.OnEventCounting += (sender, value, ct) =>    // <========== Here is missing keyword async
{
    Console.WriteLine($"OnEventCounting({value}) received (subscriber 2) - raising Exception");
    throw new Exception("Exception in subscriber 2");   // <========== This exception breaks hanlers chain and is propagated to Program.Main
};
```
### Output
```
App started
Subscribing event handlers
Long Running Task started with duration 3 seconds.
 -- Raising event OnEventCounting 1 --
OnEventCounting(1) received (subscriber 1)
OnEventCounting(1) received (subscriber 2) - raising Exception
OnEventCounting(1) processed (subscriber 1)
App crashed Exception in subscriber 2
System.Exception: Exception in subscriber 2
   at AsyncEventDemo.ProgramAsyncEventDemo.<>c.<Main>b__0_5(Object sender, Int32 value, CancellationToken ct) in Y:\Personal_Projects\AsyncEvents\AsyncEventDemo\AsyncEventDemo\ProgramAsyncEventDemo.cs:line 42
   at AsyncEventDemo.ProgramAsyncEventDemo.EventPublisherAsync.RaiseCountingEventAsync(Int32 value) in Y:\Personal_Projects\AsyncEvents\AsyncEventDemo\AsyncEventDemo\ProgramAsyncEventDemo.cs:line 117
   at AsyncEventDemo.ProgramAsyncEventDemo.Main(String[] args) in Y:\Personal_Projects\AsyncEvents\AsyncEventDemo\AsyncEventDemo\ProgramAsyncEventDemo.cs:line 83
App finished
```

# Demo4: : delegate (async) Task awaited


```
using AsyncEvents;

private class EventPublisherAsync
{
    public event EventHandlerAsync? OnLongRunningTaskFinished;

    public event EventHandlerAsync<int>? OnEventCounting;

    /// <summary>
    /// Raises (invokes) the event OnEventCounting for the specified value.
    /// It calls the subscribers one by one (as normal delegate/event would do), but it awaits each delegate instance (subscriber handler)
    /// </summary>
    /// <param name="value">Value which will be sent to subscriber</param>
    /// <returns></returns>
    public async Task RaiseCountingEventOneAwaitsOtherAsync(int value)
    {
        // Using library
        await (OnEventCounting?.InvokeAsync(this, value, true) ?? Task.CompletedTask).ConfigureAwait(false);

        //// Without using library
        //var delegateInstances = OnEventCounting?.GetInvocationList();
        //if (delegateInstances != null)
        //{
        //    foreach (var delegateInstance in delegateInstances)
        //    {
        //        var typedDelegate = delegateInstance as EventHandlerAsync<int>;
        //        if (typedDelegate != null)
        //        {
        //            try
        //            {
        //                await typedDelegate.Invoke(this, value).ConfigureAwait(false);

        //            }
        //            catch (global::System.Exception)
        //            {
        //                // Swalllow exception
        //            }
        //        }
        //    }
        //}
    }


    /// <summary>
    /// Raises (invokes) the event OnLongRunningTaskFinished event ater the task is finished ( duration is defined by input <paramref name="taskDuration"/> )
    /// It calls the subscribers one by one (as normal delegate/event would do).
    /// </summary>
    /// <param name="taskDuration"></param>
    /// <returns></returns>
    public async Task StartLongRunningTaskOneAwaitsOtherAsync(TimeSpan taskDuration)
    {
        Console.WriteLine($"Long Running Task started with duration {taskDuration.TotalSeconds} seconds.");
        // Simulate long running task
        await Task.Delay(taskDuration).ConfigureAwait(false);
        // Notify subscribers
        //Handlers are started sequentionally, but and awaited.
        // Using library
        await (OnLongRunningTaskFinished?.InvokeAsync(this, false) ?? Task.CompletedTask).ConfigureAwait(false);
    }
}


void Test()
try
    {
        Console.WriteLine("App started");

        var eventPublisher = new EventPublisherAsync();

        Console.WriteLine("Subscribing event handlers");
        // Subscribe to the event OnEventCounting first subscriber
        eventPublisher.OnEventCounting += async (sender, value, ct) =>
        {
            Console.WriteLine($"OnEventCounting({value}) received (subscriber 1)");
            await Task.Delay(1);
            Console.WriteLine($"OnEventCounting({value}) processed (subscriber 1)");
        };

        // Subscribe to the event OnEventCounting second subscriber
        eventPublisher.OnEventCounting += async (sender, value, ct) =>
        {
            Console.WriteLine($"OnEventCounting({value}) received (subscriber 2) - raising Exception");
            await Task.Delay(1);
            throw new Exception("Exception in subscriber 2");
            //Console.WriteLine($"OnEventCounting({value}) processed (subscriber 2)");
            //await Task.CompletedTask;
        };

        // Subscribe to the event OnEventCounting third subscriber (this handler is performing very time expensive handling). 
        eventPublisher.OnEventCounting += async (sender, value, ct) =>
        {
            Console.WriteLine($"OnEventCounting({value}) received (subscriber 3) - long running handler");
            await Task.Delay(5000);
            Console.WriteLine($"OnEventCounting({value}) processed (subscriber 3)");
        };

        await eventPublisher.RaiseCountingEventOneAwaitsOtherAsync(100).ConfigureAwait(false);
        // ebent was published and all subscribers finished their work as well
        Console.WriteLine($"Event published. This is the end");

        // Wait indefinitely
        await Task.Delay(-1);
    }
    catch (Exception exc)
    {
        Console.WriteLine($"App crashed {exc.Message}");
    }



```

