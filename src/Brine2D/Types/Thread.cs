namespace Brine2D
{
    /// <summary>
    /// <para>A Thread is a chunk of code that can run in parallel with other threads. Data can be sent between different threads with Channel objects.</para>
    /// </summary>
    // TODO: Requires Review
    public class Thread
    {
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
        public void NewImage() => throw new NotImplementedException();
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
        /// <summary>
        /// <para>Receive a message from a thread.</para>
        /// <para>Wait for the message to exist before returning. (Can return nil in case of an error in the thread.)</para>
        /// </summary>
        /// <param name="name">The name of the message.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The contents of the message.</description></item>
        /// </list>
        /// </returns>
        public object Demand(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Retrieves the value of a message and removes it from the thread's message box.</para>
        /// </summary>
        /// <param name="name">The name of the message.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The contents of the message or nil when no message in message box.</description></item>
        /// </list>
        /// </returns>
        public object Get(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Retrieves the error string from the thread if it produced an error.</para>
        /// </summary>
        /// <param name="err">The error message, or nil if the Thread has not caused an error.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>err</term><description>The error message, or nil if the Thread has not caused an error.</description></item>
        /// </list>
        /// </returns>
        public string GetError(string err = "nil") => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns a table with the names of all messages in the message box.</para>
        /// </summary>
        /// <param name="msgNames">A with all the message names.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>msgNames</term><description>A with all the message names.</description></item>
        /// </list>
        /// </returns>
        public object GetKeys(object msgNames) => throw new NotImplementedException();
        /// <summary>
        /// <para>Get the name of a thread.</para>
        /// </summary>
        /// <param name="name">The name of the thread.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>name</term><description>The name of the thread.</description></item>
        /// </list>
        /// </returns>
        public string GetName(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns whether the thread is currently running.</para>
        /// <para>Threads which are not running can be (re)started with Thread:start.</para>
        /// </summary>
        /// <param name="value">True if the thread is running, false otherwise.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>True if the thread is running, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool IsRunning(bool value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Forcefully terminate the thread.</para>
        /// </summary>
        public void Kill() => throw new NotImplementedException();
        /// <summary>
        /// <para>Retrieves the value of a message, but leaves it in the thread's message box. The name of the message can be any string. The value of the message can be a boolean, string, number or a LÖVE userdata. It returns nil, if there's no message with the given name.</para>
        /// </summary>
        /// <param name="name">The name of the message.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>or</term><description>The contents of the message.</description></item>
        /// </list>
        /// </returns>
        public string Peek(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Receive a message from a thread.</para>
        /// <para>Returns nil when a message is not in the message box.</para>
        /// </summary>
        /// <param name="name">The name of the message.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>value</term><description>The contents of the message or nil when no message in message box.</description></item>
        /// </list>
        /// </returns>
        public object Receive(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Send a message (put it in the message box).</para>
        /// </summary>
        /// <param name="name">The name of the message.</param>
        /// <param name="value">The contents of the message.</param>
        public void Send(string name, object value) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets a value in the message box of the thread. The name of the message can be any string. The value of the message can be a boolean, string, number or a LÖVE userdata. Foreign userdata (Lua's files, LuaSocket, ...), functions or tables are not supported.</para>
        /// </summary>
        /// <param name="name">The name of the message.</param>
        /// <param name="or">The contents of the message.</param>
        public void Set(string name, double or) => throw new NotImplementedException();
        /// <summary>
        /// <para>Starts the thread.</para>
        /// <para>Beginning with version 0.9.0, threads can be restarted after they have completed their execution.</para>
        /// </summary>
        public void Start() => throw new NotImplementedException();
        /// <summary>
        /// <para>Starts the thread.</para>
        /// <para>Beginning with version 0.9.0, threads can be restarted after they have completed their execution.</para>
        /// </summary>
        /// <param name="arg1">A string, number, boolean, LÖVE object, or simple table.</param>
        /// <param name="arg2">A string, number, boolean, LÖVE object, or simple table.</param>
        /// <param name="">You can continue passing values to the thread.</param>
        // TODO: public void Start(object arg1, object arg2, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Wait for a thread to finish.</para>
        /// <para>This call will block until the thread finishes.</para>
        /// </summary>
        public void Wait() => throw new NotImplementedException();
    }
}
