using Nancy;
using Nancy.Metadata.Modules;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;
using Nancy.Security;
using Nancy.Swagger;
using OmegaGraf.Compose.Config.Prometheus;
using Swagger.ObjectModel;

namespace OmegaGraf.Compose.MetaData
{
    public class PrometheusModule : NancyModule
    {
        public PrometheusModule() : base("/prometheus")
        {
            this.RequiresAuthentication();

            Get(
                "/{id}",
                args =>
                {
                    return HttpStatusCode.OK;
                }, null, "Info"
            );

            Post(
                "/",
                args =>
                {
                    Input<Prometheus> bind = (this).Bind<Input<Prometheus>>();

                    var bc = bind.BuildInput.ToBuildConfiguration("prom/prometheus");

                    var uuid = new Runner().AddYamlConfig(bind.Config).Build(bc);

                    return Negotiate.WithMediaRangeModel(
                        new MediaRange("application/json"),
                        new
                        {
                            Container = uuid
                        }
                    );
                }, null, "DeployPrometheus"
            );
        }
    }

    public class PrometheusMetadataModule : MetadataModule<PathItem>
    {
        public PrometheusMetadataModule(ISwaggerModelCatalog modelCatalog)
        {
            modelCatalog.AddModels(
                typeof(Prometheus),
                typeof(ScrapeConfigs),
                typeof(Global),
                typeof(StaticConfigs),
                typeof(BuildConfiguration),
                typeof(Config<Prometheus>),
                typeof(Input<Prometheus>)
            );

            Describe["DeployPrometheus"] =
                desc => desc.AsSwagger(
                    with => with.Operation(
                        op => op.OperationId("DeployPrometheus")
                        .Tag("Deploy")
                        .Summary("Deploy Prometheus")
                        .ConsumeMimeType("application/json")
                        .ProduceMimeType("application/json")
                        .SecurityRequirement(SecuritySchemes.ApiKey)
                        .BodyParameter(
                            para =>
                                para.Name("Build")
                                    .Schema(
                                        new Schema()
                                        {
                                            Example = Defaults.Prometheus
                                        }
                                    )
                                    .Build()
                        ).Response(x => x.Description("Container UUID").Build()))
                );
        }
    }
}