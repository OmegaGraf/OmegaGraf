using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using NLog;

namespace OmegaGraf.Compose
{
    public class Docker
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public DockerClient DockerClient { get; }

        private static string socketPath = "";

        private string GetDockerURI()
        {
            if (!string.IsNullOrWhiteSpace(socketPath))
            {
                return socketPath;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "unix:/var/run/docker.sock";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "npipe://./pipe/docker_engine";
            }

            throw new NotSupportedException();
        }

        public static void SetDockerURI(string value)
        {
            socketPath = value;
        }

        public Docker()
        {
            var uri = new Uri(this.GetDockerURI());

            try
            {
                using (var config = new DockerClientConfiguration(uri))
                {
                    this.DockerClient = config.CreateClient();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unable to create a Docker client");
                throw;
            }
        }

        public Task PullImage(string image, string tag) =>
            this.DockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = image,
                    Tag = tag
                },
                new AuthConfig(),
                new Progress<JSONMessage>()
            );

        /// <summary>
        /// Create the container and return the ID.
        /// Container will be STOPPED.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="ports">host = container</param>
        /// <param name="binds">host : container</param>
        /// <returns>Container UUID</returns>
        public async Task<string> CreateContainer(
            string image,
            Dictionary<int, int> ports,
            Dictionary<string, string> binds,
            string name = "",
            string tag = "latest",
            List<string> cmd = null
        )
        {
            _=await this.CreateNetwork();

            foreach (var b in binds)
            {
                var hostBind = b.Key;

                if (Regex.IsMatch(hostBind, @"^(.?:([\\/]+)|[/])((.+[\\/]+)+)(.+\..+)$"))
                {
                    System.IO.File.Create(hostBind).Dispose();
                }
                else
                {
                    System.IO.Directory.CreateDirectory(hostBind);
                }
            }

            var portBinds = (
                from port in ports
                from protocol in new string[] {
                    "tcp", "udp"
                }
                select new KeyValuePair<string, IList<PortBinding>>
                (
                    port.Key + "/" + protocol,
                    new List<PortBinding>
                    {
                        new PortBinding
                        {
                            HostIP   = "0.0.0.0",
                            HostPort = port.Value.ToString()
                        }
                    }
                )).ToDictionary(i => i.Key, i => i.Value);

            var exposedPorts = portBinds.ToDictionary(x => x.Key, x => default(EmptyStruct));
            var hostBinds    = binds.Select(x => x.Key + ":" + x.Value).ToList();
            var hostname     = "og-" + (string.IsNullOrWhiteSpace(name) ? Regex.Replace(image, @"^.*/", "") : name);

            var parameters = new CreateContainerParameters
            {
                Name             = hostname,
                Hostname         = hostname,
                NetworkingConfig = new NetworkingConfig()
                {
                    EndpointsConfig = new Dictionary<string, EndpointSettings>()
                    {
                        {
                            "og-network",
                            new EndpointSettings()
                            {
                                NetworkID = "og-network",
                                Aliases = new string[]
                                {
                                    hostname
                                }
                            }
                        }
                    }
                },
                Image        = image + ":" + tag,
                ExposedPorts = exposedPorts,
                HostConfig   = new HostConfig
                {
                    Binds           = hostBinds,
                    PortBindings    = portBinds,
                    PublishAllPorts = false,
                    RestartPolicy   = new RestartPolicy()
                    {
                        Name = RestartPolicyKind.UnlessStopped
                    }
                }
            };

            if (cmd != null && cmd.Count > 0)
            {
                parameters.Cmd = cmd;
            }

            if (Globals.Config.Overwrite)
            {
                logger.Warn("Overwriting container " + hostname);
                await this.RemoveContainers(hostname);
            }

            try
            {
                var response = await this.DockerClient.Containers.CreateContainerAsync(parameters);

                return response.ID;
            }
            catch (DockerApiException ex)
            {
                if (ex.StatusCode.ToString() == "Conflict")
                {
                    logger.Error(ex, "It looks like you already have a deployment!"
                                   + " If you want to redeploy OmegaGraf, please"
                                   + " start the application with --overwrite or --reset");
                }
                else
                {
                    logger.Error(ex, ex.StatusCode.ToString());
                }

                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw;
            }
        }

        public Task StartContainer(string id) =>
            this.DockerClient.Containers.StartContainerAsync(
                id,
                new ContainerStartParameters()
            );

        public Task<IList<ContainerListResponse>> ListContainers() =>
            this.DockerClient.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    All = true
                }
            );

        public Task StopContainer(string id) =>
            this.DockerClient.Containers
                .StopContainerAsync(
                    id,
                    new ContainerStopParameters()
                );

        public Task RemoveContainer(string id) =>
            this.DockerClient.Containers
                .RemoveContainerAsync(
                    id,
                    new ContainerRemoveParameters()
                    {
                        RemoveVolumes = false,
                        RemoveLinks   = false,
                        Force         = false
                    }
                );

        public async Task RemoveContainers(string name)
        {
            var containers = await this.ListContainers();

            foreach (var container in containers)
            {
                if (container.Names.Contains('/' + name))
                {
                    logger.Info("Removing " + name + ", " + container.ID);

                    await this.StopContainer(container.ID);
                    await this.RemoveContainer(container.ID);
                }
            }
        }

        public Task RemoveAllContainers()
        {
            var containers = this.ListContainers().Result;
            var tasks = new List<Task>();

            foreach (var container in containers)
            {
                foreach (var name in container.Names)
                {
                    if (name.StartsWith("/og-"))
                    {
                        logger.Info("Removing " + name + ", " + container.ID);

                        tasks.Add(
                            Task.Run(async () =>
                            {
                                await this.StopContainer(container.ID);
                                await this.RemoveContainer(container.ID);
                            }));
                    }
                }
            }

            return Task.WhenAll(tasks);
        }

        public async Task<NetworksCreateResponse> CreateNetwork()
        {
            var name = "og-network";
            var networks = await this.DockerClient.Networks.ListNetworksAsync();

            if (networks.Any(x => x.Name == name))
            {
                return null;
            }

            return await this.DockerClient.Networks.CreateNetworkAsync(
                new NetworksCreateParameters()
                {
                    Name           = name,
                    Driver         = "bridge",
                    CheckDuplicate = true,
                    IPAM           = new IPAM()
                    {
                        Driver = "default",
                        Config = new List<IPAMConfig>()
                        {
                            new IPAMConfig()
                            {
                                Subnet  = "172.20.0.0/16",
                                IPRange = "172.20.10.0/24",
                                Gateway = "172.20.10.11"
                            }
                        }
                    }
                }
            );
        }

        ~Docker()
        {
            this.DockerClient.Dispose();
        }
    }
}
