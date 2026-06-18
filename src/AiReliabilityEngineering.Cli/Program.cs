using AiReliabilityEngineering.Cli;

var handler = CliCommandHandler.CreateDefault(Console.Out, Console.Error);
return await handler.ExecuteAsync(args, CancellationToken.None);
