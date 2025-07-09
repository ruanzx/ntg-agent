var builder = DistributedApplication.CreateBuilder(args);

var myTestMCPServer = builder.AddProject<Projects.MyTestMCPServer>("mytestmcpserver");

var orchestrator = builder.AddProject<Projects.NTG_Agent_Orchestrator>("ntg-agent-orchestrator")
    .WithReference(myTestMCPServer)
    .WaitFor(myTestMCPServer);

builder.AddProject<Projects.NTG_Agent_WebClient>("ntg-agent-webclient")
    .WithExternalHttpEndpoints()
    .WithReference(orchestrator)
    .WaitFor(orchestrator);

builder.AddProject<Projects.NTG_Agent_Admin>("ntg-agent-admin");

builder.Build().Run();
