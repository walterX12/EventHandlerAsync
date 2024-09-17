using AsyncEvents;

namespace AsyncEventDemo
{
    internal class ProgramAsyncEventDemo
    {

        static async Task Main(string[] args)
        {
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


            var runCompeSample = false;
            if (runCompeSample)
            try
            {
                //// Make sure that all exceptions ale logged (visible before program ends). It might depend on Garbage colletion as well, so it is not guarenteed...
                //AppDomain.CurrentDomain.UnhandledException += (s,e) => Console.WriteLine($"CurrentDomain_UnhandledException: {((Exception)e.ExceptionObject).Message}");
                //TaskScheduler.UnobservedTaskException += (s, e) => Console.WriteLine($"TaskScheduler_UnobservedTaskException: {e.Exception.Message}");
                //AppDomain.CurrentDomain.FirstChanceException += (s, e) => Console.WriteLine($"CurrentDomain_FirstChanceException: {e.Exception.Message}");

                Console.WriteLine("App started");

                var eventPublisher = new EventPublisherAsync();

                Console.WriteLine("Subscribing event handlers");
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
                    // Subscribe to the event OnLongRunningTaskFinished
                    eventPublisher.OnLongRunningTaskFinished += async (sender, ct) =>
                {
                    await Task.Delay(1);
                    Console.WriteLine("***Handler 1 Long running task finished***");
                };

                // Subscribe to the event OnLongRunningTaskFinished
                eventPublisher.OnLongRunningTaskFinished += async (sender, ct) =>
                {
                    await Task.Delay(1);
                    Console.WriteLine("***Handler 2 Long running task finished***");
                };

      

                // Subscribe to the event OnEventCounting forth subscriber
                eventPublisher.OnEventCounting += async (sender, value, ct) =>
                {
                    Console.WriteLine($"OnEventCounting({value}) received (subscriber 4) - raising Exception");
                    await Task.Delay(1);
                    throw new Exception("Exception in subscriber 4");
                    //Console.WriteLine($"OnEventCounting({value}) processed (subscriber 4)");
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
                    //await eventPublisher.RaiseCountingEventAsync(i).ConfigureAwait(false);
                    await eventPublisher.RaiseCountingEventOneAwaitsOtherAsync(i).ConfigureAwait(false);
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
    }
}
