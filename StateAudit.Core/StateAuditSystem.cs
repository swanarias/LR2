using System;
using System.Collections.Generic;
using System.Linq;

namespace StateAudit.Core
{
    // === 1. ПЕРЕЧИСЛЕНИЯ (СОСТОЯНИЯ) ===
    public enum ReportStatus { New, Verified, Rejected, Archived }
    public enum PersonnelState { OffDuty, Idle, Busy }
    public enum AuditorState { Idle, Busy, OnLeave }
    public enum SystemState { Off, Operational, Malfunction }

    // === 2. ФИНАНСОВЫЙ ОТЧЕТ ===
    public class FinancialReport
    {
        public string ReportId { get; private set; }
        public decimal Amount { get; private set; }
        public ReportStatus Status { get; private set; } = ReportStatus.New;

        public FinancialReport(string id, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID не может быть пустым");
            if (amount < 0) throw new ArgumentException("Сумма не может быть отрицательной");
            ReportId = id;
            Amount = amount;
        }

        public void Verify() => Status = ReportStatus.Verified;
        public void SetStatus(ReportStatus status) => Status = status;
        public void MarkAsArchived() => Status = ReportStatus.Archived;
        
        // Методы из структурной диаграммы
        public void SetAuthorized() => Status = ReportStatus.Verified;
        public void SetChecked() => Status = ReportStatus.Archived;
    }

    // === 3. АУДИТОРСКИЙ ПРОТОКОЛ ===
    public class AuditProtocol
    {
        public string ProtocolId { get; private set; }
        public bool IsValid { get; private set; } = false;
        public string DigitalSignature { get; private set; }

        public AuditProtocol(string id) => ProtocolId = id;

        public void Sign(string signature)
        {
            DigitalSignature = signature;
            IsValid = true;
        }
    }

    // === 4. ИНТЕРФЕЙСЫ СИСТЕМЫ ===
    public interface IVerificationSystem
    {
        bool IsOperational { get; }
        int RequestCount { get; }
        void Start();
        bool PerformScan(FinancialReport report);
        void SetMalfunction();
    }

    public interface IDigitalRegistry
    {
        int EntryCount { get; }
        bool AddEntry(string protocolId);
        bool TryPrepareEntry();
        void FillToLimit();
    }

    public interface IAuditor
    {
        string Name { get; }
        PersonnelState State { get; } // Для Scenario4
        AuditorState AuditorState { get; } // Для AuditorTests
        int ProtocolsSigned { get; }
        void TakeDuty();
        void GoOnLeave();
        void AssignTask();
        void SignProtocol(AuditProtocol protocol);
        bool SignForReport(FinancialReport report, AuditProtocol protocol, IDigitalRegistry registry);
    }

    public interface ICheifInspector
    {
        void TakeDuty();
        void OffDuty();
        bool ProcessAudit(FinancialReport report, IAuditor auditor, IVerificationSystem system, IDigitalRegistry registry);
    }

    // === 5. РЕАЛИЗАЦИЯ КЛАССОВ ===

    public class VerificationSystem : IVerificationSystem
    {
        public bool IsOperational { get; private set; } = false;
        public int RequestCount { get; private set; } = 0;

        public void Start() => IsOperational = true;
        public void SetMalfunction() => IsOperational = false;

        public bool PerformScan(FinancialReport report)
        {
            if (!IsOperational) throw new InvalidOperationException("System Malfunction");
            
            RequestCount++;
            if (RequestCount > 100) 
            {
                IsOperational = false;
                throw new InvalidOperationException("System Overload");
            }

            return report.Amount < 1000000m;
        }
    }

    public class DigitalRegistry : IDigitalRegistry
    {
        private int _limit = 1000;
        public int EntryCount { get; private set; } = 0;

        public bool AddEntry(string protocolId)
        {
            if (EntryCount >= _limit) return false;
            EntryCount++;
            return true;
        }

        public bool TryPrepareEntry() => EntryCount < _limit;
        public void FillToLimit() => EntryCount = _limit;
    }

    public class Auditor : IAuditor
    {
        public string Name { get; }
        public PersonnelState State { get; private set; } = PersonnelState.OffDuty;
        public AuditorState AuditorState { get; private set; } = AuditorState.Idle;
        public int ProtocolsSigned { get; private set; } = 0;

        public Auditor(string name) => Name = name;

        public void TakeDuty() { State = PersonnelState.Idle; AuditorState = AuditorState.Idle; }
        public void GoOnLeave() => AuditorState = AuditorState.OnLeave;
        public void AssignTask() 
        {
            if (AuditorState == AuditorState.OnLeave) throw new InvalidOperationException();
            AuditorState = AuditorState.Busy;
        }

        public void SignProtocol(AuditProtocol protocol)
        {
            if (State == PersonnelState.OffDuty) throw new InvalidOperationException();
            protocol.Sign("SIG-" + Name);
            ProtocolsSigned++;
        }

        public bool SignForReport(FinancialReport report, AuditProtocol protocol, IDigitalRegistry registry)
        {
            if (State == PersonnelState.OffDuty) 
                throw new UnauthorizedAccessException("Personnel not on duty");

            SignProtocol(protocol);
            
            if (registry.AddEntry(protocol.ProtocolId))
            {
                report.MarkAsArchived();
                return true;
            }
            
            report.SetStatus(ReportStatus.Rejected);
            return false;
        }
    }

    public class ChiefInspector : ICheifInspector
    {
        public string Name { get; }
        public bool IsActive { get; private set; } = false;

        public ChiefInspector(string name) => Name = name;

        public void TakeDuty() => IsActive = true;
        public void OffDuty() => IsActive = false;

        public bool ProcessAudit(FinancialReport report, IAuditor auditor, IVerificationSystem system, IDigitalRegistry registry)
        {
            if (report.Amount < 0) return false; // Базовая проверка
            
            report.Verify();
            
            // Вызов системы верификации (используется в Scenario3)
            system.PerformScan(report);

            var protocol = new AuditProtocol("P-" + report.ReportId);
            return auditor.SignForReport(report, protocol, registry);
        }

        // Для MethodTests
        public void Inspect(FinancialReport report, IVerificationSystem system)
        {
            system.PerformScan(report);
        }
    }

    // === 6. КОНТРОЛЛЕР И РЕЗУЛЬТАТЫ ===
    public record AuditBatchResult(int TotalProcessed, int SuccessCount);
    public record VerificationResult { public bool IsLegit; public List<string> FoundViolations = new(); }

    public class AuditController
    {
        private readonly ICheifInspector _inspector;
        private readonly IAuditor _auditor;
        private readonly IDigitalRegistry _registry;

        public AuditController(ICheifInspector inspector, IAuditor auditor)
        {
            _inspector = inspector;
            _auditor = auditor;
        }

        public AuditController(IAuditor auditor, IDigitalRegistry registry)
        {
            _auditor = auditor;
            _registry = registry;
        }

        public void ExecuteBatchAudit(IEnumerable<FinancialReport> reports)
        {
            foreach (var report in reports)
            {
                _inspector.ProcessAudit(report, _auditor, null, null);
            }
            _inspector.OffDuty();
        }

        public bool RunAudit(FinancialReport report)
        {
            if (_auditor.State == PersonnelState.OffDuty) return false;
            return true;
        }
    }
}