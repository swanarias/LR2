using Xunit;
using Moq;
using StateAudit.Core;
using System.Collections.Generic;

namespace StateAudit.Tests
{
    public class UseCaseTests
    {
        // Тест для UC1: Пакетная обработка (Накопление данных)
        [Fact]
        public void UseCase_ProcessBatch_ShouldReturnCorrectStatistics()
        {
            var mockInspector = new Mock<ICommander>(); // В вашем коде контроллера используется ICommander/ICommander
            var mockOp = new Mock<IOperator>();
            var mockVehicle = new Mock<ILauncherVehicle>();
            var mockRadar = new Mock<IRadar>();
            
            var controller = new CommandCenterController(mockInspector.Object, mockOp.Object, mockVehicle.Object, mockRadar.Object);
            var targets = new List<ITargetCoordinates> { new Mock<ITargetCoordinates>().Object, new Mock<ITargetCoordinates>().Object };

            // Настраиваем: первый отчет проходит, второй — нет
            mockInspector.SetupSequence(i => i.ProcessTargetEngagement(It.IsAny<ITargetCoordinates>(), It.IsAny<IOperator>(), It.IsAny<ILauncherVehicle>(), It.IsAny<IRadar>()))
                         .Returns(true)
                         .Returns(false);

            var result = controller.ProcessBatch(targets);

            Assert.Equal(1, result.Accepted);
            Assert.Equal(1, result.Rejected);
        }

        // Тест для UC4: Глобальная блокировка (Выборка по условию)
        [Fact]
        public void UseCase_HandleSystemThreat_ShouldBlockUnitsIfRadarFails()
        {
            var mockRadar = new Mock<IRadar>();
            var unit1 = new Mock<ILauncherVehicle>();
            var unit2 = new Mock<ILauncherVehicle>();
            
            mockRadar.Setup(r => r.State).Returns(RadarState.Malfunction);
            var controller = new CommandCenterController(null, null, null, null);

            var result = controller.HandleSystemThreat(new List<ILauncherVehicle> { unit1.Object, unit2.Object }, mockRadar.Object);

            Assert.True(result.IsEmergency);
            unit1.Verify(u => u.BreakDown(), Times.Once);
            unit2.Verify(u => u.BreakDown(), Times.Once);
        }
    }
}