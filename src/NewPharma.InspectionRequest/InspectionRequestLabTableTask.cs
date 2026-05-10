using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Core.Definition;
using Thermo.SampleManager.Library.ClientControls;
using Thermo.SampleManager.Library.ClientControls.Browse;
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
        private bool _dataAssignmentPageDiagnosticsShown;
        private object _dataAssignmentPageRoot;
        private bool _normalizingDataAssignmentValue;

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
                MainForm.Selected += MainForm_Selected;
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
                SyncDataAssignmentGridValues();
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
                    new InspectionRequestSnapshotService(EntityManager).EnsureSnapshot(request, true);
                    EntityManager.Commit();
                    RefreshSnapshotCollections();
                    PopulateDataAssignmentHeader(request);
                    _dataAssignmentGridRowsLoaded = false;
                    PopulateDataAssignmentGrids(request);
                    TryPopulateDataAssignmentGridsWhenReady();
                    Library.Task.StateModified();
                }

                _snapshotRefreshRequested = true;
            }
        }

        private void MainForm_Selected(object sender, SelectedEventArgs e)
        {
            if (string.Equals(e.FormName, "Page_DataAssignment", StringComparison.OrdinalIgnoreCase))
            {
                _dataAssignmentPageRoot = ResolveSelectedPageRoot(sender, e);
            }

            if (!string.Equals(e.FormName, "Page_DataAssignment", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (MainForm?.Entity is not IEntity request)
            {
                return;
            }

            PopulateDataAssignmentHeader(request);
            _dataAssignmentGridRowsLoaded = false;
            StartDataAssignmentEditorAttachPolling();
            PopulateDataAssignmentGrids(request);
            TryPopulateDataAssignmentGridsWhenReady();
        }

        private void ShowDataAssignmentPageDiagnostics(SelectedEventArgs e)
        {
            if (_dataAssignmentPageDiagnosticsShown)
            {
                return;
            }

            string formName = e?.FormName ?? string.Empty;
            if (formName.IndexOf("Data", StringComparison.OrdinalIgnoreCase) < 0 &&
                formName.IndexOf("Assignment", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            _dataAssignmentPageDiagnosticsShown = true;
            object entryGrid = TryGetDataAssignmentMember("udgEntityGrid");
            object testGrid = TryGetDataAssignmentMember("ugdTestData");
            object root = _dataAssignmentPageRoot;
            Library.Utils.FlashMessage(
                $"Data Assignment page diagnostic: form={formName}; root={root?.GetType().FullName ?? "not found"}; udgEntityGrid={entryGrid?.GetType().FullName ?? "not found"}; ugdTestData={testGrid?.GetType().FullName ?? "not found"}",
                "Inspection Request");
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
            if (_normalizingDataAssignmentValue ||
                sender is not UnboundGrid grid ||
                !TryGetFieldFromRow(e.Row, out IEntity field))
            {
                return;
            }

            string storedValue = ToStoredAssignmentValue(e.Value);
            field.Set("OVERRIDE_VALUE", storedValue);
            EntityManager.Transaction.Add(field);

            if (e.Value is IEntity)
            {
                try
                {
                    _normalizingDataAssignmentValue = true;
                    e.Row.SetValue(grid.Columns[2].Name, storedValue);
                }
                finally
                {
                    _normalizingDataAssignmentValue = false;
                }
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
                EntityManager.Transaction.Add(field);
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
                    field.Set("OVERRIDE_VALUE", ToStoredAssignmentValue(row.GetValue(grid.Columns[2].Name)));
                    EntityManager.Transaction.Add(field);
                }
            }
        }

        private static string ToStoredAssignmentValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
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

        private string ResolveAssignmentTableName(IEntity field)
        {
            string requestId = GetString(field, "REQUEST_ID");
            string parentOrderNumber = GetString(field, "PARENT_ORDER_NUMBER");
            if (!string.IsNullOrWhiteSpace(requestId) && !string.IsNullOrWhiteSpace(parentOrderNumber))
            {
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
            PopulateFieldGrid(requestId, parentOrderNumber, tableName);
            PopulateTestFieldGrid(requestId);
        }

        private void PopulateFieldGrid(string requestId, string parentOrderNumber, string tableName)
        {
            if (TryGetDataAssignmentMember("udgEntityGrid") is not UnboundGrid grid)
            {
                return;
            }

            grid.BeginUpdate();
            grid.ClearRows();

            foreach (IEntity field in SelectRows(InspectionRequestConstants.TableIrLoginPlanField, requestId, parentOrderNumber))
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

            foreach (IEntity field in SelectRows(InspectionRequestConstants.TableIrLoginPlanTestField, requestId))
            {
                AddAssignmentGridRow(grid, field, "TEST");
            }

            grid.EndUpdate();
        }

        private void AddAssignmentGridRow(UnboundGrid grid, IEntity field, string tableName)
        {
            string propertyName = GetString(field, "PROPERTY");
            object defaultValue = ToGridValue(tableName, propertyName, GetString(field, "VALUE"));
            object overrideValue = ToGridValue(tableName, propertyName, GetString(field, "OVERRIDE_VALUE"));

            UnboundGridRow row = grid.AddRow(propertyName, defaultValue, overrideValue);
            row.Tag = GetDataAssignmentRowKey(field);
            _dataAssignmentFieldsByRow[row.Tag.ToString()] = field;

            ConfigureValueCell(grid, row, tableName, propertyName, overrideValue);
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
            ISchemaField schemaField = FindSchemaField(tableName, propertyName);
            if (schemaField == null)
            {
                grid.Columns[2].SetCellDataType(row, GridColumnType.Text);
                return;
            }

            grid.Columns[2].SetCellDataType(row, ConvertToGridColumnType(schemaField));

            if (schemaField.LinkTable != null)
            {
                EntityBrowse entityBrowse = BrowseFactory.CreateEntityBrowse(
                    schemaField.LinkTable.Name,
                    true,
                    "IDENTITY");
                grid.Columns[2].SetCellBrowse(row, entityBrowse);
            }
            else if (schemaField.PhraseValid)
            {
                PhraseBrowse phraseBrowse = BrowseFactory.CreatePhraseBrowse(
                    $"PhraseBrowse_{tableName}_{propertyName}_{Guid.NewGuid():N}",
                    schemaField.PhraseType);
                grid.Columns[2].SetCellBrowse(row, phraseBrowse);
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
