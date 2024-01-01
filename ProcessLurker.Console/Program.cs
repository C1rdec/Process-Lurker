using ProcessLurker;

var processService = new ProcessService("steam");
var processId = await processService.WaitForProcess(6666);

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
