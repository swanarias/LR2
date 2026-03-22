using Xunit;
using StateAudit.Core;
using Moq;

namespace StateAudit.Tests;

public class AuditorTests
{
    [Fact]
    public void Auditor_InitialState_IsIdle()
    {
        // Arrange & Act
        var auditor = new Auditor("Иван Иванов");

        // Assert
        Assert.Equal("Иван Иванов", auditor.Name);
        Assert.Equal(AuditorState.Idle, auditor.State);
    }

    [Fact]
    public void AssignTask_ChangesStateToBusy()
    {
        // Arrange
        var auditor = new Auditor("Иван Иванов");

        // Act
        auditor.AssignTask();

        // Assert
        Assert.Equal(AuditorState.Busy, auditor.State);
    }

    [Fact]
    public void AssignTask_WhenOnLeave_ThrowsException()
    {
        // Arrange
        var auditor = new Auditor("Иван Иванов");
        auditor.GoOnLeave();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => auditor.AssignTask());
    }
}