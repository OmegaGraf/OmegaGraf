using PowerArgs;

namespace OmegaGraf.Compose
{
    public class MyArgs
    {
        [ArgShortcut("-h"), ArgShortcut("--help"), ArgDescription("Shows this help")]
        public bool Help { get; set; }

        [ArgShortcut("-v"), ArgShortcut("--verbose"), ArgDescription("Enable verbose logging")]
        public bool Verbose { get; set; }

        [ArgShortcut("-p"), ArgShortcut("--path"), ArgDescription("Absolute path to store container data. Defaults to current directory."), ArgPosition(1), ArgDefaultValue("")]
        public string Path { get; set; }

        [ArgShortcut("--host"), ArgDescription("The listen address for this application."), ArgPosition(2), ArgDefaultValue("http://0.0.0.0:5000")]
        public string[] Host { get; set; }

        [ArgShortcut("-k"), ArgShortcut("--key"), ArgDescription("Override the OmegaGraf Secure Key."), ArgPosition(3)]
        public string Key { get; set; }

        [ArgShortcut("-r"), ArgShortcut("--reset"), ArgDescription("Removes existing OmegaGraf containers.")]
        public bool Reset { get; set; }

        [ArgShortcut("--version"), ArgDescription("Print version info and exit.")]
        public bool Version { get; set; }
    }
}