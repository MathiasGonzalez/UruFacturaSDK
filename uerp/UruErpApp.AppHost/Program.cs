var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var db = postgres.AddDatabase("saasdb");

var api = builder.AddProject<Projects.UruErpApp_Api>("api")
    .WithReference(db)
    .WaitFor(db);

builder.AddNpmApp("web", "../uerp-web", "dev")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(api);

builder.Build().Run();
