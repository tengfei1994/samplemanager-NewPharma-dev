using System;
using System.Globalization;
using System.Linq;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Common.Workflow;
using Thermo.SampleManager.Server;

namespace NewPharma.InspectionRequest
{
    internal sealed class InspectionRequestLifecycleService
    {
        public const string DefaultEntityTemplateId = "NPH_IR";
        public const string DefaultWorkflowGuid = "19940000-0000-0000-0000-000000000001";
        public const string DefaultLoginWorkflowGuid = "19940000-0000-0000-0000-000000000002";
        public const string LifecycleRootNodeGuid = "19940000-0000-0000-0000-000000000100";
        public const string DraftNodeGuid = "19940000-0000-0000-0000-000000000101";
        public const string SubmittedNodeGuid = "19940000-0000-0000-0000-000000000102";
        public const string UnderReviewNodeGuid = "19940000-0000-0000-0000-000000000103";
        public const string ApprovedNodeGuid = "19940000-0000-0000-0000-000000000104";
        public const string ExecutedNodeGuid = "19940000-0000-0000-0000-000000000105";
        public const string ExecutingNodeGuid = "19940000-0000-0000-0000-000000000106";
        public const string ExecutionFailedNodeGuid = "19940000-0000-0000-0000-000000000107";
        public const string LoginNodeGuid = "19940000-0000-0000-0000-000000000201";
        public const string LoginCreateNodeGuid = "19940000-0000-0000-0000-000000000202";
        public const string WorkflowVersion = "         1";
        private readonly IEntityManager _entityManager;

        public InspectionRequestLifecycleService(IEntityManager entityManager)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        }

        public void EnsureConfiguration()
        {
            EnsureEntityTemplate();
            EnsureWorkflow();
            EnsureLifecycleRoot();
            EnsureStatusMarkerNode(DraftNodeGuid, "Draft", 101);
            EnsureStatusMarkerNode(SubmittedNodeGuid, "Submitted", 102);
            EnsureStatusMarkerNode(UnderReviewNodeGuid, "Under Review", 103);
            EnsureStatusMarkerNode(ApprovedNodeGuid, "Approved", 104);
            EnsureStatusMarkerNode(ExecutingNodeGuid, "Executing", 105);
            EnsureStatusMarkerNode(ExecutedNodeGuid, "Executed", 106);
            EnsureStatusMarkerNode(ExecutionFailedNodeGuid, "Execution Failed", 107);
            EnsureLoginWorkflow();
            EnsureLoginWorkflowNode();
            _entityManager.Commit();
        }

        public void Initialize(IEntity request)
        {
            if (request == null)
            {
                return;
            }

            SetIfEmpty(request, InspectionRequestConstants.FieldLifecycleWorkflowId, DefaultWorkflowGuid);
            SetIfEmpty(request, InspectionRequestConstants.FieldLifecycleWorkflowVersion, WorkflowVersion);
            SetIfEmpty(request, InspectionRequestConstants.FieldLifecycleNodeId, DraftNodeGuid);
            SetIfEmpty(request, InspectionRequestConstants.FieldLifecycleEvent, "CREATE");
            SetIfEmpty(request, InspectionRequestConstants.FieldWorkflowNode, LoginCreateNodeGuid);
            SetIfEmpty(request, InspectionRequestConstants.FieldEntityTemplateId, DefaultEntityTemplateId);
            SetIfEmpty(request, InspectionRequestConstants.FieldEntityTemplateVersion, WorkflowVersion);
            SetIfEmpty(request, InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusDraft);
            ApplyEntityTemplateDefaults(request);
        }

        public void ApplyWorkflowTemplate(IEntity request)
        {
            if (request == null)
            {
                return;
            }

            string workflowId = GetFirstString(
                request,
                InspectionRequestConstants.FieldLifecycleWorkflowId,
                InspectionRequestConstants.FieldLifecycleWorkflowIdProperty);
            string workflowVersion = GetFirstString(
                request,
                InspectionRequestConstants.FieldLifecycleWorkflowVersion,
                InspectionRequestConstants.FieldLifecycleWorkflowVersionProperty);

            if (string.IsNullOrWhiteSpace(workflowId))
            {
                workflowId = DefaultWorkflowGuid;
                SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowId, workflowId);
                SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowIdProperty, workflowId);
            }

            if (string.IsNullOrWhiteSpace(workflowVersion))
            {
                workflowVersion = ResolveWorkflowVersion(workflowId);
                SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowVersion, workflowVersion);
                SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowVersionProperty, workflowVersion);
            }

            string entityTemplateId = ResolveWorkflowEntityTemplate(workflowId, workflowVersion);
            if (string.IsNullOrWhiteSpace(entityTemplateId))
            {
                entityTemplateId = DefaultEntityTemplateId;
            }

            string entityTemplateVersion = ResolveEntityTemplateVersion(entityTemplateId);
            if (string.IsNullOrWhiteSpace(entityTemplateVersion))
            {
                entityTemplateVersion = WorkflowVersion;
            }

            SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateId, entityTemplateId);
            SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateIdProperty, entityTemplateId);
            SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateVersion, entityTemplateVersion);
            SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateVersionProperty, entityTemplateVersion);
        }

        public void ApplyLoginWorkflowDefaults(IEntity request, IWorkflowDefinition loginWorkflow)
        {
            if (request == null || loginWorkflow is not IEntity workflowEntity)
            {
                return;
            }

            string loginWorkflowId = GetFirstString(workflowEntity, "WorkflowGuid", "WORKFLOW_GUID", "IDENTITY");
            string loginWorkflowVersion = GetFirstString(workflowEntity, "WorkflowVersion", "WORKFLOW_VERSION");
            if (string.IsNullOrWhiteSpace(loginWorkflowId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(loginWorkflowVersion))
            {
                loginWorkflowVersion = ResolveWorkflowVersion(loginWorkflowId);
            }

            IEntity newEntityNode = SelectLoginNewEntityNode(loginWorkflowId, loginWorkflowVersion);
            string lifecycleWorkflowId = GetString(newEntityNode, "DEFAULT_WORKFLOW_ID");
            string entityTemplateId = GetString(newEntityNode, "ENTITY_TEMPLATE_ID");

            if (string.IsNullOrWhiteSpace(lifecycleWorkflowId))
            {
                lifecycleWorkflowId = DefaultWorkflowGuid;
            }

            string lifecycleWorkflowVersion = ResolveWorkflowVersion(lifecycleWorkflowId);
            if (string.IsNullOrWhiteSpace(lifecycleWorkflowVersion))
            {
                lifecycleWorkflowVersion = WorkflowVersion;
            }

            if (string.IsNullOrWhiteSpace(entityTemplateId))
            {
                entityTemplateId = DefaultEntityTemplateId;
            }

            string entityTemplateVersion = ResolveEntityTemplateVersion(entityTemplateId);
            if (string.IsNullOrWhiteSpace(entityTemplateVersion))
            {
                entityTemplateVersion = WorkflowVersion;
            }

            SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowId, lifecycleWorkflowId);
            SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowIdProperty, lifecycleWorkflowId);
            SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowVersion, lifecycleWorkflowVersion);
            SetIfDifferent(request, InspectionRequestConstants.FieldLifecycleWorkflowVersionProperty, lifecycleWorkflowVersion);
            SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateId, entityTemplateId);
            SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateIdProperty, entityTemplateId);
            SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateVersion, entityTemplateVersion);
            SetIfDifferent(request, InspectionRequestConstants.FieldEntityTemplateVersionProperty, entityTemplateVersion);
            SetIfEmpty(request, InspectionRequestConstants.FieldWorkflowNode, LoginCreateNodeGuid);

            ApplyEntityTemplateDefaults(request);
        }

        public void ApplyEntityTemplateDefaults(IEntity request)
        {
            if (request == null)
            {
                return;
            }

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
                templateId = DefaultEntityTemplateId;
            }

            if (string.IsNullOrWhiteSpace(templateVersion))
            {
                templateVersion = WorkflowVersion;
            }

            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableEntityTemplateProperty);
            query.AddEquals("ENTITY_TEMPLATE_ID", templateId);
            query.AddAnd();
            query.AddEquals("ENTITY_TEMPLATE_VERSION", templateVersion);

            foreach (IEntity templateProperty in _entityManager.Select(query))
            {
                string propertyName = GetString(templateProperty, "PROPERTY_NAME");
                object defaultValue = templateProperty.Get("DEFAULT_VALUE");
                if (string.IsNullOrWhiteSpace(propertyName) ||
                    defaultValue == null ||
                    string.IsNullOrWhiteSpace(defaultValue.ToString()))
                {
                    continue;
                }

                SetIfEmpty(request, propertyName, NormalizeTemplateDefault(defaultValue));
            }
        }

        public void LinkCurrentNode(IEntity request)
        {
            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            string nodeId = GetString(request, InspectionRequestConstants.FieldWorkflowNode);

            if (string.IsNullOrWhiteSpace(nodeId))
            {
                nodeId = LoginCreateNodeGuid;
            }

            if (string.IsNullOrWhiteSpace(requestId))
            {
                return;
            }

            IEntity link = SelectWorkflowLink(requestId);
            if (link == null || !link.IsValid())
            {
                link = _entityManager.CreateEntity(InspectionRequestConstants.TableWorkflowLink);
                link.Set("TABLE_NAME", InspectionRequestConstants.EntityName);
                link.Set("RECORD_KEY0", requestId);
            }

            link.Set("WORKFLOW_NODE", nodeId);
            _entityManager.Transaction.Add(link);
        }

        public void Move(IEntity request, string action)
        {
            string currentStatus = GetString(request, InspectionRequestConstants.FieldStatus);
            string targetStatus;
            string targetNode;

            switch ((action ?? string.Empty).ToUpperInvariant())
            {
                case "SUBMIT":
                    RequireStatus(currentStatus, InspectionRequestConstants.StatusDraft, action);
                    targetStatus = InspectionRequestConstants.StatusSubmitted;
                    targetNode = SubmittedNodeGuid;
                    SetIfEmpty(request, "SUBMITTED_ON", DateTime.Now);
                    break;
                case "REVIEW":
                    RequireStatus(currentStatus, InspectionRequestConstants.StatusSubmitted, action);
                    targetStatus = InspectionRequestConstants.StatusUnderReview;
                    targetNode = UnderReviewNodeGuid;
                    break;
                case "APPROVE":
                    RequireAnyStatus(currentStatus, action, InspectionRequestConstants.StatusUnderReview);
                    targetStatus = InspectionRequestConstants.StatusApproved;
                    targetNode = ApprovedNodeGuid;
                    SetIfEmpty(request, "APPROVED_ON", DateTime.Now);
                    break;
                case "EXECUTED":
                    RequireAnyStatus(currentStatus, action, InspectionRequestConstants.StatusExecuting, InspectionRequestConstants.StatusApproved);
                    targetStatus = InspectionRequestConstants.StatusExecuted;
                    targetNode = ExecutedNodeGuid;
                    break;
                case "REJECT":
                    RequireAnyStatus(
                        currentStatus,
                        action,
                        InspectionRequestConstants.StatusSubmitted,
                        InspectionRequestConstants.StatusUnderReview,
                        InspectionRequestConstants.StatusApproved);
                    targetStatus = InspectionRequestConstants.StatusDraft;
                    targetNode = DraftNodeGuid;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Inspection Request lifecycle action: " + action);
            }

            request.Set(InspectionRequestConstants.FieldStatus, targetStatus);
            request.Set(InspectionRequestConstants.FieldLifecycleNodeId, targetNode);
            request.Set(InspectionRequestConstants.FieldLifecycleEvent, action?.ToUpperInvariant() ?? string.Empty);
            _entityManager.Transaction.Add(request);
            LinkCurrentNode(request);
            AddJournal(request, action, targetStatus);
            _entityManager.Commit();
        }

        public void MarkExecuting(IEntity request)
        {
            if (request == null)
            {
                return;
            }

            request.Set(InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusExecuting);
            request.Set(InspectionRequestConstants.FieldLifecycleNodeId, ExecutingNodeGuid);
            request.Set(InspectionRequestConstants.FieldLifecycleEvent, "EXECUTE");
            _entityManager.Transaction.Add(request);
            LinkCurrentNode(request);
            AddJournal(request, "EXECUTE", InspectionRequestConstants.StatusExecuting);
        }

        public void MarkExecutionFailed(IEntity request)
        {
            if (request == null)
            {
                return;
            }

            request.Set(InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusExecutionFailed);
            request.Set(InspectionRequestConstants.FieldLifecycleNodeId, ExecutionFailedNodeGuid);
            request.Set(InspectionRequestConstants.FieldLifecycleEvent, "EXECUTION_FAILED");
            _entityManager.Transaction.Add(request);
            LinkCurrentNode(request);
            AddJournal(request, "EXECUTION_FAILED", InspectionRequestConstants.StatusExecutionFailed);
        }

        private void EnsureEntityTemplate()
        {
            IEntity template = _entityManager.Select(
                InspectionRequestConstants.TableEntityTemplate,
                new Identity(DefaultEntityTemplateId, WorkflowVersion)) as IEntity;

            if (template == null || !template.IsValid())
            {
                template = _entityManager.CreateEntity(InspectionRequestConstants.TableEntityTemplate);
                template.Set("IDENTITY", DefaultEntityTemplateId);
                template.Set("ENTITY_TEMPLATE_VERSION", WorkflowVersion);
            }

            template.Set("NAME", "NewPharma Inspection Request");
            template.Set("DESCRIPTION", "Default entity template for NewPharma Inspection Request");
            template.Set("TABLE_NAME", InspectionRequestConstants.EntityName);
            template.Set("ACTIVE", true);
            template.Set("APPROVAL_STATUS", "A");
            template.Set("MODIFIABLE", true);
            template.Set("REMOVEFLAG", false);
            _entityManager.Transaction.Add(template);

            EnsureEntityTemplateProperty("Name", 1, "Name", string.Empty);
            EnsureEntityTemplateProperty("LoginPlan", 2, "Login Plan", string.Empty);
            EnsureEntityTemplateProperty("UseLastActiveVersion", 3, "Use Last Active Version", "T");
            EnsureEntityTemplateProperty("EsigRequired", 4, "E-signature Required", "F");
            EnsureEntityTemplateProperty("RootContextTable", 5, "Root Context Table", string.Empty);
            EnsureEntityTemplateProperty("RootContextId", 6, "Root Context ID", string.Empty);
            RemoveEntityTemplateProperty("IdText");
            RemoveEntityTemplateProperty("Description");
        }

        private void EnsureEntityTemplateProperty(string propertyName, int orderNumber, string title, string defaultValue)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableEntityTemplateProperty);
            query.AddEquals("PROPERTY_NAME", propertyName);
            query.AddAnd();
            query.AddEquals("ENTITY_TEMPLATE_ID", DefaultEntityTemplateId);
            query.AddAnd();
            query.AddEquals("ENTITY_TEMPLATE_VERSION", WorkflowVersion);

            IEntity templateProperty = null;
            foreach (IEntity existing in _entityManager.Select(query))
            {
                templateProperty = existing;
                break;
            }

            if (templateProperty == null || !templateProperty.IsValid())
            {
                templateProperty = _entityManager.CreateEntity(InspectionRequestConstants.TableEntityTemplateProperty);
                templateProperty.Set("PROPERTY_NAME", propertyName);
                templateProperty.Set("ENTITY_TEMPLATE_ID", DefaultEntityTemplateId);
                templateProperty.Set("ENTITY_TEMPLATE_VERSION", WorkflowVersion);
            }

            templateProperty.Set("ORDER_NUM", orderNumber.ToString(CultureInfo.InvariantCulture).PadLeft(10));
            templateProperty.Set("TITLE", title);
            templateProperty.Set("DEFAULT_VALUE", defaultValue);
            templateProperty.Set("PROMPT_TYPE", "PROMPT");
            templateProperty.Set("CATEGORY", "VALUE");
            templateProperty.Set("DEFAULT_TYPE", string.Empty);
            templateProperty.Set("CELL_WIDTH", "50");
            templateProperty.Set("DISPLAY_RULE", string.Empty);
            templateProperty.Set("PROPAGATE_VALUE", false);
            _entityManager.Transaction.Add(templateProperty);
        }

        private void RemoveEntityTemplateProperty(string propertyName)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableEntityTemplateProperty);
            query.AddEquals("PROPERTY_NAME", propertyName);
            query.AddAnd();
            query.AddEquals("ENTITY_TEMPLATE_ID", DefaultEntityTemplateId);
            query.AddAnd();
            query.AddEquals("ENTITY_TEMPLATE_VERSION", WorkflowVersion);

            foreach (IEntity existing in _entityManager.Select(query))
            {
                _entityManager.Transaction.Remove(existing);
            }
        }

        private void EnsureWorkflow()
        {
            IEntity workflow = _entityManager.Select(
                InspectionRequestConstants.TableWorkflow,
                new Identity(DefaultWorkflowGuid, WorkflowVersion)) as IEntity;

            if (workflow == null || !workflow.IsValid())
            {
                workflow = _entityManager.CreateEntity(InspectionRequestConstants.TableWorkflow);
                workflow.Set("WORKFLOW_GUID", DefaultWorkflowGuid);
                workflow.Set("WORKFLOW_VERSION", WorkflowVersion);
            }

            workflow.Set("NAME", "NewPharma Inspection Request Lifecycle");
            workflow.Set("TABLE_NAME", InspectionRequestConstants.EntityName);
            workflow.Set("WORKFLOW_TYPE", "LIFECYCLE");
            workflow.Set("DESCRIPTION", "Default lifecycle workflow for NewPharma Inspection Request");
            workflow.Set("ACTIVE", true);
            workflow.Set("MODIFIABLE", true);
            workflow.Set("REMOVEFLAG", false);
            _entityManager.Transaction.Add(workflow);
        }

        private void EnsureLifecycleRoot()
        {
            IEntity node = SelectWorkflowNode(LifecycleRootNodeGuid);
            if (node == null || !node.IsValid())
            {
                node = _entityManager.CreateEntity(InspectionRequestConstants.TableWorkflowNode);
                node.Set("WORKFLOW_NODE_GUID", LifecycleRootNodeGuid);
            }

            node.Set("WORKFLOW_ID", DefaultWorkflowGuid);
            node.Set("WORKFLOW_VERSION", WorkflowVersion);
            node.Set("ORDER_NUMBER", "1");
            node.Set("PARENT_NODE", string.Empty);
            node.Set("NAME", "Inspection Request Lifecycle");
            node.Set("DESCRIPTION", "Default lifecycle for NewPharma Inspection Request");
            node.Set("NODE_TYPE", "LIFECYCLE");
            node.Set("DEFAULT_WORKFLOW_ID", string.Empty);
            node.Set("ACTION_TABLE_NAME", string.Empty);
            node.Set("ACTION_TYPE_ID", string.Empty);
            node.Set("STATE_TABLE_NAME", string.Empty);
            node.Set("STATE_IDENTITY", string.Empty);
            node.Set("EVENT_TABLE_NAME", string.Empty);
            node.Set("EVENT_TYPE_ID", string.Empty);
            node.Set("ENTITY_TEMPLATE_ID", string.Empty);
            node.Set("ENABLED", true);
            node.Set("PARAMETERS_EXT", EntityParameters());
            _entityManager.Transaction.Add(node);
        }

        private void EnsureStatusMarkerNode(string nodeGuid, string name, int order)
        {
            IEntity node = SelectWorkflowNode(nodeGuid);

            if (node == null || !node.IsValid())
            {
                node = _entityManager.CreateEntity(InspectionRequestConstants.TableWorkflowNode);
                node.Set("WORKFLOW_NODE_GUID", nodeGuid);
            }

            node.Set("WORKFLOW_ID", DefaultWorkflowGuid);
            node.Set("WORKFLOW_VERSION", WorkflowVersion);
            node.Set("ORDER_NUMBER", order.ToString(System.Globalization.CultureInfo.InvariantCulture));
            node.Set("PARENT_NODE", LifecycleRootNodeGuid);
            node.Set("NAME", name);
            node.Set("DESCRIPTION", name);
            node.Set("NODE_TYPE", "COMMENT");
            node.Set("DEFAULT_WORKFLOW_ID", string.Empty);
            node.Set("ACTION_TABLE_NAME", string.Empty);
            node.Set("ACTION_TYPE_ID", string.Empty);
            node.Set("STATE_TABLE_NAME", string.Empty);
            node.Set("STATE_IDENTITY", string.Empty);
            node.Set("EVENT_TABLE_NAME", string.Empty);
            node.Set("EVENT_TYPE_ID", string.Empty);
            node.Set("ENTITY_TEMPLATE_ID", string.Empty);
            node.Set("ENABLED", true);
            node.Set("PARAMETERS_EXT", string.Empty);
            _entityManager.Transaction.Add(node);
        }

        private void EnsureLoginWorkflow()
        {
            IEntity workflow = _entityManager.Select(
                InspectionRequestConstants.TableWorkflow,
                new Identity(DefaultLoginWorkflowGuid, WorkflowVersion)) as IEntity;

            if (workflow == null || !workflow.IsValid())
            {
                workflow = _entityManager.CreateEntity(InspectionRequestConstants.TableWorkflow);
                workflow.Set("WORKFLOW_GUID", DefaultLoginWorkflowGuid);
                workflow.Set("WORKFLOW_VERSION", WorkflowVersion);
            }

            workflow.Set("NAME", "NewPharma Inspection Request Login");
            workflow.Set("TABLE_NAME", InspectionRequestConstants.EntityName);
            workflow.Set("WORKFLOW_TYPE", InspectionRequestConstants.WorkflowTypeInspectionRequestCreate);
            workflow.Set("DESCRIPTION", "Inspection Request create workflow entry point");
            workflow.Set("ACTIVE", true);
            workflow.Set("MODIFIABLE", true);
            workflow.Set("REMOVEFLAG", false);
            _entityManager.Transaction.Add(workflow);
        }

        private void EnsureLoginWorkflowNode()
        {
            IEntity rootNode = SelectWorkflowNode(LoginNodeGuid);
            if (rootNode == null || !rootNode.IsValid())
            {
                rootNode = _entityManager.CreateEntity(InspectionRequestConstants.TableWorkflowNode);
                rootNode.Set("WORKFLOW_NODE_GUID", LoginNodeGuid);
            }

            rootNode.Set("WORKFLOW_ID", DefaultLoginWorkflowGuid);
            rootNode.Set("WORKFLOW_VERSION", WorkflowVersion);
            rootNode.Set("ORDER_NUMBER", "1");
            rootNode.Set("PARENT_NODE", string.Empty);
            rootNode.Set("NAME", "Inspection Request Create Workflow");
            rootNode.Set("DESCRIPTION", "Inspection Request create workflow root");
            rootNode.Set("NODE_TYPE", InspectionRequestConstants.WorkflowNodeTypeInspectionRequestLogin);
            rootNode.Set("DEFAULT_WORKFLOW_ID", string.Empty);
            rootNode.Set("ACTION_TABLE_NAME", string.Empty);
            rootNode.Set("ACTION_TYPE_ID", string.Empty);
            rootNode.Set("STATE_TABLE_NAME", string.Empty);
            rootNode.Set("STATE_IDENTITY", string.Empty);
            rootNode.Set("EVENT_TABLE_NAME", string.Empty);
            rootNode.Set("EVENT_TYPE_ID", string.Empty);
            rootNode.Set("ENTITY_TEMPLATE_ID", string.Empty);
            rootNode.Set("ENABLED", true);
            rootNode.Set("PARAMETERS_EXT", string.Empty);
            _entityManager.Transaction.Add(rootNode);

            IEntity createNode = SelectWorkflowNode(LoginCreateNodeGuid);
            if (createNode == null || !createNode.IsValid())
            {
                createNode = _entityManager.CreateEntity(InspectionRequestConstants.TableWorkflowNode);
                createNode.Set("WORKFLOW_NODE_GUID", LoginCreateNodeGuid);
            }

            createNode.Set("WORKFLOW_ID", DefaultLoginWorkflowGuid);
            createNode.Set("WORKFLOW_VERSION", WorkflowVersion);
            createNode.Set("ORDER_NUMBER", "2");
            createNode.Set("PARENT_NODE", LoginNodeGuid);
            createNode.Set("NAME", "Create Inspection Request");
            createNode.Set("DESCRIPTION", "Create Inspection Request");
            createNode.Set("NODE_TYPE", InspectionRequestConstants.WorkflowNodeTypeCreateInspectionRequest);
            createNode.Set("DEFAULT_WORKFLOW_ID", DefaultWorkflowGuid);
            createNode.Set("ACTION_TABLE_NAME", string.Empty);
            createNode.Set("ACTION_TYPE_ID", string.Empty);
            createNode.Set("STATE_TABLE_NAME", string.Empty);
            createNode.Set("STATE_IDENTITY", string.Empty);
            createNode.Set("EVENT_TABLE_NAME", string.Empty);
            createNode.Set("EVENT_TYPE_ID", string.Empty);
            createNode.Set("ENTITY_TEMPLATE_ID", DefaultEntityTemplateId);
            createNode.Set("ENABLED", true);
            createNode.Set("PARAMETERS_EXT", EntityParameters());
            _entityManager.Transaction.Add(createNode);
        }

        private IEntity SelectWorkflowNode(string nodeGuid)
        {
            return _entityManager.Select(
                InspectionRequestConstants.TableWorkflowNode,
                new Identity(nodeGuid)) as IEntity;
        }

        private string ResolveWorkflowVersion(string workflowId)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
            {
                return WorkflowVersion;
            }

            IEntity workflow = _entityManager.SelectLatestVersion(
                InspectionRequestConstants.TableWorkflow,
                workflowId) as IEntity;

            return workflow == null || !workflow.IsValid()
                ? WorkflowVersion
                : GetString(workflow, "WORKFLOW_VERSION");
        }

        private IEntity SelectLoginNewEntityNode(string workflowId, string workflowVersion)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
            {
                return null;
            }

            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableWorkflowNode);
            query.AddEquals("WORKFLOW_ID", workflowId);
            if (!string.IsNullOrWhiteSpace(workflowVersion))
            {
                query.AddAnd();
                query.AddEquals("WORKFLOW_VERSION", workflowVersion);
            }

            IEntity fallback = null;
            foreach (IEntity node in _entityManager.Select(query))
            {
                string nodeType = GetString(node, "NODE_TYPE");
                if (string.Equals(nodeType, InspectionRequestConstants.WorkflowNodeTypeCreateInspectionRequest, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                if (string.Equals(nodeType, "CREATE_ENTITY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nodeType, "NEWENTITY", StringComparison.OrdinalIgnoreCase))
                {
                    fallback ??= node;
                }
            }

            return fallback;
        }

        private string ResolveWorkflowEntityTemplate(string workflowId, string workflowVersion)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
            {
                return string.Empty;
            }

            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableWorkflowNode);
            query.AddEquals("WORKFLOW_ID", workflowId);
            if (!string.IsNullOrWhiteSpace(workflowVersion))
            {
                query.AddAnd();
                query.AddEquals("WORKFLOW_VERSION", workflowVersion);
            }

            string fallbackTemplate = string.Empty;
            foreach (IEntity node in _entityManager.Select(query))
            {
                string templateId = GetString(node, "ENTITY_TEMPLATE_ID");
                if (string.IsNullOrWhiteSpace(templateId))
                {
                    continue;
                }

                string nodeType = GetString(node, "NODE_TYPE");
                if (string.Equals(nodeType, InspectionRequestConstants.WorkflowNodeTypeCreateInspectionRequest, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nodeType, "CREATE_ENTITY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nodeType, "LIFECYCLE", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(nodeType, "NEWENTITY", StringComparison.OrdinalIgnoreCase))
                {
                    return templateId;
                }

                if (string.IsNullOrWhiteSpace(fallbackTemplate))
                {
                    fallbackTemplate = templateId;
                }
            }

            return fallbackTemplate;
        }

        private string ResolveEntityTemplateVersion(string entityTemplateId)
        {
            if (string.IsNullOrWhiteSpace(entityTemplateId))
            {
                return string.Empty;
            }

            IEntity entityTemplate = _entityManager.SelectLatestVersion(
                InspectionRequestConstants.TableEntityTemplate,
                entityTemplateId) as IEntity;

            return entityTemplate == null || !entityTemplate.IsValid()
                ? string.Empty
                : GetString(entityTemplate, "ENTITY_TEMPLATE_VERSION");
        }

        private static string EntityParameters()
        {
            return "<item><key><string>ENTITY_NAME</string></key><value><string>NPH_INSPECTION_REQUEST</string></value></item>";
        }

        private IEntity SelectWorkflowLink(string requestId)
        {
            return _entityManager.Select(
                InspectionRequestConstants.TableWorkflowLink,
                new Identity(InspectionRequestConstants.EntityName, requestId)) as IEntity;
        }

        private void AddJournal(IEntity request, string action, string targetStatus)
        {
            IEntity journal = _entityManager.CreateEntity(InspectionRequestConstants.TableWorkflowJournal);
            journal.Set("IDENTITY", DateTime.Now.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture));
            journal.Set("TABLE_NAME", InspectionRequestConstants.EntityName);
            journal.Set("ENTITY_RECORD", GetString(request, InspectionRequestConstants.FieldRequestId));
            journal.Set("ENTRY_TYPE", "A");
            journal.Set("ENTRY_NAME", action?.ToUpperInvariant() ?? targetStatus);
            journal.Set("ENTER_STATES", targetStatus);
            journal.Set("PERFORMED_ON", DateTime.Now);
            _entityManager.Transaction.Add(journal);
        }

        private static void RequireStatus(string currentStatus, string requiredStatus, string action)
        {
            if (!string.Equals(currentStatus, requiredStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Inspection Request cannot {action?.ToLowerInvariant()} from status {currentStatus}. Expected {requiredStatus}.");
            }
        }

        private static void RequireAnyStatus(string currentStatus, string action, params string[] allowedStatuses)
        {
            if (!allowedStatuses.Any(status => string.Equals(currentStatus, status, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Inspection Request cannot {action?.ToLowerInvariant()} from status {currentStatus}. Expected one of: {string.Join(", ", allowedStatuses)}.");
            }
        }

        private static void SetIfEmpty(IEntity entity, string fieldName, object value)
        {
            try
            {
                object current = entity.Get(fieldName);
                if (current == null || string.IsNullOrWhiteSpace(current.ToString()))
                {
                    entity.Set(fieldName, value);
                }
            }
            catch
            {
            }
        }

        private static void SetIfDifferent(IEntity entity, string fieldName, object value)
        {
            try
            {
                object current = entity.Get(fieldName);
                if (!string.Equals(current?.ToString() ?? string.Empty, value?.ToString() ?? string.Empty, StringComparison.Ordinal))
                {
                    entity.Set(fieldName, value);
                }
            }
            catch
            {
            }
        }

        private static object NormalizeTemplateDefault(object value)
        {
            string text = value?.ToString() ?? string.Empty;
            return string.Equals(text, "T", StringComparison.OrdinalIgnoreCase)
                ? true
                : string.Equals(text, "F", StringComparison.OrdinalIgnoreCase)
                    ? false
                    : value;
        }

        private static string GetString(IEntity entity, string fieldName)
        {
            try
            {
                object value = entity.Get(fieldName);
                return value?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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
    }
}
