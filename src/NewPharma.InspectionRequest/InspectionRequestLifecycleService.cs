using System;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Server;

namespace NewPharma.InspectionRequest
{
    internal sealed class InspectionRequestLifecycleService
    {
        public const string DefaultEntityTemplateId = "NPH_IR";
        public const string DefaultWorkflowGuid = "19940000-0000-0000-0000-000000000001";
        public const string DraftNodeGuid = "19940000-0000-0000-0000-000000000101";
        public const string SubmittedNodeGuid = "19940000-0000-0000-0000-000000000102";
        public const string UnderReviewNodeGuid = "19940000-0000-0000-0000-000000000103";
        public const string ApprovedNodeGuid = "19940000-0000-0000-0000-000000000104";
        public const string ExecutedNodeGuid = "19940000-0000-0000-0000-000000000105";
        private const string WorkflowVersion = "1";
        private readonly IEntityManager _entityManager;

        public InspectionRequestLifecycleService(IEntityManager entityManager)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        }

        public void EnsureConfiguration()
        {
            EnsureEntityTemplate();
            EnsureWorkflow();
            EnsureWorkflowNode(DraftNodeGuid, "Draft", InspectionRequestConstants.StatusDraft, 1);
            EnsureWorkflowNode(SubmittedNodeGuid, "Submitted", InspectionRequestConstants.StatusSubmitted, 2);
            EnsureWorkflowNode(UnderReviewNodeGuid, "Under Review", InspectionRequestConstants.StatusUnderReview, 3);
            EnsureWorkflowNode(ApprovedNodeGuid, "Approved", InspectionRequestConstants.StatusApproved, 4);
            EnsureWorkflowNode(ExecutedNodeGuid, "Executed", InspectionRequestConstants.StatusExecuted, 5);
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
            SetIfEmpty(request, InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusDraft);
        }

        public void LinkCurrentNode(IEntity request)
        {
            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            string nodeId = GetString(request, InspectionRequestConstants.FieldLifecycleNodeId);

            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(nodeId))
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
            string targetStatus;
            string targetNode;

            switch ((action ?? string.Empty).ToUpperInvariant())
            {
                case "SUBMIT":
                    targetStatus = InspectionRequestConstants.StatusSubmitted;
                    targetNode = SubmittedNodeGuid;
                    break;
                case "REVIEW":
                    targetStatus = InspectionRequestConstants.StatusUnderReview;
                    targetNode = UnderReviewNodeGuid;
                    break;
                case "APPROVE":
                    targetStatus = InspectionRequestConstants.StatusApproved;
                    targetNode = ApprovedNodeGuid;
                    break;
                case "EXECUTED":
                    targetStatus = InspectionRequestConstants.StatusExecuted;
                    targetNode = ExecutedNodeGuid;
                    break;
                case "REJECT":
                    targetStatus = InspectionRequestConstants.StatusDraft;
                    targetNode = DraftNodeGuid;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Inspection Request lifecycle action: " + action);
            }

            request.Set(InspectionRequestConstants.FieldStatus, targetStatus);
            request.Set(InspectionRequestConstants.FieldLifecycleNodeId, targetNode);
            request.Set(InspectionRequestConstants.FieldLifecycleEvent, action?.ToUpperInvariant() ?? string.Empty);
            LinkCurrentNode(request);
            AddJournal(request, action, targetStatus);
            _entityManager.Commit();
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
            workflow.Set("WORKFLOW_TYPE", "GENERAL");
            workflow.Set("DESCRIPTION", "Default lifecycle workflow for NewPharma Inspection Request");
            workflow.Set("ACTIVE", true);
            workflow.Set("MODIFIABLE", true);
            workflow.Set("REMOVEFLAG", false);
            _entityManager.Transaction.Add(workflow);
        }

        private void EnsureWorkflowNode(string nodeGuid, string name, string status, int order)
        {
            IEntity node = _entityManager.Select(
                InspectionRequestConstants.TableWorkflowNode,
                new Identity(nodeGuid)) as IEntity;

            if (node == null || !node.IsValid())
            {
                node = _entityManager.CreateEntity(InspectionRequestConstants.TableWorkflowNode);
                node.Set("WORKFLOW_NODE_GUID", nodeGuid);
            }

            node.Set("WORKFLOW_ID", DefaultWorkflowGuid);
            node.Set("WORKFLOW_VERSION", WorkflowVersion);
            node.Set("ORDER_NUMBER", order.ToString(System.Globalization.CultureInfo.InvariantCulture));
            node.Set("NAME", name);
            node.Set("DESCRIPTION", name);
            node.Set("NODE_TYPE", "STATE");
            node.Set("ENTITY_TEMPLATE_ID", DefaultEntityTemplateId);
            node.Set("ENABLED", true);
            node.Set("PARAMETERS_EXT", status);
            _entityManager.Transaction.Add(node);
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

        private static void SetIfEmpty(IEntity entity, string fieldName, object value)
        {
            object current = entity.Get(fieldName);
            if (current == null || string.IsNullOrWhiteSpace(current.ToString()))
            {
                entity.Set(fieldName, value);
            }
        }

        private static string GetString(IEntity entity, string fieldName)
        {
            object value = entity.Get(fieldName);
            return value?.ToString() ?? string.Empty;
        }
    }
}
