namespace WAD.NET.LaunchConfig
{
    /// <summary>
    /// Serializes and deserializes <see cref="WadLaunchConfiguration"/> to and from
    /// an engine-specific format. Each source port implements this interface to
    /// convert between the common model and its native configuration.
    /// </summary>
    public interface ILaunchConfigSerializer
    {
        /// <summary>
        /// Serializes a launch configuration to the engine's native format string.
        /// </summary>
        /// <param name="config">The launch configuration to serialize.</param>
        /// <returns>A string in the engine's native configuration format.</returns>
        string Serialize(WadLaunchConfiguration config);

        /// <summary>
        /// Generates command-line arguments for launching the engine with the given configuration.
        /// </summary>
        /// <param name="config">The launch configuration to convert.</param>
        /// <returns>An array of command-line argument strings.</returns>
        string[] ToCommandLineArgs(WadLaunchConfiguration config);

        /// <summary>
        /// Parses an engine-specific configuration string into the common model.
        /// </summary>
        /// <param name="content">The engine-specific configuration content to parse.</param>
        /// <returns>A <see cref="WadLaunchConfiguration"/> populated from the content.</returns>
        WadLaunchConfiguration Deserialize(string content);
    }
}
