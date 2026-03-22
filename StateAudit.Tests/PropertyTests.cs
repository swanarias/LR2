using Xunit;
using System;
using System.Collections.Generic;

namespace StateAudit.Tests
{
    public class PropertyTests
    {
        // 4.1 Тестирование свойств FinancialReport
        [Fact]
        public void FinancialReport_Properties_ShouldHaveCorrectTypes()
        {
            var report = new Core.FinancialReport("REF-123", 500000m);

            Assert.IsType<string>(report.ReportId);
            Assert.IsType<decimal>(report.Amount);
            Assert.IsType<Core.ReportStatus>(report.Status);
        }

        [Fact]
        public void FinancialReport_Constructor_ShouldInitializeCorrectly()
        {
            string id = "REF-999";
            decimal amount = 100.50m;

            var report = new Core.FinancialReport(id, amount);

            Assert.Equal(id, report.ReportId);
            Assert.Equal(amount, report.Amount);
            Assert.Equal(Core.ReportStatus.New, report.Status);
        }

        [Theory]
        [InlineData("", 100)]
        [InlineData("ID", -1)]
        public void FinancialReport_Constructor_ShouldThrowOnInvalidData(string id, decimal amount)
        {
            Assert.Throws<ArgumentException>(() => new Core.FinancialReport(id, amount));
        }

        // 4.2 Тестирование свойств AuditProtocol
        [Fact]
        public void AuditProtocol_InitialState_ShouldBeInvalidAndUnsigned()
        {
            var protocol = new Core.AuditProtocol("PROT-001");

            Assert.False(protocol.IsValid);
            Assert.True(string.IsNullOrEmpty(protocol.DigitalSignature));
        }

        // 4.3 Тестирование свойств ChiefInspector
        [Fact]
        public void ChiefInspector_Constructor_ShouldSetInitialStatus()
        {
            var inspector = new Core.ChiefInspector("Иванов И.И.");

            Assert.Equal("Иванов И.И.", inspector.Name);
            Assert.False(inspector.IsActive);
        }

        // 4.4 Тестирование свойств VerificationSystem
        [Fact]
        public void VerificationSystem_ShouldStartWithZeroRequests()
        {
            var sys = new Core.VerificationSystem();

            Assert.Equal(0, sys.RequestCount);
            Assert.False(sys.IsOperational);
        }

        // 4.5 Тестирование свойств DigitalRegistry
        [Fact]
        public void DigitalRegistry_ShouldBeEmptyOnCreation()
        {
            var registry = new Core.DigitalRegistry();

            Assert.Equal(0, registry.EntryCount);
        }

        // 4.6 Тестирование свойств Auditor
        [Fact]
        public void Auditor_Constructor_ShouldInitializeStats()
        {
            var auditor = new Core.Auditor("Петров П.П.");

            Assert.Equal("Петров П.П.", auditor.Name);
            Assert.Equal(0, auditor.ProtocolsSigned);
        }

        // 4.7 Тестирование объектов результатов (Data Results)
        [Fact]
        public void VerificationResult_ShouldHoldViolationsList()
        {
            var result = new Core.VerificationResult();
            result.FoundViolations = new List<string> { "Ошибка в сумме НДС" };

            Assert.Single(result.FoundViolations);
            Assert.IsType<List<string>>(result.FoundViolations);
        }
    }
}