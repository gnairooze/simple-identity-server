
Test01();

void Test01()
{
    SimpleIdentityServer.CLI.Program.Main(new string[] { "app", "list" }).GetAwaiter().GetResult();
}
