using System;
using System.Text.Json;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Server;

namespace NewPharma.InspectionRequest;

internal sealed class InspectionRequestExecutionService
{
    private readonly Library _library;
    private readonly IEntityManager _entityManager;

    public InspectionRequestExecutionService(Library library, IEntityManager entityManager)
    {
        _library = library ?? throw new ArgumentNullException(nameof(library));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
    }

    public void Execute(IEntity request)
    {
        ValidateRequest(request);
        MarkExecuting(request);

        try
        {
            ExecutionResult result = ExecuteLoginPlan(request);
            MarkExecuted(request, result);
        }
        catch (Exception ex)
        {
            MarkFailed(request, ex);
            throw;
        }
    }

    private static void ValidateRequest(IEntity request)
    {
        string status = GetString(request, InspectionRequestConstants.FieldStatus);
        string executionStatus = GetString(request, InspectionRequestConstants.FieldExecutionStatus);
        string generatedSummary = GetString(request, InspectionRequestConstants.FieldGeneratedObjectSummary);

        if (!string.Equals(status, InspectionRequestConstants.StatusApproved, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Inspection Request must be Approved before execution.");
        }

        if (string.Equals(executionStatus, InspectionRequestConstants.ExecutionExecuting, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(executionStatus, InspectionRequestConstants.ExecutionExecuted, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Inspection Request has already started or completed execution.");
        }

        if (!string.IsNullOrWhiteSpace(generatedSummary))
        {
            throw new InvalidOperationException("Inspection Request already contains generated object references.");
        }
    }

    private void MarkExecuting(IEntity request)
    {
        request.Set(InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusExecuting);
        request.Set(InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionExecuting);
        request.Set(InspectionRequestConstants.FieldExecutionStartedOn, DateTime.Now);
        request.Set(InspectionRequestConstants.FieldExecutionError, string.Empty);
        _entityManager.Commit();
    }

    private ExecutionResult ExecuteLoginPlan(IEntity request)
    {
        string loginPlanId = GetString(request, InspectionRequestConstants.FieldLoginPlanId);
        string loginPlanVersion = GetString(request, InspectionRequestConstants.FieldLoginPlanVersion);
        bool useLastActiveVersion = GetBoolean(request, InspectionRequestConstants.FieldUseLastActiveVersion, true);
        string rootContextTable = GetString(request, InspectionRequestConstants.FieldRootContextTable);
        string rootContextId = GetString(request, InspectionRequestConstants.FieldRootContextId);

        if (string.IsNullOrWhiteSpace(loginPlanId))
        {
            throw new InvalidOperationException("Inspection Request does not specify a Login Plan.");
        }

        // Integration point:
        // 1. Resolve LoginPlan.ObjectModel.ExtendedLoginPlan by loginPlanId/loginPlanVersion.
        // 2. Resolve optional Lot/Job context from rootContextTable/rootContextId.
        // 3. Prefer the same supported execution path used by LP_CREATE_ENTITY rather than modifying vendor code.
        // 4. Return generated Job/Sample/Test references.
        //
        // This skeleton deliberately does not hard-code the vendor call until the VGSM table names,
        // context object, and supported API call are confirmed during the first compile/test cycle.
        throw new NotImplementedException(
            $"Login Plan execution hook pending. LoginPlan={loginPlanId}, Version={loginPlanVersion}, LastActive={useLastActiveVersion}, Context={rootContextTable}:{rootContextId}");
    }

    private void MarkExecuted(IEntity request, ExecutionResult result)
    {
        request.Set(InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusExecuted);
        request.Set(InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionExecuted);
        request.Set(InspectionRequestConstants.FieldExecutionCompletedOn, DateTime.Now);
        request.Set(InspectionRequestConstants.FieldGeneratedJobId, result.PrimaryJobId ?? string.Empty);
        request.Set(InspectionRequestConstants.FieldGeneratedObjectSummary, JsonSerializer.Serialize(result));
        _entityManager.Commit();
    }

    private void MarkFailed(IEntity request, Exception ex)
    {
        request.Set(InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusExecutionFailed);
        request.Set(InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionFailed);
        request.Set(InspectionRequestConstants.FieldExecutionCompletedOn, DateTime.Now);
        request.Set(InspectionRequestConstants.FieldExecutionError, ex.ToString());
        _entityManager.Commit();
    }

    private static string GetString(IEntity entity, string fieldName)
    {
        object value = entity.Get(fieldName);
        return value?.ToString() ?? string.Empty;
    }

    private static bool GetBoolean(IEntity entity, string fieldName, bool defaultValue)
    {
        object value = entity.Get(fieldName);
        if (value == null)
        {
            return defaultValue;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        return bool.TryParse(value.ToString(), out bool parsed) ? parsed : defaultValue;
    }

    private sealed class ExecutionResult
    {
        public string? PrimaryJobId { get; init; }
        public string[] JobIds { get; init; } = Array.Empty<string>();
        public string[] SampleIds { get; init; } = Array.Empty<string>();
        public string[] TestIds { get; init; } = Array.Empty<string>();
    }
}
