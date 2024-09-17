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
