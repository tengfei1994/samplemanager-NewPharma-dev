using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using LoginPlan.ObjectModel;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.ObjectModel;
using Thermo.SampleManager.Server;

namespace NewPharma.InspectionRequest
{

internal sealed class InspectionRequestExecutionService
{
    private const string LoginPlanEntityName = "LOGIN_PLAN";
    private const string LotDetailsEntityName = "LOT_DETAILS";
    private const string JobHeaderEntityName = "JOB_HEADER";

    private readonly IEntityManager _entityManager;

    public InspectionRequestExecutionService(IEntityManager entityManager)
    {
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
        string loginPlanId = InspectionRequestLabTableTask.GetLoginPlanIdentity(request);
        string loginPlanVersion = InspectionRequestLabTableTask.GetLoginPlanVersion(request);
        bool useLastActiveVersion = GetBoolean(request, InspectionRequestConstants.FieldUseLastActiveVersion, true);
        string rootContextTable = GetString(request, InspectionRequestConstants.FieldRootContextTable);
        string rootContextId = GetString(request, InspectionRequestConstants.FieldRootContextId);

        if (string.IsNullOrWhiteSpace(loginPlanId))
        {
            throw new InvalidOperationException("Inspection Request does not specify a Login Plan.");
        }

        ExtendedLoginPlan loginPlan = ResolveLoginPlan(loginPlanId, loginPlanVersion, useLastActiveVersion);

        var jobs = new List<JobHeaderInternal>();
        var samples = new List<SampleInternal>();

        if (IsLotContext(rootContextTable))
        {
            LotDetailsInternal lot = ResolveRootEntity<LotDetailsInternal>(LotDetailsEntityName, rootContextId, "Lot");
            string rootTableName = GetString((IEntity)loginPlan, "ROOT_TABLE_NAME");
            if (!string.Equals(rootTableName, LotDetailsEntityName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Login Plan root table is {rootTableName}; expected {LotDetailsEntityName} for Lot context.");
            }

            loginPlan.JobCreationProcess(lot, ref jobs, ref samples);
        }
        else if (IsJobContext(rootContextTable))
        {
            JobHeaderInternal job = ResolveRootEntity<JobHeaderInternal>(JobHeaderEntityName, rootContextId, "Job");
            string rootTableName = GetString((IEntity)loginPlan, "ROOT_TABLE_NAME");
            if (!string.Equals(rootTableName, JobHeaderEntityName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Login Plan root table is {rootTableName}; expected {JobHeaderEntityName} for Job context.");
            }

            loginPlan.SampleCreationProcess(job, ref samples);
            jobs.Add(job);
        }
        else
        {
            throw new InvalidOperationException("Inspection Request root context must be LOT_DETAILS or JOB_HEADER.");
        }

        _entityManager.Commit();

        return new ExecutionResult
        {
            PrimaryJobId = ToIdentityText((IEntity)jobs.FirstOrDefault()),
            JobIds = jobs.Select(x => ToIdentityText((IEntity)x)).Where(x => x.Length > 0).ToArray(),
            SampleIds = samples.Select(x => ToIdentityText((IEntity)x)).Where(x => x.Length > 0).ToArray(),
            TestIds = Array.Empty<string>()
        };
    }

    private ExtendedLoginPlan ResolveLoginPlan(string loginPlanId, string loginPlanVersion, bool useLastActiveVersion)
    {
        object entity;

        if (useLastActiveVersion || string.IsNullOrWhiteSpace(loginPlanVersion))
        {
            entity = _entityManager.SelectLatestVersion(LoginPlanEntityName, loginPlanId);
        }
        else
        {
            entity = _entityManager.Select(LoginPlanEntityName, new Identity(loginPlanId, loginPlanVersion));
        }

        var loginPlan = entity as ExtendedLoginPlan;
        if (!(loginPlan?.IsValid() ?? false))
        {
            throw new InvalidOperationException($"Login Plan was not found or is invalid: {loginPlanId} {loginPlanVersion}".Trim());
        }

        return loginPlan;
    }

    private T ResolveRootEntity<T>(string entityName, string identity, string label)
        where T : class, IEntity
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new InvalidOperationException($"{label} context id is required.");
        }

        var entity = _entityManager.Select(entityName, new Identity(identity)) as T;
        if (!(entity?.IsValid() ?? false))
        {
            throw new InvalidOperationException($"{label} context was not found: {identity}");
        }

        return entity;
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

    private static bool IsLotContext(string tableName)
    {
        return string.Equals(tableName, LotDetailsEntityName, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(tableName, "LOT", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJobContext(string tableName)
    {
        return string.Equals(tableName, JobHeaderEntityName, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(tableName, "JOB", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToIdentityText(IEntity entity)
    {
        return entity?.Identity?.ToString() ?? string.Empty;
    }

    private sealed class ExecutionResult
    {
        public string PrimaryJobId { get; init; }
        public string[] JobIds { get; init; } = Array.Empty<string>();
        public string[] SampleIds { get; init; } = Array.Empty<string>();
        public string[] TestIds { get; init; } = Array.Empty<string>();
    }
}
}
