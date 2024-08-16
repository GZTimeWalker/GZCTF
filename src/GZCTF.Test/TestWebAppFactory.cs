using Microsoft.AspNetCore.Mvc.Testing;

namespace GZCTF.Test;

public abstract class TestWebAppFactory : WebApplicationFactory<Program>
{
    static TestWebAppFactory()
    {
        Program.IsTesting = true;
    }
}
