using System;

namespace Brine2D;

// TODO: Needs review
public sealed class ThreadModule
{
/// <summary>
        /// <para>Creates or retrieves a named thread channel.</para>
        /// </summary>
        /// <param name="name">The name of the channel you want to create or retrieve.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>channel</term><description>The Channel object associated with the name.</description></item>
        /// </list>
        /// </returns>
    public object GetChannel(string name) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates or retrieves a named thread channel.</para>
        /// </summary>
    public void NewThread() => throw new NotImplementedException();

/// <summary>
        /// <para>Look for a thread and get its object.</para>
        /// </summary>
        /// <param name="name">The name of the thread to return.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>thread</term><description>The thread with that name.</description></item>
        /// </list>
        /// </returns>
    public object GetThread(string name) => throw new NotImplementedException();

/// <summary>
        /// <para>Look for a thread and get its object.</para>
        /// </summary>
        /// <param name="thread">The current thread.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>thread</term><description>The current thread.</description></item>
        /// </list>
        /// </returns>
    public object GetThread(object thread) => throw new NotImplementedException();

/// <summary>
        /// <para>Get all threads.</para>
        /// </summary>
        /// <param name="threads">A table containing all threads indexed by their names.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>threads</term><description>A table containing all threads indexed by their names.</description></item>
        /// </list>
        /// </returns>
    public object GetThreads(object threads) => throw new NotImplementedException();

/// <summary>
        /// <para>Create a new unnamed thread channel.</para>
        /// <para>One use for them is to pass new unnamed channels to other threads via Channel:push on a named channel.</para>
        /// </summary>
        /// <param name="channel">The new Channel object.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>channel</term><description>The new Channel object.</description></item>
        /// </list>
        /// </returns>
    public object NewChannel(object channel) => throw new NotImplementedException();

}
