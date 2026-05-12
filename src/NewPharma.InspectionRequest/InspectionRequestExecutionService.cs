using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using LoginPlan.ObjectModel;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Core.Definition;
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
    private readonly Func<string, string, ISchemaField> _findSchemaField;

    public InspectionRequestExecutionService(
        IEntityManager entityManager,
        Func<string, string, ISchemaField> findSchemaField)
    {
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        _findSchemaField = findSchemaField ?? throw new ArgumentNullException(nameof(findSchemaField));
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
        var lifecycleService = new InspectionRequestLifecycleService(_entityManager);
        lifecycleService.Initialize(request);
        lifecycleService.MarkExecuting(request);
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

        ApplyInspectionRequestAssignments(request, jobs, samples);
        _entityManager.Commit();

        return new ExecutionResult
        {
            PrimaryJobId = ToIdentityText((IEntity)jobs.FirstOrDefault()),
            JobIds = jobs.Select(x => ToIdentityText((IEntity)x)).Where(x => x.Length > 0).ToArray(),
            SampleIds = samples.Select(x => ToIdentityText((IEntity)x)).Where(x => x.Length > 0).ToArray(),
            TestIds = Array.Empty<string>()
        };
    }

    private void ApplyInspectionRequestAssignments(
        IEntity request,
        IReadOnlyCollection<JobHeaderInternal> jobs,
        IReadOnlyCollection<SampleInternal> samples)
    {
        string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return;
        }

        foreach (IEntity entry in SelectRows(InspectionRequestConstants.TableIrLoginPlanEntry, requestId))
        {
            string tableName = GetString(entry, "TABLE_NAME");
            object entryOrderNumber = entry.Get("ORDER_NUMBER");
            IEntityCollection fields = SelectFields(requestId, entryOrderNumber);

            if (IsSampleTable(tableName))
            {
                ApplyFields(samples.Cast<IEntity>(), fields);
            }
            else if (IsJobTable(tableName))
            {
                ApplyFields(jobs.Cast<IEntity>(), fields);
            }
        }
    }

    private IEntityCollection SelectRows(string tableName, string requestId)
    {
        IQuery query = _entityManager.CreateQuery(tableName);
        query.AddEquals("REQUEST_ID", requestId);
        return _entityManager.Select(query);
    }

    private IEntityCollection SelectFields(string requestId, object entryOrderNumber)
    {
        IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableIrLoginPlanField);
        query.AddEquals("REQUEST_ID", requestId);
        query.AddEquals("PARENT_ORDER_NUMBER", entryOrderNumber);
        return _entityManager.Select(query);
    }

    private void ApplyFields(IEnumerable<IEntity> targets, IEntityCollection fields)
    {
        foreach (IEntity target in targets)
        {
            if (!(target?.IsValid() ?? false))
            {
                continue;
            }

            foreach (IEntity field in fields)
            {
                string propertyName = GetString(field, "PROPERTY");
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    continue;
                }

                target.Set(propertyName, ResolveAssignmentValue(target, propertyName, field.Get("OVERRIDE_VALUE")));
            }
        }
    }

    private object ResolveAssignmentValue(IEntity target, string propertyName, object rawValue)
    {
        string text = rawValue?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return rawValue;
        }

        ISchemaField schemaField = FindSchemaField(target, propertyName);
        if (schemaField == null)
        {
            return rawValue;
        }

        if (schemaField.LinkTable != null)
        {
            object entity = SelectLinkedEntity(schemaField, text);
            return entity ?? rawValue;
        }

        if (schemaField.PhraseValid)
        {
            object phrase = _entityManager.SelectPhrase(schemaField.PhraseType, text);
            return phrase ?? rawValue;
        }

        if (schemaField.DatabaseType == DataVariableType.DataTypeBoolean)
        {
            return string.Equals(text, "T", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "1", StringComparison.OrdinalIgnoreCase);
        }

        if (schemaField.DatabaseType == DataVariableType.DataTypeInteger &&
            int.TryParse(text, out int integerValue))
        {
            return integerValue;
        }

        if ((schemaField.DatabaseType == DataVariableType.DataTypePackedDecimal ||
             schemaField.DatabaseType == DataVariableType.DataTypeReal) &&
            decimal.TryParse(text, out decimal decimalValue))
        {
            return decimalValue;
        }

        if (schemaField.DatabaseType == DataVariableType.DataTypeDate &&
            DateTime.TryParse(text, out DateTime dateValue))
        {
            return dateValue;
        }

        return rawValue;
    }

    private ISchemaField FindSchemaField(IEntity target, string propertyName)
    {
        string tableName = target?.EntityType;
        if (string.IsNullOrWhiteSpace(tableName) ||
            string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        return _findSchemaField(tableName, propertyName);
    }

    private IEntity SelectLinkedEntity(ISchemaField schemaField, string value)
    {
        if (schemaField?.LinkTable == null || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            string[] ids = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            object[] fields = new object[schemaField.LinkTable.KeyFields.Count];
            for (int i = 0; i < fields.Length && i < ids.Length; i++)
            {
                fields[i] = ids[i];
            }

            return _entityManager.Select(schemaField.LinkTable.Name, new Identity(fields));
        }
        catch
        {
            return null;
        }
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
        request.Set(InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionExecuted);
        request.Set(InspectionRequestConstants.FieldExecutionCompletedOn, DateTime.Now);
        request.Set(InspectionRequestConstants.FieldGeneratedJobId, result.PrimaryJobId ?? string.Empty);
        request.Set(InspectionRequestConstants.FieldGeneratedObjectSummary, JsonSerializer.Serialize(result));
        var lifecycleService = new InspectionRequestLifecycleService(_entityManager);
        lifecycleService.Initialize(request);
        lifecycleService.Move(request, "EXECUTED");
    }

    private void MarkFailed(IEntity request, Exception ex)
    {
        request.Set(InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionFailed);
        request.Set(InspectionRequestConstants.FieldExecutionCompletedOn, DateTime.Now);
        request.Set(InspectionRequestConstants.FieldExecutionError, ex.ToString());
        var lifecycleService = new InspectionRequestLifecycleService(_entityManager);
        lifecycleService.Initialize(request);
        lifecycleService.MarkExecutionFailed(request);
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

    private static bool IsSampleTable(string tableName)
    {
        return string.Equals(tableName, "SAMPLE", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(tableName, "SAMPLE_DETAILS", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJobTable(string tableName)
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
