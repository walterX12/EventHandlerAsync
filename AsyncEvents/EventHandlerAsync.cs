using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace AsyncEvents
{
    /// <summary>
    /// Delegate for async methods without parameters (only sender).
    /// Represents the method that will handle an event asynchronously 
    /// </summary>
    /// <param name="sender"> The source of the event.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    public delegate Task EventHandlerAsync(object? sender, CancellationToken ct = default);

    /// <summary>
    /// Delegate for async methods with parameters <typeparamref name="TEventArgs"/> (and sender).
    /// Represents the method that will handle an event asynchronously 
    /// </summary>
    /// <param name="sender"> The source of the event.</param>
    /// <param name="e">Event data to be sent</param>
    /// <param name="ct">Cancellation token</param>
    /// <typeparam name="TEventArgs">Type of data to be sent</typeparam>
    public delegate Task EventHandlerAsync<TEventArgs>(object? sender, TEventArgs e, CancellationToken ct = default);

    /// <summary>
    /// Extensions to enable Invoke asynchronously
    /// </summary>
    public static class EventHandlerAsyncExtensions
    {
        /// <summary>
        /// It calls the subscribers one by one (as normal synchronous delegate/event would do), but it awaits each delegate instance (subscriber handler)
        /// </summary>
        /// <returns></returns>
        /// <param name="self">Event handler</param>
        /// <param name="sender">Object instance, which initiates event</param>
        /// <param name="ignoreExceptions">If true, exception in every particular handler/subscriber (in delegte Invoication list) 
        /// is swallowed to enable process even consequent/following handler/subscriber and such exceptions is not propagated further.</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task InvokeAsync(this EventHandlerAsync self, object? sender, bool ignoreExceptions, CancellationToken ct = default)
        {
            var invocationList = self?.GetInvocationList();
            if (invocationList != null)
            {
                foreach (var delegateInstance in invocationList)
                {
                    var typedDelegate = delegateInstance as EventHandlerAsync;
                    if (typedDelegate != null)
                    {
                        if (ignoreExceptions)
                        {
                            try
                            {
                                await typedDelegate.Invoke(sender).ConfigureAwait(false);
                            }
                            catch (global::System.Exception)
                            {
                                // Swallow exception
                            }
                        }
                        else
                        {
                            // Exceptions are not ignored
                            await typedDelegate.Invoke(sender).ConfigureAwait(false);
                        }
                    }
                }
            }
        }




        /// <summary>
        /// Calls the subscribers one by one (as normal synchronous delegate/event would do), but it awaits each delegate instance (subscriber handler).
        /// Exception in every particular handler/subscriber (in delegte Invoication list) is propagated , which means it stops
        /// processing consequent/following handler/subscriber and is thrown up the call stack  
        /// </summary>
        /// <param name="self">Event handler</param>
        /// <param name="sender"> The source of the event.</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        public static Task InvokeAsync(this EventHandlerAsync self, object? sender, CancellationToken ct = default) => InvokeAsync(self, sender, false,ct);

        /// <summary>
        /// Calls the subscribers one by one (as normal synchronous delegate/event would do), but it awaits each delegate instance (subscriber handler).
        /// Exception in every particular handler/subscriber (in delegte Invoication list) is propagated , which means it stops
        /// processing consequent/following handler/subscriber and is thrown up the call stack  
        /// </summary>
        /// <returns></returns>
        /// <param name="self">Event handler</param>
        /// <param name="sender">Object instance, which initiates event</param>
        /// <param name="e">Event data to be sent</param>
        /// <param name="ignoreExceptions">If true, exception in every particular handler/subscriber (in delegte Invoication list) is swallowed to enable process even consequent/following handler/subscriber</param>
        /// <param name="ct">Cancellation token</param>
        public static async Task InvokeAsync<TEventArgs>(this EventHandlerAsync<TEventArgs> self, object? sender, TEventArgs e, bool ignoreExceptions, CancellationToken ct = default)
        {
            var invocationList = self?.GetInvocationList();
            if (invocationList != null)
            {
                foreach (var delegateInstance in invocationList)
                {
                    var typedDelegate = delegateInstance as EventHandlerAsync<TEventArgs>;
                    if (typedDelegate != null)
                    {
                        if (ignoreExceptions)
                        {
                            try
                            {
                                await typedDelegate.Invoke(sender,e).ConfigureAwait(false);
                            }
                            catch (global::System.Exception)
                            {
                                // Swallow exception
                            }
                        }
                        else
                        {
                            // Exceptions are not ignored
                            await typedDelegate.Invoke(sender, e).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// It calls the subscribers one by one (as normal synchronous delegate/event would do), but it awaits each delegate instance (subscriber handler).
        ///  Exception in every particular handler/subscriber (in delegte Invoication list) is propagated , which means it stops
        ///  processing consequent/following handler/subscriber and is thrown up the call stack        
        /// </summary>
        /// <returns></returns>
        /// <param name="self">Event handler</param>
        /// <param name="sender">Object instance, which initiates event</param>
        /// <param name="e">Event data to be sent</param>
        /// <param name="ct">Cancellation token</param>
        public static Task InvokeAsync<TEventArgs>(this EventHandlerAsync<TEventArgs> self, object? sender, TEventArgs e, CancellationToken ct = default)
            => InvokeAsync<TEventArgs>(self, sender, e,false, ct);
    }


}
