using Xunit;
using Moq;
using System;
using System.Collections.Generic;

namespace StateAudit.Tests
{
    public class MethodTests
    {
        // 6.1 Тестирование FinancialReport
        [Fact]
        public void FinancialReport_Verify_ShouldChangeStateToVerified()
        {
            var report = new Core.FinancialReport("REP-001", 1000m);
            report.Verify();
            Assert.Equal(Core.ReportStatus.Verified, report.Status);
        }

        // 6.2 Тестирование AuditProtocol
        [Fact]
        public void AuditProtocol_Sign_ShouldSetValidAndSignature()
        {
            var protocol = new Core.AuditProtocol("P-1");
            string sig = "DIGITAL_SIG_2026";
            
            protocol.Sign(sig);

            Assert.True(protocol.IsValid);
            Assert.Equal(sig, protocol.DigitalSignature);
        }

        // 6.3 Тестирование VerificationSystem (Логика и исключения)
        [Fact]
        public void VerificationSystem_Scan_ShouldThrowIfMalfunction()
        {
            var system = new Core.VerificationSystem();
            system.SetMalfunction(); // Искусственно переводим в сбой

            Assert.Throws<InvalidOperationException>(() => system.PerformScan(new Core.FinancialReport("1", 100)));
        }

        // 6.4 Тестирование Auditor (Состояния и счетчики)
        [Fact]
        public void Auditor_SignProtocol_ShouldIncrementCounter()
        {
            var auditor = new Core.Auditor("Петров");
            auditor.TakeDuty();
            var protocol = new Core.AuditProtocol("P-1");

            auditor.SignProtocol(protocol);

            Assert.Equal(1, auditor.ProtocolsSigned);
        }

        // 6.6 Тестирование ChiefInspector с использованием Mock
        [Fact]
        public void ChiefInspector_ProcessAudit_ShouldCallVerificationSystem()
        {
            // Arrange
            var mockSystem = new Mock<Core.IVerificationSystem>();
            var inspector = new Core.ChiefInspector("Сидоров");
            var report = new Core.FinancialReport("R-1", 500m);
            
            inspector.TakeDuty(); // Активируем инспектора

            // Настраиваем Mock: при любом отчете система возвращает true
            mockSystem.Setup(s => s.PerformScan(It.IsAny<Core.FinancialReport>())).Returns(true);

            // Act
            inspector.Inspect(report, mockSystem.Object);

            // Assert: Проверяем, был ли вызван метод сканирования ровно 1 раз
            mockSystem.Verify(s => s.PerformScan(report), Times.Once);
        }

        // 6.7 Тестирование логики контроллера (Mock зависимостей)
        [Fact]
        public void AuditController_Execute_ShouldAbortIfAuditorNotAvailable()
        {
            // Arrange
            var mockAuditor = new Mock<Core.IAuditor>();
            var mockRegistry = new Mock<Core.IDigitalRegistry>();
            
            // Настраиваем Mock: аудитор всегда занят (OffDuty)
            mockAuditor.Setup(a => a.State).Returns(Core.PersonnelState.OffDuty);

            var controller = new Core.AuditController(mockAuditor.Object, mockRegistry.Object);
            var report = new Core.FinancialReport("R-1", 1000m);

            // Act
            bool result = controller.RunAudit(report);

            // Assert
            Assert.False(result); // Аудит не должен начаться
            mockRegistry.Verify(r => r.AddEntry(It.IsAny<string>()), Times.Never); // В реестр ничего не пишем
        }
    }
}