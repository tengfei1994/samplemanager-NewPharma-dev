using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using Thermo.Framework.Utilities;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Common.Workflow;
using Thermo.SampleManager.Core.Definition;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.ClientControls.Browse;
using Thermo.SampleManager.Library.EntityDefinition;
using Thermo.SampleManager.ObjectModel;
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
        private readonly ISet<object> _dataAssignmentEditorGrids = new HashSet<object>(ReferenceComparer.Instance);
        private readonly IDictionary<string, IEntity> _dataAssignmentFieldsByRow = new Dictionary<string, IEntity>(StringComparer.Ordinal);
        private System.Windows.Forms.Timer _dataAssignmentEditorTimer;
        private int _dataAssignmentEditorAttachAttempts;
        private bool _dataAssignmentGridRowsLoaded;
        private object _dataAssignmentPageRoot;
        private bool _loadingDataAssignmentGrid;
        private readonly ISet<object> _templateFieldEditorGrids = new HashSet<object>(ReferenceComparer.Instance);
        private readonly IDictionary<string, string> _templateFieldPropertyByRow = new Dictionary<string, string>(StringComparer.Ordinal);
        private System.Windows.Forms.Timer _templateFieldEditorTimer;
        private int _templateFieldEditorAttachAttempts;
        private bool _templateFieldRowsLoaded;
        private object _entityTemplatePageRoot;
        private bool _loadingTemplateFieldGrid;
        private string _previewLoginPlanId;
        private string _previewLoginPlanVersion;
        private readonly List<IEntity> _previewDataAssignmentEntries = new();
        private readonly List<IEntity> _previewDataAssignmentFields = new();
        private readonly List<IEntity> _previewDataAssignmentTests = new();
        private readonly List<IEntity> _previewDataAssignmentTestFields = new();
        private readonly List<IEntity> _previewProducts = new();

        protected override void MainFormCreated()
        {
            base.MainFormCreated();

            if (MainForm?.Entity is not IEntity request)
            {
                return;
            }

            var lifecycleService = new InspectionRequestLifecycleService(EntityManager);

            EnsureRequestDefaults(request);
            SetIfEmpty(request, InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusDraft);
            SetIfEmpty(request, InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionNotExecuted);
            SetIfEmpty(request, InspectionRequestConstants.FieldUseLastActiveVersion, true);
            SetIfEmpty(request, InspectionRequestConstants.FieldEsigRequired, false);
            SetIfEmpty(request, InspectionRequestConstants.FieldApprovalRequired, false);
            SetIfEmpty(request, InspectionRequestConstants.FieldApprovalStatus, "A");
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
                MainForm.Selected += MainForm_Selected;
                request.PropertyChanged += Request_PropertyChanged;
                if (CanCommitSnapshotDuringEdit(request) &&
                    new InspectionRequestSnapshotService(EntityManager).EnsureSnapshot(request, false))
                {
                    EntityManager.Commit();
                }

                RefreshSnapshotCollections();
                PopulateDataAssignmentHeader(request);
            }
        }

        protected override bool OnPreSave()
        {
            if (MainForm?.Entity is IEntity request)
            {
                var lifecycleService = new InspectionRequestLifecycleService(EntityManager);
                EnsureRequestDefaults(request);
                SyncTemplateFieldGridValues();
                SyncDataAssignmentGridValues();
                MaterializePreviewSnapshotRows(request);
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
                StartTemplateFieldEditorAttachPolling();
                _templateFieldRowsLoaded = false;
                PopulateTemplateFieldGrid(request);
                TryPopulateTemplateFieldsWhenReady();
                StartDataAssignmentEditorAttachPolling();
                PopulateDataAssignmentHeader(request);
                _dataAssignmentGridRowsLoaded = false;
                PopulateDataAssignmentGrids(request);
                TryPopulateDataAssignmentGridsWhenReady();
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
                    EnsureLoginPlanPreviewCurrent(request);
                    PopulateDataAssignmentHeader(request);
                    _dataAssignmentGridRowsLoaded = false;
                    PopulateDataAssignmentGrids(request);
                    TryPopulateDataAssignmentGridsWhenReady();
                    Library.Task.StateModified();
                }

                _snapshotRefreshRequested = true;
            }

            if (string.Equals(e.PropertyName, InspectionRequestConstants.FieldEntityTemplate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldEntityTemplateId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldEntityTemplateIdProperty, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldEntityTemplateVersion, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldEntityTemplateVersionProperty, StringComparison.OrdinalIgnoreCase))
            {
                if (sender is IEntity request)
                {
                    SyncEntityTemplateSelection(request);
                    new InspectionRequestLifecycleService(EntityManager).ApplyEntityTemplateDefaults(request);
                    _templateFieldRowsLoaded = false;
                    PopulateTemplateFieldGrid(request);
                    TryPopulateTemplateFieldsWhenReady();
                    Library.Task.StateModified();
                }
            }
        }

        private bool CanCommitSnapshotDuringEdit(IEntity request)
        {
            return !string.Equals(Context.LaunchMode, "ADD", StringComparison.OrdinalIgnoreCase) &&
                   IsPersistedRequest(request);
        }

        private bool IsPersistedRequest(IEntity request)
        {
            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return false;
            }

            try
            {
                IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.EntityName);
                query.AddEquals(InspectionRequestConstants.FieldRequestId, requestId);
                return EntityManager.Select(query).Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void MainForm_Selected(object sender, SelectedEventArgs e)
        {
            if (string.Equals(e.FormName, "Page_EntityTemplate", StringComparison.OrdinalIgnoreCase))
            {
                _entityTemplatePageRoot = ResolveSelectedPageRoot(sender, e);
            }

            if (string.Equals(e.FormName, "Page_DataAssignment", StringComparison.OrdinalIgnoreCase))
            {
                _dataAssignmentPageRoot = ResolveSelectedPageRoot(sender, e);
            }

            if (!string.Equals(e.FormName, "Page_DataAssignment", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(e.FormName, "Page_EntityTemplate", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (MainForm?.Entity is not IEntity request)
            {
                return;
            }

            if (string.Equals(e.FormName, "Page_EntityTemplate", StringComparison.OrdinalIgnoreCase))
            {
                StartTemplateFieldEditorAttachPolling();
                PopulateTemplateFieldGrid(request);
                TryPopulateTemplateFieldsWhenReady();
                return;
            }

            SyncTemplateFieldGridValues();
            EnsureLoginPlanPreviewCurrent(request);
            PopulateDataAssignmentHeader(request);
            _dataAssignmentGridRowsLoaded = false;
            StartDataAssignmentEditorAttachPolling();
            PopulateDataAssignmentGrids(request);
            TryPopulateDataAssignmentGridsWhenReady();
        }

        protected override string GetWorkflowType()
        {
            return InspectionRequestConstants.WorkflowTypeInspectionRequestCreate;
        }

        protected override IEntity CreateNewEntity()
        {
            Workflow workflow = PromptForWorkflow();
            if (workflow == null || !workflow.IsValid())
            {
                Exit();
                return null;
            }

            Workflow = workflow;
            return RunWorkflow(workflow);
        }

        protected override void CreateFromWorkflow()
        {
            Workflow = PromptForWorkflow();
            if (Workflow == null || !Workflow.IsValid())
            {
                Exit();
                return;
            }

            IEntity entity = RunWorkflow(Workflow);
            if (entity == null || !entity.IsValid())
            {
                ShowEntityError();
                Exit();
                return;
            }

            ShowEditor(entity, null);
        }

        protected override Workflow PromptForWorkflow()
        {
            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableWorkflow);
            query.AddEquals("WorkflowType", GetWorkflowType());
            query.AddAnd();
            query.AddEquals("TableName", InspectionRequestConstants.EntityName);
            query.AddAnd();
            query.AddEquals("Active", true);

            string title = Context?.MenuItem?.Description ?? "Inspection Request Workflow Login";
            string text = "Select an Inspection Request login workflow.";

            FormResult result = Library.Utils.PromptForEntity(
                text,
                title,
                query,
                out IEntity selectedWorkflow,
                TriState.Default,
                Context?.MenuProcedureNumber ?? 0);

            return result == FormResult.OK
                ? selectedWorkflow as Workflow
                : null;
        }

        protected override IEntity RunWorkflow(IWorkflowDefinition workflow)
        {
            IEntity entity = RunWorkflow(workflow, InspectionRequestConstants.EntityName);
            if (entity != null && entity.IsValid())
            {
                new InspectionRequestLifecycleService(EntityManager).ApplyLoginWorkflowDefaults(entity, workflow);
            }

            return entity;
        }

        private object ResolveSelectedPageRoot(object sender, SelectedEventArgs e)
        {
            foreach (object root in new[]
            {
                TryGetDirectMember(e, "Form"),
                TryGetDirectMember(e, "SelectedForm"),
                TryGetDirectMember(e, "Page"),
                TryGetDirectMember(e, "SelectedPage"),
                TryGetDirectMember(e, "CurrentForm"),
                TryGetDirectMember(e, "Value"),
                TryGetDirectMember(sender, e?.FormName ?? string.Empty),
                TryGetMember(sender, e?.FormName ?? string.Empty)
            })
            {
                if (root != null)
                {
                    return root;
                }
            }

            return null;
        }

        private static void EnsureRequestDefaults(IEntity request)
        {
            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            if (string.IsNullOrWhiteSpace(requestId) ||
                string.Equals(requestId, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(requestId, "TBD", StringComparison.OrdinalIgnoreCase))
            {
                requestId = GenerateRequestId();
                request.Set(InspectionRequestConstants.FieldRequestId, requestId);
            }

            string idText = GetString(request, InspectionRequestConstants.FieldIdText);
            if (string.IsNullOrWhiteSpace(idText) ||
                string.Equals(idText, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(idText, "TBD", StringComparison.OrdinalIgnoreCase))
            {
                request.Set(InspectionRequestConstants.FieldIdText, requestId);
            }

            string name = GetFirstString(
                request,
                InspectionRequestConstants.FieldNameProperty,
                InspectionRequestConstants.FieldName);
            if (string.IsNullOrWhiteSpace(name) ||
                string.Equals(name, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "TBD", StringComparison.OrdinalIgnoreCase))
            {
                SetIfEmpty(request, InspectionRequestConstants.FieldNameProperty, requestId);
                SetIfEmpty(request, InspectionRequestConstants.FieldName, requestId);
            }

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

        private static void SyncEntityTemplateSelection(IEntity request)
        {
            string entityTemplateId = GetFirstString(
                request,
                InspectionRequestConstants.FieldEntityTemplateIdProperty,
                InspectionRequestConstants.FieldEntityTemplateId);
            string entityTemplateVersion = GetFirstString(
                request,
                InspectionRequestConstants.FieldEntityTemplateVersionProperty,
                InspectionRequestConstants.FieldEntityTemplateVersion);

            object selectedEntityTemplate = TryGet(request, InspectionRequestConstants.FieldEntityTemplate);
            if (selectedEntityTemplate is IEntity entityTemplate)
            {
                entityTemplateId = ToIdentityText(entityTemplate);
                entityTemplateVersion = GetFirstString(entityTemplate, "EntityTemplateVersion", "ENTITY_TEMPLATE_VERSION");
            }

            if (!string.IsNullOrWhiteSpace(entityTemplateId))
            {
                SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateId, entityTemplateId);
                SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateIdProperty, entityTemplateId);
            }

            if (!string.IsNullOrWhiteSpace(entityTemplateVersion))
            {
                SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateVersion, entityTemplateVersion);
                SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateVersionProperty, entityTemplateVersion);
            }
        }

        private static void SyncWorkflowSelection(IEntity request)
        {
            string workflowId = GetFirstString(
                request,
                InspectionRequestConstants.FieldLifecycleWorkflowIdProperty,
                InspectionRequestConstants.FieldLifecycleWorkflowId);
            string workflowVersion = GetFirstString(
                request,
                InspectionRequestConstants.FieldLifecycleWorkflowVersionProperty,
                InspectionRequestConstants.FieldLifecycleWorkflowVersion);

            object selectedWorkflow = TryGet(request, InspectionRequestConstants.FieldLifecycleWorkflow);
            if (selectedWorkflow is IEntity workflow)
            {
                workflowId = GetFirstString(workflow, "WorkflowGuid", "WORKFLOW_GUID");
                workflowVersion = GetFirstString(workflow, "WorkflowVersion", "WORKFLOW_VERSION");
            }

            if (!string.IsNullOrWhiteSpace(workflowId))
            {
                SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowId, workflowId);
                SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowIdProperty, workflowId);
            }

            if (!string.IsNullOrWhiteSpace(workflowVersion))
            {
                SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowVersion, workflowVersion);
                SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowVersionProperty, workflowVersion);
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
            return "NPHIR" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }

        private void RefreshSnapshotCollections()
        {
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

        private void SubscribeDataAssignmentEditors()
        {
            bool entryAttached = TrySubscribeDataAssignmentGrid("udgEntityGrid");
            bool testAttached = TrySubscribeDataAssignmentGrid("ugdTestData");

            if (entryAttached && testAttached)
            {
                TryPopulateDataAssignmentGridsWhenReady();
            }

            if (entryAttached && testAttached && _dataAssignmentGridRowsLoaded)
            {
                StopDataAssignmentEditorAttachPolling();
            }
        }

        private bool TrySubscribeDataAssignmentGrid(string gridName)
        {
            if (TryGetDataAssignmentMember(gridName) is not UnboundGrid grid)
            {
                return false;
            }

            if (_dataAssignmentEditorGrids.Add(grid))
            {
                grid.CellValueChanged += DataAssignmentGrid_CellValueChanged;
                grid.CellButtonClicked += DataAssignmentGrid_CellButtonClicked;
            }

            return true;
        }

        private void StartDataAssignmentEditorAttachPolling()
        {
            SubscribeDataAssignmentEditors();
            if (_dataAssignmentEditorTimer != null)
            {
                return;
            }

            _dataAssignmentEditorAttachAttempts = 0;
            _dataAssignmentEditorTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            _dataAssignmentEditorTimer.Tick += DataAssignmentEditorTimer_Tick;
            _dataAssignmentEditorTimer.Start();
        }

        private void StopDataAssignmentEditorAttachPolling()
        {
            if (_dataAssignmentEditorTimer == null)
            {
                return;
            }

            _dataAssignmentEditorTimer.Stop();
            _dataAssignmentEditorTimer.Tick -= DataAssignmentEditorTimer_Tick;
            _dataAssignmentEditorTimer.Dispose();
            _dataAssignmentEditorTimer = null;
        }

        private void DataAssignmentEditorTimer_Tick(object sender, EventArgs e)
        {
            _dataAssignmentEditorAttachAttempts++;
            SubscribeDataAssignmentEditors();
            TryPopulateDataAssignmentGridsWhenReady();

            if (_dataAssignmentEditorAttachAttempts >= 60)
            {
                StopDataAssignmentEditorAttachPolling();
            }
        }

        private void SubscribeTemplateFieldEditors()
        {
            bool attached = TrySubscribeTemplateFieldGrid();
            if (attached)
            {
                TryPopulateTemplateFieldsWhenReady();
            }

            if (attached && _templateFieldRowsLoaded)
            {
                StopTemplateFieldEditorAttachPolling();
            }
        }

        private bool TrySubscribeTemplateFieldGrid()
        {
            if (TryGetEntityTemplateMember("TemplateFieldsGrid") is not UnboundGrid grid)
            {
                return false;
            }

            if (_templateFieldEditorGrids.Add(grid))
            {
                grid.CellValueChanged += TemplateFieldsGrid_CellValueChanged;
                grid.CellButtonClicked += TemplateFieldsGrid_CellButtonClicked;
            }

            return true;
        }

        private void StartTemplateFieldEditorAttachPolling()
        {
            SubscribeTemplateFieldEditors();
            if (_templateFieldEditorTimer != null)
            {
                return;
            }

            _templateFieldEditorAttachAttempts = 0;
            _templateFieldEditorTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            _templateFieldEditorTimer.Tick += TemplateFieldEditorTimer_Tick;
            _templateFieldEditorTimer.Start();
        }

        private void StopTemplateFieldEditorAttachPolling()
        {
            if (_templateFieldEditorTimer == null)
            {
                return;
            }

            _templateFieldEditorTimer.Stop();
            _templateFieldEditorTimer.Tick -= TemplateFieldEditorTimer_Tick;
            _templateFieldEditorTimer.Dispose();
            _templateFieldEditorTimer = null;
        }

        private void TemplateFieldEditorTimer_Tick(object sender, EventArgs e)
        {
            _templateFieldEditorAttachAttempts++;
            SubscribeTemplateFieldEditors();
            TryPopulateTemplateFieldsWhenReady();

            if (_templateFieldEditorAttachAttempts >= 60)
            {
                StopTemplateFieldEditorAttachPolling();
            }
        }

        private void TryPopulateTemplateFieldsWhenReady()
        {
            if (_templateFieldRowsLoaded || MainForm?.Entity is not IEntity request)
            {
                return;
            }

            if (TryGetEntityTemplateMember("TemplateFieldsGrid") is not UnboundGrid grid)
            {
                return;
            }

            PopulateTemplateFieldGrid(request);
            _templateFieldRowsLoaded = grid.Rows.Count > 0;
        }

        private void TryPopulateDataAssignmentGridsWhenReady()
        {
            if (_dataAssignmentGridRowsLoaded || MainForm?.Entity is not IEntity request)
            {
                return;
            }

            object entryControl = TryGetDataAssignmentMember("udgEntityGrid");
            object testControl = TryGetDataAssignmentMember("ugdTestData");
            if (entryControl is not UnboundGrid entryGrid ||
                testControl is not UnboundGrid testGrid)
            {
                return;
            }

            PopulateDataAssignmentGrids(request);
            _dataAssignmentGridRowsLoaded = entryGrid.Rows.Count > 0 || testGrid.Rows.Count > 0;
        }

        private void DataAssignmentGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            if (_loadingDataAssignmentGrid ||
                sender is not UnboundGrid ||
                !TryGetFieldFromRow(e.Row, out IEntity field))
            {
                return;
            }

            string storedValue = ToStoredAssignmentValue(e.Value);
            if (ShouldIgnoreEmptyEditorEcho(field, e.Value, storedValue))
            {
                return;
            }

            field.Set("OVERRIDE_VALUE", storedValue);
            if (!IsPreviewSnapshotEntity(field))
            {
                EntityManager.Transaction.Add(field);
            }
            Library.Task.StateModified();
        }

        private void DataAssignmentGrid_CellButtonClicked(object sender, UnboundGridCellButtonClickedEventArgs e)
        {
            if (sender is not UnboundGrid grid || !TryGetFieldFromRow(e.Row, out IEntity field))
            {
                return;
            }

            string propertyName = GetString(field, "PROPERTY");
            string tableName = ResolveAssignmentTableName(field);
            ISchemaField schemaField = FindSchemaField(tableName, propertyName);
            if (schemaField?.LinkTable != null || schemaField?.PhraseValid == true)
            {
                return;
            }

            string valueBefore = e.Row.GetValue(grid.Columns[2].Name)?.ToString();
            string exprValue = Library.Formula.EditFormula(tableName, valueBefore);
            if (!string.Equals(valueBefore, exprValue, StringComparison.Ordinal))
            {
                e.Row.SetValue(grid.Columns[2].Name, exprValue);
                field.Set("OVERRIDE_VALUE", exprValue ?? string.Empty);
                if (!IsPreviewSnapshotEntity(field))
                {
                    EntityManager.Transaction.Add(field);
                }
                Library.Task.StateModified();
            }
        }

        private void SyncDataAssignmentGridValues()
        {
            SyncDataAssignmentGridValues("udgEntityGrid");
            SyncDataAssignmentGridValues("ugdTestData");
        }

        private void SyncDataAssignmentGridValues(string gridName)
        {
            if (TryGetDataAssignmentMember(gridName) is not UnboundGrid grid)
            {
                return;
            }

            foreach (UnboundGridRow row in grid.Rows)
            {
                if (TryGetFieldFromRow(row, out IEntity field))
                {
                    object cellValue = row.GetValue(grid.Columns[2].Name);
                    string storedValue = ToStoredAssignmentValue(cellValue);
                    if (ShouldIgnoreEmptyEditorEcho(field, cellValue, storedValue))
                    {
                        continue;
                    }

                    field.Set("OVERRIDE_VALUE", storedValue);
                    if (!IsPreviewSnapshotEntity(field))
                    {
                        EntityManager.Transaction.Add(field);
                    }
                }
            }
        }

        private void TemplateFieldsGrid_CellValueChanged(object sender, UnboundGridValueChangedEventArgs e)
        {
            if (_loadingTemplateFieldGrid ||
                sender is not UnboundGrid ||
                MainForm?.Entity is not IEntity request ||
                !TryGetTemplatePropertyFromRow(e.Row, out string propertyName))
            {
                return;
            }

            SetTemplateRuntimeValue(request, propertyName, e.Value);
            Library.Task.StateModified();
        }

        private void TemplateFieldsGrid_CellButtonClicked(object sender, UnboundGridCellButtonClickedEventArgs e)
        {
            if (sender is not UnboundGrid grid ||
                MainForm?.Entity is not IEntity request ||
                !TryGetTemplatePropertyFromRow(e.Row, out string propertyName))
            {
                return;
            }

            ISchemaField schemaField = FindSchemaField(InspectionRequestConstants.EntityName, propertyName);
            if (schemaField?.LinkTable != null || schemaField?.PhraseValid == true)
            {
                return;
            }

            string valueBefore = e.Row.GetValue(grid.Columns[1].Name)?.ToString();
            string exprValue = Library.Formula.EditFormula(InspectionRequestConstants.EntityName, valueBefore);
            if (!string.Equals(valueBefore, exprValue, StringComparison.Ordinal))
            {
                e.Row.SetValue(grid.Columns[1].Name, exprValue);
                SetTemplateRuntimeValue(request, propertyName, exprValue);
                Library.Task.StateModified();
            }
        }

        private void SyncTemplateFieldGridValues()
        {
            if (TryGetEntityTemplateMember("TemplateFieldsGrid") is not UnboundGrid grid ||
                MainForm?.Entity is not IEntity request)
            {
                return;
            }

            foreach (UnboundGridRow row in grid.Rows)
            {
                if (TryGetTemplatePropertyFromRow(row, out string propertyName))
                {
                    object cellValue = row.GetValue(grid.Columns[1].Name);
                    SetTemplateRuntimeValue(request, propertyName, cellValue);
                }
            }
        }

        private void PopulateTemplateFieldGrid(IEntity request)
        {
            if (TryGetEntityTemplateMember("TemplateFieldsGrid") is not UnboundGrid grid)
            {
                return;
            }

            _templateFieldPropertyByRow.Clear();
            try
            {
                _loadingTemplateFieldGrid = true;
                grid.BeginUpdate();
                try
                {
                    grid.ClearRows();

                    foreach (IEntity templateProperty in SelectTemplateProperties(request))
                    {
                        AddTemplateFieldRow(grid, request, templateProperty);
                    }

                    _templateFieldRowsLoaded = grid.Rows.Count > 0;
                }
                finally
                {
                    grid.EndUpdate();
                }
            }
            finally
            {
                _loadingTemplateFieldGrid = false;
            }
        }

        private void AddTemplateFieldRow(UnboundGrid grid, IEntity request, IEntity templateProperty)
        {
            string propertyName = GetTemplatePropertyName(templateProperty);
            if (string.IsNullOrWhiteSpace(propertyName) || IsStandaloneTemplateProperty(propertyName))
            {
                return;
            }

            string title = GetFirstString(templateProperty, "TITLE", "Title");
            string defaultText = GetFirstString(templateProperty, "DEFAULT_VALUE", "DefaultValue");
            string currentText = GetTemplateRuntimeStoredValue(request, propertyName);
            if (string.IsNullOrWhiteSpace(currentText))
            {
                currentText = defaultText;
            }

            object currentValue = ToGridValue(InspectionRequestConstants.EntityName, propertyName, currentText);

            UnboundGridRow row = grid.AddRow(string.IsNullOrWhiteSpace(title) ? propertyName : title, currentValue);
            row.Tag = GetTemplatePropertyRowKey(templateProperty, propertyName);
            _templateFieldPropertyByRow[row.Tag.ToString()] = propertyName;

            ConfigureValueCell(grid, row, InspectionRequestConstants.EntityName, propertyName, currentValue, 1);
            RestoreTypedDisplayValue(grid, row, InspectionRequestConstants.EntityName, propertyName, currentText, 1);
        }

        private IList<IEntity> SelectTemplateProperties(IEntity request)
        {
            string templateId = GetFirstString(
                request,
                InspectionRequestConstants.FieldEntityTemplateId,
                InspectionRequestConstants.FieldEntityTemplateIdProperty);
            string templateVersion = GetFirstString(
                request,
                InspectionRequestConstants.FieldEntityTemplateVersion,
                InspectionRequestConstants.FieldEntityTemplateVersionProperty);

            if (string.IsNullOrWhiteSpace(templateId))
            {
                templateId = InspectionRequestLifecycleService.DefaultEntityTemplateId;
            }

            if (string.IsNullOrWhiteSpace(templateVersion))
            {
                templateVersion = InspectionRequestLifecycleService.WorkflowVersion;
            }

            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableEntityTemplateProperty);
            query.AddEquals("ENTITY_TEMPLATE_ID", templateId);
            query.AddAnd();
            query.AddEquals("ENTITY_TEMPLATE_VERSION", templateVersion);

            var rows = new List<IEntity>();
            foreach (IEntity row in EntityManager.Select(query))
            {
                rows.Add(row);
            }

            return rows
                .OrderBy(row => ToDecimal(TryGet(row, "ORDER_NUM")))
                .ThenBy(row => GetTemplatePropertyName(row), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void SetTemplateRuntimeValue(IEntity request, string propertyName, object value)
        {
            if (request == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            if (value is IEntity entity && entity.IsValid())
            {
                TrySet(request, propertyName, entity);
                SyncTemplateSideEffects(request, propertyName, ToIdentityText(entity));
                return;
            }

            if (value is PhraseBase phrase && phrase.IsValid())
            {
                TrySet(request, propertyName, phrase.PhraseId);
                SyncTemplateSideEffects(request, propertyName, phrase.PhraseId);
                return;
            }

            string storedValue = ToStoredAssignmentValue(value);
            ISchemaField schemaField = FindSchemaField(InspectionRequestConstants.EntityName, propertyName);
            if (schemaField?.LinkTable != null && !string.IsNullOrWhiteSpace(storedValue))
            {
                IEntity linkedEntity = SelectLinkedEntity(schemaField, storedValue);
                if (linkedEntity != null && linkedEntity.IsValid())
                {
                    TrySet(request, propertyName, linkedEntity);
                    SyncTemplateSideEffects(request, propertyName, ToIdentityText(linkedEntity));
                    return;
                }
            }

            TrySet(request, propertyName, NormalizeTemplateRuntimeValue(schemaField, storedValue));
            SyncTemplateSideEffects(request, propertyName, storedValue);
        }

        private void SyncTemplateSideEffects(IEntity request, string propertyName, string storedValue)
        {
            if (!string.Equals(propertyName, InspectionRequestConstants.FieldLoginPlan, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string loginPlanId = string.Empty;
            string loginPlanVersion = string.Empty;
            IEntity loginPlan = TryGet(request, InspectionRequestConstants.FieldLoginPlan) as IEntity;

            if (!string.IsNullOrWhiteSpace(storedValue) &&
                (loginPlan == null || !loginPlan.IsValid()))
            {
                loginPlan = EntityManager.SelectLatestVersion(InspectionRequestConstants.TableLoginPlan, storedValue) as IEntity;
                if (loginPlan != null && loginPlan.IsValid())
                {
                    TrySet(request, InspectionRequestConstants.FieldLoginPlan, loginPlan);
                }
            }

            if (loginPlan != null && loginPlan.IsValid())
            {
                loginPlanId = ToIdentityText(loginPlan);
                loginPlanVersion = GetFirstString(loginPlan, "Version", "VERSION");
            }

            if (string.IsNullOrWhiteSpace(loginPlanId))
            {
                loginPlanId = storedValue ?? string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(loginPlanId) && string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                IEntity latestLoginPlan = EntityManager.SelectLatestVersion(InspectionRequestConstants.TableLoginPlan, loginPlanId) as IEntity;
                if (latestLoginPlan != null && latestLoginPlan.IsValid())
                {
                    loginPlanVersion = GetFirstString(latestLoginPlan, "Version", "VERSION");
                    TrySet(request, InspectionRequestConstants.FieldLoginPlan, latestLoginPlan);
                }
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

            SyncLoginPlanSelection(request);
            _snapshotRefreshRequested = true;
            EnsureLoginPlanPreviewCurrent(request);
            PopulateDataAssignmentHeader(request);
            _dataAssignmentGridRowsLoaded = false;
            PopulateDataAssignmentGrids(request);
            TryPopulateDataAssignmentGridsWhenReady();
        }

        private void EnsureLoginPlanPreviewCurrent(IEntity request)
        {
            SyncLoginPlanSelection(request);
            if (string.IsNullOrWhiteSpace(GetLoginPlanIdentity(request)))
            {
                ClearLoginPlanPreview();
                return;
            }

            if (IsLoginPlanPreviewCurrent(request))
            {
                return;
            }

            _snapshotRefreshRequested = true;
            BuildLoginPlanPreview(request);
        }

        private bool IsLoginPlanPreviewCurrent(IEntity request)
        {
            string loginPlanId = GetLoginPlanIdentity(request);
            string loginPlanVersion = GetLoginPlanVersion(request);
            if (string.IsNullOrWhiteSpace(loginPlanId) || _previewDataAssignmentEntries.Count == 0)
            {
                return false;
            }

            return string.Equals(_previewLoginPlanId, loginPlanId, StringComparison.OrdinalIgnoreCase) &&
                   (string.IsNullOrWhiteSpace(loginPlanVersion) ||
                    string.Equals(_previewLoginPlanVersion, loginPlanVersion, StringComparison.OrdinalIgnoreCase));
        }

        private void BuildLoginPlanPreview(IEntity request)
        {
            ClearLoginPlanPreview();
            EnsureRequestDefaults(request);

            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            string loginPlanId = GetLoginPlanIdentity(request);
            string loginPlanVersion = ResolveLoginPlanVersionForPreview(loginPlanId, GetLoginPlanVersion(request));
            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(loginPlanId))
            {
                return;
            }

            _previewLoginPlanId = loginPlanId;
            _previewLoginPlanVersion = loginPlanVersion;
            CopyPreviewDataAssignment(requestId, loginPlanId, loginPlanVersion);
            CopyPreviewProductSpec(requestId, loginPlanId, loginPlanVersion);
        }

        private void ClearLoginPlanPreview()
        {
            _previewLoginPlanId = null;
            _previewLoginPlanVersion = null;
            _previewDataAssignmentEntries.Clear();
            _previewDataAssignmentFields.Clear();
            _previewDataAssignmentTests.Clear();
            _previewDataAssignmentTestFields.Clear();
            _previewProducts.Clear();
        }

        private string ResolveLoginPlanVersionForPreview(string loginPlanId, string loginPlanVersion)
        {
            if (!string.IsNullOrWhiteSpace(loginPlanVersion) || string.IsNullOrWhiteSpace(loginPlanId))
            {
                return loginPlanVersion ?? string.Empty;
            }

            IEntity loginPlan = EntityManager.SelectLatestVersion(InspectionRequestConstants.TableLoginPlan, loginPlanId) as IEntity;
            return loginPlan == null || !loginPlan.IsValid()
                ? string.Empty
                : GetFirstString(loginPlan, "VERSION", "Version");
        }

        private void CopyPreviewDataAssignment(string requestId, string loginPlanId, string loginPlanVersion)
        {
            IQuery entryQuery = EntityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanEntry);
            entryQuery.AddEquals("IDENTITY", loginPlanId);
            if (!string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                entryQuery.AddEquals("VERSION", loginPlanVersion);
            }

            foreach (IEntity sourceEntry in EntityManager.Select(entryQuery))
            {
                object orderNumber = TryGet(sourceEntry, "ORDER_NUMBER");
                object parentOrderNumber = TryGet(sourceEntry, "PARENT_ENTRY_ORDER_NUMBER");
                IEntity targetEntry = EntityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanEntry);
                targetEntry.Set("REQUEST_ID", requestId);
                targetEntry.Set("ORDER_NUMBER", orderNumber);
                targetEntry.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetEntry.Set("SOURCE_LOGIN_PLAN_ID", loginPlanId);
                targetEntry.Set("SOURCE_LOGIN_PLAN_VERSION", TryGet(sourceEntry, "VERSION"));
                targetEntry.Set("SOURCE_ORDER_NUMBER", orderNumber);
                targetEntry.Set("TABLE_NAME", TryGet(sourceEntry, "TABLE_NAME"));
                targetEntry.Set("NODE_NAME", TryGet(sourceEntry, "NODE_NAME"));
                targetEntry.Set("LOGIN_WORKFLOW_ID", TryGet(sourceEntry, "LOGIN_WORKFLOW_ID"));
                targetEntry.Set("LOGIN_WORKFLOW_VERSION", TryGet(sourceEntry, "LOGIN_WORKFLOW_VERSION"));
                targetEntry.Set("USE_LAST_ACTIVE_LOGIN_WORKFLOW", TryGet(sourceEntry, "USE_LAST_ACTIVE_LOGIN_WORKFLOW"));
                targetEntry.Set("ENTITY_TEMPLATE_ID", TryGet(sourceEntry, "ENTITY_TEMPLATE_ID"));
                targetEntry.Set("ENTITY_TEMPLATE_VERSION", TryGet(sourceEntry, "ENTITY_TEMPLATE_VERSION"));
                targetEntry.Set("LOGIN_CONDITION", TryGet(sourceEntry, "LOGIN_CONDITION"));
                targetEntry.Set("COUNT_EXPRESSION", TryGet(sourceEntry, "COUNT_EXPRESSION"));
                targetEntry.Set("MODIFIABLE", true);
                targetEntry.Set("REMOVEFLAG", false);
                _previewDataAssignmentEntries.Add(targetEntry);

                if (string.IsNullOrWhiteSpace(GetString(sourceEntry, "ENTITY_TEMPLATE_ID")))
                {
                    CopyPreviewEntryFields(requestId, loginPlanId, sourceEntry, orderNumber);
                }
                else
                {
                    CopyPreviewEntityTemplateFields(requestId, loginPlanId, sourceEntry, orderNumber);
                }

                CopyPreviewEntryTests(requestId, loginPlanId, sourceEntry, orderNumber);
            }
        }

        private void CopyPreviewEntryFields(string requestId, string loginPlanId, IEntity sourceEntry, object parentOrderNumber)
        {
            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanField);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", TryGet(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_ORDER_NUMBER", parentOrderNumber);

            foreach (IEntity sourceField in EntityManager.Select(query))
            {
                IEntity targetField = EntityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanField);
                targetField.Set("REQUEST_ID", requestId);
                targetField.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetField.Set("ORDER_NUMBER", TryGet(sourceField, "ORDER_NUMBER"));
                targetField.Set("PROPERTY", TryGet(sourceField, "PROPERTY"));
                targetField.Set("VALUE", TryGet(sourceField, "VALUE"));
                targetField.Set("OVERRIDE_VALUE", TryGet(sourceField, "VALUE"));
                targetField.Set("MODIFIABLE", true);
                _previewDataAssignmentFields.Add(targetField);
            }
        }

        private void CopyPreviewEntityTemplateFields(string requestId, string loginPlanId, IEntity sourceEntry, object parentOrderNumber)
        {
            string entityTemplateId = GetString(sourceEntry, "ENTITY_TEMPLATE_ID");
            string entityTemplateVersion = GetString(sourceEntry, "ENTITY_TEMPLATE_VERSION");
            if (string.IsNullOrWhiteSpace(entityTemplateId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(entityTemplateVersion))
            {
                entityTemplateVersion = ResolveEntityTemplateVersionForPreview(entityTemplateId);
            }

            IQuery propertyQuery = EntityManager.CreateQuery(InspectionRequestConstants.TableEntityTemplateProperty);
            propertyQuery.AddEquals("ENTITY_TEMPLATE_ID", entityTemplateId);
            if (!string.IsNullOrWhiteSpace(entityTemplateVersion))
            {
                propertyQuery.AddEquals("ENTITY_TEMPLATE_VERSION", entityTemplateVersion);
            }

            foreach (IEntity templateProperty in EntityManager.Select(propertyQuery))
            {
                string propertyName = GetString(templateProperty, "PROPERTY_NAME");
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    continue;
                }

                object value = ResolvePreviewLoginPlanFieldValue(loginPlanId, sourceEntry, parentOrderNumber, propertyName)
                    ?? NormalizePreviewTemplateValue(TryGet(templateProperty, "DEFAULT_VALUE"));

                IEntity targetField = EntityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanField);
                targetField.Set("REQUEST_ID", requestId);
                targetField.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetField.Set("ORDER_NUMBER", TryGet(templateProperty, "ORDER_NUM"));
                targetField.Set("PROPERTY", propertyName);
                targetField.Set("VALUE", value);
                targetField.Set("OVERRIDE_VALUE", value);
                targetField.Set("MODIFIABLE", true);
                _previewDataAssignmentFields.Add(targetField);
            }
        }

        private object ResolvePreviewLoginPlanFieldValue(string loginPlanId, IEntity sourceEntry, object parentOrderNumber, string propertyName)
        {
            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanField);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", TryGet(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_ORDER_NUMBER", parentOrderNumber);
            query.AddEquals("PROPERTY", propertyName);

            foreach (IEntity sourceField in EntityManager.Select(query))
            {
                return TryGet(sourceField, "VALUE");
            }

            return null;
        }

        private string ResolveEntityTemplateVersionForPreview(string entityTemplateId)
        {
            IEntity entityTemplate = EntityManager.SelectLatestVersion(InspectionRequestConstants.TableEntityTemplate, entityTemplateId) as IEntity;
            return entityTemplate == null || !entityTemplate.IsValid()
                ? string.Empty
                : GetString(entityTemplate, "ENTITY_TEMPLATE_VERSION");
        }

        private void CopyPreviewEntryTests(string requestId, string loginPlanId, IEntity sourceEntry, object parentOrderNumber)
        {
            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanTest);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", TryGet(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_ORDER_NUMBER", parentOrderNumber);

            foreach (IEntity sourceTest in EntityManager.Select(query))
            {
                object orderNumber = TryGet(sourceTest, "ORDER_NUMBER");
                IEntity targetTest = EntityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanTest);
                targetTest.Set("REQUEST_ID", requestId);
                targetTest.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetTest.Set("ORDER_NUMBER", orderNumber);
                targetTest.Set("ANALYSIS_IDENTITY", TryGet(sourceTest, "ANALYSIS_IDENTITY"));
                targetTest.Set("ANALYSIS_VERSION", TryGet(sourceTest, "ANALYSIS_VERSION"));
                targetTest.Set("COMP_LIST_ANALYSIS", TryGet(sourceTest, "COMP_LIST_ANALYSIS"));
                targetTest.Set("COMP_LIST_ANALYSIS_VERSION", TryGet(sourceTest, "COMP_LIST_ANALYSIS_VERSION"));
                targetTest.Set("COMP_LIST_IDENTITY", TryGet(sourceTest, "COMP_LIST_IDENTITY"));
                targetTest.Set("TEST_SCHEDULE_ID", TryGet(sourceTest, "TEST_SCHEDULE_ID"));
                targetTest.Set("RED_TEST_ID", TryGet(sourceTest, "RED_TEST_ID"));
                targetTest.Set("RED_TEST_VERSION", TryGet(sourceTest, "RED_TEST_VERSION"));
                targetTest.Set("LAST_ACTIVE_VERSION", TryGet(sourceTest, "LAST_ACTIVE_VERSION"));
                targetTest.Set("MODIFIABLE", true);
                _previewDataAssignmentTests.Add(targetTest);

                CopyPreviewTestFields(requestId, loginPlanId, sourceEntry, sourceTest, parentOrderNumber, orderNumber);
            }
        }

        private void CopyPreviewTestFields(string requestId, string loginPlanId, IEntity sourceEntry, IEntity sourceTest, object entryOrderNumber, object testOrderNumber)
        {
            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanTestField);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", TryGet(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_PARENT_ORDER_NUMBER", entryOrderNumber);
            query.AddEquals("PARENT_ORDER_NUMBER", testOrderNumber);

            foreach (IEntity sourceField in EntityManager.Select(query))
            {
                IEntity targetField = EntityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanTestField);
                targetField.Set("REQUEST_ID", requestId);
                targetField.Set("PARENT_PARENT_ORDER_NUMBER", entryOrderNumber);
                targetField.Set("PARENT_ORDER_NUMBER", testOrderNumber);
                targetField.Set("ORDER_NUMBER", TryGet(sourceField, "ORDER_NUMBER"));
                targetField.Set("PROPERTY", TryGet(sourceField, "PROPERTY"));
                targetField.Set("VALUE", TryGet(sourceField, "VALUE"));
                targetField.Set("OVERRIDE_VALUE", TryGet(sourceField, "VALUE"));
                targetField.Set("MODIFIABLE", true);
                _previewDataAssignmentTestFields.Add(targetField);
            }
        }

        private void CopyPreviewProductSpec(string requestId, string loginPlanId, string loginPlanVersion)
        {
            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableMlpHeader);
            query.AddEquals("LOGIN_PLAN_ID", loginPlanId);
            if (!string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                query.AddEquals("LOGIN_PLAN_VERSION", loginPlanVersion);
            }

            int orderNumber = 1;
            foreach (IEntity product in EntityManager.Select(query))
            {
                IEntity targetProduct = EntityManager.CreateEntity(InspectionRequestConstants.TableIrProduct);
                targetProduct.Set("REQUEST_ID", requestId);
                targetProduct.Set("ORDER_NUMBER", orderNumber.ToString(CultureInfo.InvariantCulture));
                orderNumber++;
                targetProduct.Set("PRODUCT_ID", TryGet(product, "IDENTITY"));
                targetProduct.Set("PRODUCT_VERSION", TryGet(product, "PRODUCT_VERSION"));
                targetProduct.Set("PRODUCT_CODE", TryGet(product, "PRODUCT_CODE"));
                targetProduct.Set("PRODUCT_DESCRIPTION", TryGet(product, "DESCRIPTION"));
                targetProduct.Set("TEST_SCHEDULE_ID", TryGet(product, "TEST_SCHEDULE"));
                targetProduct.Set("INSPECTION_PLAN", TryGet(product, "INSPECTION_PLAN"));
                targetProduct.Set("MODIFIABLE", true);
                targetProduct.Set("REMOVEFLAG", false);
                _previewProducts.Add(targetProduct);
            }
        }

        private void MaterializePreviewSnapshotRows(IEntity request)
        {
            if (!IsLoginPlanPreviewCurrent(request))
            {
                return;
            }

            RemovePersistedSnapshotRows(GetString(request, InspectionRequestConstants.FieldRequestId));
            foreach (IEntity entity in _previewDataAssignmentEntries
                         .Concat(_previewDataAssignmentFields)
                         .Concat(_previewDataAssignmentTests)
                         .Concat(_previewDataAssignmentTestFields)
                         .Concat(_previewProducts))
            {
                EntityManager.Transaction.Add(entity);
            }

            _snapshotRefreshRequested = false;
        }

        private void RemovePersistedSnapshotRows(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return;
            }

            foreach (string tableName in new[]
            {
                InspectionRequestConstants.TableIrLoginPlanTestField,
                InspectionRequestConstants.TableIrLoginPlanTest,
                InspectionRequestConstants.TableIrLoginPlanField,
                InspectionRequestConstants.TableIrLoginPlanEntry,
                InspectionRequestConstants.TableIrProduct
            })
            {
                IQuery query = EntityManager.CreateQuery(tableName);
                query.AddEquals("REQUEST_ID", requestId);
                foreach (IEntity entity in EntityManager.Select(query))
                {
                    EntityManager.Transaction.Remove(entity);
                }
            }
        }

        private static object NormalizePreviewTemplateValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string text = value.ToString();
            if (string.Equals(text, "True", StringComparison.OrdinalIgnoreCase))
            {
                return "T";
            }

            if (string.Equals(text, "False", StringComparison.OrdinalIgnoreCase))
            {
                return "F";
            }

            return value;
        }

        private bool IsPreviewSnapshotEntity(IEntity entity)
        {
            return entity != null &&
                   (_previewDataAssignmentFields.Contains(entity) ||
                    _previewDataAssignmentTestFields.Contains(entity) ||
                    _previewDataAssignmentEntries.Contains(entity) ||
                    _previewDataAssignmentTests.Contains(entity) ||
                    _previewProducts.Contains(entity));
        }

        private void RefreshLoginPlanSnapshot(IEntity request)
        {
            EnsureRequestDefaults(request);
            if (string.IsNullOrWhiteSpace(GetLoginPlanIdentity(request)))
            {
                return;
            }

            bool changed = new InspectionRequestSnapshotService(EntityManager).EnsureSnapshot(request, true);
            if (!changed)
            {
                return;
            }

            if (CanCommitSnapshotDuringEdit(request))
            {
                EntityManager.Commit();
            }

            RefreshSnapshotCollections();
            PopulateDataAssignmentHeader(request);
            _dataAssignmentGridRowsLoaded = false;
            PopulateDataAssignmentGrids(request);
            TryPopulateDataAssignmentGridsWhenReady();
            StartDataAssignmentEditorAttachPolling();
        }

        private static object NormalizeTemplateRuntimeValue(ISchemaField schemaField, string storedValue)
        {
            if (schemaField != null && schemaField.DatabaseType.Equals(DataVariableType.DataTypeBoolean))
            {
                return IsTrue(storedValue);
            }

            return storedValue ?? string.Empty;
        }

        private string GetTemplateRuntimeStoredValue(IEntity request, string propertyName)
        {
            object value = TryGet(request, propertyName);
            if (value == null)
            {
                ISchemaField schemaField = FindSchemaField(InspectionRequestConstants.EntityName, propertyName);
                if (schemaField != null)
                {
                    value = TryGet(request, schemaField.Name);
                }
            }

            if (value is IEntity entity && entity.IsValid())
            {
                return ToIdentityText(entity);
            }

            if (value is PhraseBase phrase && phrase.IsValid())
            {
                return phrase.PhraseId;
            }

            if (value is bool booleanValue)
            {
                return booleanValue ? "T" : "F";
            }

            return value?.ToString() ?? string.Empty;
        }

        private bool TryGetTemplatePropertyFromRow(UnboundGridRow row, out string propertyName)
        {
            propertyName = null;
            string key = row?.Tag?.ToString();
            if (!string.IsNullOrWhiteSpace(key) &&
                _templateFieldPropertyByRow.TryGetValue(key, out propertyName) &&
                !string.IsNullOrWhiteSpace(propertyName))
            {
                return true;
            }

            return false;
        }

        private static string GetTemplatePropertyName(IEntity templateProperty)
        {
            return GetFirstString(templateProperty, "PROPERTY_NAME", "PropertyName", "Property");
        }

        private static bool IsStandaloneTemplateProperty(string propertyName)
        {
            return string.Equals(propertyName, InspectionRequestConstants.FieldNameProperty, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(propertyName, InspectionRequestConstants.FieldName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(propertyName, InspectionRequestConstants.FieldDescriptionProperty, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(propertyName, InspectionRequestConstants.FieldDescription, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(propertyName, "IdText", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(propertyName, InspectionRequestConstants.FieldIdText, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTemplatePropertyRowKey(IEntity templateProperty, string propertyName)
        {
            string identity = ToIdentityText(templateProperty);
            return $"{templateProperty?.GetType().FullName}|{propertyName}|{identity}";
        }

        private static bool ShouldIgnoreEmptyEditorEcho(IEntity field, object rawValue, string storedValue)
        {
            return rawValue == null &&
                   string.IsNullOrWhiteSpace(storedValue) &&
                   !string.IsNullOrWhiteSpace(GetString(field, "OVERRIDE_VALUE"));
        }

        private static string ToStoredAssignmentValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is PhraseBase phrase && phrase.IsValid())
            {
                return phrase.PhraseId;
            }

            if (value is IEntity entity && entity.IsValid())
            {
                return ToIdentityText(entity);
            }

            if (value is Identity identity)
            {
                return identity.ToString();
            }

            if (value is bool booleanValue)
            {
                return booleanValue ? "T" : "F";
            }

            return value.ToString();
        }

        private static object ToDateDisplayValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            try
            {
                if (TextUtils.IsUniversalString(value))
                {
                    return TextUtils.UniversalStringToLocalDateTime(value);
                }

                if (TextUtils.IsUTCString(value))
                {
                    return TextUtils.UTCStringToLocalDateTime(value);
                }
            }
            catch
            {
            }

            if (DateTime.TryParse(value, out DateTime dateTime))
            {
                return dateTime;
            }

            if (TimeSpan.TryParse(value, out TimeSpan timeSpan))
            {
                return timeSpan;
            }

            return null;
        }

        private string ResolveAssignmentTableName(IEntity field)
        {
            string requestId = GetString(field, "REQUEST_ID");
            string parentOrderNumber = GetString(field, "PARENT_ORDER_NUMBER");
            if (!string.IsNullOrWhiteSpace(requestId) && !string.IsNullOrWhiteSpace(parentOrderNumber))
            {
                foreach (IEntity entry in _previewDataAssignmentEntries)
                {
                    if (string.Equals(GetString(entry, "REQUEST_ID"), requestId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(GetString(entry, "ORDER_NUMBER"), parentOrderNumber, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetString(entry, "TABLE_NAME");
                    }
                }

                IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableIrLoginPlanEntry);
                query.AddEquals("REQUEST_ID", requestId);
                query.AddAnd();
                query.AddEquals("ORDER_NUMBER", parentOrderNumber);

                foreach (IEntity entry in EntityManager.Select(query))
                {
                    return GetString(entry, "TABLE_NAME");
                }
            }

            return "TEST";
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
            SetControlValue("teName", GetString(entry, "NODE_NAME"));
            SetControlValue("DataAssignmentLoginWorkflow", loginWorkflow);
            SetControlValue("ebpLoginWorkflow", loginWorkflow);
            SetControlValue("DataAssignmentLastActiveWorkflow", ToBooleanText(TryGet(entry, "USE_LAST_ACTIVE_LOGIN_WORKFLOW")));
            SetControlValue("cfLastActiveVersion", TryGet(entry, "USE_LAST_ACTIVE_LOGIN_WORKFLOW"));
            SetControlValue("DataAssignmentEntityTemplate", entityTemplate);
            SetControlValue("ebpEntityTemplate", entityTemplate);
            SetControlValue("DataAssignmentEntryTable", GetString(entry, "TABLE_NAME"));
            SetControlValue("TestEntityBrowsePrompt", GetString(entry, "TABLE_NAME"));
            SetControlValue("DataAssignmentLoginCondition", GetString(entry, "LOGIN_CONDITION"));
            SetControlValue("expLoginCondition", GetString(entry, "LOGIN_CONDITION"));
            SetControlValue("LoginConditionTextValue", string.Empty);
            SetControlValue("DataAssignmentCountExpression", GetString(entry, "COUNT_EXPRESSION"));
            SetControlValue("expCountExpression", GetString(entry, "COUNT_EXPRESSION"));
            SetControlValue("CountExpressionTextValue", string.Empty);
        }

        private void PopulateDataAssignmentGrids(IEntity request)
        {
            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return;
            }

            IEntity entry = SelectFirstDataAssignmentEntry(requestId);
            string parentOrderNumber = GetString(entry, "ORDER_NUMBER");
            string tableName = GetString(entry, "TABLE_NAME");

            _dataAssignmentFieldsByRow.Clear();
            try
            {
                _loadingDataAssignmentGrid = true;
                PopulateFieldGrid(requestId, parentOrderNumber, tableName);
                PopulateTestFieldGrid(requestId);
            }
            finally
            {
                _loadingDataAssignmentGrid = false;
            }
        }

        private void PopulateFieldGrid(string requestId, string parentOrderNumber, string tableName)
        {
            if (TryGetDataAssignmentMember("udgEntityGrid") is not UnboundGrid grid)
            {
                return;
            }

            grid.BeginUpdate();
            grid.ClearRows();

            foreach (IEntity field in SelectDataAssignmentRows(InspectionRequestConstants.TableIrLoginPlanField, requestId, parentOrderNumber))
            {
                AddAssignmentGridRow(grid, field, tableName);
            }

            grid.EndUpdate();
        }

        private void PopulateTestFieldGrid(string requestId)
        {
            if (TryGetDataAssignmentMember("ugdTestData") is not UnboundGrid grid)
            {
                return;
            }

            grid.BeginUpdate();
            grid.ClearRows();

            foreach (IEntity field in SelectDataAssignmentRows(InspectionRequestConstants.TableIrLoginPlanTestField, requestId))
            {
                AddAssignmentGridRow(grid, field, "TEST");
            }

            grid.EndUpdate();
        }

        private void AddAssignmentGridRow(UnboundGrid grid, IEntity field, string tableName)
        {
            string propertyName = GetString(field, "PROPERTY");
            string rawOverrideValue = GetString(field, "OVERRIDE_VALUE");
            object defaultValue = ToGridValue(tableName, propertyName, GetString(field, "VALUE"));
            object overrideValue = ToGridValue(tableName, propertyName, rawOverrideValue);

            UnboundGridRow row = grid.AddRow(propertyName, defaultValue, overrideValue);
            row.Tag = GetDataAssignmentRowKey(field);
            _dataAssignmentFieldsByRow[row.Tag.ToString()] = field;

            ConfigureValueCell(grid, row, tableName, propertyName, overrideValue);
            RestoreTypedDisplayValue(grid, row, tableName, propertyName, rawOverrideValue);
        }

        private object ToGridValue(string tableName, string propertyName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            ISchemaField schemaField = FindSchemaField(tableName, propertyName);
            if (schemaField?.LinkTable != null)
            {
                return value;
            }

            if (schemaField?.PhraseValid == true)
            {
                return value;
            }

            if (schemaField != null && schemaField.DatabaseType.Equals(DataVariableType.DataTypeDate))
            {
                object dateValue = ToDateDisplayValue(value);
                if (dateValue != null)
                {
                    return dateValue;
                }
            }

            if (string.Equals(value, "T", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "F", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return value;
        }

        private void ConfigureValueCell(UnboundGrid grid, UnboundGridRow row, string tableName, string propertyName, object value)
        {
            ConfigureValueCell(grid, row, tableName, propertyName, value, 2);
        }

        private void ConfigureValueCell(UnboundGrid grid, UnboundGridRow row, string tableName, string propertyName, object value, int valueColumnIndex)
        {
            ISchemaField schemaField = FindSchemaField(tableName, propertyName);
            if (schemaField == null)
            {
                grid.Columns[valueColumnIndex].SetCellDataType(row, GridColumnType.Text);
                return;
            }

            grid.Columns[valueColumnIndex].SetCellDataType(row, ConvertToGridColumnType(schemaField));
            if (schemaField.LinkTable == null &&
                !schemaField.PhraseValid &&
                schemaField.DatabaseType.Equals(DataVariableType.DataTypeText))
            {
                grid.Columns[valueColumnIndex].ShowCellButton(row);
            }

            if (schemaField.LinkTable != null)
            {
                EntityBrowse entityBrowse = BrowseFactory.CreateEntityBrowse(
                    $"EntityBrowse_{tableName}_{propertyName}_{Guid.NewGuid():N}",
                    schemaField.LinkTable.Name);
                grid.Columns[valueColumnIndex].SetCellEntityBrowse(row, entityBrowse);
            }
            else if (schemaField.PhraseValid)
            {
                PhraseBrowse phraseBrowse = BrowseFactory.CreatePhraseBrowse(
                    $"PhraseBrowse_{tableName}_{propertyName}_{Guid.NewGuid():N}",
                    schemaField.PhraseType);
                grid.Columns[valueColumnIndex].SetCellBrowse(row, phraseBrowse);
            }
        }

        private void RestoreTypedDisplayValue(UnboundGrid grid, UnboundGridRow row, string tableName, string propertyName, string storedValue)
        {
            RestoreTypedDisplayValue(grid, row, tableName, propertyName, storedValue, 2);
        }

        private void RestoreTypedDisplayValue(UnboundGrid grid, UnboundGridRow row, string tableName, string propertyName, string storedValue, int valueColumnIndex)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
            {
                return;
            }

            ISchemaField schemaField = FindSchemaField(tableName, propertyName);
            if (schemaField == null)
            {
                return;
            }

            if (schemaField.LinkTable != null)
            {
                IEntity linkedEntity = SelectLinkedEntity(schemaField, storedValue);
                if (linkedEntity != null && linkedEntity.IsValid())
                {
                    row.SetValue(grid.Columns[valueColumnIndex].Name, linkedEntity);
                }

                return;
            }

            if (schemaField.PhraseValid)
            {
                IEntity phrase = EntityManager.SelectPhrase(schemaField.PhraseType, storedValue);
                if (phrase != null && phrase.IsValid())
                {
                    row.SetValue(grid.Columns[valueColumnIndex].Name, phrase);
                }

                return;
            }

            if (schemaField.DatabaseType.Equals(DataVariableType.DataTypeDate))
            {
                object dateValue = ToDateDisplayValue(storedValue);
                if (dateValue != null)
                {
                    row.SetValue(grid.Columns[valueColumnIndex].Name, dateValue);
                }
            }
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

                return EntityManager.Select(schemaField.LinkTable.Name, new Identity(fields));
            }
            catch
            {
                return null;
            }
        }

        private bool TryGetFieldFromRow(UnboundGridRow row, out IEntity field)
        {
            field = null;
            string key = row?.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(key) || !_dataAssignmentFieldsByRow.TryGetValue(key, out field))
            {
                return false;
            }

            return field != null && field.IsValid();
        }

        private static string GetDataAssignmentRowKey(IEntity field)
        {
            string identity = ToIdentityText(field);
            return $"{field?.GetType().FullName}|{identity}";
        }

        private ISchemaField FindSchemaField(string tableName, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(tableName) ||
                string.IsNullOrWhiteSpace(propertyName) ||
                !Library.Schema.Tables.Contains(tableName))
            {
                return null;
            }

            ISchemaTable schemaTable = Library.Schema.Tables[tableName];
            return schemaTable.GetFieldFromProperty(propertyName) ??
                   schemaTable.Fields
                       .OfType<ISchemaField>()
                       .FirstOrDefault(field =>
                           string.Equals(field.Relationship?.Replace("_", string.Empty), propertyName, StringComparison.OrdinalIgnoreCase));
        }

        private static GridColumnType ConvertToGridColumnType(ISchemaField schemaField)
        {
            switch (schemaField.DatabaseType)
            {
                case DataVariableType.DataTypeBoolean:
                    return GridColumnType.Boolean;
                case DataVariableType.DataTypeInteger:
                    return GridColumnType.Integer;
                case DataVariableType.DataTypeDate:
                    return GridColumnType.DateTime;
                case DataVariableType.DataTypePackedDecimal:
                    return GridColumnType.PackedDecimal;
                case DataVariableType.DataTypeInterval:
                    return GridColumnType.Interval;
                case DataVariableType.DataTypeReal:
                    return GridColumnType.Real;
                default:
                    return GridColumnType.Text;
            }
        }

        private IEntityCollection SelectRows(string tableName, string requestId, string parentOrderNumber = null)
        {
            IQuery query = EntityManager.CreateQuery(tableName);
            query.AddEquals("REQUEST_ID", requestId);
            if (!string.IsNullOrWhiteSpace(parentOrderNumber))
            {
                query.AddAnd();
                query.AddEquals("PARENT_ORDER_NUMBER", parentOrderNumber);
            }
            return EntityManager.Select(query);
        }

        private IEnumerable<IEntity> SelectDataAssignmentRows(string tableName, string requestId, string parentOrderNumber = null)
        {
            IEnumerable<IEntity> previewRows = tableName switch
            {
                InspectionRequestConstants.TableIrLoginPlanField => _previewDataAssignmentFields,
                InspectionRequestConstants.TableIrLoginPlanTestField => _previewDataAssignmentTestFields,
                InspectionRequestConstants.TableIrLoginPlanEntry => _previewDataAssignmentEntries,
                InspectionRequestConstants.TableIrLoginPlanTest => _previewDataAssignmentTests,
                InspectionRequestConstants.TableIrProduct => _previewProducts,
                _ => Enumerable.Empty<IEntity>()
            };

            var matchingPreviewRows = previewRows
                .Where(row => string.Equals(GetString(row, "REQUEST_ID"), requestId, StringComparison.OrdinalIgnoreCase))
                .Where(row => string.IsNullOrWhiteSpace(parentOrderNumber) ||
                              string.Equals(GetString(row, "PARENT_ORDER_NUMBER"), parentOrderNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchingPreviewRows.Count > 0)
            {
                return matchingPreviewRows;
            }

            return SelectRows(tableName, requestId, parentOrderNumber).Cast<IEntity>();
        }

        private IEntity SelectFirstDataAssignmentEntry(string requestId)
        {
            IEntity firstPreview = null;
            foreach (IEntity entry in _previewDataAssignmentEntries.Where(entry =>
                         string.Equals(GetString(entry, "REQUEST_ID"), requestId, StringComparison.OrdinalIgnoreCase)))
            {
                if (firstPreview == null || CompareOrder(entry, firstPreview) < 0)
                {
                    firstPreview = entry;
                }
            }

            if (firstPreview != null && firstPreview.IsValid())
            {
                return firstPreview;
            }

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
                "teName",
                "DataAssignmentLoginWorkflow",
                "ebpLoginWorkflow",
                "DataAssignmentLastActiveWorkflow",
                "cfLastActiveVersion",
                "DataAssignmentEntityTemplate",
                "ebpEntityTemplate",
                "DataAssignmentEntryTable",
                "TestEntityBrowsePrompt",
                "DataAssignmentLoginCondition",
                "expLoginCondition",
                "LoginConditionTextValue",
                "DataAssignmentCountExpression",
                "expCountExpression",
                "CountExpressionTextValue"
            })
            {
                SetControlValue(controlName, string.Empty);
            }
        }

        private void SetControlValue(string controlName, object value)
        {
            object control = TryGetDataAssignmentMember(controlName) ?? TryGetMember(MainForm, controlName);
            if (control == null)
            {
                return;
            }

            TrySetMember(control, "Value", value);
            TrySetMember(control, "EditValue", value);
            TrySetMember(control, "TextValue", value);
            TrySetMember(control, "Text", value?.ToString() ?? string.Empty);
            TrySetMember(control, "DisplayText", value?.ToString() ?? string.Empty);
            TrySetMember(control, "RawText", value?.ToString() ?? string.Empty);
            TrySetMember(control, "Checked", IsTrue(value));
            TrySetMember(control, "Enabled", false);
            TrySetMember(control, "ReadOnly", true);
        }

        private object TryGetDataAssignmentMember(string memberName)
        {
            if (_dataAssignmentPageRoot != null)
            {
                object pageMember = TryGetMember(_dataAssignmentPageRoot, memberName);
                if (pageMember != null)
                {
                    return pageMember;
                }
            }

            return TryGetMember(MainForm, memberName);
        }

        private object TryGetEntityTemplateMember(string memberName)
        {
            if (_entityTemplatePageRoot != null)
            {
                object pageMember = TryGetMember(_entityTemplatePageRoot, memberName);
                if (pageMember != null)
                {
                    return pageMember;
                }
            }

            return TryGetMember(MainForm, memberName);
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

            object direct = TryGetDirectMember(target, memberName);
            if (direct != null)
            {
                return direct;
            }

            foreach (string collectionName in new[] { "Controls", "NonVisualControls", "Components", "Items", "TabPages", "Pages" })
            {
                object indexed = TryGetIndexedMember(target, collectionName, memberName);
                if (indexed != null)
                {
                    return indexed;
                }
            }

            return FindNamedControl(target, memberName);
        }

        private static object FindNamedControl(object root, string controlName)
        {
            var visited = new HashSet<object>(ReferenceComparer.Instance);
            return FindNamedControl(root, controlName, visited, 0);
        }

        private static object FindNamedControl(object target, string controlName, ISet<object> visited, int depth)
        {
            if (target == null || depth > 8 || !visited.Add(target))
            {
                return null;
            }

            object name = TryGetDirectMember(target, "Name");
            if (string.Equals(name?.ToString(), controlName, StringComparison.Ordinal))
            {
                return target;
            }

            foreach (string collectionName in new[] { "Controls", "NonVisualControls", "Components", "Items", "TabPages", "Pages" })
            {
                object indexed = TryGetIndexedMember(target, collectionName, controlName);
                if (indexed != null)
                {
                    return indexed;
                }
            }

            foreach (string collectionName in new[] { "Controls", "Items", "TabPages", "Pages" })
            {
                if (TryGetDirectMember(target, collectionName) is IEnumerable collection)
                {
                    foreach (object item in collection)
                    {
                        object match = FindNamedControl(item, controlName, visited, depth + 1);
                        if (match != null)
                        {
                            return match;
                        }
                    }
                }
            }

            if (IsUiObject(target))
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (PropertyInfo property in target.GetType().GetProperties(flags))
                {
                    if (property.GetIndexParameters().Length != 0 ||
                        !IsSafeControlProperty(property.Name))
                    {
                        continue;
                    }

                    object child = null;
                    try
                    {
                        child = property.GetValue(target);
                    }
                    catch
                    {
                    }

                    if (child == null || child is string)
                    {
                        continue;
                    }

                    object match = FindNamedControl(child, controlName, visited, depth + 1);
                    if (match != null)
                    {
                        return match;
                    }
                }
            }

            return null;
        }

        private static object TryGetIndexedMember(object target, string collectionName, string itemName)
        {
            object collection = TryGetDirectMember(target, collectionName);
            if (collection == null)
            {
                return null;
            }

            if (collection is IDictionary dictionary && dictionary.Contains(itemName))
            {
                return dictionary[itemName];
            }

            try
            {
                return collection.GetType().GetProperty("Item", new[] { typeof(string) })?.GetValue(collection, new object[] { itemName });
            }
            catch
            {
            }

            try
            {
                return collection.GetType().GetMethod("get_Item", new[] { typeof(string) })?.Invoke(collection, new object[] { itemName });
            }
            catch
            {
                return null;
            }
        }

        private static bool IsUiObject(object target)
        {
            string fullName = target.GetType().FullName ?? string.Empty;
            return fullName.StartsWith("Thermo.SampleManager.Library.ClientControls", StringComparison.Ordinal) ||
                   fullName.StartsWith("System.Windows.Forms", StringComparison.Ordinal) ||
                   fullName.IndexOf("Form", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   fullName.IndexOf("Page", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   fullName.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   fullName.IndexOf("Control", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSafeControlProperty(string propertyName)
        {
            return propertyName.IndexOf("Control", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   propertyName.IndexOf("Form", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   propertyName.IndexOf("Page", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   propertyName.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   propertyName.IndexOf("Grid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   string.Equals(propertyName, "Content", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(propertyName, "Parent", StringComparison.OrdinalIgnoreCase);
        }

        private static object TryGetDirectMember(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = target.GetType();

            try
            {
                return type.GetProperty(memberName, flags)?.GetValue(target) ??
                       type.GetField(memberName, flags)?.GetValue(target);
            }
            catch
            {
                return null;
            }
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

        private void SubscribeEvent(object target, string eventName, string handlerName)
        {
            if (target == null)
            {
                return;
            }

            try
            {
                EventInfo eventInfo = target.GetType().GetEvent(
                    eventName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo handler = GetType().GetMethod(
                    handlerName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (eventInfo == null || handler == null)
                {
                    return;
                }

                Delegate callback = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, handler, false);
                if (callback != null)
                {
                    eventInfo.AddEventHandler(target, callback);
                }
            }
            catch
            {
            }
        }

        private static IEntity TryGetEventEntity(EventArgs args)
        {
            object entity = TryGetDirectMember(args, "Entity") ??
                            TryGetDirectMember(args, "RowEntity") ??
                            TryGetDirectMember(args, "FocusedEntity");
            return entity as IEntity;
        }

        private static IEntity TryGetFocusedEntity(object grid)
        {
            object entity = TryGetDirectMember(grid, "FocusedEntity") ??
                            TryGetDirectMember(grid, "FocusedRowEntity") ??
                            TryGetDirectMember(grid, "CurrentEntity");
            return entity as IEntity;
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

        private static void TryInvoke(object target, string methodName, params object[] arguments)
        {
            if (target == null)
            {
                return;
            }

            foreach (MethodInfo method in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                try
                {
                    if (parameters.Length == arguments.Length)
                    {
                        method.Invoke(target, arguments);
                        return;
                    }

                    if (parameters.Length == 1 &&
                        parameters[0].GetCustomAttribute<ParamArrayAttribute>() != null)
                    {
                        method.Invoke(target, new object[] { arguments });
                        return;
                    }

                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(object[]))
                    {
                        method.Invoke(target, new object[] { arguments });
                        return;
                    }

                    return;
                }
                catch
                {
                }
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

    }
}
