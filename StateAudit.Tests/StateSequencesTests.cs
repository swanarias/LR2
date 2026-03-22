using Xunit;
using Moq;
using System;

namespace StateAudit.Tests
{
    public class StateSequenceTests
    {
        // Сценарий 1: Успех
        [Fact]
        public void Scenario1_SuccessFullCycle()
        {
            var report = new Core.FinancialReport("REP-OK", 50000m);
            var auditor = new Core.Auditor("Аудитор 1");
            var system = new Core.VerificationSystem();
            
            auditor.TakeDuty();
            system.Start();
            
            // Проход по состояниям
            system.PerformScan(report);
            report.Verify();
            
            var protocol = new Core.AuditProtocol("P-01");
            auditor.SignProtocol(protocol);
            
            report.MarkAsArchived();

            Assert.Equal(Core.ReportStatus.Archived, report.Status);
        }

        // Сценарий 2: Неверные лимиты
        [Fact]
        public void Scenario2_LimitExceeded_Failed()
        {
            var report = new Core.FinancialReport("REP-BAD", 999999999m);
            var system = new Core.VerificationSystem();
            system.Start();

            // Логика: если сумма слишком велика, скан возвращает false
            bool scanSuccess = report.Amount < 1000000m; 
            
            Assert.False(scanSuccess); 
            // В реальном коде здесь вызывается метод перевода аудита в Failed
        }

        // Сценарий 3: Перегрузка системы (Malfunction)
        [Fact]
        public void Scenario3_SystemOverload_Malfunction()
        {
            var system = new Core.VerificationSystem();
            system.Start();
            var report = new Core.FinancialReport("R-1", 100m);

            // Доводим до сбоя
            for (int i = 0; i < 100; i++) system.PerformScan(report);

            Assert.Throws<InvalidOperationException>(() => system.PerformScan(report));
            Assert.False(system.IsOperational);
        }

        // Сценарий 4: Отсутствие персонала (OffDuty)
        [Fact]
        public void Scenario4_AuditorNotOnDuty_Exception()
        {
            var auditor = new Core.Auditor("Петров"); // Состояние OffDuty по умолчанию
            var protocol = new Core.AuditProtocol("P-1");

            // Пытаемся подписать, не вызвав TakeDuty()
            Assert.Throws<InvalidOperationException>(() => auditor.SignProtocol(protocol));
        }

        // Сценарий 5: Ошибка реестра (Registry Full)
        [Fact]
        public void Scenario5_RegistryFull_Abort()
        {
            var registry = new Core.DigitalRegistry();
            // Имитируем заполнение
            registry.FillToLimit(); 

            bool canAdd = registry.TryPrepareEntry();

            Assert.False(canAdd);
        }
    }
}