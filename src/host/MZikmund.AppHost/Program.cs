var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MZikmund_Web>("mzikmund-web");

builder.Build().Run();
