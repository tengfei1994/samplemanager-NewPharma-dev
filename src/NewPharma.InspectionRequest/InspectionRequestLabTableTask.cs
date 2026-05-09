using System;
using System.Reflection;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Tasks;

namespace NewPharma.InspectionRequest
{
    /// <summary>
    /// Standard LabTable maintenance entry for the NewPharma Inspection Request table.
    /// </summary>
    [SampleManagerTask("NewPharmaInspectionRequestTask", "LABTABLE", InspectionRequestConstants.EntityName)]
    public class InspectionRequestLabTableTask : GenericLabtableTask
    {
        private bool _snapshotRefreshRequested;

        protected override void MainFormCreated()
        {
            base.MainFormCreated();

            if (!string.Equals(Context.LaunchMode, "ADD", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (MainForm?.Entity is not IEntity request)
            {
                return;
            }

            var lifecycleService = new InspectionRequestLifecycleService(EntityManager);
            lifecycleService.EnsureConfiguration();

            string requestId = GenerateRequestId();

            SetIfEmpty(request, InspectionRequestConstants.FieldRequestId, requestId);
            SetIfEmpty(request, InspectionRequestConstants.FieldIdText, requestId);
            SetIfEmpty(request, InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusDraft);
            SetIfEmpty(request, InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionNotExecuted);
            SetIfEmpty(request, InspectionRequestConstants.FieldUseLastActiveVersion, true);
            SetIfEmpty(request, InspectionRequestConstants.FieldEsigRequired, false);
            SetIfEmpty(request, InspectionRequestConstants.FieldRequestedOn, DateTime.Now);
            lifecycleService.Initialize(request);

            if (Library.Environment.CurrentUser != null)
            {
                SetIfEmpty(request, InspectionRequestConstants.FieldRequestedBy, Library.Environment.CurrentUser);
            }

            Library.Task.StateModified();
        }

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            if (MainForm?.Entity is IEntity request)
            {
                request.PropertyChanged += Request_PropertyChanged;
                new InspectionRequestSnapshotService(EntityManager).EnsureSnapshot(request, false);
                EntityManager.Commit();
                RefreshSnapshotCollections();
                PopulateDataAssignmentHeader(request);
            }
        }

        protected override bool OnPreSave()
        {
            if (MainForm?.Entity is IEntity request)
            {
                var lifecycleService = new InspectionRequestLifecycleService(EntityManager);
                lifecycleService.EnsureConfiguration();
                EnsureRequestDefaults(request);
                SyncLoginPlanSelection(request);
                lifecycleService.Initialize(request);
                ValidateRequest(request);
            }

            return base.OnPreSave();
        }

        protected override void OnPostSave()
        {
            base.OnPostSave();

            if (MainForm?.Entity is IEntity request &&
                (string.Equals(Context.LaunchMode, "ADD", StringComparison.OrdinalIgnoreCase) || _snapshotRefreshRequested))
            {
                var snapshotService = new InspectionRequestSnapshotService(EntityManager);
                snapshotService.EnsureSnapshot(request, _snapshotRefreshRequested);
                new InspectionRequestLifecycleService(EntityManager).LinkCurrentNode(request);
                EntityManager.Commit();
                RefreshSnapshotCollections();
                PopulateDataAssignmentHeader(request);
                _snapshotRefreshRequested = false;
            }
        }

        private void Request_PropertyChanged(object sender, PropertyEventArgs e)
        {
            if (string.Equals(e.PropertyName, "LoginPlan", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldLoginPlanId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldLoginPlanIdProperty, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldLoginPlanVersionProperty, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldLoginPlanVersion, StringComparison.OrdinalIgnoreCase))
            {
                if (sender is IEntity request)
                {
                    SyncLoginPlanSelection(request);
                    Library.Task.StateModified();
                }

                _snapshotRefreshRequested = true;
            }
        }

        private static void EnsureRequestDefaults(IEntity request)
        {
            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            if (string.IsNullOrWhiteSpace(requestId) || string.Equals(requestId, "TBD", StringComparison.OrdinalIgnoreCase))
            {
                requestId = GenerateRequestId();
                request.Set(InspectionRequestConstants.FieldRequestId, requestId);
            }

            SetIfEmpty(request, InspectionRequestConstants.FieldIdText, requestId);
            SetIfEmpty(request, InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusDraft);
            SetIfEmpty(request, InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionNotExecuted);
            SetIfEmpty(request, InspectionRequestConstants.FieldUseLastActiveVersion, true);
        }

        private void ValidateRequest(IEntity request)
        {
            if (string.IsNullOrWhiteSpace(GetLoginPlanIdentity(request)))
            {
                Library.Utils.FlashMessage("Please select a Login Plan before saving the Inspection Request.", "Inspection Request");
                throw new InvalidOperationException("Inspection Request requires a Login Plan.");
            }
        }

        private static void SyncLoginPlanSelection(IEntity request)
        {
            string loginPlanId = GetLoginPlanIdentity(request);
            string loginPlanVersion = GetFirstString(
                request,
                InspectionRequestConstants.FieldLoginPlanVersionProperty,
                InspectionRequestConstants.FieldLoginPlanVersion);

            object selectedLoginPlan = TryGet(request, InspectionRequestConstants.FieldLoginPlan);
            if (selectedLoginPlan is IEntity loginPlan)
            {
                loginPlanId = ToIdentityText(loginPlan);
                loginPlanVersion = GetFirstString(loginPlan, "Version", "VERSION");
            }

            if (!string.IsNullOrWhiteSpace(loginPlanId))
            {
                SetIfDifferent(request, InspectionRequestConstants.FieldLoginPlanId, loginPlanId);
                SetIfDifferent(request, InspectionRequestConstants.FieldLoginPlanIdProperty, loginPlanId);
            }

            if (!string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                SetIfDifferent(request, InspectionRequestConstants.FieldLoginPlanVersion, loginPlanVersion);
                SetIfDifferent(request, InspectionRequestConstants.FieldLoginPlanVersionProperty, loginPlanVersion);
            }
        }

        internal static string GetLoginPlanIdentity(IEntity request)
        {
            object selectedLoginPlan = TryGet(request, InspectionRequestConstants.FieldLoginPlan);
            if (selectedLoginPlan is IEntity loginPlan)
            {
                return ToIdentityText(loginPlan);
            }

            return GetFirstString(
                request,
                InspectionRequestConstants.FieldLoginPlanIdProperty,
                InspectionRequestConstants.FieldLoginPlanId,
                InspectionRequestConstants.FieldLoginPlan);
        }

        internal static string GetLoginPlanVersion(IEntity request)
        {
            object selectedLoginPlan = TryGet(request, InspectionRequestConstants.FieldLoginPlan);
            if (selectedLoginPlan is IEntity loginPlan)
            {
                return GetFirstString(loginPlan, "Version", "VERSION");
            }

            return GetFirstString(
                request,
                InspectionRequestConstants.FieldLoginPlanVersionProperty,
                InspectionRequestConstants.FieldLoginPlanVersion);
        }

        private static void SetIfEmpty(IEntity entity, string fieldName, object value)
        {
            object current = TryGet(entity, fieldName);
            if (current == null || string.IsNullOrWhiteSpace(current.ToString()))
            {
                TrySet(entity, fieldName, value);
            }
        }

        private static void SetIfDifferent(IEntity entity, string fieldName, object value)
        {
            object current = TryGet(entity, fieldName);
            if (!string.Equals(current?.ToString() ?? string.Empty, value?.ToString() ?? string.Empty, StringComparison.Ordinal))
            {
                TrySet(entity, fieldName, value);
            }
        }

        private static object TryGet(IEntity entity, string fieldName)
        {
            try
            {
                return entity.Get(fieldName);
            }
            catch
            {
                return null;
            }
        }

        private static void TrySet(IEntity entity, string fieldName, object value)
        {
            try
            {
                entity.Set(fieldName, value);
            }
            catch
            {
            }
        }

        private static string GetString(IEntity entity, string fieldName)
        {
            object value = TryGet(entity, fieldName);
            return value?.ToString() ?? string.Empty;
        }

        private static string GetFirstString(IEntity entity, params string[] fieldNames)
        {
            foreach (string fieldName in fieldNames)
            {
                string value = GetString(entity, fieldName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string ToIdentityText(IEntity entity)
        {
            string identity = GetFirstString(entity, "Identity", "IDENTITY");
            if (!string.IsNullOrWhiteSpace(identity))
            {
                return identity;
            }

            return entity?.Identity?.ToString() ?? string.Empty;
        }

        private static string GenerateRequestId()
        {
            return "NPHIR" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        private void RefreshSnapshotCollections()
        {
            TryInvoke(MainForm?.Entity, "Reload");

            foreach (string componentName in new[]
            {
                "DataAssignmentEntries",
                "DataAssignmentFields",
                "DataAssignmentTests",
                "DataAssignmentTestFields",
                "ProductSpecRows"
            })
            {
                object component = TryGetMember(MainForm, componentName);
                TryInvoke(component, "Reload");
                TryInvoke(component, "Refresh");
                TryInvoke(component, "Requery");
                TryInvoke(component, "Load");
            }
        }

        private void PopulateDataAssignmentHeader(IEntity request)
        {
            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return;
            }

            IEntity entry = SelectFirstDataAssignmentEntry(requestId);
            if (entry == null || !entry.IsValid())
            {
                ClearDataAssignmentHeader();
                return;
            }

            string loginWorkflow = ResolveWorkflowName(
                GetString(entry, "LOGIN_WORKFLOW_ID"),
                GetString(entry, "LOGIN_WORKFLOW_VERSION"));
            string entityTemplate = ResolveEntityTemplateName(
                GetString(entry, "ENTITY_TEMPLATE_ID"),
                GetString(entry, "ENTITY_TEMPLATE_VERSION"));

            SetControlValue("DataAssignmentEntryName", GetString(entry, "NODE_NAME"));
            SetControlValue("DataAssignmentLoginWorkflow", loginWorkflow);
            SetControlValue("DataAssignmentLastActiveWorkflow", ToBooleanText(TryGet(entry, "USE_LAST_ACTIVE_LOGIN_WORKFLOW")));
            SetControlValue("DataAssignmentEntityTemplate", entityTemplate);
            SetControlValue("DataAssignmentEntryTable", GetString(entry, "TABLE_NAME"));
            SetControlValue("DataAssignmentLoginCondition", GetString(entry, "LOGIN_CONDITION"));
            SetControlValue("DataAssignmentCountExpression", GetString(entry, "COUNT_EXPRESSION"));
        }

        private IEntity SelectFirstDataAssignmentEntry(string requestId)
        {
            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableIrLoginPlanEntry);
            query.AddEquals("REQUEST_ID", requestId);

            IEntity first = null;
            foreach (IEntity entry in EntityManager.Select(query))
            {
                if (first == null || CompareOrder(entry, first) < 0)
                {
                    first = entry;
                }
            }

            return first;
        }

        private string ResolveWorkflowName(string workflowId, string workflowVersion)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
            {
                return string.Empty;
            }

            IEntity workflow = string.IsNullOrWhiteSpace(workflowVersion)
                ? EntityManager.SelectLatestVersion(InspectionRequestConstants.TableWorkflow, workflowId) as IEntity
                : EntityManager.Select(InspectionRequestConstants.TableWorkflow, new Identity(workflowId, workflowVersion)) as IEntity;

            return workflow == null || !workflow.IsValid()
                ? workflowId
                : GetFirstString(workflow, "Name", "NAME", "WorkflowName", "WORKFLOW_NAME", "IDENTITY");
        }

        private string ResolveEntityTemplateName(string entityTemplateId, string entityTemplateVersion)
        {
            if (string.IsNullOrWhiteSpace(entityTemplateId))
            {
                return string.Empty;
            }

            IEntity entityTemplate = string.IsNullOrWhiteSpace(entityTemplateVersion)
                ? EntityManager.SelectLatestVersion(InspectionRequestConstants.TableEntityTemplate, entityTemplateId) as IEntity
                : EntityManager.Select(InspectionRequestConstants.TableEntityTemplate, new Identity(entityTemplateId, entityTemplateVersion)) as IEntity;

            return entityTemplate == null || !entityTemplate.IsValid()
                ? entityTemplateId
                : GetFirstString(entityTemplate, "Name", "NAME", "IDENTITY");
        }

        private void ClearDataAssignmentHeader()
        {
            foreach (string controlName in new[]
            {
                "DataAssignmentEntryName",
                "DataAssignmentLoginWorkflow",
                "DataAssignmentLastActiveWorkflow",
                "DataAssignmentEntityTemplate",
                "DataAssignmentEntryTable",
                "DataAssignmentLoginCondition",
                "DataAssignmentCountExpression"
            })
            {
                SetControlValue(controlName, string.Empty);
            }
        }

        private void SetControlValue(string controlName, object value)
        {
            object control = TryGetMember(MainForm, controlName);
            if (control == null)
            {
                return;
            }

            TrySetMember(control, "Value", value);
            TrySetMember(control, "Text", value?.ToString() ?? string.Empty);
            TrySetMember(control, "RawText", value?.ToString() ?? string.Empty);
            TrySetMember(control, "Checked", IsTrue(value));
            TrySetMember(control, "Enabled", false);
            TrySetMember(control, "ReadOnly", true);
        }

        private static int CompareOrder(IEntity left, IEntity right)
        {
            decimal leftOrder = ToDecimal(TryGet(left, "ORDER_NUMBER"));
            decimal rightOrder = ToDecimal(TryGet(right, "ORDER_NUMBER"));
            return leftOrder.CompareTo(rightOrder);
        }

        private static decimal ToDecimal(object value)
        {
            return decimal.TryParse(value?.ToString(), out decimal number) ? number : decimal.Zero;
        }

        private static string ToBooleanText(object value)
        {
            return IsTrue(value) ? "True" : "False";
        }

        private static bool IsTrue(object value)
        {
            string text = value?.ToString() ?? string.Empty;
            return string.Equals(text, "T", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "1", StringComparison.OrdinalIgnoreCase);
        }

        private static object TryGetMember(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = target.GetType();
            return type.GetProperty(memberName, flags)?.GetValue(target) ??
                   type.GetField(memberName, flags)?.GetValue(target);
        }

        private static void TrySetMember(object target, string memberName, object value)
        {
            if (target == null)
            {
                return;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = target.GetType();

            try
            {
                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(target, ConvertValue(value, property.PropertyType));
                    return;
                }

                FieldInfo field = type.GetField(memberName, flags);
                if (field != null)
                {
                    field.SetValue(target, ConvertValue(value, field.FieldType));
                }
            }
            catch
            {
            }
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (targetType == typeof(bool))
            {
                return IsTrue(value);
            }

            if (targetType == typeof(string))
            {
                return value?.ToString() ?? string.Empty;
            }

            return value;
        }

        private static void TryInvoke(object target, string methodName)
        {
            if (target == null)
            {
                return;
            }

            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (method == null)
            {
                return;
            }

            try
            {
                method.Invoke(target, null);
            }
            catch
            {
            }
        }
    }
}
