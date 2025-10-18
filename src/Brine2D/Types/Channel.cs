namespace Brine2D
{
    /// <summary>
    /// <para>An object which can be used to send and receive data between different threads.</para>
    /// </summary>
    // TODO: Requires Review
    public class Channel
    {
        /// <summary>
        /// <para>Clears all the messages in the Channel queue.</para>
        /// </summary>
        public void Clear() => throw new NotImplementedException();
        /// <summary>
        /// <para>Retrieves the value of a Channel message and removes it from the message queue.</para>
        /// <para>It waits until a message is in the queue then returns the message value.</para>
        /// </summary>
        /// <param name="value">The contents of the message.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The contents of the message.</description></item>
        /// </list>
        /// </returns>
        public object Demand(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Retrieves the value of a Channel message and removes it from the message queue.</para>
        /// <para>It waits until a message is in the queue then returns the message value.</para>
        /// </summary>
        /// <param name="timeout">The maximum amount of time to wait, in seconds. Given as a decimal, accurate to the millisecond.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The contents of the message or nil if the timeout expired.</description></item>
        /// </list>
        /// </returns>
        public object Demand(double timeout) => throw new NotImplementedException();
        /// <summary>
        /// <para>Retrieves the number of messages in the thread Channel queue.</para>
        /// </summary>
        /// <param name="count">The number of messages in the queue.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>count</term><description>The number of messages in the queue.</description></item>
        /// </list>
        /// </returns>
        public double GetCount(double count) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether a pushed value has been popped or otherwise removed from the Channel.</para>
        /// </summary>
        /// <param name="id">An id value previously returned by .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>hasread</term><description>Whether the value represented by the id has been removed from the Channel via , , or .</description></item>
        /// </list>
        /// </returns>
        public bool HasRead(double id) => throw new NotImplementedException();
        /// <summary>
        /// <para>Retrieves the value of a Channel message, but leaves it in the queue.</para>
        /// <para>It returns nil if there's no message in the queue.</para>
        /// </summary>
        /// <param name="value">The contents of the message.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The contents of the message.</description></item>
        /// </list>
        /// </returns>
        public object Peek(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Executes the specified function atomically with respect to this Channel.</para>
        /// <para>Calling multiple methods in a row on the same Channel is often useful. However if multiple Threads are calling this Channel's methods at the same time, the different calls on each Thread might end up interleaved (e.g. one or more of the second thread's calls may happen in between the first thread's calls.)</para>
        /// <para>This method avoids that issue by making sure the Thread calling the method has exclusive access to the Channel until the specified function has returned.</para>
        /// </summary>
        /// <param name="func">The function to call, the form of . The Channel is passed as the first argument to the function when it is called.</param>
        /// <param name="arg1">Additional arguments that the given function will receive when it is called.</param>
        /// <param name="">Additional arguments that the given function will receive when it is called.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>ret1</term><description>The first return value of the given function (if any.)</description></item>
        /// <item><term></term><description>Any other return values.</description></item>
        /// </list>
        /// </returns>
        // TODO: public (object ret1, object) PerformAtomic(object func, object arg1, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Executes the specified function atomically with respect to this Channel.</para>
        /// <para>Calling multiple methods in a row on the same Channel is often useful. However if multiple Threads are calling this Channel's methods at the same time, the different calls on each Thread might end up interleaved (e.g. one or more of the second thread's calls may happen in between the first thread's calls.)</para>
        /// <para>This method avoids that issue by making sure the Thread calling the method has exclusive access to the Channel until the specified function has returned.</para>
        /// </summary>
        public void GetChannel() => throw new NotImplementedException();
        /// <summary>
        /// <para>Retrieves the value of a Channel message and removes it from the message queue.</para>
        /// <para>It returns nil if there are no messages in the queue.</para>
        /// </summary>
        /// <param name="value">The contents of the message.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The contents of the message.</description></item>
        /// </list>
        /// </returns>
        public object Pop(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Send a message to the thread Channel.</para>
        /// <para>See Variant for the list of supported types.</para>
        /// </summary>
        /// <param name="value">The contents of the message.</param>
        public void Push(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Send a message to the thread Channel and wait for a thread to accept it.</para>
        /// <para>See Variant for the list of supported types.</para>
        /// </summary>
        /// <param name="value">The contents of the message.</param>
        public void Supply(object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Send a message to the thread Channel and wait for a thread to accept it.</para>
        /// <para>See Variant for the list of supported types.</para>
        /// </summary>
        /// <param name="value">The contents of the message.</param>
        /// <param name="timeout">The maximum amount of time to wait, in seconds.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>Whether the message was successfully supplied before the timeout expired.</description></item>
        /// </list>
        /// </returns>
        public bool Supply(object value, double timeout) => throw new NotImplementedException();
        /// <summary>
        /// <para>Destroys the object's Lua reference. The object will be completely deleted if it's not referenced by any other LÖVE object or thread.</para>
        /// <para>This method can be used to immediately clean up resources without waiting for Lua's garbage collector.</para>
        /// </summary>
        /// <param name="success">True if the object was released by this call, false if it had been previously released.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the object was released by this call, false if it had been previously released.</description></item>
        /// </list>
        /// </returns>
        public bool Release(bool success) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        /// <param name="type">The type as a string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>type</term><description>The type as a string.</description></item>
        /// </list>
        /// </returns>
        public string Type(string type) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        /// <param name="name">The name of the type to check for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>b</term><description>True if the object is of the specified type, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool TypeOf(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
    }
}
