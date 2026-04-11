var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var db = postgres.AddDatabase("erpdb");

var api = builder.AddProject<Projects.ErpApp_Api>("api")
    .WithReference(db)
    .WaitFor(db);

builder.AddNpmApp("web", "../erp-web", "dev")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(api);

builder.Build().Run();
