using Microsoft.Extensions.Logging;
using OmniSharp.Services;

namespace TestUtility.Fake
{
    public class FakeEnvironment : IOmniSharpEnvironment
    {
        public LogLevel TraceType { get; }
        public int Port { get; }
        public int HostPID { get; }
        public string Path { get { return "."; } }
        public string SolutionFilePath { get; }
        public string ConfigurationPath { get; }
        public TransportType TransportType { get; }
        public string[] OtherArgs { get; }
    }
}