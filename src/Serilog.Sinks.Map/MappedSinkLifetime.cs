namespace Serilog
{
    /// <summary>
    /// Describes the policy used for mapped sink creation and shut-down.
    /// </summary>
    public enum MappedSinkLifetime
    {
        /// <summary>
        /// Once a sink has been created to receieve events with a particular key,
        /// it will be kept open until the logging pipeline is shut down.
        /// </summary>
        /// <remarks>This lifetime has the best performance for small numbers
        /// of map keys, but will cause memory exhaustion if the number of keys is
        /// very large or unbounded.</remarks>
        Pipeline,

        /// <summary>
        /// A new sink instance will be created and shut down for each event.
        /// </summary>
        /// <remarks>This option has a per-event performance overhead due to the
        /// setting up and shutting down of sinks, but allows the memory and resources
        /// assigned to each sink to be reclaimed immediately.</remarks>
        Event
    }
}
