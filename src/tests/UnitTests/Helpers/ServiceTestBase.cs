// src/tests/UnitTests/Helpers/ServiceTestBase.cs
using CKY.MultiAgentFramework.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CKY.MultiAgentFramework.Tests.Helpers;

public abstract class ServiceTestBase
{
    protected Mock<ILogger<T>> CreateLoggerMock<T>() where T : class
    {
        return new Mock<ILogger<T>>();
    }

    protected Mock<ICacheStore> CreateCacheStoreMock()
    {
        return new Mock<ICacheStore>();
    }

    protected Mock<IVectorStore> CreateVectorStoreMock()
    {
        return new Mock<IVectorStore>();
    }

    protected Mock<IRelationalDatabase> CreateRelationalDatabaseMock()
    {
        return new Mock<IRelationalDatabase>();
    }
}
