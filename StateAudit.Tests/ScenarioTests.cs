using Xunit;
using Moq;
using System;

namespace StateAudit.Tests
{
    public class ScenarioTests
    {
        [Fact]
        public void Scenario1_SingleReportAudit_FullInteractionFlow()
        {
            // Arrange - Создаем Mock-объекты для всех участников процесса
            var mockInspector = new Core.ChiefInspector("Сидоров");
            var mockAuditor = new Mock<Core.IAuditor>();
            var mockSystem = new Mock<Core.IVerificationSystem>();
            var mockRegistry = new Mock<Core.IDigitalRegistry>();
            
            // Реальный объект отчета, так как мы проверяем его финальное состояние
            var report = new Core.FinancialReport("ACC-001", 250000m);
            var protocol = new Core.AuditProtocol("P-001");

            // Настраиваем поведение Mock-ов
            mockSystem.Setup(s => s.PerformScan(report)).Returns(true);
            mockAuditor.Setup(a => a.SignForReport(report, It.IsAny<Core.AuditProtocol>(), mockRegistry.Object))
                       .Returns(true);

            // Act
            // Имитируем вызов от внешней системы к Инспектору
            bool result = mockInspector.ProcessAudit(report, mockAuditor.Object, mockSystem.Object, mockRegistry.Object);

            // Assert - Проверяем поведение (Behavioral Testing)
            
            // 1. Проверяем, что отчет был верифицирован инспектором
            Assert.Equal(Core.ReportStatus.Verified, report.Status);

            // 2. Проверяем, что инспектор действительно обратился к аудитору
            mockAuditor.Verify(a => a.SignForReport(report, It.IsAny<Core.AuditProtocol>(), mockRegistry.Object), Times.Once);

            // 3. Результат всего сценария должен быть успешным
            Assert.True(result);
        }

        // Сценарий 2: Пакетная обработка
        [Fact]
        public void Scenario2_BatchProcessing_ShouldProcessAllReports()
        {
            // Arrange
            var mockInspector = new Mock<Core.ICheifInspector>();
            var mockAuditor = new Mock<Core.IAuditor>();
            var reports = new List<Core.FinancialReport> {
                new Core.FinancialReport("R1", 100),
                new Core.FinancialReport("R2", 200),
                new Core.FinancialReport("R3", 300)
            };
            var controller = new Core.AuditController(mockInspector.Object, mockAuditor.Object);

            // Act
            controller.ExecuteBatchAudit(reports);

            // Assert
            // Проверяем, что инспектор вызывался ровно 3 раза (по числу отчетов)
            mockInspector.Verify(i => i.ProcessAudit(
                It.IsAny<Core.FinancialReport>(), 
                mockAuditor.Object, 
                It.IsAny<Core.IVerificationSystem>(), 
                It.IsAny<Core.IDigitalRegistry>()
            ), Times.Exactly(3));
            
            mockInspector.Verify(i => i.OffDuty(), Times.Once);
        }

        // Сценарий 3: Сбой системы верификации
        [Fact]
        public void Scenario3_SystemFailure_ShouldHandleException()
        {
            // Arrange
            var mockSystem = new Mock<Core.IVerificationSystem>();
            var mockAuditor = new Mock<Core.IAuditor>();
            var report = new Core.FinancialReport("FAIL-REP", 500);

            // Настраиваем Mock так, чтобы он выкидывал ошибку при сканировании
            mockSystem.Setup(s => s.PerformScan(report))
                      .Throws(new InvalidOperationException("System Malfunction"));

            var inspector = new Core.ChiefInspector("Главный");

            // Act & Assert
            // Проверяем, что исключение пробрасывается или обрабатывается
            Assert.Throws<InvalidOperationException>(() => 
                inspector.ProcessAudit(report, mockAuditor.Object, mockSystem.Object, new Mock<Core.IDigitalRegistry>().Object)
            );
        }
        // Сценарий 4: Ошибка авторизации
        [Fact]
        public void Scenario4_AuditorOffDuty_ShouldPreventSigning()
        {
            // Arrange
            var mockInspector = new Core.ChiefInspector("Иванов");
            var mockAuditor = new Mock<Core.IAuditor>();
            var report = new Core.FinancialReport("REP-004", 1000);

            // Настраиваем Mock на выброс ошибки доступа
            mockAuditor.Setup(a => a.SignForReport(It.IsAny<Core.FinancialReport>(), It.IsAny<Core.AuditProtocol>(), It.IsAny<Core.IDigitalRegistry>()))
                       .Throws(new UnauthorizedAccessException("Personnel not on duty"));

            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() => 
                mockInspector.ProcessAudit(report, mockAuditor.Object, new Mock<Core.IVerificationSystem>().Object, new Mock<Core.IDigitalRegistry>().Object)
            );
        }

        // Сценарий 5: Переполнение реестра
        [Fact]
        public void Scenario5_RegistryFull_ShouldRejectRegistration()
        {
            // Arrange
            var mockRegistry = new Mock<Core.IDigitalRegistry>();
            var auditor = new Core.Auditor("Сидоров");
            var report = new Core.FinancialReport("REP-005", 5000);
            var protocol = new Core.AuditProtocol("P-005");

            auditor.TakeDuty();
            // Имитируем, что реестр возвращает false (места нет)
            mockRegistry.Setup(r => r.AddEntry(It.IsAny<string>())).Returns(false);

            // Act
            bool result = auditor.SignForReport(report, protocol, mockRegistry.Object);

            // Assert
            Assert.False(result); // Операция не удалась
            Assert.NotEqual(Core.ReportStatus.Archived, report.Status); // Статус не изменился на "Архив"
            mockRegistry.Verify(r => r.AddEntry(It.IsAny<string>()), Times.Once); // Проверка, что попытка записи была
        }
    }
}